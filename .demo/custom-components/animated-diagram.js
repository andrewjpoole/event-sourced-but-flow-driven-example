const SLIDE_WIDTH = 960;
const SLIDE_HEIGHT = 540;
const TYPEWRITER_SPEED_FACTOR = 0.25;

class SimpleAnimateSvgComponent extends HTMLElement {
  static get observedAttributes() {
    return [
      'svg-file-path',
      'animation-speed',
      'width',
      'height',
      'sizing',
      'background-color',
      'border',
      'invert-colors',
      'auto-play'
    ];
  }

  constructor() {
    super();
    this._shadow = this.attachShadow({ mode: 'open' });
    this._container = document.createElement('div');
    this._shadow.appendChild(this._container);

    this._paths = [];
    this._elementSegments = [];
    this._pausePoints = [];
    this._animationFrameId = null;
    this._timedPauseTimeout = null;
    this._currentPauseIndex = 0;
    this._currentSegmentIndex = 0;
    this._isPlaying = false;
    this._isFinished = false;
    this._elapsedBeforePause = 0;
    this._startTime = null;
    this._totalLength = 1;
    this._speed = 100;
    this._duration = 2000;
    this._autoPlay = true;
    this._intersectionObserver = null;
    this._wasVisible = false;
    this._hasAutoPlayed = false;
    this._isReadyForVisibility = false;
    this._slowDraw = false;
    this._speedHints = [];
    this._waitingForManualResume = false;
    this._wrapper = null;
    this._parseWarningBanner = null;
    this._currentSvgElement = null;
    this._isSvgRevealed = false;
    this._activeLoadController = null;
  }

  connectedCallback() {
    this._render();
  }

  disconnectedCallback() {
    this._cancelAnimation();
    if (this._intersectionObserver) {
      this._intersectionObserver.disconnect();
      this._intersectionObserver = null;
    }
    if (this._timedPauseTimeout) {
      clearTimeout(this._timedPauseTimeout);
      this._timedPauseTimeout = null;
    }
    if (this._activeLoadController) {
      this._activeLoadController.abort();
      this._activeLoadController = null;
    }
  }

  attributeChangedCallback(name, oldValue, newValue) {
    if (oldValue !== newValue) {
      this._render();
    }
  }

