const _SW = 960, _SH = 540, _TW = 0.25;

class SimpleAnimateSvgComponent extends HTMLElement {
  static get observedAttributes() {
    return ['svg-file-path','animation-speed','width','height','sizing',
            'background-color','border','invert-colors','auto-play','start-at','stop-at'];
  }

  constructor() {
    super();
    this._root = this.attachShadow({ mode: 'open' });
    this._wrap = document.createElement('div');
    this._root.appendChild(this._wrap);
    this._paths = [];
    this._pauses = [];
    this._pauseIdx = 0;
    this._revealed = 0;
    this._rafId = null;
    this._timerId = null;
    this._playing = false;
    this._finished = false;
    this._elapsed = 0;
    this._t0 = null;
    this._duration = 2000;
    this._speed = 100;
    this._autoPlay = true;
    this._waitResume = false;
    this._svgEl = null;
    this._svgShown = false;
    this._loadCtrl = null;
    this._iobs = null;
    this._wasVis = false;
    this._played = false;
    this._ready = false;
    this._startRef = null;
    this._stopRef = null;
  }

  connectedCallback() {
    this._build();
    this._onClickResume = e => {
      if (!this._waitResume) return;
      e.stopPropagation(); e.preventDefault();
      this._waitResume = false; this._play();
    };
    this.addEventListener('click', this._onClickResume);
    this._onKey = e => {
      if (e.key === 'MediaPlayPause' && this._waitResume) {
        e.preventDefault(); this._waitResume = false; this._play();
      }
    };
    document.addEventListener('keydown', this._onKey, true);
  }

  disconnectedCallback() {
    this._cancelRaf();
    this.removeEventListener('click', this._onClickResume);
    document.removeEventListener('keydown', this._onKey, true);
    this._iobs?.disconnect(); this._iobs = null;
    this._loadCtrl?.abort();
  }

  attributeChangedCallback(n, o, v) { if (o !== v) this._build(); }

  _build() {
    this._cancelRaf();
    this._loadCtrl?.abort(); this._loadCtrl = null;
    this._iobs?.disconnect(); this._iobs = null;
    this._paths = []; this._pauses = []; this._elapsed = 0;
    this._revealed = 0; this._pauseIdx = 0; this._waitResume = false;
    this._svgEl = null; this._svgShown = false; this._ready = false; this._played = false;
    this._wrap.innerHTML = '';
    this._wrap.style.cssText = 'position:relative;width:100%;height:100%';

    const w   = this.getAttribute('width') || '100%';
    const h   = this.getAttribute('height');
    const siz = (this.getAttribute('sizing') || 'fill').toLowerCase();
    const bg  = this.getAttribute('background-color');
    const brd = this.getAttribute('border');
    const src = this.getAttribute('svg-file-path');
    const inv = this.getAttribute('invert-colors') === 'true';
    const spd = parseInt(this.getAttribute('animation-speed'), 10);
    this._autoPlay = this.getAttribute('auto-play') !== 'false';
    this._speed    = Number.isNaN(spd) ? 100 : Math.max(1, spd);
    this._startRef = this.getAttribute('start-at') || null;
    this._stopRef  = this.getAttribute('stop-at')  || null;

    this.style.display = 'inline-block'; this.style.boxSizing = 'border-box';
    this.style.width = w; this.style.height = siz === 'fill' ? '100%' : (h || 'auto');
    this.style.padding = '0'; this.style.margin = '0';
    this.style.lineHeight = '0'; this.style.verticalAlign = 'top';
    if (bg)  this.style.backgroundColor = bg;
    if (brd) this.style.border = brd;
    if (siz === 'fill') {
      this.style.position = 'absolute'; this.style.top = '0'; this.style.left = '0';
      this.style.width = '100%'; this.style.height = '100%'; this.style.overflow = 'hidden';
      this.style.aspectRatio = `${_SW}/${_SH}`;
    }

    const style = document.createElement('style');
    style.textContent = `
      :host{contain:layout style}
      .sw{position:relative;width:100%;height:100%;overflow:hidden}
      .sc{width:100%;height:100%;display:block}
      .co{position:absolute;bottom:5px;right:5px;display:flex;gap:3px;padding:2px 6px;
          border-radius:999px;background:rgba(16,16,16,.65);color:#fff;opacity:0;
          pointer-events:none;transition:opacity .3s;align-items:center;
          backdrop-filter:blur(4px);z-index:10}
      .sw:hover+.co,:host(:hover) .co,.co.fv,.co:hover{opacity:1;pointer-events:auto}
      .cb{width:22px;height:22px;border-radius:50%;border:none;background:transparent;
          color:inherit;display:flex;align-items:center;justify-content:center;
          cursor:pointer;transition:transform .15s,background .15s}
      .cb:focus-visible{outline:2px solid #fff;outline-offset:2px}
      .cb:hover{transform:scale(1.1)}
      .cb svg{width:14px;height:14px;fill:currentColor}`;
    this._wrap.appendChild(style);

    const sw = document.createElement('div'); sw.className = 'sw';
    this._sc = document.createElement('div'); this._sc.className = 'sc';
    sw.appendChild(this._sc); this._wrap.appendChild(sw);
    this._co = document.createElement('div'); this._co.className = 'co';
    this._wrap.appendChild(this._co);
    this._buildControls();

    if (!src) { this._msg('svg-file-path attribute is required'); return; }
    this._loadSvg(src, { siz, w, inv });
  }

