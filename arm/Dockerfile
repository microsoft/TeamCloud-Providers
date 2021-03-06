FROM teamcloud/tcrunner-core:latest
WORKDIR /

RUN az bicep install

COPY docker-runner.d/* /docker-runner.d/
RUN find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;