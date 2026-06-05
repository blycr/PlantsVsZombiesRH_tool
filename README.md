# 植物大战僵尸融合版 3.6.1 修改器与辅助工具

[English](./README_EN.md) | [简体中文](./README.md)

提供游戏内动态开关极速冷却、阳光越花越多、任意种植与重叠、植物无敌、僵尸一击必杀、特定植物状态加速以及游戏整体速率调节等 7 大核心修改功能。

## 下载最新版

请前往 [GitHub Releases 页面](https://github.com/blycr/PlantsVsZombiesRH_tool/releases) 下载最新发布的修改器。

---

## 运行方式选择

你可以根据个人需要选择以下任意一种方式运行，无需配置其他形式：

### 方式 A（推荐）：图形界面修改器 (WPF GUI)
* **适用人群**：普通玩家，喜欢直观鼠标操作和倍速调节滑块。
* **下载文件**：在 Release 页面下载 [pvz_fusion_cheats_wpf.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_wpf.exe)。
* **依赖环境**：需要安装 .NET 10 运行库。
* **使用方法**：进入游戏关卡后，直接运行该程序，在窗口中勾选开关或拖动速率滑块。修改器支持自动以当前用户权限附加游戏进程，免提权。

### 方式 B：控制台快捷修改器 (C# Console)
* **适用人群**：喜欢终端命令行交互但不想配置 Python 的玩家。
* **下载文件**：在 Release 页面下载 [pvz_fusion_cheats_cs.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_cs.exe)。
* **依赖环境**：需要安装 .NET 10 运行库。
* **使用方法**：进入游戏关卡后运行程序，根据控制台菜单提示输入对应按键数字控制开关。

### 方式 C：Cheat Engine 辅助修改 (CE Table)
* **适用人群**：熟悉 Cheat Engine 的调试与自定义修改玩家。
* **下载文件**：在 Release 页面下载 [pvz_fusion_cheats.ct](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats.ct)。
* **依赖环境**：需提前安装 [Cheat Engine](https://www.cheatengine.org/) 软件。
* **使用方法**：启动游戏进入关卡后，双击打开该 `.ct` 文件，在 CE 中附加 `PlantsVsZombiesRH.exe` 进程，勾选底部列表中的功能激活。

### 方式 D：Python 源码运行（开发者模式）
* **适用人群**：开发者，需要修改逻辑或查看完整实现。
* **源文件**：项目中的 [pvz_fusion_cheats_v7.py](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_v7.py) 脚本。
* **使用步骤**：
  1. 安装 uv 包管理器（PowerShell 中运行）：`powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"`
  2. 安装 Python 3.12：`uv python install 3.12`
  3. 进入游戏关卡后，在项目目录下执行：`.venv\Scripts\python pvz_fusion_cheats_v7.py`

---

## 功能说明

| 快捷键 | 功能名称 | 游戏内表现 |
| :---: | :--- | :--- |
| 1 | 极速冷却 ×100 | 卡牌、手套、锤子 CD 瞬间冷却完毕，可无限连种。 |
| 2 | 阳光越花越多 | 消耗阳光或拾取阳光时，数值均以 100 倍增加。 |
| 3 | 任意种植与重叠 | 解除地形限制（可水上、屋顶直接种植），且保留兼容植物自动融合逻辑。 |
| 4 | 植物无敌 | 免疫啃食与环境秒杀（不影响玩家铲除和爆炸植物自毁）。 |
| 5 | 僵尸一击必杀 | 僵尸受到任何伤害立即死亡。 |
| 6 | 特定植物加速 | 大嘴花咀嚼与土豆地雷准备时间缩短至 1/20，保留动画。 |
| 7 | 整体速率调节 | 支持 0.1x 到 10.0x 游戏运行速度的无级调节。 |

---

## 注意事项

* **纯内存修改**：修改器为纯内存操作，不修改游戏存档与本地文件，退出后游戏即恢复正常。
* **版本绑定**：所有内存特征码与偏移量仅适配 《植物大战僵尸融合版 3.6.1》 游戏版本，其他版本不保证可用。