  _msg(text) {
    this._sc.innerHTML = `<div style="padding:12px;color:#fff;font-size:14px">${text}</div>`;
  }

  _buildControls() {
    const rew = '<svg viewBox="0 0 24 24"><g transform="translate(24 0) scale(-1 1)"><path d="M6 6v12l10-6z"/><path d="M18 6h2v12h-2z"/></g></svg>';
    const play = '<svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>';
    const end  = '<svg viewBox="0 0 24 24"><path d="M6 6v12l10-6z"/><path d="M18 6h2v12h-2z"/></svg>';
    this._btnR = this._mkBtn('Restart', rew,  () => { this._restart(); this._forceCo(true); });
    this._btnP = this._mkBtn('Play',    play, () => this._playing ? this._pause() : this._play());
    this._btnE = this._mkBtn('Finish',  end,  () => this._finish());
    this._co.append(this._btnR, this._btnP, this._btnE);
    this._syncBtn();
  }

  _mkBtn(title, html, fn) {
    const b = document.createElement('button');
    b.className = 'cb'; b.title = title; b.innerHTML = html;
    b.onclick = e => { e.stopPropagation(); fn(); };
    return b;
  }

  async _loadSvg(src, opts) {
    const ctrl = new AbortController();
    this._loadCtrl = ctrl;
    try {
      const res = await fetch(src, { signal: ctrl.signal });
      if (!res.ok) throw new Error('fetch failed');
      const txt = await res.text();
      const rawPauses = this._parsePauses(txt);
      const speedHints = this._parseSpeed(txt);
      const doc = new DOMParser().parseFromString(txt, 'image/svg+xml');
      const err = doc.querySelector('parsererror');
      if (err) {
        let detail = err.textContent?.trim() || 'Unknown';
        const lo = detail.toLowerCase(), fi = lo.indexOf('error on line');
        if (fi >= 0) { const si = lo.indexOf('error on line', fi+1); if (si >= 0) detail = detail.slice(0, si).trim(); }
        this._showBanner(`SVG parse error: ${detail}`);
        throw new Error(detail);
      }
      const svg = doc.querySelector('svg');
      if (!svg) throw new Error('no <svg>');
      this._sc.innerHTML = '';
      this._sc.appendChild(svg);
      this._svgEl = svg; this._svgShown = false;
      svg.style.visibility = 'hidden';
      this._applySizing(svg, opts);
      if (opts.inv) this._invertColors(svg);
      this._prepare(svg, txt, speedHints, rawPauses);
      this._ready = true; this._loadCtrl = null;
      this._setupIobs();
      if (!this._autoPlay) this._draw(0);
    } catch (e) {
      if (e.name === 'AbortError') { this._loadCtrl = null; return; }
      this._msg('Error loading SVG'); console.error(e); this._loadCtrl = null;
    }
  }

