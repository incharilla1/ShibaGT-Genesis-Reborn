$root = Split-Path $PSScriptRoot
dotnet clean "$root\ShibaGTGenesisReborn.csproj" -c Release
dotnet build "$root\ShibaGTGenesisReborn.csproj" -c Release
Remove-Item -Recurse -Force "$root\obj", "$root\bin" -ErrorAction SilentlyContinue
# Start-Process "steam://run/1533390"