  _render() {
    this._cancelAnimation();
    this._isReadyForVisibility = false;
    this._hasAutoPlayed = false;
    this._elapsedBeforePause = 0;
    this._paths = [];
    this._elementSegments = [];
    this._pausePoints = [];
    this._waitingForManualResume = false;
    this._speedHints = [];
    this._totalLength = 1;
    this._container.innerHTML = '';
    this._container.style.position = 'relative';
    this._container.style.width = '100%';
    this._container.style.height = '100%';
    if (this._parseWarningBanner) {
      this._parseWarningBanner.remove();
      this._parseWarningBanner = null;
    }
    if (this._activeLoadController) {
      this._activeLoadController.abort();
      this._activeLoadController = null;
    }
    this._currentSvgElement = null;
    this._isSvgRevealed = false;

    const width = this.getAttribute('width') || '100%';
    const height = this.getAttribute('height');
    const sizing = (this.getAttribute('sizing') || 'fill').toLowerCase();
    const backgroundColor = this.getAttribute('background-color');
    const border = this.getAttribute('border');
    const filePath = this.getAttribute('svg-file-path');
    const invertColors = this.getAttribute('invert-colors') === 'true';
    const autoPlayAttr = this.getAttribute('auto-play');
    this._autoPlay = autoPlayAttr !== 'false';

    const speed = parseInt(this.getAttribute('animation-speed'), 10);
    this._speed = Number.isNaN(speed) ? 100 : Math.max(1, speed);

    this.style.display = 'inline-block';
    this.style.boxSizing = 'border-box';
    this.style.width = width;
    this.style.height = sizing === 'fill' ? '100%' : height || 'auto';
    this.style.padding = '0';
    this.style.margin = '0';
    this.style.lineHeight = '0';
    this.style.verticalAlign = 'top';
    if (backgroundColor) this.style.backgroundColor = backgroundColor;
    if (border) this.style.border = border;

    if (sizing === 'fill') {
      this.style.position = 'absolute';
      this.style.top = '0';
      this.style.left = '0';
      this.style.width = '100%';
      this.style.height = '100%';
      this.style.overflow = 'hidden';
      this.style.aspectRatio = `${SLIDE_WIDTH}/${SLIDE_HEIGHT}`;
    }

    const style = document.createElement('style');
    style.textContent = `
      :host {
        contain: layout style;
      }
      .wrapper {
        position: relative;
        width: 100%;
        height: 100%;
        overflow: hidden;
      }
      .svg-content {
        width: 100%;
        height: 100%;
        display: block;
      }
      .controls-overlay {
        position: absolute;
        bottom: 10px;
        right: 10px;
        display: flex;
        gap: 8px;
        padding: 6px 12px;
        border-radius: 999px;
        background: rgba(16, 16, 16, 0.65);
        color: #fff;
        opacity: 0;
        pointer-events: none;
        transition: opacity 0.3s ease;
        align-items: center;
        backdrop-filter: blur(4px);
        z-index: 10;
      }
      .wrapper:hover + .controls-overlay,
      :host(:hover) .controls-overlay,
      .controls-overlay.force-visible,
      .controls-overlay:hover {
        opacity: 1;
        pointer-events: auto;
      }
      .control-btn {
        width: 28px;
        height: 28px;
        border-radius: 50%;
        border: none;
        background: transparent;
        color: inherit;
        display: flex;
        align-items: center;
        justify-content: center;
        cursor: pointer;
        transition: transform 0.15s, background 0.15s;
      }
      .control-btn:focus-visible {
        outline: 2px solid #fff;
        outline-offset: 2px;
      }
      .control-btn:hover {
        transform: scale(1.1);
      }
      .control-btn svg {
        width: 18px;
        height: 18px;
        fill: currentColor;
      }
      svg .finished path,
      svg .finished line,
      svg .finished polyline,
      svg .finished polygon,
      svg .finished rect,
      svg .finished circle,
      svg .finished ellipse {
        fill-opacity: 1 !important;
      }
    `;
    this._container.appendChild(style);

    const wrapper = document.createElement('div');
    wrapper.className = 'wrapper';
    this._wrapper = wrapper;

    this._svgWrapper = document.createElement('div');
    this._svgWrapper.className = 'svg-content';
    wrapper.appendChild(this._svgWrapper);
    this._container.appendChild(wrapper);

    this._controlsOverlay = document.createElement('div');
    this._controlsOverlay.className = 'controls-overlay';
    this._container.appendChild(this._controlsOverlay);

    this._setupControls();

    if (!filePath) {
      this._showMessage('svg-file-path attribute is required');
      return;
    }

    this._loadSvg(filePath, { sizing, width, invertColors, autoPlay: this._autoPlay });
  }

  _showMessage(message) {
    const notice = document.createElement('div');
    notice.style.padding = '12px';
    notice.style.color = '#fff';
    notice.style.fontSize = '14px';
    notice.innerText = message;
    this._svgWrapper.innerHTML = '';
    this._svgWrapper.appendChild(notice);
  }

