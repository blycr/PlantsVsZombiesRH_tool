# Plants vs. Zombies Fusion Edition 4.0.5 Trainer v10

[English](./README_EN.md) | [简体中文](./README.md)

7 features: Instant Cooldown, Multiplying Sun, Free Planting & Overlap, Invincible Plants, One-Hit Kill Zombies, Specific Plant Speedup, and Game Speed Control.

## Download

Download only from this repo’s [GitHub Releases](https://github.com/blycr/PlantsVsZombiesRH_tool/releases). Paid mirrors and repacked archives are not trusted.

Current release: [v10 · game 4.0.5](https://github.com/blycr/PlantsVsZombiesRH_tool/releases/tag/v10). See the [changelog](./CHANGELOG.md) for changes.

The release includes:

* `pvz_fusion_cheats_wpf.exe` — GUI (requires .NET Desktop Runtime 10, x64)
* `pvz_fusion_cheats_cs.exe` — Console (requires .NET Runtime 10, x64)
* `pvz_fusion_cheats.ct` — Cheat Engine table
* `pvz_fusion_cheats_v10.py` — Python source
* `SHA256SUMS` — SHA256 checksums for the four assets

In PowerShell, run `Get-FileHash .\pvz_fusion_cheats_wpf.exe -Algorithm SHA256` and compare the result with the matching filename in `SHA256SUMS`. Verify the other assets the same way.

---

## Usage

For best results, start the trainer after you enter a level.

### A. GUI (recommended)

1. Install [.NET Desktop Runtime 10 (Windows x64)](https://dotnet.microsoft.com/download/dotnet/10.0) if needed.
2. Run `pvz_fusion_cheats_wpf.exe`.
3. Use the toggles for features and the slider for game speed.

### B. Console

1. Install [.NET Runtime 10 (Windows x64)](https://dotnet.microsoft.com/download/dotnet/10.0). Desktop Runtime also includes the required runtime.
2. Run `pvz_fusion_cheats_cs.exe`.
3. Press the menu numbers to toggle features.

### C. Cheat Engine

1. Install [Cheat Engine](https://www.cheatengine.org/).
2. Open `pvz_fusion_cheats.ct` and attach to `PlantsVsZombiesRH.exe`.
3. Check the features you want.

### D. Python source

1. Download `pvz_fusion_cheats_v10.py` from the Release page.
2. Install Python 3.12 (e.g. via [uv](https://github.com/astral-sh/uv)) and run `python -m pip install pymem`. If the dependency is missing, the script attempts to install it over the network.
3. After entering a level, in the folder that contains the script, run:

```text
python pvz_fusion_cheats_v10.py
```

---

## Features

| Key | Feature | Effect |
| :---: | :--- | :--- |
| 1 | Instant Cooldown | Seed packets, glove, and hammer cool down instantly |
| 2 | Multiplying Sun | Picking up or spending sun increases it by 100x |
| 3 | Free Planting & Overlap | Plant anywhere; compatible plants still fuse |
| 4 | Invincible Plants | Immune to chewing and environmental kills; shovel/self-destruct still work |
| 5 | One-Hit Kill | Any damage kills zombies |
| 6 | Plant Speedup | Faster Chomper chew and mine arming |
| 7 | Game Speed | 0.1x–10.0x; keeps the rate across levels |

GUI “Enable All” and console `A` enable the first six features; set game speed separately. Console `R` restores patches, `Q` restores and exits, and `L` switches language. The GUI supports Chinese/English and reconnects after the game restarts.

---

## Notes

* Memory-only; save files are not modified. C# / Python attempt to restore patches on normal exit. Uncheck CE entries before closing CE. Restart the game to recover after forcibly terminating a trainer.
* v10 supports **PVZ Fusion 4.0.5, Windows x64**. C# / Python verify the game build before attaching; the CE table checks original instructions at patch sites.
* Supported `GameAssembly.dll` SHA256: `48096b917ee6aaf6e35c95c98e666a4399a9e0c3d5adbab727cee941eccf2d42`.
* Best used after you enter a level.
* Release builds, isolated memory patch/restore tests, 72 x64 hook execution scenarios, and CE instruction/branch checks passed. Gameplay effects, level transitions, and execution inside CE have not been tested in-game.

## Troubleshooting

* **Unsupported build**: the same game version can have different builds. Run `Get-FileHash -Algorithm SHA256` on the game's `GameAssembly.dll` and compare it with the supported hash above. A different build requires separate adaptation.
* **Missing runtime**: the GUI requires Desktop Runtime, specifically x64. The regular .NET Runtime alone cannot run WPF.
* **Cannot attach or enable a feature**: enter a level and use one trainer at a time. If the game runs as Administrator, the trainer needs the same privileges. Restart the game if another tool has already modified a patch site.

See [BUILDING.md](./BUILDING.md) for source builds and verification. The repository contains C# source; Python, the CE table, executables, and checksums are distributed through Releases.

---

## License & Disclaimer

* **100% Free**: This trainer (including all `.exe` binaries, `.ct` files, and Python source) is **completely free**. If you paid to obtain it, you have been scammed — request a refund.
* **Non-Commercial Use**: Released under a custom **Non-Commercial, Educational Use Only and Anti-Scam License**. See [LICENSE](./LICENSE). Commercial sale, paid bundling, or paid redistribution is prohibited.
* **Disclaimer**: For offline entertainment and technical learning only. You use this tool at your own risk. The author is not liable for crashes, data loss, bans, or any other consequences.