  _applySizing(svg, { siz, w }) {
    const PAD = 20;
    // Prefer the SVG's own viewBox — the author set it correctly
    const existingVB = svg.getAttribute('viewBox');
    if (!existingVB) {
      // No viewBox set — calculate from bounding box (original behavior)
      let bb; try { bb = svg.getBBox(); } catch { bb = { x:0, y:0, width:0, height:0 }; }
      const vbw = bb.width  > 0 ? bb.width  : parseFloat(svg.getAttribute('width'))  || 100;
      const vbh = bb.height > 0 ? bb.height : parseFloat(svg.getAttribute('height')) || 100;
      svg.setAttribute('viewBox', `${bb.x-PAD} ${bb.y-PAD} ${vbw+PAD*2} ${vbh+PAD*2}`);
    }
    if (siz === 'fill') {
      svg.removeAttribute('width'); svg.removeAttribute('height');
      svg.style.width = '100%'; svg.style.height = '100%';
      svg.style.maxWidth = '100%'; svg.style.maxHeight = '100%';
      svg.setAttribute('preserveAspectRatio', 'xMidYMid meet');
    } else if (siz === 'exact') {
      svg.style.width = w; svg.style.height = 'auto';
      svg.setAttribute('preserveAspectRatio', 'xMidYMid meet');
    }
  }

  _prepare(svg, svgTxt, speedHints, rawPauses) {
    const drawPos = this._extractPos(svgTxt);
    const nodes = Array.from(svg.querySelectorAll(
      'path,line,polyline,polygon,rect,circle,ellipse,text,image,use'));
    const startIdx = this._resolveRef(this._startRef, nodes);
    const stopIdx  = this._resolveRef(this._stopRef,  nodes);

    let cumTime = 0, hIdx = 0, mult = 1;
    const entries = [], segs = [], ranges = [];

    nodes.forEach((node, ni) => {
      const dpos = drawPos[ni] || { index: Infinity };
      while (hIdx < speedHints.length && speedHints[hIdx].index <= dpos.index) mult = speedHints[hIdx++].multiplier;
      const effSpd = Math.max(this._speed * mult, 1e-4);
      const eStart = entries.length;
      let nodeDur = 0;

      if (node.tagName.toLowerCase() === 'text') {
        try {
          const { entries: te, totalDuration: td } = this._processText(node, cumTime, this._speed, mult);
          te.forEach(e => entries.push(e));
          nodeDur = td;
        } catch (ex) { console.warn('[AnimDiag] text error:', ex); }
      } else {
        let len = 0;
        if (typeof node.getTotalLength === 'function') { try { len = node.getTotalLength(); } catch { len = 0; } }
        const cs = typeof window !== 'undefined' ? window.getComputedStyle(node) : null;
        if (this._canStroke(node, len, cs)) {
          const dur = (len / effSpd) * 1000;
          entries.push({ el: node, len, startTime: cumTime, endTime: cumTime+dur, dur,
            isText: false, behavior: 'stroke',
            finalFill: cs?.fillOpacity || '1', finalOp: cs?.opacity || '1',
            origDash: cs?.strokeDasharray || '' });
          nodeDur = dur;
          node.style.fillOpacity = '0';
        } else if (this._canFillReveal(node, cs)) {
          const dur = this._estimateFillDuration(node, effSpd);
          entries.push({ el: node, len: 0, startTime: cumTime, endTime: cumTime+dur, dur,
            isText: false, behavior: 'fill-reveal',
            finalOp: cs?.opacity || '1', finalFillOp: cs?.fillOpacity || '1' });
          nodeDur = dur;
          node.style.clipPath = 'inset(0 100% 0 0)';
          node.style.opacity = '1';
        } else {
          entries.push({ el: node, len: 0, startTime: cumTime, endTime: cumTime, dur: 0,
            isText: false, behavior: 'instant', finalOp: cs?.opacity || '1' });
          node.style.opacity = '0';
          if (!node.style.transition) node.style.transition = 'opacity 0.15s linear';
        }
      }

      segs.push({ startTime: cumTime, endTime: cumTime + nodeDur });
      ranges.push({ s: eStart, e: entries.length });
      cumTime += nodeDur;
    });

    const { filtered, timeOff, nodeFirstEntry } = this._applyRange(entries, nodes, ranges, startIdx, stopIdx);
    this._paths = filtered;
    this._revealed = 0;
    this._duration = filtered.length > 0 ? Math.max(filtered[filtered.length-1].endTime, 1) : 2000;
    if (!isFinite(this._duration) || this._duration <= 0) this._duration = 2000;

    // Set initial hidden state for all filtered entries
    filtered.forEach(seg => {
      if (seg.isText) {
        seg.el.style.fillOpacity = '0';
        seg.el.style.transition = 'fill-opacity 0.15s linear';
      } else if (seg.behavior === 'instant') {
        seg.el.style.opacity = '0';
        if (!seg.el.style.transition) seg.el.style.transition = 'opacity 0.15s linear';
      } else if (seg.behavior === 'fill-reveal') {
        seg.el.style.clipPath = 'inset(0 100% 0 0)';
        seg.el.style.opacity = '1';
      } else {
        seg.el.style.fillOpacity = '0';
        seg.el.style.strokeDasharray = `${seg.len} ${seg.len}`;
        seg.el.style.strokeDashoffset = `${seg.len}`;
        seg.el.style.transition = 'stroke-dashoffset 0.1s linear';
      }
    });

    const effStart = startIdx >= 0 ? startIdx : 0;
    const effStop  = stopIdx  >= 0 ? stopIdx  : nodes.length - 1;
    this._pauses = this._mapPauses(rawPauses, drawPos, segs, this._duration, timeOff, effStart, effStop, nodeFirstEntry, filtered.length);
    if (this._pauses.length) console.log(`[AnimDiag] ${this._pauses.length} pause(s)`);
  }

