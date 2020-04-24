@echo off

cd "%~dp0"
start "" cmd /C azurestorageemulator start
dotnet build -c Debug

SET terminal=wt -d .\TeamCloud.Providers.Azure.AppInsights cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% split-pane -V -d .\TeamCloud.Providers.Azure.DevOps cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% split-pane -H -d .\TeamCloud.Providers.Azure.DevTestLabs cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1"

start "" /B %terminal%
