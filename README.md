# 植物大战僵尸融合版 3.8.1 修改器

[English](./README_EN.md) | [简体中文](./README.md)

7 个功能：极速冷却、阳光越花越多、任意种植与重叠、植物无敌、僵尸一击必杀、特定植物加速、游戏速率调节。

## 下载

请只从本仓库 [GitHub Releases](https://github.com/blycr/PlantsVsZombiesRH_tool/releases) 下载。付费渠道或网盘二次打包均不可信。

Release 中通常包含：

* `pvz_fusion_cheats_wpf.exe` — 图形界面（需 .NET 10）
* `pvz_fusion_cheats_cs.exe` — 控制台（需 .NET 10）
* `pvz_fusion_cheats.ct` — Cheat Engine 表
* `pvz_fusion_cheats_v9.py` — Python 源码
* `SHA256SUMS` — 文件校验清单（可选对照）

---

## 用法

进关卡后再运行修改器效果最完整。

### A. 图形界面（推荐）

1. 安装 [.NET 10 运行库](https://dotnet.microsoft.com/download)（若尚未安装）。
2. 运行 `pvz_fusion_cheats_wpf.exe`。
3. 用开关打开/关闭功能，用滑块调节游戏速度。

### B. 控制台

1. 安装 .NET 10 运行库。
2. 运行 `pvz_fusion_cheats_cs.exe`。
3. 按菜单提示输入数字开关功能。

### C. Cheat Engine

1. 安装 [Cheat Engine](https://www.cheatengine.org/)。
2. 打开 `pvz_fusion_cheats.ct`，附加进程 `PlantsVsZombiesRH.exe`。
3. 勾选需要的功能。

### D. Python 源码

1. 安装 Python 3.12（可用 [uv](https://github.com/astral-sh/uv)）。
2. 进关卡后，在含有脚本的目录执行：

```text
python pvz_fusion_cheats_v9.py
```

---

## 功能一览

| 键 | 功能 | 说明 |
| :---: | :--- | :--- |
| 1 | 极速冷却 | 卡牌、手套、锤子 CD 瞬间完成 |
| 2 | 阳光越花越多 | 拾取或消耗阳光时按 100 倍增加 |
| 3 | 任意种植与重叠 | 解除地形限制，兼容植物可融合 |
| 4 | 植物无敌 | 免疫啃食与环境秒杀；铲除、自爆仍有效 |
| 5 | 僵尸一击必杀 | 僵尸受到任意伤害即死 |
| 6 | 特定植物加速 | 大嘴花咀嚼、地雷系列准备大幅加快 |
| 7 | 游戏速率 | 0.1x–10.0x；过关后仍保持 |

---

## 注意

* 只改内存，不改存档；退出修改器后游戏恢复正常。
* 仅适配《植物大战僵尸融合版 3.8.1》。
* 本工具完全免费；若付费获得，说明被骗。
* 仅供单机娱乐与学习，使用风险自负。
