param(
    [Parameter(Mandatory = $true)]
    [string]$m
)

$root = Split-Path $PSScriptRoot
pwsh -File "$PSScriptRoot\build.ps1"

Remove-Item -Recurse -Force "$root\obj", "$root\bin", "$root\artifacts" -ErrorAction SilentlyContinue

git -C $root add .
git -C $root commit -m $m
git -C $root push