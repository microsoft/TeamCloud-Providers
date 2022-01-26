# Base Container Definition: https://github.com/nginxinc/docker-nginx/tree/master/stable/alpine

FROM mcr.microsoft.com/azure-cli
WORKDIR /

RUN set -x \
    # Install APK packages
    && apk update \
    && apk add --no-cache nginx bash jq git util-linux coreutils py3-pip \
    # Install PIP packages
    && pip3 install --upgrade pip \
    && pip install certbot certbot-nginx \
    # Finalize RUN command
    && true

COPY docker-entrypoint.sh /docker-entrypoint.sh
COPY docker-entrypoint.d/* /docker-entrypoint.d/
COPY docker-runner.d/* /docker-runner.d/

RUN chmod +x /docker-entrypoint.sh \
    && mkdir -p /docker-entrypoint.d && find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \; \
    && mkdir -p /docker-runner.d && find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;

# Replace nginx configuration files
# COPY config/default.conf /etc/nginx/conf.d/default.conf
COPY config/default.conf /etc/nginx/http.d/default.conf

# Expose HTTP and HTTPS
EXPOSE 80 443

# Block default command set by base image
CMD [ "" ]

# Ensure our custom entrypoint is used
ENTRYPOINT [ "/docker-entrypoint.sh" ]