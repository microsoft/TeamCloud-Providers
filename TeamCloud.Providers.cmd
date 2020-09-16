@echo off

cls
cd "%~dp0"
set "lock=%temp%\wait%random%.lock"

echo   ______                     ________                __  
echo  /_  __/__  ____ _____ ___  / ____/ /___  __  ______/ /  
echo   / / / _ \/ __ `/ __ `__ \/ /   / / __ \/ / / / __  / 
echo  / / /  __/ /_/ / / / / / / /___/ / /_/ / /_/ / /_/ / 
echo /_/  \___/\__,_/_/ /_/ /_/\____/_/\____/\__,_/\__,_/     
echo.

echo     ____                  _     __              
echo    / __ \_________ _   __(_)___/ /__  __________
echo   / /_/ / ___/ __ \ ^| / / / __  / _ \/ ___/ ___/ 
echo  / ____/ /  / /_/ / ^|/ / / /_/ /  __/ /  (__  )  
echo /_/   /_/   \____/^|___/_/\__,_/\___/_/  /____/  
echo.

echo - Starting Azure Storage Emulator
start "" cmd /C azurestorageemulator start

echo - Building Providers
start /min "" 9>"%lock%1" dotnet build --force -c Debug

:Wait 
1>nul 2>nul ping /n 2 ::1
for %%N in (1 2) do (
  (call ) 9>"%lock%%%N" || goto :Wait
) 2>nul

del "%lock%*"

SET terminal=wt --title Azure.AppInsights -d .\Azure\TeamCloud.Providers.Azure.AppInsights cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% new-tab --title Azure.DevOps -d .\Azure\TeamCloud.Providers.Azure.DevOps cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% new-tab --title Azure.DevTestLabs -d .\Azure\TeamCloud.Providers.Azure.DevTestLabs cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" ;
SET terminal=%terminal% new-tab --title GitHub -d .\GitHub\TeamCloud.Providers.GitHub cmd /C "func host start --no-build --script-root bin/Debug/netcoreapp3.1" 

start "" /B %terminal%
