FROM teamcloud/tcrunner-core:latest
WORKDIR /

COPY docker-entrypoint.d/* /docker-entrypoint.d/
COPY docker-runner.d/* /docker-runner.d/

ADD https://github.com/tmccombs/hcl2json/releases/download/v0.3.2/hcl2json_linux_amd64 /usr/local/bin/hcl2json

RUN apk add --no-cache terraform --repository=http://dl-cdn.alpinelinux.org/alpine/edge/community \
	&& chmod +x /usr/local/bin/hcl2json \
    # Mark scripts as executable
    && mkdir -p /docker-entrypoint.d && find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \; \
    && mkdir -p /docker-runner.d && find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;