  _setupControls() {
    this._controlsOverlay.innerHTML = '';
    const icons = {
      start: '<svg viewBox="0 0 24 24"><g transform="translate(24 0) scale(-1 1)"><path d="M6 6v12l10-6z"/><path d="M18 6h2v12h-2z"/></g></svg>',
      play: '<svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>',
      pause: '<svg viewBox="0 0 24 24"><path d="M6 19h4V5H6zm8-14v14h4V5z"/></svg>',
      end: '<svg viewBox="0 0 24 24"><path d="M6 6v12l10-6z"/><path d="M18 6h2v12h-2z"/></svg>'
    };

    this._btnStart = document.createElement('button');
    this._btnStart.className = 'control-btn';
    this._btnStart.title = 'Restart';
    this._btnStart.innerHTML = icons.start;
    this._btnStart.onclick = (event) => {
      event.stopPropagation();
      this._reset();
      this._showControls(true);
    };

    this._btnPlayPause = document.createElement('button');
    this._btnPlayPause.className = 'control-btn';
    this._btnPlayPause.title = 'Play';
    this._btnPlayPause.innerHTML = icons.play;
    this._btnPlayPause.onclick = (event) => {
      event.stopPropagation();
      if (this._isPlaying) {
        this._pause();
      } else {
        this._play();
      }
    };

    this._btnEnd = document.createElement('button');
    this._btnEnd.className = 'control-btn';
    this._btnEnd.title = 'Finish';
    this._btnEnd.innerHTML = icons.end;
    this._btnEnd.onclick = (event) => {
      event.stopPropagation();
      this._finish();
    };

    this._controlsOverlay.appendChild(this._btnStart);
    this._controlsOverlay.appendChild(this._btnPlayPause);
    this._controlsOverlay.appendChild(this._btnEnd);
    this._updatePlayButtonIcon();
  }

  async _loadSvg(filePath, options) {
    try {
      const controller = new AbortController();
      this._activeLoadController = controller;
      const response = await fetch(filePath, { signal: controller.signal });
      if (!response.ok) throw new Error('Unable to fetch SVG');
      const svgText = await response.text();
      this._pauseComments = this._parsePauseComments(svgText);
      this._speedHints = this._parseSpeedHints(svgText);
      const parser = new DOMParser();
      const doc = parser.parseFromString(svgText, 'image/svg+xml');
      const parserError = doc.querySelector('parsererror');
      if (parserError) {
        let detail = parserError.textContent?.trim() || 'Unknown parse error';
        const lower = detail.toLowerCase();
        const firstOp = lower.indexOf('error on line');
        if (firstOp >= 0) {
          const secondOp = lower.indexOf('error on line', firstOp + 1);
          if (secondOp >= 0) {
            detail = detail.slice(0, secondOp).trim();
          }
        }
        const message = `SVG parse error: ${detail}`;
        console.error(`[AnimateSvg] ${message}`);
        this._showParseWarningBanner(message);
        throw new Error(message);
      }
      const svgEl = doc.querySelector('svg');
      if (!svgEl) throw new Error('SVG tag not found');
      this._svgWrapper.innerHTML = '';
      this._svgWrapper.appendChild(svgEl);
      this._currentSvgElement = svgEl;
      this._isSvgRevealed = false;
      svgEl.style.visibility = 'hidden';
      this._applySizing(svgEl, options.sizing, options.width);
      if (options.invertColors) {
        this._invertColors(svgEl);
      }
      this._prepareAnimation(svgEl, svgText);
      this._isReadyForVisibility = true;
      this._setupVisibilityObserver();
      if (!this._autoPlay) {
        this._draw(0);
      }
      this._activeLoadController = null;
    } catch (error) {
      if (error.name === 'AbortError') {
        this._activeLoadController = null;
        return;
      }
      this._showMessage('Error loading SVG');
      console.error(error);
      this._activeLoadController = null;
    }
  }

  _applySizing(svgEl, sizing, width) {
    const padding = 20;
    let bbox;
    try {
      bbox = svgEl.getBBox();
    } catch {
      bbox = { x: 0, y: 0, width: 0, height: 0 };
    }
    const hasBBox = Number.isFinite(bbox.width) && Number.isFinite(bbox.height) && bbox.width > 0 && bbox.height > 0;
    const viewBoxWidth = hasBBox ? bbox.width : parseFloat(svgEl.getAttribute('width')) || 100;
    const viewBoxHeight = hasBBox ? bbox.height : parseFloat(svgEl.getAttribute('height')) || 100;
    const viewBoxX = Number.isFinite(bbox.x) ? bbox.x - padding : 0;
    const viewBoxY = Number.isFinite(bbox.y) ? bbox.y - padding : 0;
    const viewBoxString = `${viewBoxX} ${viewBoxY} ${viewBoxWidth + padding * 2} ${viewBoxHeight + padding * 2}`;
    svgEl.setAttribute('viewBox', viewBoxString);
    if (sizing === 'fill') {
      svgEl.removeAttribute('width');
      svgEl.removeAttribute('height');
      svgEl.style.width = '100%';
      svgEl.style.height = '100%';
      svgEl.style.maxWidth = '100%';
      svgEl.style.maxHeight = '100%';
      svgEl.setAttribute('preserveAspectRatio', 'xMidYMid meet');
    } else if (sizing === 'exact') {
      svgEl.style.width = width;
      svgEl.style.height = 'auto';
      svgEl.setAttribute('preserveAspectRatio', 'xMidYMid meet');
    }
  }

