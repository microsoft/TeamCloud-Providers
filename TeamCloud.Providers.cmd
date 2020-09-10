@echo off

cd "%~dp0"
start "" cmd /C azurestorageemulator start
dotnet build -c Debug .\**\*.csproj

SET terminal=wt --title Azure.AppInsights -d .\Azure\TeamCloud.Providers.Azure.AppInsights cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% new-tab --title Azure.DevOps -d .\Azure\TeamCloud.Providers.Azure.DevOps cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% new-tab --title Azure.DevTestLabs -d .\Azure\TeamCloud.Providers.Azure.DevTestLabs cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% new-tab --title GitHub -d .\GitHub\TeamCloud.Providers.GitHub cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" 

start "" /B %terminal%
