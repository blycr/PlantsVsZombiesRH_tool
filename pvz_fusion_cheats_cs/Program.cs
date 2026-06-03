using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;

namespace pvz_fusion_cheats_cs
{
    public struct PatchRecord
    {
        public IntPtr Address;
        public byte[] OriginalBytes;
        public byte[] PatchedBytes;

        public PatchRecord(IntPtr address, byte[] originalBytes, byte[] patchedBytes)
        {
            Address = address;
            OriginalBytes = originalBytes;
            PatchedBytes = patchedBytes;
        }
    }

    public abstract class CheatFeature
    {
        public string Key { get; }
        public string Name { get; }
        public string Description { get; }
        public bool Enabled { get; protected set; } = false;

        protected List<PatchRecord> Patches = new List<PatchRecord>();
        protected List<IntPtr> Caves = new List<IntPtr>();

        protected CheatFeature(string key, string name, string description)
        {
            Key = key;
            Name = name;
            Description = description;
        }

        public string GetStatusStr()
        {
            return Enabled ? "[ 已激活 ]" : "[ 已关闭 ]";
        }

        public virtual string OnClick(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled)
            {
                if (Disable(pm, baseAddress, modifier))
                    return $"已关闭功能: '{Name}'";
            }
            else
            {
                if (Enable(pm, baseAddress, modifier))
                    return $"开启成功: '{Name}'";
                else
                    return $"开启失败: '{Name}'，请确认已在关卡内";
            }
            return null;
        }

