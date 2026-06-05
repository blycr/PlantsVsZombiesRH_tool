# PVZ 融合版 3.6.1 修改器与辅助工具包 (v7)

本工具包专为 **《植物大战僵尸融合版 3.6.1》** 打造，提供游戏内动态开关极速冷却、阳光越花越多、任意种植与重叠、植物无敌、僵尸一击必杀、特定植物状态加速以及游戏整体速率调节等 7 大核心修改功能。全功能随开随关，安全稳定，且支持多种运行方式以满足不同用户的需求。

---

## 🚀 快速选择使用方式

为了方便不同需求的用户，我们提供了以下四种使用方式，您可以直接选择最适合您的一种使用（无需配置其他方式）：

| 运行选项 | 界面形式 | 推荐对象 | 环境依赖 | 对应文件/文件夹 |
| :--- | :--- | :--- | :--- | :--- |
| **方式 A（推荐）** | **图形界面修改器 (WPF GUI)** | 普通玩家，喜欢直观鼠标操作 | ❌ 免配置，直接运行（需 .NET 10） | [pvz_fusion_cheats_wpf.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_wpf.exe) |
| **方式 B** | **控制台快捷修改器 (C# Console)** | 喜欢终端交互、不想装 Python 的玩家 | ❌ 免配置，直接运行（需 .NET 10） | [pvz_fusion_cheats_cs.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_cs.exe) |
| **方式 C** | **Cheat Engine 表格 (CE Table)** | 喜欢使用 CE、想自定义修改或研究内存的玩家 | ⚠️ 需提前安装 [Cheat Engine](https://www.cheatengine.org/) | [pvz_fusion_cheats.ct](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats.ct) |
| **方式 D** | **Python 源码运行 (Developer)** | 开发者，需要修改逻辑或查看完整实现 | ⚙️ 需安装 Python 3.12 运行环境 | [pvz_fusion_cheats_v7.py](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_v7.py) |

---

## 🛠️ 各方式详细使用指南

### 方式 A：图形界面修改器 (WPF GUI)
这是对大部分玩家最友好、操作最简单的方案。
1. **启动游戏**：双击运行 [PlantsVsZombiesRH.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/PlantsVsZombiesRH.exe) 并进入任意关卡。
2. **运行修改器**：直接双击运行根目录下的 [pvz_fusion_cheats_wpf.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_wpf.exe)。
3. **功能控制**：在弹出的精致图形界面中，直接通过**勾选框**开启/关闭对应的功能，使用**滑动条**或输入数值来无级调节游戏运行速率。
> **💡 权限说明**：修改器配备了智能权限自适应机制。如果游戏以普通权限启动，修改器也无需管理员权限，双击直接运行即可，免除手动提权烦恼。

---

### 方式 B：控制台修改器 (C# Console)
适合喜欢命令行交互但不想安装 Python 环境的用户。
1. **启动游戏**：进入任意游戏关卡。
2. **运行修改器**：直接双击运行根目录下的 [pvz_fusion_cheats_cs.exe](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats_cs.exe)。
3. **功能控制**：根据命令行窗口中显示的菜单，输入对应的数字（`1`~`7`）或字母（`A` 一键开启，`R` 还原重置，`Q` 还原并退出）并按回车即可。

---

### 方式 C：Cheat Engine 表格 (CE Table)
如果您是 Cheat Engine 的忠实用户，可以使用已封装好的 `.ct` 文件。
1. **安装软件**：确保系统中已安装 [Cheat Engine](https://www.cheatengine.org/) (推荐 7.4 或更高版本)。
2. **启动游戏**：进入游戏关卡。
3. **打开表格**：双击根目录下的 [pvz_fusion_cheats.ct](file:///c:/Users/blycr/Desktop/sth/PC_PVZ-Fusion-3.6.1/pvz_fusion_cheats.ct)。
4. **附加进程**：在 Cheat Engine 中点击左上角绿色的电脑图标，在进程列表中选中 `PlantsVsZombiesRH.exe` 并点击 Open（附加）。
5. **激活功能**：在 CE 底部的地址列表中，勾选对应功能左侧的激活方块。

---

### 方式 D：Python 源码运行（开发者模式）
如果您想深入研究修改器的内存 Hook 机制、跳转溢出处理或重新编写汇编 Shellcode，可以使用此方式。
#### 1. 环境准备
* **步骤一**：安装极速 Python 包管理器 `uv`（推荐）：
  ```powershell
  powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
  ```
  安装完成后重启终端，验证安装：
  ```bash
  uv --version
  ```
* **步骤二**：通过 `uv` 安装 Python 3.12：
  ```bash
  uv python install 3.12
  ```

#### 2. 运行修改器
1. 进入游戏关卡。
2. 在项目根目录下打开命令行终端（如 PowerShell），运行：
   ```bash
   .venv\Scripts\python pvz_fusion_cheats_v7.py
   ```
3. 在命令行交互菜单中输入指令控制。

---

## 📋 功能明细说明

| 快捷键 | 功能名称 | 详细效果与游戏表现 |
| :---: | :--- | :--- |
| `1` | **极速冷却 ×100** | 所有卡牌、手套、锤子的 CD 瞬间冷却完毕，可无限连种。 |
| `2` | **阳光越花越多** | 捡起阳光或种植消耗阳光时，阳光值不仅不会减少，还会以 100 倍的数值增加。 |
| `3` | **任意种植与重叠** | 解除地形限制（如无睡莲直接种在水上、无花盆种在屋顶）。兼容植物正常融合，不兼容植物在同一格内物理重叠各自独立运行。 |
| `4` | **植物无敌** | 植物免疫常规啃食、落石碾压、冰车碾压和落水死亡，不影响玩家主动铲除以及爆炸植物（如樱桃炸弹）的正常自毁。 |
| `5` | **僵尸一击必杀** | 任意僵尸在受到任何微小伤害时都会瞬间死亡。 |
| `6` | **特定植物加速** | 大嘴花咀嚼时间缩短至 1/20，土豆地雷准备时间缩短至 1/20，保留正常的吞噬和上升动画。 |
| `7` | **游戏整体速率调节** | 支持 0.1x 到 10.0x 游戏引擎倍速的连续调节（默认为 1.0x 正常速度）。 |

---

## ⚠️ 注意事项

- **运行库说明**：若运行 `*.exe` 方式时系统提示缺少 .NET 运行库，说明您的 Windows 系统未安装 `.NET 10` 环境。您可以直接在系统弹出的提示中下载安装，或选择**方式 C (CE 表格)** 或 **方式 D (Python 源码)** 运行。
- **纯内存操作**：本工具包所有修改方案均为纯内存操作，不修改游戏本身的存档或文件，关闭修改器后游戏即刻恢复原状，安全无后顾之忧。
- **版本绑定**：所有内存特征码与硬编码偏移均针对 **PVZ 融合版 3.6.1** 进行了适配与校验，其他版本不保证能正常工作。
