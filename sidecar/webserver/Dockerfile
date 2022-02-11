# Base Container Definition: https://github.com/nginxinc/docker-nginx/tree/master/stable/alpine

FROM nginx:stable-alpine
WORKDIR /

ARG TCRUNNER_BRANCH=undefined
ENV TCRUNNER_BRANCH=$TCRUNNER_BRANCH

ARG TCRUNNER_COMMIT=undefined
ENV TCRUNNER_COMMIT=$TCRUNNER_COMMIT

ARG TCRUNNER_VERSION=undefined
ENV TCRUNNER_VERSION=$TCRUNNER_VERSION

COPY docker-entrypoint.sh /docker-entrypoint.sh

RUN set -x \
    # install APK packages
    && apk update \
    && apk add --no-cache bash certbot certbot-nginx bind-tools curl sed \
    # get rid of entrypoint scripts
    && rm -rf /docker-entrypoint.d \
    # make entrypoint executable
    && chmod +x /docker-entrypoint.sh \
    && true

# Expose HTTP and HTTPS
EXPOSE 80 443

# Override default CMD
CMD [ "" ]