  _prepareAnimation(svgEl, svgText) {
    const drawablePositions = this._extractDrawablePositions(svgText);
    const speedHints = this._speedHints || [];
    const nodes = Array.from(
      svgEl.querySelectorAll('path, line, polyline, polygon, rect, circle, ellipse, text, image, use')
    );
    let cumulativeLength = 0;
    let cumulativeTime = 0;
    let hintIndex = 0;
    let currentMultiplier = 1;
    const animEntries = [];
    const elementSegments = [];

    nodes.forEach((node, idx) => {
      const tag = node.tagName.toLowerCase();
      const drawableInfo = drawablePositions[idx] || { index: Number.MAX_SAFE_INTEGER };
      while (hintIndex < speedHints.length && speedHints[hintIndex].index <= drawableInfo.index) {
        const hint = speedHints[hintIndex];
        currentMultiplier = hint.multiplier;
        hintIndex += 1;
      }

      const effectiveSpeed = Math.max(this._speed * currentMultiplier, 0.0001);
      const startAt = cumulativeLength;
      const startTime = cumulativeTime;
      let length = 0;
      let nodeDuration = 0;

      if (tag === 'text') {
        const { entries, totalLength, totalDuration } = this._processTextElement(
          node,
          cumulativeLength,
          startTime,
          this._speed,
          currentMultiplier
        );
        entries.forEach((entry) => animEntries.push(entry));
        length = totalLength;
        nodeDuration = totalDuration;
      } else {
        if (typeof node.getTotalLength === 'function') {
          try {
            length = node.getTotalLength();
          } catch {
            length = 0;
          }
        }
        const computedStyle =
          typeof window !== 'undefined' && typeof window.getComputedStyle === 'function'
            ? window.getComputedStyle(node)
            : null;
        const canAnimateStroke = this._canAnimateStroke(node, length, computedStyle);
        if (canAnimateStroke) {
          const segmentDuration = (length / effectiveSpeed) * 1000;
          const segmentStartTime = startTime;
          const segmentEndTime = segmentStartTime + segmentDuration;
          animEntries.push({
            el: node,
            length,
            startAt,
            endAt: startAt + length,
            isText: false,
            duration: segmentDuration,
            startTime: segmentStartTime,
            endTime: segmentEndTime,
            behavior: 'stroke',
            finalFillOpacity: computedStyle ? computedStyle.fillOpacity : '1',
            finalOpacity: computedStyle ? computedStyle.opacity : '1'
          });
          nodeDuration = segmentDuration;
          node.style.fillOpacity = '0';
          node.style.strokeOpacity = node.style.strokeOpacity || '1';
        } else {
          animEntries.push({
            el: node,
            length: 0,
            startAt,
            endAt: startAt,
            isText: false,
            duration: 0,
            startTime,
            endTime: startTime,
            behavior: 'instant',
            finalOpacity: computedStyle ? computedStyle.opacity : '1'
          });
          node.style.opacity = '0';
          if (!node.style.transition) {
            node.style.transition = 'opacity 0.15s linear';
          }
        }
      }

      const endAt = cumulativeLength + length;
      const endTime = startTime + nodeDuration;
      elementSegments.push({ startAt, endAt, tag, startTime, endTime });
      cumulativeLength = endAt;
      cumulativeTime = endTime;
    });

    this._totalLength = cumulativeLength > 0 ? cumulativeLength : 1;
    this._paths = animEntries;
    this._elementSegments = elementSegments;
    this._currentSegmentIndex = 0;
    this._duration = cumulativeTime > 0 ? cumulativeTime : 2000;
    if (!isFinite(this._duration) || this._duration <= 0) {
      this._duration = 2000;
    }


    this._paths.forEach((segment) => {
      if (segment.isText) {
        segment.el.style.fillOpacity = '0';
        segment.el.style.transition = 'fill-opacity 0.15s linear';
        return;
      }
      if (segment.behavior === 'instant') {
        segment.el.style.opacity = '0';
        if (!segment.el.style.transition) {
          segment.el.style.transition = 'opacity 0.15s linear';
        }
        return;
      }
      segment.el.style.fillOpacity = '0';
      const len = segment.length;
      segment.el.style.strokeDasharray = `${len} ${len}`;
      segment.el.style.strokeDashoffset = `${len}`;
      segment.el.style.transition = 'stroke-dashoffset 0.1s linear';
    });

    this._pausePoints = this._mapCommentsToPausePoints(
      this._pauseComments,
      drawablePositions,
      elementSegments,
      this._duration
    );
  }

