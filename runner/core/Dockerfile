# Base Container Definition: https://github.com/nginxinc/docker-nginx/tree/master/stable/alpine

# FROM mcr.microsoft.com/azure-cli:2.0.80
FROM mcr.microsoft.com/azure-cli
WORKDIR /

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