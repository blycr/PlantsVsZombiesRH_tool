# 源码构建与验证 / Build and verification

## 环境 / Requirements

Windows x64、[.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0)。构建不需要游戏文件；补丁验证需要当前适配的 `GameAssembly.dll`，不会随仓库分发。

Use Windows x64 and .NET SDK 10. Building does not require the game; verification requires the supported `GameAssembly.dll`, which is not distributed with the repository.

## 构建 / Build

在仓库根目录运行 / Run from the repository root:

```powershell
dotnet publish pvz_fusion_cheats_cs/pvz_fusion_cheats_cs.csproj -c Release
dotnet publish pvz_fusion_cheats_wpf/pvz_fusion_cheats_wpf.csproj -c Release
```

产物分别位于 / Executables are generated in:

* `pvz_fusion_cheats_cs/bin/Release/net10.0-windows/win-x64/publish/`
* `pvz_fusion_cheats_wpf/bin/Release/net10.0-windows/win-x64/publish/`

两个程序均为依赖运行库的单文件发布；GUI 需要 .NET Desktop Runtime 10 x64，控制台需要 .NET Runtime 10 x64。游戏构建校验和版本标识在 `shared/GameBuild.cs`；构建版本号在两个 `.csproj` 中。

Both programs use framework-dependent single-file publishing. The GUI requires .NET Desktop Runtime 10 x64; the console requires .NET Runtime 10 x64. `shared/GameBuild.cs` defines the supported game fingerprint and version labels; the two project files define the assembly version.

## 补丁验证 / Patch verification

```powershell
dotnet run --project tests/TrainerVerification.csproj -- "C:/path/to/game/GameAssembly.dll"
```

验证器先检查构建哈希，再将游戏 DLL 的 PE 节复制到自己的临时可执行内存中，测试两个 C# 实现的指令边界、原字节保护、七项功能同时开启、重复开关、代码洞复用、原字节还原和倍速恢复。Unity 倍速调用被替换为记录值的测试桩，不会启动游戏或修改运行中的游戏进程。

The runner checks the supported build fingerprint and copies PE sections into disposable memory in its own process. It verifies instruction boundaries, original-byte guards, simultaneous activation of seven features, repeat toggles, cave reuse, original-byte restoration, and a recorded speed reset in both C# implementations. It does not start or attach to a game.

可用第二个参数导出汇编补丁，供反汇编或模拟执行检查 / An optional second argument exports generated hook bytes for disassembly or emulation:

```powershell
dotnet run --project tests/TrainerVerification.csproj -- "C:/path/to/game/GameAssembly.dll" ./hooks.json
```

导出的 JSON 含测试进程地址和补丁字节，属于临时验证产物，不入库 / The JSON contains test-process addresses and hook bytes. Keep it out of source control.

这些测试验证补丁机制；关卡内效果、切换关卡和 CE 内执行需要单独实测。Python、CE 表和发布校验清单通过 [GitHub Releases](https://github.com/blycr/PlantsVsZombiesRH_tool/releases) 获取；游戏本体、生成的符号导出和本地维护资料保持不入库。

These checks verify patch mechanics. Gameplay, level transitions, and CE in-app execution require separate testing. Python, the CE table, and release checksums are available through Releases. Game files, generated dumps, and local maintainer material stay out of source control.