  _processTextElement(textEl, startAt, startTime, baseSpeed, multiplier) {
    const text = textEl.textContent || '';
    textEl.textContent = '';
    const characters = Array.from(text);
    const computedStyle = window.getComputedStyle(textEl);
    const entries = [];
    let cursor = startAt;
    let currentTime = startTime;
    const effectiveSpeed = Math.max(baseSpeed * Math.max(multiplier, 0.01) * TYPEWRITER_SPEED_FACTOR, 0.0001);

    characters.forEach((char, index) => {
      const displayChar = char === ' ' ? '\u00A0' : char;
      const tspan = document.createElementNS('http://www.w3.org/2000/svg', 'tspan');
      tspan.textContent = displayChar;
      if (index === 0) {
        ['x', 'y', 'dx', 'dy'].forEach((attr) => {
          const value = textEl.getAttribute(attr);
          if (value !== null) tspan.setAttribute(attr, value);
        });
      }
      tspan.style.fontFamily = computedStyle.fontFamily;
      tspan.style.fontSize = computedStyle.fontSize;
      tspan.style.fontWeight = computedStyle.fontWeight;
      tspan.style.fontStyle = computedStyle.fontStyle;
      tspan.style.fill = computedStyle.fill || computedStyle.color || '#000';
      tspan.style.stroke = computedStyle.stroke || 'none';
      tspan.style.strokeWidth = computedStyle.strokeWidth;
      tspan.style.fillOpacity = '0';
      tspan.style.transition = 'fill-opacity 0.15s linear';
      textEl.appendChild(tspan);

      const charLength = Math.max(tspan.getComputedTextLength(), 1);
      const charDuration = (charLength / effectiveSpeed) * 1000;
      entries.push({
        el: tspan,
        length: charLength,
        startAt: cursor,
        endAt: cursor + charLength,
        isText: true,
        startTime: currentTime,
        endTime: currentTime + charDuration,
        duration: charDuration,
        behavior: 'text'
      });
      cursor += charLength;
      currentTime += charDuration;
    });

    return { entries, totalLength: cursor - startAt, totalDuration: currentTime - startTime };
  }

  _mapCommentsToPausePoints(comments, drawables, segments, totalDuration) {
    if (!comments || !comments.length) return [];
    const points = [];

    comments.forEach((comment) => {
      const indexBefore = drawables.filter((item) => item.index < comment.index).length;
      let progress = 0;
      if (segments.length && totalDuration > 0) {
        if (indexBefore <= 0) {
          progress = 0;
        } else if (indexBefore >= segments.length) {
          progress = 1;
        } else {
          const targetSegment = segments[indexBefore - 1];
          progress = targetSegment ? targetSegment.endTime / totalDuration : 0;
        }
      }
      points.push({
        progress: Math.min(1, Math.max(0, progress)),
        type: comment.type,
        duration: comment.duration,
        triggered: false
      });
    });

    points.sort((a, b) => a.progress - b.progress);
    return points;
  }

