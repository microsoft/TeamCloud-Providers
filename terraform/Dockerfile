FROM teamcloud/tcrunner-core:latest
WORKDIR /

RUN apk add --no-cache terraform --repository=http://dl-cdn.alpinelinux.org/alpine/edge/community

ADD https://github.com/tmccombs/hcl2json/releases/download/v0.3.2/hcl2json_linux_amd64 /usr/local/bin/hcl2json
RUN chmod +x /usr/local/bin/hcl2json 

COPY docker-runner.d/* /docker-runner.d/
RUN find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;