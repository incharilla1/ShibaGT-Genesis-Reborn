<div align="center">
<img src="opensource.svg" width="120" alt="Shiba GT Genesis Reborn">

# Shiba GT Genesis Reborn

Open source Gorilla Tag menu by Incharilla.

[GitHub](https://github.com/incharilla1) · [Discord](https://discord.gg/XRmtJu8aUj)
</div>

## Install

1. Grab the latest `.dll` from Releases.
2. Drop it in `BepInEx/plugins`.
3. Launch Gorilla Tag.

Or run `installer.ps1`. **It reinstalls BepInEx, which deletes all your existing plugins.**

## Build

```bash
git clone https://github.com/incharilla1/ShibaGT-Genesis-Reborn.git
```

Open `GenesisReborn.sln` in Visual Studio, fix the BepInEx and Gorilla Tag references if needed, and build in Release.

## Backend

The menu pulls its server data from `https://cxs.incharilla.workers.dev`.

```json
{
  "status": "online",
  "discord-invite": "https://discord.gg/XRmtJu8aUj",
  "user-count": 0,
  "lockdown": false,
  "lockdown-reason": "Menu is temporarily closed for updates.",
  "bring-room": "",
  "bring-targets": [],
  "global-notify": "",
  "admins": [
    {
      "Incharilla": "C7887DEBBCC18F92"
    }
  ],
  "blacklisted-ids": [],
  "disabled-mods": []
}
```

| Field | Purpose |
| --- | --- |
| `status`, `lockdown`, `lockdown-reason` | Menu availability |
| `user-count` | Active users |
| `admins` | IDs allowed to use global admin commands |
| `blacklisted-ids` | Blocked user IDs |
| `disabled-mods` | Disabled mods |
| `bring-room`, `bring-targets` | Pull commands |
| `global-notify` | Global message sent to active users |
| `discord-invite` | Discord link |