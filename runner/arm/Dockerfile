FROM teamcloud/tcrunner-core:latest
WORKDIR /

COPY docker-entrypoint.d/* /docker-entrypoint.d/
COPY docker-runner.d/* /docker-runner.d/

RUN az bicep install \
	# Mark scripts as executable
    && mkdir -p /docker-entrypoint.d && find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \; \
    && mkdir -p /docker-runner.d && find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;
