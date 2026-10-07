# 更新日志 / Changelog

## v10 — 2026-10-07 — PVZ Fusion 4.0.5

* 将七项功能同步适配到融合版 4.0.5 Windows x64，更新函数地址、原始指令和对象字段。
* 修正工具冷却、阳光字段和植物状态偏移；植物加速补丁改为使用新版 `xmm9` 路径，僵尸受伤入口按完整 7 字节指令生成跳转。
* C# / Python 附加前验证本体 SHA256；CE 表增加原始指令校验。GUI 在游戏退出后清除旧功能实例，重新附加时使用新地址。
* 发布 GUI、控制台、`pvz_fusion_cheats_v10.py`、CE 表及 `SHA256SUMS`；补充安装、校验、构建和测试说明。
* 验证：两个 C# 实现的隔离内存测试、Python 开关与还原测试、72 项 x64 汇编执行场景、CE 15 处指令校验和 Release 编译均通过。实际关卡、关卡切换和 CE 内运行尚未实测。

English: updates all seven features for PVZ Fusion 4.0.5 Windows x64, corrects changed field layouts and hook instructions, verifies supported builds before attachment, adds CE instruction guards, and resets cached GUI feature addresses after the game exits. Four assets and their checksums are distributed through Releases. Validation covers isolated patch mechanics and x64 execution; gameplay and CE in-app execution remain unverified.

## v9.1 — PVZ Fusion 3.8.1

* 加固进程内存读写、句柄清理和补丁提交失败时的回滚。
* Hardened process memory I/O, handle cleanup, and rollback after failed patch commits.

## v9 — PVZ Fusion 3.8.1

* 适配融合版 3.8.1，修复僵尸一击必杀和游戏倍速跨关保持，同步四种分发形式。
* Adapted to 3.8.1, corrected zombie damage hooks, and added persistent game speed across levels in the four distributed variants.
