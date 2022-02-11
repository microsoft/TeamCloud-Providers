FROM teamcloud/tcrunner-core:latest
WORKDIR /

COPY docker-entrypoint.d/* /docker-entrypoint.d/
COPY docker-runner.d/* /docker-runner.d/

RUN apk add --update --no-cache curl ca-certificates bash git openssl \
    # Install kubectl (latest release)
    && curl -sLO "https://dl.k8s.io/release/$(curl -sL https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl" \
    && mv kubectl /usr/bin/kubectl \
    && chmod +x /usr/bin/kubectl \
    # Install helm (latest release)
    && curl -s https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash \
    # Mark scripts as executable
    && mkdir -p /docker-entrypoint.d && find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \; \
    && mkdir -p /docker-runner.d && find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;