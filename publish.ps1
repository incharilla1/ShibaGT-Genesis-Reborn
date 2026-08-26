param(
    [Parameter(Mandatory = $true)]
    [string]$m
)

pwsh -File ".\build.ps1"

Remove-Item -Recurse -Force .\obj, .\bin, .\artifacts -ErrorAction SilentlyContinue

git add .
git commit -m $m
git push