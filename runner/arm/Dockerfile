FROM teamcloud/tcrunner-core:latest
WORKDIR /

ARG TCRUNNER_BRANCH=undefined
ENV TCRUNNER_BRANCH=$TCRUNNER_BRANCH

ARG TCRUNNER_COMMIT=undefined
ENV TCRUNNER_COMMIT=$TCRUNNER_COMMIT

ARG TCRUNNER_VERSION=undefined
ENV TCRUNNER_VERSION=$TCRUNNER_VERSION

COPY docker-entrypoint.d/* /docker-entrypoint.d/
COPY docker-runner.d/* /docker-runner.d/

RUN az bicep install \
	# Mark scripts as executable
    && mkdir -p /docker-entrypoint.d && find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \; \
    && mkdir -p /docker-runner.d && find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;
