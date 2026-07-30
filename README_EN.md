# Plants vs. Zombies Fusion Edition 3.8.1 Trainer & Helper Tool

[English](./README_EN.md) | [简体中文](./README.md)

7 features: Instant Cooldown, Multiplying Sun, Free Planting & Overlap, Invincible Plants, One-Hit Kill Zombies, Specific Plant Speedup, and Global Game Speed Adjustment.

## Download Latest Version

Get it from the [GitHub Releases page](https://github.com/blycr/PlantsVsZombiesRH_tool/releases).

---

## How to Run

Pick one. No extra setup needed.

### Method A (Recommended): GUI Trainer (WPF GUI)
* **For**: Regular players who prefer mouse controls and a speed slider.
* **Download**: Grab [pvz_fusion_cheats_wpf.exe](./pvz_fusion_cheats_wpf.exe) from the Release page.
* **Requires**: .NET 10 Runtime.
* **Usage**: Run after entering a level. Check/uncheck features or drag the speed slider. Auto-attaches to the game process, no elevation needed.

### Method B: Console Trainer (C# Console)
* **For**: Players who like the command line but don't want to install Python.
* **Download**: Grab [pvz_fusion_cheats_cs.exe](./pvz_fusion_cheats_cs.exe) from the Release page.
* **Requires**: .NET 10 Runtime.
* **Usage**: Run after entering a level. Press the number keys shown in the menu to toggle features.

### Method C: Cheat Engine Cheat Table (CE Table)
* **For**: Players familiar with Cheat Engine.
* **Download**: Grab [pvz_fusion_cheats.ct](./pvz_fusion_cheats.ct) from the Release page.
* **Requires**: [Cheat Engine](https://www.cheatengine.org/) installed.
* **Usage**: After entering a level, double-click the `.ct` file, attach to `PlantsVsZombiesRH.exe` in CE, and check the features to activate them.

### Method D: Python Source Execution (Developer Mode)
* **For**: Developers who want to modify logic or see the full implementation.
* **Source**: [pvz_fusion_cheats_v9.py](./pvz_fusion_cheats_v9.py) in the project.
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

---

## License & Disclaimer

* **100% Free**: This trainer (including all compiled `.exe` binaries, `.ct` files, and Python source code) is **completely free**. If you paid any money to obtain this tool, you have been scammed.
* **Non-Commercial Use**: This project is licensed under a custom **Non-Commercial, Educational Use Only and Anti-Scam License**. Any commercial distribution, resale, or packaging is strictly prohibited.
* **Disclaimer**: This tool is provided solely for personal offline entertainment and educational research. The author holds no liability for any game crashes, data corruption, or any other issues resulting from its usage.