        public abstract bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier);

        public virtual bool Disable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Patches.Count == 0)
            {
                Enabled = false;
                return true;
            }

            Console.WriteLine($"[*] 正在还原 {Name} 的修改点...");
            // Reverse restore to prevent race conditions in nested hooks
            for (int i = Patches.Count - 1; i >= 0; i--)
            {
                var patch = Patches[i];
                try
                {
                    pm.WriteBytes(patch.Address, patch.OriginalBytes);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[!] 还原地址 0x{patch.Address.ToInt64():X} 失败: {e.Message}");
                    return false;
                }
            }

            Patches.Clear();
            Enabled = false;
            return true;
        }

        public virtual void Cleanup(NativeMemory pm)
        {
            Enabled = false;
            foreach (var cave in Caves)
            {
                try
                {
                    pm.Free(cave);
                }
                catch { }
            }
            Patches.Clear();
            Caves.Clear();
        }

        // Helper helpers
        protected static byte[] MakeJmp(IntPtr from, IntPtr to)
        {
            int offset = (int)((long)to - ((long)from + 5));
            byte[] code = new byte[5];
            code[0] = 0xE9;
            Array.Copy(BitConverter.GetBytes(offset), 0, code, 1, 4);
            return code;
        }

        protected static byte[] MakeCall(IntPtr from, IntPtr to)
        {
            int offset = (int)((long)to - ((long)from + 5));
            byte[] code = new byte[5];
            code[0] = 0xE8;
            Array.Copy(BitConverter.GetBytes(offset), 0, code, 1, 4);
            return code;
        }
    }

    // ============================================================================
    // 功能 1：极速冷却 ×100
    // ============================================================================
    public class CooldownFeature : CheatFeature
    {
        private IntPtr _float100Addr = IntPtr.Zero;
        private IntPtr _targetAddr = IntPtr.Zero;
        private IntPtr _caveAddr = IntPtr.Zero;

        public CooldownFeature() : base("1", "极速冷却 ×100", "所有卡牌和手套的CD瞬间冷却完毕") { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _targetAddr = (IntPtr)((long)baseAddress + 0x7A3519);

            try
            {
                byte[] verify = pm.ReadBytes(_targetAddr, 5);
                byte[] origBytes = { 0xE8, 0x52, 0x23, 0x4F, 0x01 }; // call GameAssembly.dll + 0x1C95870
                for (int i = 0; i < verify.Length; i++)
                {
                    if (verify[i] != origBytes[i])
                    {
                        Console.WriteLine($"[-] 冷却点字节验证失败 @ 0x{_targetAddr.ToInt64():X}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 读取冷却点失败: {e.Message}");
                return false;
            }

            if (_float100Addr == IntPtr.Zero)
            {
                _float100Addr = pm.FindFloat100();
                if (_float100Addr == IntPtr.Zero)
                {
                    Console.WriteLine("[-] 未能在内存中定位 100.0f 常量");
                    return false;
                }
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(18, _targetAddr);
                    Caves.Add(_caveAddr);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[-] 获取代码洞穴失败: {e.Message}");
                    return false;
                }

                // Compile cave bytes:
                // 1. call 0x1C95870
                IntPtr callTarget = (IntPtr)((long)baseAddress + 0x1C95870);
                byte[] callCode = MakeCall(_caveAddr, callTarget);

                // 2. mulss xmm0, [float100_addr] (F3 0F 59 05 + offset)
                byte[] mulCode = { 0xF3, 0x0F, 0x59, 0x05, 0x00, 0x00, 0x00, 0x00 };
                int mulOffset = (int)((long)_float100Addr - ((long)_caveAddr + 5 + 8)); // 5 (call) + 8 (mulss instruction length)
                Array.Copy(BitConverter.GetBytes(mulOffset), 0, mulCode, 4, 4);

                // 3. jmp back (_targetAddr + 5)
                IntPtr backAddr = (IntPtr)((long)_targetAddr + 5);
                byte[] jmpCode = MakeJmp((IntPtr)((long)_caveAddr + 13), backAddr); // 5 (call) + 8 (mulss) = 13

                byte[] caveCode = new byte[18];
                Array.Copy(callCode, 0, caveCode, 0, 5);
                Array.Copy(mulCode, 0, caveCode, 5, 8);
                Array.Copy(jmpCode, 0, caveCode, 13, 5);

                pm.WriteBytes(_caveAddr, caveCode);
            }

            byte[] patchBytes = MakeJmp(_targetAddr, _caveAddr);
            byte[] origVerify = { 0xE8, 0x52, 0x23, 0x4F, 0x01 };
            Patches.Add(new PatchRecord(_targetAddr, origVerify, patchBytes));

            pm.WriteBytes(_targetAddr, patchBytes);
            Enabled = true;
            return true;
        }
    }

    // ============================================================================
    // 功能 2：阳光越花越多
    // ============================================================================
    public class SunFeature : CheatFeature
    {
        private IntPtr _getSunAddr = IntPtr.Zero;
        private IntPtr _useSunAddr = IntPtr.Zero;
        private IntPtr _cave1Addr = IntPtr.Zero;
        private IntPtr _cave2Addr = IntPtr.Zero;

        public SunFeature() : base("2", "阳光越花越多", "捡阳光或种植消耗阳光时，阳光皆为 100 倍增加") { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            // Pattern scan for GetSun and UseSun
            if (_getSunAddr == IntPtr.Zero)
            {
                byte[] getsunPattern = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 }; // add [rsi+0x108], eax
                _getSunAddr = pm.FindPattern(getsunPattern, 0x7DAF00, 0x7DCF00);
                if (_getSunAddr == IntPtr.Zero)
                {
                    Console.WriteLine("[-] 未能定位 Board.GetSun 阳光增加点");
                    return false;
                }
            }

            if (_useSunAddr == IntPtr.Zero)
            {
                byte[] usesunPattern = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 }; // sub [rbx+0x108], eax
                _useSunAddr = pm.FindPattern(usesunPattern, 0x7E8100, 0x7EA100);
                if (_useSunAddr == IntPtr.Zero)
                {
                    Console.WriteLine("[-] 未能定位 Board.UseSun 阳光扣除点");
                    return false;
                }
            }

            try
            {
                byte[] v1 = pm.ReadBytes(_getSunAddr, 6);
                byte[] v2 = pm.ReadBytes(_useSunAddr, 6);
                byte[] origGet = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
                byte[] origUse = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 };

                for (int i = 0; i < 6; i++)
                {
                    if (v1[i] != origGet[i] || v2[i] != origUse[i])
                    {
                        Console.WriteLine("[-] 阳光控制点数据验证失败，已被修改过");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 读取阳光控制点数据失败: {e.Message}");
                return false;
            }

            // Allocate caves
            if (_cave1Addr == IntPtr.Zero)
            {
                try
                {
                    _cave1Addr = pm.GetCave(14, _getSunAddr);
                    Caves.Add(_cave1Addr);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[-] 分配 Board.GetSun 洞穴失败: {e.Message}");
                    return false;
                }

                // getsun cave bytes:
                // 1. imul eax, eax, 100 (6B C0 64)
                // 2. add [rsi+0x108], eax (01 86 08 01 00 00)
                // 3. jmp getsun_addr + 6
                byte[] caveCode1 = new byte[14];
                byte[] instr = { 0x6B, 0xC0, 0x64, 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
                Array.Copy(instr, 0, caveCode1, 0, 9);
                byte[] jmp = MakeJmp((IntPtr)((long)_cave1Addr + 9), (IntPtr)((long)_getSunAddr + 6));
                Array.Copy(jmp, 0, caveCode1, 9, 5);

                pm.WriteBytes(_cave1Addr, caveCode1);
            }

            if (_cave2Addr == IntPtr.Zero)
            {
                try
                {
                    _cave2Addr = pm.GetCave(14, _useSunAddr);
                    Caves.Add(_cave2Addr);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[-] 分配 Board.UseSun 洞穴失败: {e.Message}");
                    return false;
                }

                // usesun cave bytes:
                // 1. imul eax, eax, 100 (6B C0 64)
                // 2. add [rbx+0x108], eax (01 83 08 01 00 00) -> convert sub to add!
                // 3. jmp usesun_addr + 6
                byte[] caveCode2 = new byte[14];
                byte[] instr = { 0x6B, 0xC0, 0x64, 0x01, 0x83, 0x08, 0x01, 0x00, 0x00 };
                Array.Copy(instr, 0, caveCode2, 0, 9);
                byte[] jmp = MakeJmp((IntPtr)((long)_cave2Addr + 9), (IntPtr)((long)_useSunAddr + 6));
                Array.Copy(jmp, 0, caveCode2, 9, 5);

                pm.WriteBytes(_cave2Addr, caveCode2);
            }

            byte[] patchGet = new byte[6];
            byte[] jmpGet = MakeJmp(_getSunAddr, _cave1Addr);
            Array.Copy(jmpGet, 0, patchGet, 0, 5);
            patchGet[5] = 0x90; // NOP

            byte[] patchUse = new byte[6];
            byte[] jmpUse = MakeJmp(_useSunAddr, _cave2Addr);
            Array.Copy(jmpUse, 0, patchUse, 0, 5);
            patchUse[5] = 0x90; // NOP

            byte[] origGetVerify = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
            byte[] origUseVerify = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 };

            Patches.Add(new PatchRecord(_getSunAddr, origGetVerify, patchGet));
            Patches.Add(new PatchRecord(_useSunAddr, origUseVerify, patchUse));

            pm.WriteBytes(_getSunAddr, patchGet);
            pm.WriteBytes(_useSunAddr, patchUse);

            Enabled = true;
            return true;
        }
    }

    // ============================================================================
    // 功能 3：任意种植与重叠融合
    // ============================================================================
    public class PlacementFeature : CheatFeature
    {
        private IntPtr _chkboxAddr = IntPtr.Zero;
        private IntPtr _skipAddr = IntPtr.Zero;
        private IntPtr _failAddr = IntPtr.Zero;

        public PlacementFeature() : base("3", "任意种植与重叠融合", "解除地形限制可水面重叠种植，且保留兼容植物自动融合逻辑") { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _chkboxAddr = (IntPtr)((long)baseAddress + 0x7B1130);
            _skipAddr = (IntPtr)((long)baseAddress + 0x7BA33F);
            _failAddr = (IntPtr)((long)baseAddress + 0x7BA380);

            try
            {
                byte[] v1 = pm.ReadBytes(_chkboxAddr, 3);
                byte[] v2 = pm.ReadBytes(_skipAddr, 6);
                byte[] v3 = pm.ReadBytes(_failAddr, 6);

                byte[] orig1 = { 0x48, 0x8B, 0xC4 };
                byte[] orig2 = { 0x0F, 0x85, 0xDF, 0x00, 0x00, 0x00 };
                byte[] orig3 = { 0x0F, 0x84, 0xB8, 0xFD, 0xFF, 0xFF };

                for (int i = 0; i < 3; i++)
                {
                    if (v1[i] != orig1[i])
                    {
                        Console.WriteLine("[-] 种植合法检测点验证失败");
                        return false;
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    if (v2[i] != orig2[i] || v3[i] != orig3[i])
                    {
                        Console.WriteLine("[-] 种植判断跳转点验证失败");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 读取种植控制数据失败: {e.Message}");
                return false;
            }

            // Patch bytes
            byte[] patch1 = { 0xB0, 0x01, 0xC3 }; // mov al, 1; ret
            byte[] patch2 = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }; // NOPs
            byte[] patch3 = { 0x0F, 0x84, 0x9E, 0x00, 0x00, 0x00 }; // Redirect jump to normal placement

            byte[] orig1V = { 0x48, 0x8B, 0xC4 };
            byte[] orig2V = { 0x0F, 0x85, 0xDF, 0x00, 0x00, 0x00 };
            byte[] orig3V = { 0x0F, 0x84, 0xB8, 0xFD, 0xFF, 0xFF };

            Patches.Add(new PatchRecord(_chkboxAddr, orig1V, patch1));
            Patches.Add(new PatchRecord(_skipAddr, orig2V, patch2));
            Patches.Add(new PatchRecord(_failAddr, orig3V, patch3));

            pm.WriteBytes(_chkboxAddr, patch1);
            pm.WriteBytes(_skipAddr, patch2);
            pm.WriteBytes(_failAddr, patch3);

            Enabled = true;
            return true;
        }
    }

    // ============================================================================
    // 功能 4：植物无敌
    // ============================================================================
    public class InvincibleFeature : CheatFeature
    {
        private IntPtr _takedamageAddr = IntPtr.Zero;
        private IntPtr _dieAddr = IntPtr.Zero;
        private IntPtr _caveAddr = IntPtr.Zero;

        public InvincibleFeature() : base("4", "植物无敌", "植物免疫啃食/秒杀/碾压/落水，且不影响铲除与爆炸自毁") { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _takedamageAddr = (IntPtr)((long)baseAddress + 0x3F2730);
            _dieAddr = (IntPtr)((long)baseAddress + 0x3EBBB0);

            try
            {
                byte[] v1 = pm.ReadBytes(_takedamageAddr, 7);
                byte[] v2 = pm.ReadBytes(_dieAddr, 14);

                byte[] orig1 = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x10 };
                byte[] orig2 = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };

                for (int i = 0; i < 7; i++)
                {
                    if (v1[i] != orig1[i])
                    {
                        Console.WriteLine("[-] 植物受伤害保护点验证失败");
                        return false;
                    }
                }

                for (int i = 0; i < 14; i++)
                {
                    if (v2[i] != orig2[i])
                    {
                        Console.WriteLine("[-] 植物死亡判定点验证失败");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 读取植物无敌控制数据失败: {e.Message}");
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(25, _dieAddr);
                    Caves.Add(_caveAddr);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[-] 分配植物死亡判定洞穴失败: {e.Message}");
                    return false;
                }

                // Compile cave bytes:
                // 1. cmp edx, 11 (83 FA 0B)
                // 2. jge target_ret (7D 13) -> target_ret is offset 24 (ret)
                // 3. original 14 bytes
                // 4. jmp back to _dieAddr + 14
                // 5. ret (C3)
                byte[] caveCode = new byte[25];
                byte[] cmpInstruction = { 0x83, 0xFA, 0x0B, 0x7D, 0x13 };
                byte[] origDie = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };
                
                Array.Copy(cmpInstruction, 0, caveCode, 0, 5);
                Array.Copy(origDie, 0, caveCode, 5, 14);

                IntPtr jmpBackDest = (IntPtr)((long)_dieAddr + 14);
                byte[] jmpCode = MakeJmp((IntPtr)((long)_caveAddr + 19), jmpBackDest);
                Array.Copy(jmpCode, 0, caveCode, 19, 5);
                caveCode[24] = 0xC3; // ret

                pm.WriteBytes(_caveAddr, caveCode);
            }

            byte[] patchTakedamage = { 0xC3, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }; // ret + 6 NOPs
            
            byte[] patchDie = new byte[14];
            byte[] jmpToCave = MakeJmp(_dieAddr, _caveAddr);
            Array.Copy(jmpToCave, 0, patchDie, 0, 5);
            for (int i = 5; i < 14; i++) patchDie[i] = 0x90; // NOP padding

            byte[] origTakedamageVerify = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x10 };
            byte[] origDieVerify = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };

            Patches.Add(new PatchRecord(_takedamageAddr, origTakedamageVerify, patchTakedamage));
            Patches.Add(new PatchRecord(_dieAddr, origDieVerify, patchDie));

            pm.WriteBytes(_takedamageAddr, patchTakedamage);
            pm.WriteBytes(_dieAddr, patchDie);

            Enabled = true;
            return true;
        }
    }

    // ============================================================================
    // 功能 5：僵尸一击必杀
    // ============================================================================
    public class OneHitKillFeature : CheatFeature
    {
        private IntPtr _targetAddr = IntPtr.Zero;
        private IntPtr _caveAddr = IntPtr.Zero;

        public OneHitKillFeature() : base("5", "僵尸一击必杀", "所有僵尸在受到任意伤害时立即死亡") { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _targetAddr = (IntPtr)((long)baseAddress + 0x564120);

            try
            {
                byte[] verify = pm.ReadBytes(_targetAddr, 14);
                byte[] origBytes = { 0x40, 0x56, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x83, 0xEC, 0x60 };

                for (int i = 0; i < 14; i++)
                {
                    if (verify[i] != origBytes[i])
                    {
                        Console.WriteLine($"[-] 僵尸伤害函数验证失败 @ 0x{_targetAddr.ToInt64():X}");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 读取僵尸伤害接口失败: {e.Message}");
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(32, _targetAddr);
                    Caves.Add(_caveAddr);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[-] 分配僵尸伤害劫持洞穴失败: {e.Message}");
                    return false;
                }

                // Compile cave bytes:
                // 1. mov r8d, 999999 (41 C7 C0 40 42 0F 00)
                // 2. original 14 bytes
                // 3. jmp back to _targetAddr + 14
                byte[] caveCode = new byte[26];
                byte[] movR8D = { 0x41, 0xC7, 0xC0, 0x40, 0x42, 0x0F, 0x00 };
                byte[] origBytes = { 0x40, 0x56, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x83, 0xEC, 0x60 };
                
                Array.Copy(movR8D, 0, caveCode, 0, 7);
                Array.Copy(origBytes, 0, caveCode, 7, 14);

                IntPtr jmpBackDest = (IntPtr)((long)_targetAddr + 14);
                byte[] jmpCode = MakeJmp((IntPtr)((long)_caveAddr + 21), jmpBackDest);
                Array.Copy(jmpCode, 0, caveCode, 21, 5);

                pm.WriteBytes(_caveAddr, caveCode);
            }

            byte[] patchBytes = new byte[14];
            byte[] jmpToCave = MakeJmp(_targetAddr, _caveAddr);
            Array.Copy(jmpToCave, 0, patchBytes, 0, 5);
            for (int i = 5; i < 14; i++) patchBytes[i] = 0x90; // NOP padding

            byte[] origVerify = { 0x40, 0x56, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x83, 0xEC, 0x60 };
            Patches.Add(new PatchRecord(_targetAddr, origVerify, patchBytes));

            pm.WriteBytes(_targetAddr, patchBytes);
            Enabled = true;
            return true;
        }
    }

    // ============================================================================
    // 功能 6：特定植物状态加速
    // ============================================================================
    public class AccelerateFeature : CheatFeature
    {
        private IntPtr _chewHookAddr = IntPtr.Zero;
        private IntPtr _riseHookAddr = IntPtr.Zero;
        private IntPtr _caveAddr = IntPtr.Zero;
        private IntPtr _chewCaveAddr = IntPtr.Zero;
        private IntPtr _riseCaveAddr = IntPtr.Zero;

        public AccelerateFeature() : base("6", "特定植物状态加速", "大嘴花咀嚼与土豆地雷准备等时间加速 20 倍 (非瞬爆，保留正常动作)") { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _chewHookAddr = (IntPtr)((long)baseAddress + 0x3F00F4);
            _riseHookAddr = (IntPtr)((long)baseAddress + 0x40EE00);

            try
            {
                byte[] v1 = pm.ReadBytes(_chewHookAddr, 8);
                byte[] v2 = pm.ReadBytes(_riseHookAddr, 9);

                byte[] origChew = { 0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00 }; // movss xmm6, [rdi+14Ch]
                byte[] origRise = { 0x40, 0x53, 0x48, 0x81, 0xEC, 0x90, 0x00, 0x00, 0x00 }; // push rbx; sub rsp, 90h

                for (int i = 0; i < 8; i++)
                {
                    if (v1[i] != origChew[i])
                    {
                        Console.WriteLine("[-] 咀嚼加速点验证失败");
                        return false;
                    }
                }

                for (int i = 0; i < 9; i++)
                {
                    if (v2[i] != origRise[i])
                    {
                        Console.WriteLine("[-] 准备加速点验证失败");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 读取加速点数据失败: {e.Message}");
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(256, _chewHookAddr);
                    Caves.Add(_caveAddr);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[-] 分配加速代码洞穴失败: {e.Message}");
                    return false;
                }

                _chewCaveAddr = _caveAddr;
                _riseCaveAddr = (IntPtr)((long)_caveAddr + 128);

                // Build chew cave (Assembly logic):
                // 1. push rax (50)
                // 2. mov eax, [rdi+0x18c] (8B 87 8C 01 00 00)
                // 3. cmp eax, 5 (Chomper) (83 F8 05)
                // 4. je do_acc (74 23) (jump length = 35 bytes)
                // 5. cmp eax, 354 (3D 62 01 00 00)
                // 6. je do_acc (74 1C)
                // 7. cmp eax, 356 (3D 64 01 00 00)
                // 8. je do_acc (74 15)
                // 9. cmp eax, 368 (3D 70 01 00 00)
                // 10. je do_acc (74 0E)
                // 11. cmp eax, 900 (3D 84 03 00 00)
                // 12. jl no_acc (7C 1D)
                // 13. cmp eax, 1405 (3D 7D 05 00 00)
                // 14. jg no_acc (7F 16)
                // do_acc:
                // 15. movss xmm6, [rdi+0x14c] (F3 0F 10 B7 4C 01 00 00)
                // 16. mulss xmm6, [rip+20] (F3 0F 59 35 14 00 00 00) -> 20.0f
                // 17. pop rax (58)
                // 18. jmp back to chew_hook + 8 (E9 + offset)
                // no_acc:
                // 19. movss xmm6, [rdi+0x14c] (F3 0F 10 B7 4C 01 00 00)
                // 20. pop rax (58)
                // 21. jmp back to chew_hook + 8 (E9 + offset)
                // float_20 (4 bytes float at offset 83)
                byte[] chewCode = new byte[87];
                byte[] chewHeader = {
                    0x50, 0x8B, 0x87, 0x8C, 0x01, 0x00, 0x00, 0x83, 0xF8, 0x05, 0x74, 0x23,
                    0x3D, 0x62, 0x01, 0x00, 0x00, 0x74, 0x1C, 0x3D, 0x64, 0x01, 0x00, 0x00, 0x74, 0x15,
                    0x3D, 0x70, 0x01, 0x00, 0x00, 0x74, 0x0E, 0x3D, 0x84, 0x03, 0x00, 0x00, 0x7C, 0x1D,
                    0x3D, 0x7D, 0x05, 0x00, 0x00, 0x7F, 0x16,
                    0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00, // do_acc: movss xmm6, [rdi+14ch]
                    0xF3, 0x0F, 0x59, 0x35, 0x14, 0x00, 0x00, 0x00, // mulss xmm6, [rip+20]
                    0x58 // pop rax
                };
                Array.Copy(chewHeader, 0, chewCode, 0, chewHeader.Length);
                IntPtr backChew = (IntPtr)((long)_chewHookAddr + 8);
                byte[] jmpChew1 = MakeJmp((IntPtr)((long)_chewCaveAddr + 64), backChew); // do_acc jmp
                Array.Copy(jmpChew1, 0, chewCode, 64, 5);

                byte[] chewNoAcc = {
                    0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00, // no_acc: movss xmm6, [rdi+14ch]
                    0x58 // pop rax
                };
                Array.Copy(chewNoAcc, 0, chewCode, 69, chewNoAcc.Length);
                byte[] jmpChew2 = MakeJmp((IntPtr)((long)_chewCaveAddr + 78), backChew); // no_acc jmp
                Array.Copy(jmpChew2, 0, chewCode, 78, 5);

                Array.Copy(BitConverter.GetBytes(20.0f), 0, chewCode, 83, 4);

                // Build rise cave (Assembly logic):
                // 1. mulss xmm1, [rip+14] (F3 0F 59 0D 0E 00 00 00) -> 0.05f
                // 2. push rbx (40 53)
                // 3. sub rsp, 90h (48 81 EC 90 00 00 00)
                // 4. jmp back to rise_hook + 9 (E9 + offset)
                // float_0_05 (4 bytes float at offset 22)
                byte[] riseCode = new byte[26];
                byte[] riseHeader = {
                    0xF3, 0x0F, 0x59, 0x0D, 0x0E, 0x00, 0x00, 0x00,
                    0x40, 0x53,
                    0x48, 0x81, 0xEC, 0x90, 0x00, 0x00, 0x00
                };
                Array.Copy(riseHeader, 0, riseCode, 0, riseHeader.Length);
                IntPtr backRise = (IntPtr)((long)_riseHookAddr + 9);
                byte[] jmpRise = MakeJmp((IntPtr)((long)_riseCaveAddr + 17), backRise);
                Array.Copy(jmpRise, 0, riseCode, 17, 5);
                Array.Copy(BitConverter.GetBytes(0.05f), 0, riseCode, 22, 4);

                pm.WriteBytes(_chewCaveAddr, chewCode);
                pm.WriteBytes(_riseCaveAddr, riseCode);
            }

            byte[] patchChew = new byte[8];
            byte[] jmpChew = MakeJmp(_chewHookAddr, _chewCaveAddr);
            Array.Copy(jmpChew, 0, patchChew, 0, 5);
            patchChew[5] = 0x90; patchChew[6] = 0x90; patchChew[7] = 0x90; // NOPs

            byte[] patchRise = new byte[9];
            byte[] jmpRiseHook = MakeJmp(_riseHookAddr, _riseCaveAddr);
            Array.Copy(jmpRiseHook, 0, patchRise, 0, 5);
            for (int i = 5; i < 9; i++) patchRise[i] = 0x90; // NOPs

            byte[] origChewVerify = { 0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00 };
            byte[] origRiseVerify = { 0x40, 0x53, 0x48, 0x81, 0xEC, 0x90, 0x00, 0x00, 0x00 };

            Patches.Add(new PatchRecord(_chewHookAddr, origChewVerify, patchChew));
            Patches.Add(new PatchRecord(_riseHookAddr, origRiseVerify, patchRise));

            pm.WriteBytes(_chewHookAddr, patchChew);
            pm.WriteBytes(_riseHookAddr, patchRise);

            Enabled = true;
            return true;
        }
    }

    // ============================================================================
    // 功能 7：自由调节游戏整体速率
    // ============================================================================
    public class SpeedFeature : CheatFeature
    {
        public double Speed { get; private set; } = 1.0;

        public SpeedFeature() : base("7", "自由调节游戏整体速率", "自由调节游戏整体运行速率 (支持加速/减速，默认 1.0x)") { }

        public string GetSpeedStatusStr()
        {
            if (Enabled && Speed != 1.0)
                return $"[ 速率: {Speed}x ]";
            return "[ 已关闭 ]";
        }

        public override string OnClick(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            Console.Write("\n[*] 请输入新的游戏速度倍率 (范围 0.1 ~ 10.0，输入 1.0 恢复常规): ");
            string valStr = Console.ReadLine()?.Trim();
            if (double.TryParse(valStr, out double val))
            {
                if (val < 0.1 || val > 10.0)
                {
                    return "速度倍率超出安全范围 (0.1 ~ 10.0)";
                }

                if (SetSpeed(pm, baseAddress, val))
                {
                    Speed = val;
                    Enabled = (val != 1.0);
                    return $"游戏速度已成功设置为 {val}x";
                }
                else
                {
                    return "设置游戏速度失败，请确认已在关卡内";
                }
            }
            return "输入无效，必须是数字";
        }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            // Speed feature has no static patch. Enabled by remote thread invocation.
            return true;
        }

        public override bool Disable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (SetSpeed(pm, baseAddress, 1.0))
            {
                Speed = 1.0;
                Enabled = false;
                return true;
            }
            return false;
        }

        private bool SetSpeed(NativeMemory pm, IntPtr baseAddress, double speed)
        {
            IntPtr setTimeScaleAddr = (IntPtr)((long)baseAddress + 0x1C95A90);
            try
            {
                // Allocate transient cave
                IntPtr cave = pm.Allocate(64);
                if (cave == IntPtr.Zero) return false;

                // Build transient shellcode (x64):
                // 1. sub rsp, 28h (48 83 EC 28)
                // 2. movss xmm0, [rip+20] (F3 0F 10 05 14 00 00 00)
                // 3. mov rax, setTimeScaleAddr (48 B8 + 8 bytes address)
                // 4. call rax (FF D0)
                // 5. add rsp, 28h (48 83 C4 28)
                // 6. ret (C3)
                // 7. NOP padding (90 90 90)
                // 8. float speed (4 bytes)
                byte[] shellcode = new byte[40];
                byte[] header = {
                    0x48, 0x83, 0xEC, 0x28,
                    0xF3, 0x0F, 0x10, 0x05, 0x14, 0x00, 0x00, 0x00,
                    0x48, 0xB8
                };
                Array.Copy(header, 0, shellcode, 0, header.Length);
                Array.Copy(BitConverter.GetBytes(setTimeScaleAddr.ToInt64()), 0, shellcode, 14, 8);
                
                byte[] tail = {
                    0xFF, 0xD0,
                    0x48, 0x83, 0xC4, 0x28,
                    0xC3,
                    0x90, 0x90, 0x90
                };
                Array.Copy(tail, 0, shellcode, 22, tail.Length);
                Array.Copy(BitConverter.GetBytes((float)speed), 0, shellcode, 32, 4);

                pm.WriteBytes(cave, shellcode);

                // Run remote thread
                if (pm.StartThread(cave, out IntPtr threadHandle))
                {
                    NativeMemory.WaitForSingleObject(threadHandle, 500);
                    NativeMemory.CloseHandle(threadHandle);
                }

                pm.Free(cave);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[-] 设置游戏速度失败: {e.Message}");
                return false;
            }
        }
    }

    // ============================================================================
    // 主修改器管理控制台
    // ============================================================================
    public class Program
    {
        private NativeMemory _pm = null;
        private IntPtr _baseAddress = IntPtr.Zero;
        private List<CheatFeature> _features = new List<CheatFeature>();

        public Program()
        {
            _features.Add(new CooldownFeature());
            _features.Add(new SunFeature());
            _features.Add(new PlacementFeature());
            _features.Add(new InvincibleFeature());
            _features.Add(new OneHitKillFeature());
            _features.Add(new AccelerateFeature());
            _features.Add(new SpeedFeature());
        }

        private static bool IsAdmin()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        private static void ElevateUac()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Environment.ProcessPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);
                Environment.Exit(0);
            }
            catch
            {
                Console.WriteLine("[-] 授权失败。请手动以管理员身份运行此程序。");
                Console.ReadLine();
                Environment.Exit(1);
            }
        }

        private string AttachGame()
        {
            _pm = new NativeMemory();
            if (_pm.Attach("PlantsVsZombiesRH", "GameAssembly.dll"))
            {
                _baseAddress = _pm.BaseAddress;
                Console.WriteLine($"[+] 已成功附加游戏进程，GameAssembly.dll 基址: 0x{_baseAddress.ToInt64():X}");
                return "SUCCESS";
            }

            // Test if process exists but couldn't open (access denied)
            Process[] p = Process.GetProcessesByName("PlantsVsZombiesRH");
            if (p.Length > 0)
            {
                return "ACCESS_DENIED";
            }

            return "NOT_RUNNING";
        }

        private void RestoreAll()
        {
            Console.WriteLine("\n[*] 正在还原所有内存补丁并清理资源...");
            foreach (var f in _features)
            {
                if (f.Enabled)
                {
                    f.Disable(_pm, _baseAddress, this);
                }
                f.Cleanup(_pm);
            }
            Console.WriteLine("[+] 所有修改点已恢复原样");
        }

        public void RunLoop()
        {
            string lastErr = "";
            while (true)
            {
                if (!_pm.IsGameRunning())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[-] 检测到游戏进程已退出，正在执行自动清理...");
                    Console.ResetColor();
                    break;
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=========================================================================================");
                Console.WriteLine($"        PVZ Fusion 3.6.1 修改器 C# 原生版 - 已附加进程 PID: {_pm.GameProcess.Id}");
                Console.WriteLine("=========================================================================================");
                Console.ResetColor();

                foreach (var f in _features)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($" [{f.Key}] ");
                    Console.ResetColor();

                    string nameStr = f.Name.PadRight(26);
                    Console.Write(nameStr);

                    // Print status in green/red
                    if (f.Enabled)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        if (f is SpeedFeature speedFeat && speedFeat.Speed != 1.0)
                            Console.Write(speedFeat.GetSpeedStatusStr().PadRight(13));
                        else
                            Console.Write(f.GetStatusStr().PadRight(13));
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write(f.GetStatusStr().PadRight(13));
                    }
                    Console.ResetColor();

                    Console.WriteLine($" - {f.Description}");
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("-----------------------------------------------------------------------------------------");
                Console.ResetColor();
                Console.WriteLine(" [A] 一键开启所有功能");
                Console.WriteLine(" [R] 还原所有修改 (全部重置)");
                Console.WriteLine(" [Q] 还原并退出修改器");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=========================================================================================");
                Console.ResetColor();

                if (!string.IsNullOrEmpty(lastErr))
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[*] {lastErr}");
                    Console.ResetColor();
                    lastErr = "";
                }
                else
                {
                    Console.WriteLine();
                }

                Console.Write("请输入选项序号 (例如 1) 并按 Enter: ");
                string choice = Console.ReadLine()?.Trim().ToUpper();

                if (choice == "Q") break;
                else if (choice == "R")
                {
                    lastErr = "正在重置所有补丁...";
                    foreach (var f in _features)
                    {
                        if (f.Enabled) f.Disable(_pm, _baseAddress, this);
                    }
                    lastErr = "所有补丁重置完成！";
                }
                else if (choice == "A")
                {
                    lastErr = "正在一键开启所有功能...";
                    int success = 0;
                    foreach (var f in _features)
                    {
                        if (f.Key == "7") continue; // Skip speed regulation
                        if (!f.Enabled)
                        {
                            if (f.Enable(_pm, _baseAddress, this)) success++;
                        }
                    }
                    lastErr = $"一键开启完成！共新激活了 {success} 项功能。";
                }
                else
                {
                    bool found = false;
                    foreach (var f in _features)
                    {
                        if (f.Key == choice)
                        {
                            found = true;
                            lastErr = f.OnClick(_pm, _baseAddress, this);
                            break;
                        }
                    }
                    if (!found)
                    {
                        lastErr = "无效的指令，请重新输入";
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            // Configure Console properties
            Console.Title = "PVZ Fusion 3.6.1 修改器 C# 原生版";
            
            Program program = new Program();

            // Attempt initial attachment
            string status = program.AttachGame();

            if (status == "ACCESS_DENIED")
            {
                if (!IsAdmin())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("[!] 权限不足，正在通过 UAC 请求管理员权限...");
                    Console.ResetColor();
                    Thread.Sleep(1000);
                    ElevateUac();
                    return;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[-] 已经是管理员但仍无法打开游戏进程，可能被安全软件拦截。");
                    Console.ResetColor();
                    Console.WriteLine("\n按任意键退出...");
                    Console.ReadLine();
                    return;
                }
            }
            else if (status == "NOT_RUNNING")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[-] 游戏未运行，请先启动 PlantsVsZombiesRH.exe 并进入关卡。");
                Console.ResetColor();
                Console.WriteLine("\n按任意键退出...");
                Console.ReadLine();
                return;
            }

            // Normal running loop
            try
            {
                program.RunLoop();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[-] 修改器运行时发生未捕获异常: {e.Message}");
                Console.ResetColor();
                Console.WriteLine("\n按任意键退出...");
                Console.ReadLine();
            }
            finally
            {
                program.RestoreAll();
                program._pm.Dispose();
                Thread.Sleep(1000);
            }
        }
    }
}
