@pushd %~dp0
@dotnet run --project "./targets/targets.csproj" squeaky-clean %*
@popd