  _parseSpeedHints(svgText) {
    const pattern = /<!--\s*Speed:(\d*\.?\d+)\s*-->/g;
    const hints = [];
    let match;
    while ((match = pattern.exec(svgText)) !== null) {
      const multiplier = parseFloat(match[1]);
      if (!Number.isNaN(multiplier)) {
        hints.push({ index: match.index, multiplier: multiplier });
      }
    }
    hints.sort((a, b) => a.index - b.index);
    return hints;
  }

  _extractDrawablePositions(svgText) {
    const regex = /<(path|line|polyline|polygon|rect|circle|ellipse|text|image|use)(\s[^>]*)*>/g;
    const positions = [];
    let match;
    while ((match = regex.exec(svgText)) !== null) {
      positions.push({ index: match.index, tag: match[1] });
    }
    return positions;
  }

  _canAnimateStroke(node, length, computedStyle = null) {
    if (length <= 0) return false;
    let style = computedStyle;
    if (!style) {
      if (typeof window === 'undefined' || typeof window.getComputedStyle !== 'function') {
        return true;
      }
      style = window.getComputedStyle(node);
    }
    const stroke = style.stroke;
    if (!stroke || stroke === 'none') return false;
    const strokeOpacity = parseFloat(style.strokeOpacity);
    const opacity = Number.isFinite(strokeOpacity) ? strokeOpacity : 1;
    if (opacity <= 0) return false;
    const strokeWidth = parseFloat(style.strokeWidth);
    const hasStrokeWidth = Number.isFinite(strokeWidth) ? strokeWidth > 0 : true;
    return hasStrokeWidth;
  }

  _parsePauseComments(svgText) {
    const pattern = /<!--\s*Pause:(UntilPlay|(\d+))\s*-->/g;
    const pauses = [];
    let match;
    while ((match = pattern.exec(svgText)) !== null) {
      if (match[1] === 'UntilPlay') {
        pauses.push({ type: 'manual', duration: 0, index: match.index });
      } else if (match[2]) {
        pauses.push({ type: 'timed', duration: parseFloat(match[2]) * 1000, index: match.index });
      }
    }
    return pauses;
  }

  _invertColors(svgEl) {
    const elements = svgEl.querySelectorAll('path, line, polyline, polygon, rect, circle, ellipse, text, tspan');
    elements.forEach((element) => {
      const style = window.getComputedStyle(element);
      this._invertIfGrayscale(element, 'stroke', style.stroke);
      this._invertIfGrayscale(element, 'fill', style.fill);
    });
  }

  _invertIfGrayscale(el, property, color) {
    if (!color || color === 'none' || color === 'transparent') return;
    const rgb = this._parseRgbColor(color);
    if (!rgb) return;
    const { r, g, b } = rgb;
    if (Math.max(r, g, b) - Math.min(r, g, b) < 30) {
      el.style[property] = `rgb(${255 - r}, ${255 - g}, ${255 - b})`;
    }
  }

  _parseRgbColor(color) {
    const rgbMatch = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/i);
    if (rgbMatch) {
      return {
        r: parseInt(rgbMatch[1], 10),
        g: parseInt(rgbMatch[2], 10),
        b: parseInt(rgbMatch[3], 10)
      };
    }

    const hexMatch = color.match(/^#([0-9a-f]{3}|[0-9a-f]{6})$/i);
    if (hexMatch) {
      let hex = hexMatch[1];
      if (hex.length === 3) {
        hex = hex.split('').map((h) => h + h).join('');
      }
      return {
        r: parseInt(hex.slice(0, 2), 16),
        g: parseInt(hex.slice(2, 4), 16),
        b: parseInt(hex.slice(4, 6), 16)
      };
    }

    return null;
  }

