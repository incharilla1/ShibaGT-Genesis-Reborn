dotnet clean ShibaGTGenesisReborn.csproj -c Release
dotnet build ShibaGTGenesisReborn.csproj -c Release
Remove-Item -Recurse -Force .\obj, .\bin -ErrorAction SilentlyContinue
# Start-Process "steam://run/1533390"