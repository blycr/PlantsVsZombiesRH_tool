# Plants vs. Zombies Fusion Edition 3.8.1 Trainer & Helper Tool

[English](./README_EN.md) | [简体中文](./README.md)

7 features: Instant Cooldown, Multiplying Sun, Free Planting & Overlap, Invincible Plants, One-Hit Kill Zombies, Specific Plant Speedup, and Global Game Speed Adjustment.

## Download Latest Version

Get binaries and `SHA256SUMS` from the [GitHub Releases page](https://github.com/blycr/PlantsVsZombiesRH_tool/releases).

This repository mainly ships: **usage docs**, **current source code**, and **checksum scripts**. Built `.exe` files are distributed via Releases only, not stored in git history.

---

## How to Run

Pick one. No extra setup needed.

### Method A (Recommended): GUI Trainer (WPF GUI)
* **For**: Regular players who prefer mouse controls and a speed slider.
* **Download**: `pvz_fusion_cheats_wpf.exe` from the Release page.
* **Requires**: .NET 10 Runtime.
* **Usage**: Run after entering a level. Check/uncheck features or drag the speed slider. Auto-attaches to the game process, no elevation needed.
* **Source**: `pvz_fusion_cheats_wpf/` in this repo.

### Method B: Console Trainer (C# Console)
* **For**: Players who like the command line but don't want to install Python.
* **Download**: `pvz_fusion_cheats_cs.exe` from the Release page.
* **Requires**: .NET 10 Runtime.
* **Usage**: Run after entering a level. Press the number keys shown in the menu to toggle features.
* **Source**: `pvz_fusion_cheats_cs/` in this repo.

### Method C: Cheat Engine Cheat Table (CE Table)
* **For**: Players familiar with Cheat Engine.
* **Download**: `pvz_fusion_cheats.ct` from the Release page (same file is also in the repo).
* **Requires**: [Cheat Engine](https://www.cheatengine.org/) installed.
* **Usage**: After entering a level, double-click the `.ct` file, attach to `PlantsVsZombiesRH.exe` in CE, and check the features to activate them.

### Method D: Python Source Execution (Developer Mode)
* **For**: Developers who want to modify logic or see the full implementation.
* **Source**: [pvz_fusion_cheats_v9.py](./pvz_fusion_cheats_v9.py) in the repo (also attached on Releases).
* **Steps**:
  1. Install uv package manager (PowerShell): `powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"`
  2. Install Python 3.12: `uv python install 3.12`
  3. Enter a level, then run from the project root: `.venv\Scripts\python pvz_fusion_cheats_v9.py`

---

## Features

| Key | Feature | In-Game Effect |
| :---: | :--- | :--- |
| 1 | Instant Cooldown | Seed packets, shovel, glove, and hammer cool down instantly. Infinite planting. |
| 2 | Multiplying Sun | Picking up or consuming sun increases the count by 100x instead of decreasing. |
| 3 | Free Planting & Overlap | Removes terrain restrictions (plant on water/roof directly). Compatible plants still fuse automatically. |
| 4 | Invincible Plants | Immune to chewing and environmental instant kills. Shovel and explosive self-destruction still work. |
| 5 | One-Hit Kill Zombies | Zombies die immediately upon taking any damage. |
| 6 | Specific Plant Speedup | Chomper chewing and all Potato Mines (including base and fusion variants) arm much faster. |
| 7 | Game Speed Controller | Smooth adjustment from 0.1x to 10.0x. The chosen speed persists across levels. |

---

## Notes

* **Pure Memory Trainer**: Only touches game memory. Does not modify save files or local data. Game returns to normal once the trainer exits.
* **Version Locked**: All memory offsets and signatures are tailored for "Plants vs. Zombies Fusion Edition 3.8.1". Other versions may not work.
* **Best used in a level**: Attaching may succeed on the main menu, but most effects only show up after a level starts.
* **Trust only official Releases**: Download only from this repo's [GitHub Releases](https://github.com/blycr/PlantsVsZombiesRH_tool/releases). Paid mirrors and repacked archives are not supported.

---

## Verify Downloads (anti-tamper)

Each formal Release includes a `SHA256SUMS` file. Check hashes after download so you can detect corruption or swapped files.

### Method 1: PowerShell (recommended)

In the folder that contains the release files and `SHA256SUMS`:

```powershell
Get-Content .\SHA256SUMS
Get-FileHash .\pvz_fusion_cheats_wpf.exe -Algorithm SHA256
Get-FileHash .\pvz_fusion_cheats_cs.exe -Algorithm SHA256
Get-FileHash .\pvz_fusion_cheats.ct -Algorithm SHA256
Get-FileHash .\pvz_fusion_cheats_v9.py -Algorithm SHA256
```

Hashes must match the corresponding lines in `SHA256SUMS` (case-insensitive).

If you cloned this repo, you can also batch-verify:

```powershell
pwsh .\scripts\verify-release.ps1 -Dir path\to\download\folder
```

### Method 2: Optional minisign signature

If the release also ships `SHA256SUMS.minisig` and the repo contains `minisign.pub`, you can verify that the checksum list itself was signed by the author (requires [minisign](https://jedisct1.github.io/minisign/)):

```powershell
minisign -V -p minisign.pub -m SHA256SUMS -x SHA256SUMS.minisig
```

Only after that, check each file against `SHA256SUMS`.

---

## License & Disclaimer

* **100% Free**: This trainer (including all compiled `.exe` binaries, `.ct` files, and Python source code) is **completely free**. If you paid any money to obtain this tool, you have been scammed.
* **Non-Commercial Use**: This project is licensed under a custom **Non-Commercial, Educational Use Only and Anti-Scam License**. Any commercial distribution, resale, or packaging is strictly prohibited.
* **Disclaimer**: This tool is provided solely for personal offline entertainment and educational research. The author holds no liability for any game crashes, data corruption, or any other issues resulting from its usage.
