# 植物大战僵尸融合版 4.0.5 修改器 v10

[English](./README_EN.md) | [简体中文](./README.md)

7 个功能：极速冷却、阳光越花越多、任意种植与重叠、植物无敌、僵尸一击必杀、特定植物加速、游戏速率调节。

## 下载

请只从本仓库 [GitHub Releases](https://github.com/blycr/PlantsVsZombiesRH_tool/releases) 下载。付费渠道或网盘二次打包均不可信。

当前版本：[v10 · 游戏 4.0.5](https://github.com/blycr/PlantsVsZombiesRH_tool/releases/tag/v10)。更新内容见 [更新日志](./CHANGELOG.md)。

Release 包含：

* `pvz_fusion_cheats_wpf.exe` — 图形界面（需 .NET Desktop Runtime 10，x64）
* `pvz_fusion_cheats_cs.exe` — 控制台（需 .NET Runtime 10，x64）
* `pvz_fusion_cheats.ct` — Cheat Engine 表
* `pvz_fusion_cheats_v10.py` — Python 源码
* `SHA256SUMS` — 四个附件的 SHA256 校验清单

下载后可在 PowerShell 中执行 `Get-FileHash .\pvz_fusion_cheats_wpf.exe -Algorithm SHA256`，将结果与 `SHA256SUMS` 中同名文件的一行比较；其他附件同样校验。

---

## 用法

进关卡后再运行修改器效果最完整。

### A. 图形界面（推荐）

1. 安装 [.NET Desktop Runtime 10（Windows x64）](https://dotnet.microsoft.com/download/dotnet/10.0)（若尚未安装）。
2. 运行 `pvz_fusion_cheats_wpf.exe`。
3. 用开关打开/关闭功能，用滑块调节游戏速度。

### B. 控制台

1. 安装 [.NET Runtime 10（Windows x64）](https://dotnet.microsoft.com/download/dotnet/10.0)。Desktop Runtime 也包含所需运行库。
2. 运行 `pvz_fusion_cheats_cs.exe`。
3. 按菜单提示输入数字开关功能。

### C. Cheat Engine

1. 安装 [Cheat Engine](https://www.cheatengine.org/)。
2. 打开 `pvz_fusion_cheats.ct`，附加进程 `PlantsVsZombiesRH.exe`。
3. 勾选需要的功能。

### D. Python 源码

1. 从 Release 下载 `pvz_fusion_cheats_v10.py`。
2. 安装 Python 3.12（可用 [uv](https://github.com/astral-sh/uv)），执行 `python -m pip install pymem` 安装依赖。缺少依赖时脚本会尝试联网安装。
3. 进关卡后，在脚本所在目录执行：

```text
python pvz_fusion_cheats_v10.py
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

GUI 的“一键开启所有”和控制台的 `A` 开启前六项，游戏速率单独设置。控制台 `R` 还原、`Q` 还原并退出、`L` 切换语言；GUI 支持中英文切换和游戏重启后重新附加。

---

## 注意

* 只改内存，不改存档；C# / Python 正常关闭时尝试还原补丁。CE 请先取消勾选功能再关闭。强制结束修改器后可重启游戏恢复。
* v10 仅适配《植物大战僵尸融合版 4.0.5》Windows x64。C# / Python 会校验本体构建，不匹配时拒绝附加；CE 表会校验补丁位置的原始指令。
* 当前适配的 `GameAssembly.dll` SHA256：`48096b917ee6aaf6e35c95c98e666a4399a9e0c3d5adbab727cee941eccf2d42`。
* 建议进关卡后再运行。
* 已完成编译、隔离内存开关与还原测试、72 项汇编执行验证，以及 CE 指令和跳转校验；实际关卡效果、过关场景与 CE 内执行尚未实测。

## 常见问题

* **提示版本不匹配**：相同的游戏版本号也可能对应不同构建。对游戏目录的 `GameAssembly.dll` 执行 `Get-FileHash -Algorithm SHA256`，与上方适配值比较；不匹配的构建需要另行适配。
* **提示缺少运行库**：GUI 需要 Desktop Runtime，且必须安装 x64 版本；仅安装普通 .NET Runtime 不足以运行 WPF 界面。
* **无法附加或功能开启失败**：确认游戏已进入关卡，并只使用一个修改器。若游戏以管理员身份运行，修改器也需要同等权限。其他工具已改过补丁位置时，请重启游戏再试。

源码构建与测试说明见 [BUILDING.md](./BUILDING.md)。仓库保存 C# 源码；Python、CE 表、可执行文件及校验清单通过 Release 分发。

---

## 开源协议与免责声明

* **完全免费**：本修改器（包括所有 `.exe`、`.ct`、Python 源码）**完全免费**。若通过付费渠道购买，说明您已被骗，请立即申请退款。
* **非商业使用**：本项目采用自定义**非商业用途、教育学习与防骗禁售许可证**，详见 [LICENSE](./LICENSE)。严禁将本软件或任何衍生作品用于商业买卖、收费服务或有偿打包。
* **免责声明**：本工具仅供单机娱乐和技术学习交流。使用本工具产生的一切后果（包括但不限于游戏崩溃、数据丢失、封号等）由使用者自行承担，作者不承担任何责任。
