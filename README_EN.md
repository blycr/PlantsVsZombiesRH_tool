# Plants vs. Zombies Fusion Edition 3.6.1 Trainer & Helper Tool

[English](./README_EN.md) | [简体中文](./README.md)

Provides 7 core trainer features including Instant Cooldown, Multiplying Sun, Free Planting & Overlap, Invincible Plants, One-Hit Kill Zombies, Specific Plant Status Speedup, and Global Game Speed Adjustment.

## Download Latest Version

Please go to the [GitHub Releases page](https://github.com/blycr/PlantsVsZombiesRH_tool/releases) to download the latest trainer release.

---

## Runner Selection

You can choose any of the following methods to run the trainer based on your preference; no other setups are needed:

### Method A (Recommended): GUI Trainer (WPF GUI)
* **Target Users**: Regular players who prefer a visual mouse interface and a speed slider.
* **Download File**: Download [pvz_fusion_cheats_wpf.exe](./pvz_fusion_cheats_wpf.exe) from the Release page.
* **Environment Requirement**: Needs .NET 10 Runtime installed.
* **Usage**: Run the program after entering a game level, then check/uncheck the features or drag the speed slider. The trainer supports auto-attaching to the game process with current privileges (no elevation required).

### Method B: Console Trainer (C# Console)
* **Target Users**: Players who like command-line interactions but don't want to install Python.
* **Download File**: Download [pvz_fusion_cheats_cs.exe](./pvz_fusion_cheats_cs.exe) from the Release page.
* **Environment Requirement**: Needs .NET 10 Runtime installed.
* **Usage**: Run the program after entering a game level, and follow the console menu prompts to toggle cheats by pressing the corresponding numbers.

### Method C: Cheat Engine Cheat Table (CE Table)
* **Target Users**: Players familiar with Cheat Engine debugging and custom modifications.
* **Download File**: Download [pvz_fusion_cheats.ct](./pvz_fusion_cheats.ct) from the Release page.
* **Environment Requirement**: Needs [Cheat Engine](https://www.cheatengine.org/) installed beforehand.
* **Usage**: After launching the game and entering a level, double-click the `.ct` file, attach to the `PlantsVsZombiesRH.exe` process in CE, and check the features in the table list below to activate them.

### Method D: Python Source Execution (Developer Mode)
* **Target Users**: Developers who want to modify logic or check full source implementation.
* **Source File**: [pvz_fusion_cheats_v7.py](./pvz_fusion_cheats_v7.py) in the project.
* **Steps**:
  1. Install uv package manager (run in PowerShell): `powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"`
  2. Install Python 3.12: `uv python install 3.12`
  3. Enter a game level, then run from the project root: `.venv\Scripts\python pvz_fusion_cheats_v7.py`

---

## Features

| Key | Feature Name | In-Game Behavior |
| :---: | :--- | :--- |
| 1 | Instant Cooldown x100 | Seed packets, shovel, glove, and hammer cool down instantly, allowing infinite planting. |
| 2 | Multiplying Sun | When picking up or consuming sun, the sun count increases by 100x instead of decreasing/normal adding. |
| 3 | Free Planting & Overlap | Removes terrain planting constraints (can plant directly on water/roof) while keeping compatible plant automatic fusion logic. |
| 4 | Invincible Plants | Plants are immune to chewing and environmental instant kills (does not affect shovel-up and explosive plant self-destruction). |
| 5 | One-Hit Kill Zombies | Zombies die immediately upon taking any damage. |
| 6 | Specific Plant Speedup | Reduces Chomper chewing and Potato Mine arming time to 1/20 while retaining animations. |
| 7 | Game Speed Controller | Supports smooth global game speed adjustment from 0.1x to 10.0x. |

---

## Notes

* **Pure Memory Trainer**: This trainer operates solely on game memory. It does not modify game save files or local files. The game restores to normal once the trainer exits.
* **Version Bound**: All memory offsets and signatures are tailored specifically for "Plants vs. Zombies Fusion Edition 3.6.1". No guarantee is provided for other versions.
