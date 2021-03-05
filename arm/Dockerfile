FROM markusheiliger/tcrunner:latest
WORKDIR /

COPY docker-runner.d/* /docker-runner.d/
RUN find /docker-runner.d/ -type f -iname "*.sh" -exec chmod +x {} \;