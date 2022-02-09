FROM teamcloud/tcrunner-core:latest
WORKDIR /

RUN az bicep install

# to support ARM linked templates 
# we need to enable the webserver 
# running on the container instance
ENV EnableWebServer=1

COPY docker-entrypoint.d/* /docker-entrypoint.d/
RUN find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \;

COPY docker-runner.d/* /docker-runner.d/
RUN find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;