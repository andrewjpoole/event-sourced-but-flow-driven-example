# The QueryableTraceCollector is a container resource

When added to an Aspire project, a container image will be pulled from Docker Hub, if making any changes to the QueryableTraceCollector, we need to build a new Dicker image and push it...

- First log into Docker Hub and create a repository, that will give you a tag to push to. Mine is [here](https://hub.docker.com/repository/docker/andrewjpoole/queryabletracecollector/general)
- open terminal at root of repo
- build the image
`docker build -f .\aspire-experiments\QueryableTraceCollector\Dockerfile . -t andrewjpoole/queryabletracecollector`
- check it exists locally
`docker image ls`
- push to Docker registry
`docker push andrewjpoole/queryabletracecollector`