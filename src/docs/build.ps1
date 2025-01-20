$currentDirectory = Get-Location

Set-Location ..\blazor\powerfx
dotnet build --configuration Release
Set-Location $currentDirectory 

hugo --minify

if ((Test-Path -Path '..\..\docs'))
{ 
    Remove-Item '..\..\docs' -Recurse -Force
    
}
New-Item -ItemType Directory -Path '..\..\docs'

Copy-Item -Path 'site\*' -Destination '..\..\docs' -Recurse -Force
