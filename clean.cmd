@if "%_echo%"=="" echo off
pushd "%~dp0"
git clean -fdx -e [*.dev.json]||[\samples]
popd
