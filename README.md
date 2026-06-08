# 植物大战僵尸融合版 3.7 修改器与辅助工具

[English](./README_EN.md) | [简体中文](./README.md)

7 个功能：极速冷却、阳光越花越多、任意种植与重叠、植物无敌、僵尸一击必杀、特定植物加速、游戏速率调节。

## 下载最新版

去 [GitHub Releases](https://github.com/blycr/PlantsVsZombiesRH_tool/releases) 下载。

---

## 运行方式

选一种就行，不用额外配置。

### 方式 A（推荐）：图形界面修改器 (WPF GUI)
* **适合**：普通玩家，喜欢鼠标操作 and 滑块调速度。
* **下载**：Release 页面下载 [pvz_fusion_cheats_wpf.exe](./pvz_fusion_cheats_wpf.exe)。
* **环境**：需要 .NET 10 运行库。
* **用法**：进关卡后运行，勾选开关或拖滑块。自动附加游戏进程，不用提权。

### 方式 B：控制台快捷修改器 (C# Console)
* **适合**：喜欢用命令行但不想装 Python 的玩家。
* **下载**：Release 页面下载 [pvz_fusion_cheats_cs.exe](./pvz_fusion_cheats_cs.exe)。
* **环境**：需要 .NET 10 运行库。
* **用法**：进关卡后运行，按菜单提示输数字开关功能。

### 方式 C：Cheat Engine 辅助修改 (CE Table)
* **适合**：熟悉 Cheat Engine 的玩家。
* **下载**：Release 页面下载 [pvz_fusion_cheats.ct](./pvz_fusion_cheats.ct)。
* **环境**：需安装 [Cheat Engine](https://www.cheatengine.org/)。
* **用法**：进关卡后双击 `.ct` 文件，在 CE 里附加 `PlantsVsZombiesRH.exe`，勾选功能激活。

### 方式 D：Python 源码运行（开发者模式）
* **适合**：开发者，想改逻辑或看完整实现。
* **源文件**：项目里的 [pvz_fusion_cheats_v8.py](./pvz_fusion_cheats_v8.py)。
* **步骤**：
  1. 装 uv 包管理器（PowerShell）：`powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"`
  2. 装 Python 3.12：`uv python install 3.12`
  3. 进关卡后，在项目目录执行：`.venv\Scripts\python pvz_fusion_cheats_v8.py`

---

## 功能说明

| 快捷键 | 功能 | 效果 |
| :---: | :--- | :--- |
| 1 | 极速冷却 | 卡牌、手套、锤子 CD 瞬间冷却完毕，可无限连种。 |
| 2 | 阳光越花越多 | 消耗或拾取阳光时，数值以 100 倍增加。 |
| 3 | 任意种植与重叠 | 解除地形限制（水上、屋顶直接种），兼容植物自动融合。 |
| 4 | 植物无敌 | 免疫啃食和环境秒杀（不影响铲除和爆炸植物自毁）。 |
| 5 | 僵尸一击必杀 | 僵尸受到任何伤害立即死亡。 |
| 6 | 特定植物加速 | 大嘴花咀嚼与所有地雷系列（含基础与全部融合分支）准备时间大幅缩短。 |
| 7 | 整体速率调节 | 0.1x 到 10.0x 无级调速。 |

---

## 注意事项

* **纯内存修改**：只改内存，不动存档和本地文件，退出后游戏恢复正常。
* **版本绑定**：所有内存特征码和偏移量只适配《植物大战僵尸融合版 3.7》，其他版本不保证可用。

---

## 开源协议与免责声明

* **完全免费**：本修改器（包括所有编译生成的 `.exe` 文件、`.ct` 脚本、Python 源码）**完全免费**。如果您是通过付费渠道购买的，说明您已被骗，请立即申请退款。
* **非商业使用**：本项目采用自定义**非商业用途、教育学习与防骗禁售许可证**，严禁将本软件或任何衍生作品用于商业买卖、收费服务或有偿打包。
* **免责声明**：本工具仅供单机娱乐和技术学习交流，使用本工具产生的一切后果（包括但不限于游戏崩溃、数据丢失、封号等）由使用者自行承担，作者不承担任何责任。