  _setupVisibilityObserver() {
    if (!this._autoPlay) return;
    if (typeof IntersectionObserver === 'undefined') return;
    if (this._intersectionObserver) {
      this._intersectionObserver.disconnect();
      this._intersectionObserver = null;
    }
    this._intersectionObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => this._handleVisibility(entry));
    }, { threshold: 0.2 });
    this._intersectionObserver.observe(this);
  }

  _handleVisibility(entry) {
    if (!this._autoPlay || !this._isReadyForVisibility) return;
    if (entry.isIntersecting) {
      if (!this._wasVisible) {
        this._wasVisible = true;
        if (!this._hasAutoPlayed) {
          this._play();
        } else {
          this._reset();
          this._play();
        }
        this._hasAutoPlayed = true;
      }
    } else if (this._wasVisible) {
      this._wasVisible = false;
      if (this._isPlaying) {
        this._pause();
      }
    }
  }

  _play() {
    if (!this._paths.length || this._isPlaying) return;
    if (this._isFinished) {
      this._reset();
    }
    this._cancelAnimation();
    this._isPlaying = true;
    this._startTime = performance.now() - this._elapsedBeforePause;
    const drawFrame = (time) => {
      if (!this._isPlaying) return;
      const elapsed = time - this._startTime;
      if (elapsed >= this._duration) {
        this._finish();
        return;
      }
      const clampedElapsed = Math.max(0, elapsed);
      this._draw(clampedElapsed);
      let progress = clampedElapsed / this._duration;
      while (
        this._currentPauseIndex < this._pausePoints.length &&
        this._pausePoints[this._currentPauseIndex].triggered
      ) {
        this._currentPauseIndex += 1;
      }
      if (this._currentPauseIndex < this._pausePoints.length) {
        const pause = this._pausePoints[this._currentPauseIndex];
        if (!pause.triggered && progress >= pause.progress) {
          pause.triggered = true;
          const pausedElapsed = Math.max(0, Math.min(elapsed, this._duration));
          if (pause.type === 'timed') {
            this._pause();
            this._elapsedBeforePause = pausedElapsed;
            this._currentPauseIndex += 1;
            this._timedPauseTimeout = setTimeout(() => {
              this._timedPauseTimeout = null;
              this._play();
            }, pause.duration);
          } else {
            this._pause();
            this._waitingForManualResume = true;
            this._showControls(true);
            this._updatePlayButtonIcon();
          }
          return;
        }
      }
      this._animationFrameId = requestAnimationFrame(drawFrame);
    };
    this._showControls(false);
    this._animationFrameId = requestAnimationFrame(drawFrame);
    this._updatePlayButtonIcon();
  }

  _pause() {
    if (!this._isPlaying) return;
    this._isPlaying = false;
    if (this._animationFrameId) {
      cancelAnimationFrame(this._animationFrameId);
      this._animationFrameId = null;
    }
    this._elapsedBeforePause = performance.now() - this._startTime;
    this._updatePlayButtonIcon();
  }

  _reset() {
    this._pause();
    this._isFinished = false;
    this._elapsedBeforePause = 0;
    this._currentPauseIndex = 0;
    this._pausePoints.forEach((point) => (point.triggered = false));
    this._currentSegmentIndex = 0;
    this._draw(0);
    this._showControls(this._autoPlay === false);
    this._waitingForManualResume = false;
  }

  _finish() {
    this._pause();
    this._draw(this._duration);
    this._isFinished = true;
    this._elapsedBeforePause = this._duration;
    this._currentSegmentIndex = this._paths.length;
    this._showControls(true);
    this._waitingForManualResume = false;
  }

  _draw(currentTime) {
    const time = Math.max(0, Math.min(currentTime, this._duration));
    while (
      this._currentSegmentIndex < this._paths.length &&
      time >= this._paths[this._currentSegmentIndex].endTime
    ) {
      this._currentSegmentIndex += 1;
    }

    this._paths.forEach((segment, index) => {
      if (index < this._currentSegmentIndex) {
        if (segment.isText) {
          segment.el.style.fillOpacity = '1';
        } else if (segment.behavior === 'instant') {
          segment.el.style.opacity = segment.finalOpacity ?? '1';
        } else {
          segment.el.style.strokeDashoffset = '0';
          if (segment.finalFillOpacity !== undefined) {
            segment.el.style.fillOpacity = segment.finalFillOpacity;
          }
        }
        return;
      }

      if (index > this._currentSegmentIndex) {
        if (segment.isText) {
          segment.el.style.fillOpacity = '0';
        } else if (segment.behavior === 'instant') {
          segment.el.style.opacity = '0';
        } else {
          segment.el.style.strokeDashoffset = `${segment.length}`;
          if (segment.finalFillOpacity !== undefined) {
            segment.el.style.fillOpacity = '0';
          }
        }
        return;
      }

      if (segment.isText) {
        segment.el.style.fillOpacity = time >= segment.endTime ? '1' : '0';
        return;
      }

      if (segment.behavior === 'instant') {
        const targetOpacity = segment.finalOpacity ?? '1';
        segment.el.style.opacity = time >= segment.endTime ? targetOpacity : '0';
        return;
      }

      const elapsedInSegment = Math.max(0, Math.min(segment.duration, time - segment.startTime));
      const segmentProgress = segment.duration > 0 ? elapsedInSegment / segment.duration : time >= segment.endTime ? 1 : 0;
      const offset = Math.max(segment.length * (1 - segmentProgress), 0);
      segment.el.style.strokeDashoffset = `${offset}`;
      if (segment.finalFillOpacity !== undefined) {
        segment.el.style.fillOpacity = '0';
      }
    });
    this._showSvg();
  }

  _cancelAnimation() {
    if (this._animationFrameId) {
      cancelAnimationFrame(this._animationFrameId);
      this._animationFrameId = null;
    }
    if (this._timedPauseTimeout) {
      clearTimeout(this._timedPauseTimeout);
      this._timedPauseTimeout = null;
    }
  }

  _updatePlayButtonIcon() {
    if (!this._btnPlayPause) return;
    this._btnPlayPause.innerHTML = this._isPlaying ? '<svg viewBox="0 0 24 24"><path d="M6 19h4V5H6zm8-14v14h4V5z"/></svg>' : '<svg viewBox="0 0 24 24"><path d="M8 5v14l11-7z"/></svg>';
    this._btnPlayPause.title = this._isPlaying ? 'Pause' : 'Play';
  }

  _showControls(forceVisible) {
    if (!this._controlsOverlay) return;
    if (forceVisible) {
      this._controlsOverlay.classList.add('force-visible');
    } else {
      this._controlsOverlay.classList.remove('force-visible');
    }
  }

  _showSvg() {
    if (this._isSvgRevealed || !this._currentSvgElement) return;
    this._currentSvgElement.style.visibility = 'visible';
    this._isSvgRevealed = true;
  }

  _showParseWarningBanner(message) {
    if (!this._container) return;
    if (this._parseWarningBanner) {
      this._parseWarningBanner.textContent = message;
      return;
    }
    const banner = document.createElement('div');
    banner.textContent = message;
    banner.style.position = 'absolute';
    banner.style.top = '0';
    banner.style.left = '0';
    banner.style.right = '0';
    banner.style.padding = '6px 12px';
    banner.style.background = 'rgba(255, 179, 0, 0.95)';
    banner.style.color = '#1b1b1b';
    banner.style.fontSize = '11px';
    banner.style.fontFamily = 'system-ui, sans-serif';
    banner.style.textAlign = 'center';
    banner.style.zIndex = '20';
    banner.style.borderBottomLeftRadius = '8px';
    banner.style.borderBottomRightRadius = '8px';
    banner.style.pointerEvents = 'none';
    banner.style.whiteSpace = 'pre-wrap';
    banner.style.wordBreak = 'break-word';
    banner.style.lineHeight = '1.2';
    banner.style.maxHeight = 'none';
    banner.style.boxSizing = 'border-box';
    banner.style.display = 'block';
    banner.style.margin = '0 auto';
    this._container.appendChild(banner);
    this._parseWarningBanner = banner;
  }
}

if (!customElements.get('simple-animate-svg-component')) {
  customElements.define('simple-animate-svg-component', SimpleAnimateSvgComponent);
}