  _resolveRef(ref, nodes) {
    if (ref == null) return -1;
    const n = parseInt(ref, 10);
    if (!isNaN(n) && String(n) === ref.trim()) return n >= 0 && n < nodes.length ? n : -1;
    return nodes.findIndex(nd => nd.id === ref);
  }

  _applyRange(entries, nodes, ranges, startIdx, stopIdx) {
    // nodeFirstEntry[ni] = index into filtered array of the first entry for node ni
    const nodeFirstEntry = [];

    if (startIdx < 0 && stopIdx < 0) {
      ranges.forEach((r, ni) => { nodeFirstEntry[ni] = r.s; });
      return { filtered: entries, timeOff: 0, nodeFirstEntry };
    }

    const effStart = startIdx >= 0 ? startIdx : 0;
    const effStop  = stopIdx  >= 0 ? stopIdx  : nodes.length - 1;
    const sr = ranges[effStart];
    const timeOff = sr ? (entries[sr.s]?.startTime || 0) : 0;

    for (let ni = 0; ni < effStart; ni++) {
      const { s, e } = ranges[ni];
      for (let i = s; i < e; i++) this._revealSeg(entries[i]);
    }
    for (let ni = effStop+1; ni < nodes.length; ni++) {
      const { s, e } = ranges[ni];
      for (let i = s; i < e; i++) this._hideSeg(entries[i]);
    }

    const filtered = [];
    for (let ni = effStart; ni <= effStop; ni++) {
      nodeFirstEntry[ni] = filtered.length;
      const { s, e } = ranges[ni];
      for (let i = s; i < e; i++) {
        const seg = { ...entries[i] };
        seg.startTime -= timeOff; seg.endTime -= timeOff;
        filtered.push(seg);
      }
    }
    return { filtered, timeOff, nodeFirstEntry };
  }

