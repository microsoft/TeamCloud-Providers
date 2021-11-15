FROM teamcloud/tcrunner-core:latest
WORKDIR /

# Install kubectl (latest release)
RUN curl -sLO "https://dl.k8s.io/release/$(curl -sL https://dl.k8s.io/release/stable.txt)/bin/linux/amd64/kubectl" && \
    mv kubectl /usr/bin/kubectl && \
    chmod +x /usr/bin/kubectl

# Install helm (latest release)
RUN apk add --update --no-cache curl ca-certificates bash git openssl && \
    curl -s https://raw.githubusercontent.com/helm/helm/main/scripts/get-helm-3 | bash

COPY docker-entrypoint.d/* /docker-entrypoint.d/
RUN find /docker-entrypoint.d/ -type f -iname "*.sh" -exec chmod +x {} \;

COPY docker-runner.d/* /docker-runner.d/
RUN find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;