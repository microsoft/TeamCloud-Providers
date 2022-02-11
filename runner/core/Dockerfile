FROM mcr.microsoft.com/azure-cli
WORKDIR /

ARG TCRUNNER_BRANCH=undefined
ENV TCRUNNER_BRANCH=$TCRUNNER_BRANCH

ARG TCRUNNER_COMMIT=undefined
ENV TCRUNNER_COMMIT=$TCRUNNER_COMMIT

ARG TCRUNNER_VERSION=undefined
ENV TCRUNNER_VERSION=$TCRUNNER_VERSION

RUN set -x \
    # Install APK packages
    && apk update \
    && apk add --no-cache bash util-linux bind-tools coreutils expect \
    # Finalize RUN command
    && true

COPY docker-entrypoint.sh /docker-entrypoint.sh
COPY docker-entrypoint.d/* /docker-entrypoint.d/
COPY docker-runner.d/* /docker-runner.d/

RUN chmod +x /docker-entrypoint.sh \
    && mkdir -p /docker-entrypoint.d && find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \; \
    && mkdir -p /docker-runner.d && find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;

# Disable the container webserver by default
ENV WebServerEnabled=0

# Block default command set by base image
CMD [ "" ]

# Ensure our custom entrypoint is used
ENTRYPOINT [ "/docker-entrypoint.sh" ]