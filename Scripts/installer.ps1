$path = (Read-Host "Gorilla Tag path").Trim('"')

if (!(Test-Path (Join-Path $path "Gorilla Tag.exe") -PathType Leaf)) {
    Write-Error "Gorilla Tag.exe not found."
    exit 1
}

$bepUrl = "https://github.com/BepInEx/BepInEx/releases/download/v5.4.23.5/BepInEx_win_x64_5.4.23.5.zip"
$dllUrl = "https://github.com/incharilla1/ShibaGT-Genesis-Reborn-Updater/releases/download/first_release/GenesisLoader.dll"

$zip = Join-Path $env:TEMP "BepInEx_5.4.23.5.zip"
$tmp = Join-Path $env:TEMP "BepInEx_5.4.23.5"

try {
    @(
        ".doorstop_version"
        "doorstop_config.ini"
        "winhttp.dll"
        "BepInEx"
    ) | ForEach-Object {
        $item = Join-Path $path $_
        if (Test-Path $item) {
            Remove-Item $item -Recurse -Force
        }
    }

    Remove-Item $zip, $tmp -Recurse -Force -ErrorAction SilentlyContinue

    Invoke-WebRequest $bepUrl -OutFile $zip
    Expand-Archive $zip -DestinationPath $tmp -Force

    Get-ChildItem $tmp -Force | ForEach-Object {
        Copy-Item $_.FullName -Destination $path -Recurse -Force
    }

    $plugins = Join-Path $path "BepInEx\plugins"
    New-Item -ItemType Directory -Path $plugins -Force | Out-Null

    Invoke-WebRequest $dllUrl -OutFile (Join-Path $plugins "GenesisLoader.dll")

    Write-Host "installed auto updater"
    Write-Host "that means you dont gotta install new version everytime a new genesis reborn comes out"
    Write-Host "just launch your game and itll be there"
}
finally {
    Remove-Item $zip, $tmp -Recurse -Force -ErrorAction SilentlyContinue
}