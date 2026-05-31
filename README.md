# PVZ 融合版 3.6.1 一键修改器

游戏内实现 **极速冷却** 和 **阳光倍率**，不改变数值上限，只加速获取速度，避免游戏异常。

## 环境准备

### 1. 安装 uv

`uv` 是一个快速的 Python 包管理器，如果未安装，在 PowerShell 中运行：

```powershell
powershell -ExecutionPolicy ByPass -c "irm https://astral.sh/uv/install.ps1 | iex"
```

安装完成后，建议**重启终端**，然后验证：

```bash
uv --version
```

### 2. 配置国内镜像（推荐）

如果下载依赖速度慢，配置清华镜像：

```bash
# 配置 pip 镜像
uv pip config set global.index-url https://pypi.tuna.tsinghua.edu.cn/simple
```

### 3. 安装 Python

`uv` 会自动管理 Python 版本，首次运行脚本时会自动下载。如需提前安装：

```bash
uv python install 3.12
```

### 4. 手动安装依赖（备选）

如果 `uv run` 自动安装失败，或你想使用系统自带的 Python，可以手动安装依赖：

```bash
# 使用 uv
uv pip install pymem

# 或使用系统 pip
pip install pymem -i https://pypi.tuna.tsinghua.edu.cn/simple
```

安装完成后，直接用 `python pvz_fusion_cheats.py` 运行即可（不再需要 `uv run`）。

## 使用方法

### 第一步：进入游戏关卡

启动 **植物大战僵尸融合版 3.6.1**，并**进入任意关卡**（主菜单无法使用）。

### 第二步：运行脚本

将 `pvz_fusion_cheats.py` 放在任意文件夹，在该文件夹打开终端，执行：

```bash
uv run python pvz_fusion_cheats.py
```

首次运行会自动安装依赖，稍等片刻即可。

### 第三步：享受效果

正常输出应显示：

```
[+] 已附加游戏
[+] 冷却加速 ×100 已激活
[+] 阳光倍率 ×100 已激活

[*] 修改器运行中... 按 Enter 键停止并恢复
```

此时：
- **种植物**：卡片冷却瞬间完成
- **拾取阳光**：1 个阳光 = 100 阳光

### 停止修改

直接**按 Enter 键**，脚本会自动恢复并退出。

## 常见问题

**Q：提示"游戏未运行"**

确保已启动游戏并进入了关卡，主菜单状态下无法识别。

**Q：脚本闪退或报错**

- 检查 uv 是否安装成功：`uv --version`
- **不要**以**管理员身份**运行终端（部分系统需要管理员权限读写游戏内存，但本脚本不需要）
- 确认游戏版本是 **融合版3.6.1**

**Q：可以改倍率吗？**

可以。用文本编辑器打开 `pvz_fusion_cheats.py`：
- 搜索 `100.0`（带小数点），改成你想要的倍数（如 `10.0`、`50.0`）
- 搜索 `, 100)`，改成你想要的整数倍数（如 `, 10)`、`50)`）

## 注意事项

- 本工具仅修改内存，**不会修改游戏文件**，退出后完全恢复
- 建议备份存档后再使用
- 仅供个人学习研究