  _processText(textEl, startTime, baseSpeed, mult) {
    const entries = [];
    let curTime = startTime;
    const spd = Math.max(baseSpeed * Math.max(mult, 0.01) * _TW, 1e-4);
    const segs = this._collectSegs(textEl);
    while (textEl.firstChild) textEl.removeChild(textEl.firstChild);

    segs.forEach(seg => {
      Array.from(seg.text).forEach((ch, ci) => {
        const ts = document.createElementNS('http://www.w3.org/2000/svg', 'tspan');
        ts.textContent = ch === ' ' ? ' ' : ch;
        if (ci === 0) {
          const p = seg.pos;
          if (p.x  !== null) ts.setAttribute('x',  p.x);
          if (p.y  !== null) ts.setAttribute('y',  p.y);
          if (p.dx !== null) ts.setAttribute('dx', p.dx);
          if (p.dy !== null) ts.setAttribute('dy', p.dy);
        }
        const s = seg.style;
        if (s.fontFamily) ts.setAttribute('font-family', s.fontFamily.replace(/^['"]|['"]$/g,''));
        if (s.fontSize)   ts.setAttribute('font-size',   s.fontSize);
        if (s.fontWeight && s.fontWeight !== 'normal') ts.setAttribute('font-weight', s.fontWeight);
        if (s.fontStyle  && s.fontStyle  !== 'normal') ts.setAttribute('font-style',  s.fontStyle);
        ts.style.fillOpacity = '0';
        ts.style.transition  = 'fill-opacity 0.15s linear';
        textEl.appendChild(ts);

        const clen = Math.max(ts.getComputedTextLength(), 1);
        const cdur = (clen / spd) * 1000;
        entries.push({ el: ts, len: clen, startTime: curTime, endTime: curTime+cdur,
          dur: cdur, isText: true, behavior: 'text' });
        curTime += cdur;
      });
    });

    return { entries, totalDuration: curTime - startTime };
  }

  _collectSegs(el) {
    const kids = Array.from(el.childNodes);
    const hasTspan = kids.some(n => n.nodeType === 1 && n.tagName.toLowerCase() === 'tspan');
    const pStyle = this._getStyle(el);
    const norm = t => (t || '').replace(/[\r\n]/g,' ').replace(/\s+/g,' ').trim();

    if (!hasTspan) {
      const text = norm(el.textContent);
      return text ? [{ text, pos: this._pos(el), style: pStyle }] : [];
    }

    const segs = []; let first = true;
    kids.forEach(kid => {
      if (kid.nodeType === 3) {
        const t = norm(kid.textContent);
        if (!t) return;
        segs.push({ text: t, pos: first ? this._pos(el) : { x:null,y:null,dx:null,dy:null }, style: pStyle });
        first = false;
      } else if (kid.nodeType === 1 && kid.tagName.toLowerCase() === 'tspan') {
        const t = norm(kid.textContent);
        if (!t) return;
        const pos = this._pos(kid);
        if (first) {
          if (pos.x  === null) pos.x  = el.getAttribute('x');
          if (pos.y  === null) pos.y  = el.getAttribute('y');
        }
        segs.push({ text: t, pos, style: this._getStyle(kid, pStyle) });
        first = false;
      }
    });
    return segs;
  }

  _pos(el) {
    return { x: el.getAttribute('x'), y: el.getAttribute('y'),
             dx: el.getAttribute('dx'), dy: el.getAttribute('dy') };
  }

  _getStyle(el, fb = null) {
    const cs = window.getComputedStyle(el);
    const r = (attr, prop) => el.getAttribute(attr) || el.style[prop] || (fb && fb[prop]) || cs[prop];
    return { fontFamily: r('font-family','fontFamily'), fontSize: r('font-size','fontSize'),
             fontWeight: r('font-weight','fontWeight'), fontStyle: r('font-style','fontStyle'),
             fill: cs.fill || (fb && fb.fill) || cs.color || '#000' };
  }

  _mapPauses(raw, drawPos, segs, totalDur, timeOff, rangeStart, rangeStop, nodeFirstEntry, filteredLen) {
    if (!raw.length || totalDur <= 0) return [];
    return raw.map(p => {
      const before = drawPos.filter(d => d.index < p.index).length;
      if (before <= rangeStart || before > rangeStop) return null;
      const seg = segs[before];
      const time = seg ? Math.max(0, seg.startTime - timeOff) : 0;
      // Store path entry index so hideFrom is unambiguous for zero-duration (instant) elements
      const pathIndex = nodeFirstEntry[before] !== undefined ? nodeFirstEntry[before] : filteredLen;
      return { time, type: p.type, dur: p.duration, triggered: false, pathIndex };
    }).filter(Boolean).sort((a,b) => a.time - b.time);
  }

  _parsePauses(txt) {
    const re = /<!--\s*Pause:(UntilPlay|(\d+))\s*-->/g, out = [];
    let m;
    while ((m = re.exec(txt)) !== null) {
      if (m[1] === 'UntilPlay') out.push({ type:'manual', duration:0, index:m.index });
      else if (m[2]) out.push({ type:'timed', duration:parseFloat(m[2])*1000, index:m.index });
    }
    return out;
  }

  _parseSpeed(txt) {
    const re = /<!--\s*Speed:(\d*\.?\d+)\s*-->/g, out = [];
    let m;
    while ((m = re.exec(txt)) !== null) {
      const v = parseFloat(m[1]);
      if (!isNaN(v)) out.push({ index:m.index, multiplier:v });
    }
    return out.sort((a,b) => a.index - b.index);
  }

  _extractPos(txt) {
    const re = /<(path|line|polyline|polygon|rect|circle|ellipse|text|image|use)(\s[^>]*)*>/g;
    const out = []; let m;
    while ((m = re.exec(txt)) !== null) out.push({ index:m.index, tag:m[1] });
    return out;
  }

  _canStroke(node, len, cs) {
    if (len <= 0 || !cs) return false;
    if (!cs.stroke || cs.stroke === 'none') return false;
    const op = parseFloat(cs.strokeOpacity);
    if (isFinite(op) && op <= 0) return false;
    const sw = parseFloat(cs.strokeWidth);
    return isFinite(sw) ? sw > 0 : true;
  }

  _canFillReveal(node, cs) {
    if (!cs) return false;
    const fill = cs.fill;
    if (!fill || fill === 'none') return false;
    const fo = parseFloat(cs.fillOpacity);
    if (isFinite(fo) && fo <= 0) return false;
    // Must NOT have a visible stroke (otherwise stroke animation would have handled it)
    if (cs.stroke && cs.stroke !== 'none') {
      const sw = parseFloat(cs.strokeWidth);
      if (isFinite(sw) && sw > 0) return false;
    }
    return true;
  }

  _estimateFillDuration(node, effSpd) {
    // Estimate duration from bounding box diagonal or path length
    let size = 0;
    try {
      const bb = node.getBBox();
      size = Math.sqrt(bb.width * bb.width + bb.height * bb.height);
    } catch { size = 0; }
    if (size <= 0) {
      try { if (typeof node.getTotalLength === 'function') size = node.getTotalLength() * 0.3; } catch { size = 0; }
    }
    if (size <= 0) size = 20; // fallback minimum
    return (size / effSpd) * 1000;
  }

  _invertColors(svg) {
    svg.querySelectorAll('path,line,polyline,polygon,rect,circle,ellipse,text,tspan').forEach(el => {
      const cs = window.getComputedStyle(el);
      this._invertGray(el, 'stroke', cs.stroke);
      this._invertGray(el, 'fill',   cs.fill);
    });
  }

  _invertGray(el, prop, color) {
    if (!color || color === 'none') return;
    const rgb = this._parseRgb(color);
    if (!rgb) return;
    if (Math.max(rgb.r,rgb.g,rgb.b) - Math.min(rgb.r,rgb.g,rgb.b) < 30)
      el.style[prop] = `rgb(${255-rgb.r},${255-rgb.g},${255-rgb.b})`;
  }

  _parseRgb(c) {
    let m = c.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/i);
    if (m) return { r:+m[1], g:+m[2], b:+m[3] };
    m = c.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/i);
    if (m) {
      let h = m[1];
      if (h.length===3) h = h.split('').map(x=>x+x).join('');
      return { r:parseInt(h.slice(0,2),16), g:parseInt(h.slice(2,4),16), b:parseInt(h.slice(4,6),16) };
    }
    return null;
  }

  _setupIobs() {
    if (!this._autoPlay || typeof IntersectionObserver === 'undefined') return;
    this._iobs = new IntersectionObserver(entries => {
      entries.forEach(e => {
        if (!this._autoPlay || !this._ready) return;
        if (e.isIntersecting) {
          if (!this._wasVis) {
            this._wasVis = true;
            if (!this._played) this._play(); else { this._restart(); this._play(); }
            this._played = true;
          }
        } else if (this._wasVis) {
          this._wasVis = false;
          if (this._playing) this._pause();
        }
      });
    }, { threshold: 0.2 });
    this._iobs.observe(this);
  }

  // ─── Playback ────────────────────────────────────────────────────────────────

  _play() {
    if (!this._paths.length || this._playing) return;
    if (this._finished && this._elapsed >= this._duration) this._restart();
    this._finished = false;
    this._cancelRaf();
    this._playing = true;
    this._t0 = performance.now() - this._elapsed;

    const frame = t => {
      if (!this._playing) return;
      const el = t - this._t0;
      const ct = Math.max(0, Math.min(el, this._duration));
      this._draw(ct);

      while (this._pauseIdx < this._pauses.length && this._pauses[this._pauseIdx].triggered)
        this._pauseIdx++;

      if (this._pauseIdx < this._pauses.length) {
        const p = this._pauses[this._pauseIdx];
        if (!p.triggered && ct >= p.time) {
          p.triggered = true;
          // pathIndex is the first entry AFTER the pause — reveal everything before it,
          // hide everything from it onward. Using index (not time) avoids the ambiguity
          // where instant elements (dur=0) share the same startTime as the pause point.
          const hideFrom = p.pathIndex;
          for (let i = this._revealed; i < hideFrom; i++) this._revealSeg(this._paths[i]);
          this._revealed = hideFrom;
          for (let i = hideFrom; i < this._paths.length; i++) this._hideSeg(this._paths[i]);

          this._pause();
          this._elapsed = Math.min(p.time, this._duration - 1);
          this._pauseIdx++;
          if (p.type === 'timed') {
            this._timerId = setTimeout(() => { this._timerId = null; this._play(); }, p.dur);
          } else {
            this._waitResume = true;
            this._forceCo(true); this._syncBtn();
          }
          return;
        }
      }

      if (el >= this._duration) { this._finish(); return; }
      this._rafId = requestAnimationFrame(frame);
    };

    this._forceCo(false);
    this._rafId = requestAnimationFrame(frame);
    this._syncBtn();
  }

  _pause() {
    if (!this._playing) return;
    this._playing = false;
    this._cancelRaf();
    this._elapsed = Math.min(performance.now() - this._t0, this._duration - 1);
    this._syncBtn();
  }

  _restart() {
    if (this._playing) this._pause();
    this._finished = false; this._elapsed = 0; this._pauseIdx = 0;
    this._pauses.forEach(p => p.triggered = false);
    for (let i = 0; i < this._revealed; i++) this._hideSeg(this._paths[i]);
    this._revealed = 0;
    this._waitResume = false;
    this._forceCo(!this._autoPlay); this._syncBtn();
  }

  _finish() {
    this._pause();
    this._draw(this._duration);
    this._finished = true; this._elapsed = this._duration;
    this._forceCo(true); this._waitResume = false;
  }

  // ─── Draw: O(1) per frame ────────────────────────────────────────────────────

  _draw(t) {
    const time = Math.max(0, Math.min(t, this._duration));
    const paths = this._paths, n = paths.length;

    // Rewind: un-reveal all if any done segment is now after current time
    if (this._revealed > 0 && paths[this._revealed-1].endTime > time) {
      for (let i = 0; i < this._revealed; i++) this._hideSeg(paths[i]);
      this._revealed = 0;
    }

    // Advance: reveal newly-completed segments
    while (this._revealed < n && time >= paths[this._revealed].endTime) {
      this._revealSeg(paths[this._revealed]);
      this._revealed++;
    }

    // Animate the one segment currently in progress
    if (this._revealed < n) {
      const seg = paths[this._revealed];
      if (time >= seg.startTime) this._animSeg(seg, time);
    }

    this._showSvg();
  }

  _revealSeg(seg) {
    if (seg.isText) {
      seg.el.style.fillOpacity = '1';
    } else if (seg.behavior === 'instant') {
      seg.el.style.opacity = seg.finalOp ?? '1';
    } else if (seg.behavior === 'fill-reveal') {
      seg.el.style.clipPath = 'inset(0 0% 0 0)';
      seg.el.style.opacity = seg.finalOp ?? '1';
    } else {
      seg.el.style.strokeDashoffset = '0';
      seg.el.style.strokeDasharray = seg.origDash || 'none';
      if (seg.finalFill !== undefined) seg.el.style.fillOpacity = seg.finalFill;
    }
  }

  _hideSeg(seg) {
    if (seg.isText) {
      seg.el.style.fillOpacity = '0';
    } else if (seg.behavior === 'instant') {
      seg.el.style.opacity = '0';
    } else if (seg.behavior === 'fill-reveal') {
      seg.el.style.clipPath = 'inset(0 100% 0 0)';
    } else {
      seg.el.style.strokeDasharray = `${seg.len} ${seg.len}`;
      seg.el.style.strokeDashoffset = `${seg.len}`;
      if (seg.finalFill !== undefined) seg.el.style.fillOpacity = '0';
    }
  }

  _animSeg(seg, time) {
    if (seg.isText) {
      seg.el.style.fillOpacity = time >= seg.endTime ? '1' : '0';
      return;
    }
    if (seg.behavior === 'instant') {
      seg.el.style.opacity = time >= seg.endTime ? (seg.finalOp ?? '1') : '0';
      return;
    }
    if (seg.behavior === 'fill-reveal') {
      const elapsed = Math.max(0, Math.min(seg.dur, time - seg.startTime));
      const prog = seg.dur > 0 ? elapsed / seg.dur : (time >= seg.endTime ? 1 : 0);
      const clipRight = Math.max(0, (1 - prog) * 100);
      seg.el.style.clipPath = `inset(0 ${clipRight}% 0 0)`;
      return;
    }
    const elapsed = Math.max(0, Math.min(seg.dur, time - seg.startTime));
    const prog = seg.dur > 0 ? elapsed / seg.dur : (time >= seg.endTime ? 1 : 0);
    seg.el.style.strokeDasharray  = `${seg.len} ${seg.len}`;
    seg.el.style.strokeDashoffset = `${Math.max(seg.len * (1 - prog), 0)}`;
    if (seg.finalFill !== undefined) seg.el.style.fillOpacity = '0';
  }

  // ─── Helpers ─────────────────────────────────────────────────────────────────

  _cancelRaf() {
    if (this._rafId)   { cancelAnimationFrame(this._rafId); this._rafId = null; }
    if (this._timerId) { clearTimeout(this._timerId); this._timerId = null; }
  }

  _syncBtn() {
    if (!this._btnP) return;
    this._btnP.innerHTML = this._playing
      ? '<svg viewBox="0 0 24 24"><path d="M6 19h4V5H6zm8-14v14h4V5z"/></svg>'
      : '<svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>';
    this._btnP.title = this._playing ? 'Pause' : 'Play';
  }

  _forceCo(on) { this._co?.classList.toggle('fv', on); }

  _showSvg() {
    if (this._svgShown || !this._svgEl) return;
    this._svgEl.style.visibility = 'visible';
    this._svgShown = true;
  }

  _showBanner(msg) {
    if (!this._wrap) return;
    const b = document.createElement('div');
    b.textContent = msg;
    Object.assign(b.style, {
      position:'absolute', top:'0', left:'0', right:'0',
      padding:'6px 12px', background:'rgba(255,179,0,.95)', color:'#1b1b1b',
      fontSize:'11px', fontFamily:'system-ui,sans-serif', textAlign:'center',
      zIndex:'20', borderRadius:'0 0 8px 8px', pointerEvents:'none',
      whiteSpace:'pre-wrap', wordBreak:'break-word', boxSizing:'border-box'
    });
    this._wrap.appendChild(b);
  }
}

if (!customElements.get('simple-animate-svg-component')) {
  customElements.define('simple-animate-svg-component', SimpleAnimateSvgComponent);
}
