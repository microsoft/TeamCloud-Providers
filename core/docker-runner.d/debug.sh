#!/bin/bash

# this is a default task implementation which in case of
# system commands only writes some informational output.
# if this task should do something you must create your
# own runner container and replace this file with your
# custom task implementation.

echo "CAUTION !!!"
echo ""
echo "This command will keep the process alive until the container is terminated"
echo "manually or it reaches the max TTL monitored by the TeamCloud orchestrator."
echo ""
echo "During this time you could connect to the container instance to debug."

tail -f /dev/null