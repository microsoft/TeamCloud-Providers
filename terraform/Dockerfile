FROM teamcloud/tcrunner-core:latest
WORKDIR /

ADD https://github.com/tmccombs/hcl2json/releases/download/v0.3.2/hcl2json_darwin_amd64 /usr/local/bin/hcl2json
RUN apk add terraform

COPY docker-runner.d/* /docker-runner.d/
RUN find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;