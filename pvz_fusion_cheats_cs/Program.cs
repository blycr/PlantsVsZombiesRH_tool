using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
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
        public string Name => Program.IsEnglish ? NameEn : NameZh;
        public string Description => Program.IsEnglish ? DescriptionEn : DescriptionZh;
        public string NameZh { get; }
        public string NameEn { get; }
        public string DescriptionZh { get; }
        public string DescriptionEn { get; }
        public bool Enabled { get; protected set; } = false;

        protected List<PatchRecord> Patches = new List<PatchRecord>();
        protected List<IntPtr> Caves = new List<IntPtr>();

        protected CheatFeature(string key, string nameZh, string nameEn, string descriptionZh, string descriptionEn)
        {
            Key = key;
            NameZh = nameZh;
            NameEn = nameEn;
            DescriptionZh = descriptionZh;
            DescriptionEn = descriptionEn;
        }

        public string GetStatusStr()
        {
            return Enabled ? Program.T("[ 已激活 ]", "[  Active ]") : Program.T("[ 已关闭 ]", "[  Closed ]");
        }

        public virtual string OnClick(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled)
            {
                if (Disable(pm, baseAddress, modifier))
                    return Program.T($"已关闭功能: '{Name}'", $"Disabled feature: '{Name}'");
            }
            else
            {
                if (Enable(pm, baseAddress, modifier))
                    return Program.T($"开启成功: '{Name}'", $"Enabled successfully: '{Name}'");
                else
                    return Program.T($"开启失败: '{Name}'，请确认已在关卡内", $"Failed to enable: '{Name}', please make sure you are in a level");
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

            Console.WriteLine(Program.T($"[*] 正在还原 {Name} 的修改点...", $"[*] Restoring patch points for {Name}..."));
            // Restore in reverse order to avoid race conditions in nested hooks
            for (int i = Patches.Count - 1; i >= 0; i--)
            {
                var patch = Patches[i];
                try
                {
                    pm.WriteBytes(patch.Address, patch.OriginalBytes);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Program.T($"[!] 还原地址 0x{patch.Address.ToInt64():X} 失败: {e.Message}", $"[!] Failed to restore address 0x{patch.Address.ToInt64():X}: {e.Message}"));
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

        // Helper methods
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
    // 功能 1：极速冷却
    // ============================================================================
    public class CooldownFeature : CheatFeature
    {
        private IntPtr _cardCdAddr = IntPtr.Zero;
        private IntPtr _toolCdAddr = IntPtr.Zero;
        private IntPtr _cardCave = IntPtr.Zero;
        private IntPtr _toolCave = IntPtr.Zero;

        public CooldownFeature() : base(
            "1",
            "极速冷却", "Instant Cooldown",
            "所有卡牌和手套的CD瞬间冷却完毕", "Seed packets, gloves, and hammers cool down instantly"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            long baseLong = baseAddress.ToInt64();
            _cardCdAddr = (IntPtr)(baseLong + 0x8548B0);
            _toolCdAddr = (IntPtr)(baseLong + 0x666B20);

            byte[] origCard = { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x30 };
            byte[] origTool = { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x40 };

            try
            {
                byte[] v1 = pm.ReadBytes(_cardCdAddr, 6);
                byte[] v2 = pm.ReadBytes(_toolCdAddr, 6);

                for (int i = 0; i < 6; i++)
                {
                    if (v1[i] != origCard[i] || v2[i] != origTool[i])
                    {
                        Console.WriteLine(Program.T("[-] 冷却点字节验证失败，已被修改", "[-] Cooldown byte verification failed, already modified"));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取冷却点数据失败: {e.Message}", $"[-] Failed to read cooldown data: {e.Message}"));
                return false;
            }

            // Allocate CardUI cave
            if (_cardCave == IntPtr.Zero)
            {
                try
                {
                    _cardCave = pm.GetCave(32, _cardCdAddr);
                    Caves.Add(_cardCave);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Program.T($"[-] 获取 CardUI 冷却洞穴失败: {e.Message}", $"[-] Failed to get CardUI cave: {e.Message}"));
                    return false;
                }

                List<byte> cardCode = new List<byte>();
                // mov eax, [rcx+0x48] (8B 41 48)
                cardCode.AddRange(new byte[] { 0x8B, 0x41, 0x48 });
                // mov [rcx+0x44], eax (89 41 44)
                cardCode.AddRange(new byte[] { 0x89, 0x41, 0x44 });
                // push rbx; sub rsp, 30h (40 53 48 83 EC 30)
                cardCode.AddRange(new byte[] { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x30 });
                // jmp back
                IntPtr backCard = (IntPtr)((long)_cardCdAddr + 6);
                cardCode.AddRange(MakeJmp((IntPtr)((long)_cardCave + cardCode.Count), backCard));

                pm.WriteBytes(_cardCave, cardCode.ToArray());
            }

            // Allocate InGameTool cave
            if (_toolCave == IntPtr.Zero)
            {
                try
                {
                    _toolCave = pm.GetCave(32, _toolCdAddr);
                    Caves.Add(_toolCave);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Program.T($"[-] 获取 InGameTool 冷却洞穴失败: {e.Message}", $"[-] Failed to get InGameTool cave: {e.Message}"));
                    return false;
                }

                List<byte> toolCode = new List<byte>();
                // mov eax, [rcx+0x20] (8B 41 20)
                toolCode.AddRange(new byte[] { 0x8B, 0x41, 0x20 });
                // mov [rcx+0x24], eax (89 41 24)
                toolCode.AddRange(new byte[] { 0x89, 0x41, 0x24 });
                // push rbx; sub rsp, 40h (40 53 48 83 EC 40)
                toolCode.AddRange(new byte[] { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x40 });
                // jmp back
                IntPtr backTool = (IntPtr)((long)_toolCdAddr + 6);
                toolCode.AddRange(MakeJmp((IntPtr)((long)_toolCave + toolCode.Count), backTool));

                pm.WriteBytes(_toolCave, toolCode.ToArray());
            }

            // Write Hooks
            byte[] patchCard = new byte[6];
            byte[] jmpCard = MakeJmp(_cardCdAddr, _cardCave);
            Array.Copy(jmpCard, 0, patchCard, 0, 5);
            patchCard[5] = 0x90; // NOP

            byte[] patchTool = new byte[6];
            byte[] jmpTool = MakeJmp(_toolCdAddr, _toolCave);
            Array.Copy(jmpTool, 0, patchTool, 0, 5);
            patchTool[5] = 0x90; // NOP

            Patches.Add(new PatchRecord(_cardCdAddr, origCard, patchCard));
            Patches.Add(new PatchRecord(_toolCdAddr, origTool, patchTool));

            pm.WriteBytes(_cardCdAddr, patchCard);
            pm.WriteBytes(_toolCdAddr, patchTool);

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

        public SunFeature() : base(
            "2",
            "阳光越花越多", "Multiplying Sun",
            "捡阳光或种植消耗阳光时，阳光都会 100 倍增加", "Picking up or consuming sun increases sun by 100x"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            // Pattern scan for GetSun and UseSun
            if (_getSunAddr == IntPtr.Zero)
            {
                byte[] getsunPattern = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 }; // add [rsi+0x108], eax
                _getSunAddr = pm.FindPattern(getsunPattern, 0x88C000, 0x88D000);
                if (_getSunAddr == IntPtr.Zero)
                {
                    Console.WriteLine(Program.T("[-] 未能定位 Board.GetSun 阳光增加点", "[-] Failed to locate Board.GetSun (add sun address)"));
                    return false;
                }
            }

            if (_useSunAddr == IntPtr.Zero)
            {
                byte[] usesunPattern = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 }; // sub [rbx+0x108], eax
                _useSunAddr = pm.FindPattern(usesunPattern, 0x89A000, 0x89B000);
                if (_useSunAddr == IntPtr.Zero)
                {
                    Console.WriteLine(Program.T("[-] 未能定位 Board.UseSun 阳光扣除点", "[-] Failed to locate Board.UseSun (subtract sun address)"));
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
                        Console.WriteLine(Program.T("[-] 阳光控制点数据验证失败，已被修改过", "[-] Sun control points verification failed, already modified"));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取阳光控制点数据失败: {e.Message}", $"[-] Failed to read sun control points: {e.Message}"));
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
                    Console.WriteLine(Program.T($"[-] 分配 Board.GetSun 洞穴失败: {e.Message}", $"[-] Failed to allocate getsun cave: {e.Message}"));
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
                    Console.WriteLine(Program.T($"[-] 分配 Board.UseSun 洞穴失败: {e.Message}", $"[-] Failed to allocate usesun cave: {e.Message}"));
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

        public PlacementFeature() : base(
            "3",
            "任意种植与重叠融合", "Free Planting & Overlap",
            "解除地形限制，可水面重叠种植，兼容植物自动融合", "Plant anywhere including water/roof, overlapping compatible plants fuses them"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _chkboxAddr = (IntPtr)((long)baseAddress + 0x85A4F0);
            _skipAddr = (IntPtr)((long)baseAddress + 0x863A5C);
            _failAddr = (IntPtr)((long)baseAddress + 0x863A9D);

            try
            {
                byte[] v1 = pm.ReadBytes(_chkboxAddr, 3);
                byte[] v2 = pm.ReadBytes(_skipAddr, 6);
                byte[] v3 = pm.ReadBytes(_failAddr, 6);

                byte[] orig1 = { 0x48, 0x8B, 0xC4 };
                byte[] orig2 = { 0x0F, 0x85, 0xE0, 0x00, 0x00, 0x00 };
                byte[] orig3 = { 0x0F, 0x84, 0x26, 0xFF, 0xFF, 0xFF };

                for (int i = 0; i < 3; i++)
                {
                    if (v1[i] != orig1[i])
                    {
                        Console.WriteLine(Program.T("[-] 种植合法检测点验证失败", "[-] Placement check validation failed"));
                        return false;
                    }
                }

                for (int i = 0; i < 6; i++)
                {
                    if (v2[i] != orig2[i] || v3[i] != orig3[i])
                    {
                        Console.WriteLine(Program.T("[-] 种植判断跳转点验证失败", "[-] Placement logic jump validation failed"));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取种植控制数据失败: {e.Message}", $"[-] Failed to read placement control data: {e.Message}"));
                return false;
            }

            // Patch bytes
            byte[] patch1 = { 0xB0, 0x01, 0xC3 }; // mov al, 1; ret
            byte[] patch2 = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 }; // NOPs
            byte[] patch3 = { 0x0F, 0x84, 0x9F, 0x00, 0x00, 0x00 }; // Redirect jump to normal placement

            byte[] orig1V = { 0x48, 0x8B, 0xC4 };
            byte[] orig2V = { 0x0F, 0x85, 0xE0, 0x00, 0x00, 0x00 };
            byte[] orig3V = { 0x0F, 0x84, 0x26, 0xFF, 0xFF, 0xFF };

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

        public InvincibleFeature() : base(
            "4",
            "植物无敌", "Invincible Plants",
            "植物免疫啃食/秒杀/碾压/落水，铲除与爆炸仍可销毁", "Plants ignore chewing and instant kills; shovel and explosions still work"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _takedamageAddr = (IntPtr)((long)baseAddress + 0x3F51D0);
            _dieAddr = (IntPtr)((long)baseAddress + 0x3EE9C0);

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
                        Console.WriteLine(Program.T("[-] 植物受伤害保护点验证失败", "[-] Plant damage protection verification failed"));
                        return false;
                    }
                }

                for (int i = 0; i < 14; i++)
                {
                    if (v2[i] != orig2[i])
                    {
                        Console.WriteLine(Program.T("[-] 植物死亡判定点验证失败", "[-] Plant death determination verification failed"));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取植物无敌控制数据失败: {e.Message}", $"[-] Failed to read plant invincibility control data: {e.Message}"));
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
                    Console.WriteLine(Program.T($"[-] 分配植物死亡判定洞穴失败: {e.Message}", $"[-] Failed to allocate death determination cave: {e.Message}"));
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
        private IntPtr _applyAddr = IntPtr.Zero;
        private IntPtr _caveAddr = IntPtr.Zero;
        private IntPtr _applyCave = IntPtr.Zero;

        public OneHitKillFeature() : base(
            "5",
            "僵尸一击必杀", "One-Hit Kill Zombies",
            "所有僵尸受任何伤害即死", "All zombies die immediately upon taking any damage"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            // TakeDamage prologue is 10 bytes; ApplyDamage prologue is 9 bytes
            _targetAddr = (IntPtr)((long)baseAddress + 0x5A0BD0);
            _applyAddr = (IntPtr)((long)baseAddress + 0x592900);
            byte[] origBytes = { 0x40, 0x56, 0x41, 0x56, 0x41, 0x57, 0x48, 0x83, 0xEC, 0x60 };
            byte[] origApply = { 0x40, 0x53, 0x57, 0x41, 0x56, 0x48, 0x83, 0xEC, 0x30 };

            try
            {
                byte[] verify = pm.ReadBytes(_targetAddr, 10);
                byte[] verifyApply = pm.ReadBytes(_applyAddr, 9);

                for (int i = 0; i < 10; i++)
                {
                    if (verify[i] != origBytes[i])
                    {
                        Console.WriteLine(Program.T($"[-] 僵尸伤害函数验证失败 @ 0x{_targetAddr.ToInt64():X}", $"[-] Zombie damage function verification failed @ 0x{_targetAddr.ToInt64():X}"));
                        return false;
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    if (verifyApply[i] != origApply[i])
                    {
                        Console.WriteLine(Program.T($"[-] 僵尸伤害结算函数验证失败 @ 0x{_applyAddr.ToInt64():X}", $"[-] Zombie ApplyDamage verification failed @ 0x{_applyAddr.ToInt64():X}"));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取僵尸伤害接口失败: {e.Message}", $"[-] Failed to read zombie damage function: {e.Message}"));
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(48, _targetAddr);
                    Caves.Add(_caveAddr);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Program.T($"[-] 分配僵尸伤害劫持洞穴失败: {e.Message}", $"[-] Failed to allocate zombie damage cave: {e.Message}"));
                    return false;
                }

                // mov edx, 999999 + original 10 bytes + jmp back
                byte[] caveCode = new byte[20];
                byte[] movEdx = { 0xBA, 0x40, 0x42, 0x0F, 0x00 };
                Array.Copy(movEdx, 0, caveCode, 0, 5);
                Array.Copy(origBytes, 0, caveCode, 5, 10);
                byte[] jmpCode = MakeJmp((IntPtr)((long)_caveAddr + 15), (IntPtr)((long)_targetAddr + 10));
                Array.Copy(jmpCode, 0, caveCode, 15, 5);
                pm.WriteBytes(_caveAddr, caveCode);
            }

            if (_applyCave == IntPtr.Zero)
            {
                try
                {
                    _applyCave = pm.GetCave(32, _applyAddr);
                    Caves.Add(_applyCave);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Program.T($"[-] 分配 ApplyDamage 洞穴失败: {e.Message}", $"[-] Failed to allocate ApplyDamage cave: {e.Message}"));
                    return false;
                }

                // mov r8d, 999999 + original 9 bytes + jmp back
                byte[] applyCode = new byte[20];
                byte[] movR8d = { 0x41, 0xB8, 0x40, 0x42, 0x0F, 0x00 };
                Array.Copy(movR8d, 0, applyCode, 0, 6);
                Array.Copy(origApply, 0, applyCode, 6, 9);
                byte[] jmpApply = MakeJmp((IntPtr)((long)_applyCave + 15), (IntPtr)((long)_applyAddr + 9));
                Array.Copy(jmpApply, 0, applyCode, 15, 5);
                pm.WriteBytes(_applyCave, applyCode);
            }

            byte[] patchTd = new byte[10];
            Array.Copy(MakeJmp(_targetAddr, _caveAddr), 0, patchTd, 0, 5);
            for (int i = 5; i < 10; i++) patchTd[i] = 0x90;

            byte[] patchAp = new byte[9];
            Array.Copy(MakeJmp(_applyAddr, _applyCave), 0, patchAp, 0, 5);
            for (int i = 5; i < 9; i++) patchAp[i] = 0x90;

            Patches.Add(new PatchRecord(_targetAddr, origBytes, patchTd));
            Patches.Add(new PatchRecord(_applyAddr, origApply, patchAp));

            pm.WriteBytes(_targetAddr, patchTd);
            pm.WriteBytes(_applyAddr, patchAp);
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

        public CooldownFeature CooldownFeature = new CooldownFeature();

        public AccelerateFeature() : base(
            "6",
            "特定植物状态加速", "Specific Plant Speedup",
            "大嘴花咀嚼与土豆地雷准备等时间加速 20 倍 (保持动画)", "Chomper chewing and Potato Mine arming run 20x faster (animations preserved)"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            if (Enabled) return true;

            _chewHookAddr = (IntPtr)((long)baseAddress + 0x3F2E88);
            _riseHookAddr = (IntPtr)((long)baseAddress + 0x412580);

            try
            {
                byte[] v1 = pm.ReadBytes(_chewHookAddr, 8);
                byte[] v2 = pm.ReadBytes(_riseHookAddr, 9);

                byte[] origChew = { 0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00 };
                byte[] origRise = { 0x40, 0x53, 0x48, 0x81, 0xEC, 0x90, 0x00, 0x00, 0x00 };

                for (int i = 0; i < 8; i++)
                {
                    if (v1[i] != origChew[i])
                    {
                        Console.WriteLine(Program.T("[-] 咀嚼加速点验证失败", "[-] Chewing speedup point verification failed"));
                        return false;
                    }
                }

                for (int i = 0; i < 9; i++)
                {
                    if (v2[i] != origRise[i])
                    {
                        Console.WriteLine(Program.T("[-] 准备加速点验证失败", "[-] Arming speedup point verification failed"));
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取加速点数据失败: {e.Message}", $"[-] Failed to read speedup points: {e.Message}"));
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
                    Console.WriteLine(Program.T($"[-] 分配加速代码洞穴失败: {e.Message}", $"[-] Failed to allocate speedup cave: {e.Message}"));
                    return false;
                }

                _chewCaveAddr = _caveAddr;
                _riseCaveAddr = (IntPtr)((long)_caveAddr + 128);

                // Build chew cave with optimized 88-byte hex code (speeds up Chomper chew and all base/fused Potato Mines arming)
                string chewHex = "508B878801000083F817743083F81D742B83F81E742685C075148B878C01000083F80474173DC80000007D10EB00F30F10B74C01000058E900000000F30F10B74C010000F30F59350800000058E90000000090900000A041";
                byte[] chewCode = new byte[chewHex.Length / 2];
                for (int i = 0; i < chewCode.Length; i++)
                {
                    chewCode[i] = Convert.ToByte(chewHex.Substring(i * 2, 2), 16);
                }

                IntPtr backChew = (IntPtr)((long)_chewHookAddr + 8);
                // Overwrite the relative jmp back offsets
                // First jmp back (at offset 55) -> next instruction is at 60
                byte[] disp1 = BitConverter.GetBytes((int)((long)backChew - ((long)_chewCaveAddr + 60)));
                Array.Copy(disp1, 0, chewCode, 56, 4);
                // Second jmp back (at offset 77) -> next instruction is at 82
                byte[] disp2 = BitConverter.GetBytes((int)((long)backChew - ((long)_chewCaveAddr + 82)));
                Array.Copy(disp2, 0, chewCode, 78, 4);

                // Build rise cave
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
    // 通过 Board.Update 持续重写 timeScale，过关后被游戏重置也会自动拉回
    // ============================================================================
    public class SpeedFeature : CheatFeature
    {
        public double Speed { get; private set; } = 1.0;
        private IntPtr _boardUpdateAddr = IntPtr.Zero;
        private IntPtr _caveAddr = IntPtr.Zero;
        private IntPtr _speedFloatAddr = IntPtr.Zero;

        public SpeedFeature() : base(
            "7",
            "自由调节游戏整体速率", "Game Speed Controller",
            "自由调节游戏整体运行速率 (支持加速/减速，过关后保持)", "Adjust global game speed from 0.1x to 10.0x (persists across levels)"
        ) { }

        public string GetSpeedStatusStr()
        {
            if (Enabled && Speed != 1.0)
                return Program.T($"[ 速率: {Speed}x ]", $"[ Speed: {Speed}x ]");
            return Program.T("[ 已关闭 ]", "[  Closed ]");
        }

        public override string OnClick(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            Console.Write(Program.T("\n[*] 输入游戏速度倍率 (范围 0.1 ~ 10.0，输入 1.0 恢复正常): ", "\n[*] Enter game speed (range 0.1 ~ 10.0, enter 1.0 for normal): "));
            string valStr = Console.ReadLine()?.Trim();
            if (double.TryParse(valStr, out double val))
            {
                if (val < 0.1 || val > 10.0)
                    return Program.T("速度倍率超出范围 (0.1 ~ 10.0)", "Speed value out of range (0.1 ~ 10.0)");

                if (Math.Abs(val - 1.0) < 1e-6)
                {
                    if (Disable(pm, baseAddress, modifier))
                        return Program.T("游戏速度已恢复为 1.0x", "Game speed restored to 1.0x");
                    return Program.T("设置游戏速度失败，请确认已在关卡内", "Failed to set speed, please make sure you are in a level");
                }

                if (SetSpeed(pm, baseAddress, modifier, val))
                    return Program.T($"游戏速度已设为 {val}x", $"Game speed set to {val}x");
                return Program.T("设置游戏速度失败，请确认已在关卡内", "Failed to set speed, please make sure you are in a level");
            }
            return Program.T("输入无效，必须是数字", "Invalid input, must be a number");
        }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            return SetSpeed(pm, baseAddress, modifier, Speed != 1.0 ? Speed : 2.0);
        }

        public override bool Disable(NativeMemory pm, IntPtr baseAddress, Program modifier)
        {
            base.Disable(pm, baseAddress, modifier);
            Speed = 1.0;
            Enabled = false;
            OneShotSetTimeScale(pm, baseAddress, 1.0);
            return true;
        }

        public bool SetSpeed(NativeMemory pm, IntPtr baseAddress, Program modifier, double speed)
        {
            _boardUpdateAddr = (IntPtr)((long)baseAddress + 0x899A20);
            IntPtr setTs = (IntPtr)((long)baseAddress + 0x1E7C290);
            byte[] orig = { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x40 };

            try
            {
                if (Patches.Count == 0)
                {
                    byte[] verify = pm.ReadBytes(_boardUpdateAddr, 6);
                    for (int i = 0; i < 6; i++)
                    {
                        if (verify[i] != orig[i])
                        {
                            Console.WriteLine(Program.T("[-] Board.Update 字节验证失败", "[-] Board.Update verification failed"));
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Program.T($"[-] 读取 Board.Update 失败: {e.Message}", $"[-] Failed to read Board.Update: {e.Message}"));
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(128, _boardUpdateAddr);
                    Caves.Add(_caveAddr);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Program.T($"[-] 分配速率洞穴失败: {e.Message}", $"[-] Failed to allocate speed cave: {e.Message}"));
                    return false;
                }

                List<byte> code = new List<byte>();
                code.AddRange(new byte[] { 0x51 });                         // push rcx
                code.AddRange(new byte[] { 0x52 });                         // push rdx
                code.AddRange(new byte[] { 0x41, 0x50 });                   // push r8
                code.AddRange(new byte[] { 0x41, 0x51 });                   // push r9
                code.AddRange(new byte[] { 0x41, 0x52 });                   // push r10
                code.AddRange(new byte[] { 0x41, 0x53 });                   // push r11
                code.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x28 });       // sub rsp, 28h
                int movssOff = code.Count;
                code.AddRange(new byte[] { 0xF3, 0x0F, 0x10, 0x05, 0x00, 0x00, 0x00, 0x00 }); // movss xmm0,[rip+disp]
                code.AddRange(new byte[] { 0x48, 0xB8 });
                code.AddRange(BitConverter.GetBytes(setTs.ToInt64()));      // mov rax, set_timeScale
                code.AddRange(new byte[] { 0xFF, 0xD0 });                   // call rax
                code.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x28 });       // add rsp, 28h
                code.AddRange(new byte[] { 0x41, 0x5B });                   // pop r11
                code.AddRange(new byte[] { 0x41, 0x5A });                   // pop r10
                code.AddRange(new byte[] { 0x41, 0x59 });                   // pop r9
                code.AddRange(new byte[] { 0x41, 0x58 });                   // pop r8
                code.AddRange(new byte[] { 0x5A });                         // pop rdx
                code.AddRange(new byte[] { 0x59 });                         // pop rcx
                code.AddRange(orig);                                        // original Board.Update head
                code.AddRange(MakeJmp((IntPtr)((long)_caveAddr + code.Count), (IntPtr)((long)_boardUpdateAddr + 6)));

                while (code.Count % 4 != 0) code.Add(0x90);
                int floatOff = code.Count;
                code.AddRange(BitConverter.GetBytes((float)speed));

                int disp = floatOff - (movssOff + 8);
                byte[] dispBytes = BitConverter.GetBytes(disp);
                code[movssOff + 4] = dispBytes[0];
                code[movssOff + 5] = dispBytes[1];
                code[movssOff + 6] = dispBytes[2];
                code[movssOff + 7] = dispBytes[3];

                _speedFloatAddr = (IntPtr)((long)_caveAddr + floatOff);
                pm.WriteBytes(_caveAddr, code.ToArray());
            }
            else if (_speedFloatAddr != IntPtr.Zero)
            {
                pm.WriteBytes(_speedFloatAddr, BitConverter.GetBytes((float)speed));
            }

            if (Patches.Count == 0)
            {
                byte[] patch = new byte[6];
                Array.Copy(MakeJmp(_boardUpdateAddr, _caveAddr), 0, patch, 0, 5);
                patch[5] = 0x90;
                Patches.Add(new PatchRecord(_boardUpdateAddr, orig, patch));
                pm.WriteBytes(_boardUpdateAddr, patch);
            }

            Speed = speed;
            Enabled = (speed != 1.0);
            return true;
        }

        private static bool OneShotSetTimeScale(NativeMemory pm, IntPtr baseAddress, double speed)
        {
            IntPtr setTimeScaleAddr = (IntPtr)((long)baseAddress + 0x1E7C290);
            try
            {
                IntPtr cave = pm.Allocate(64);
                if (cave == IntPtr.Zero) return false;

                byte[] shellcode = new byte[40];
                byte[] header = {
                    0x48, 0x83, 0xEC, 0x28,
                    0xF3, 0x0F, 0x10, 0x05, 0x14, 0x00, 0x00, 0x00,
                    0x48, 0xB8
                };
                Array.Copy(header, 0, shellcode, 0, header.Length);
                Array.Copy(BitConverter.GetBytes(setTimeScaleAddr.ToInt64()), 0, shellcode, 14, 8);
                byte[] tail = { 0xFF, 0xD0, 0x48, 0x83, 0xC4, 0x28, 0xC3, 0x90, 0x90, 0x90 };
                Array.Copy(tail, 0, shellcode, 22, tail.Length);
                Array.Copy(BitConverter.GetBytes((float)speed), 0, shellcode, 32, 4);
                pm.WriteBytes(cave, shellcode);

                if (pm.StartThread(cave, out IntPtr threadHandle))
                {
                    NativeMemory.WaitForSingleObject(threadHandle, 500);
                    NativeMemory.CloseHandle(threadHandle);
                }
                pm.Free(cave);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // ============================================================================
    // 主修改器管理控制台
    // ============================================================================
    public class Program
    {
        public static bool IsEnglish { get; set; } = false;

        public static string T(string zh, string en)
        {
            return IsEnglish ? en : zh;
        }

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
                Console.WriteLine(T("[-] 授权失败。请手动以管理员身份运行此程序。", "[-] Elevation failed. Please run this program as Administrator manually."));
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
                Console.WriteLine(T($"[+] 已附加游戏进程，GameAssembly.dll 基址: 0x{_baseAddress.ToInt64():X}", $"[+] Attached to game process. GameAssembly.dll base: 0x{_baseAddress.ToInt64():X}"));
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
            Console.WriteLine(T("\n[*] 还原所有内存补丁并清理资源...", "\n[*] Restoring all memory patches and cleaning resources..."));
            foreach (var f in _features)
            {
                if (f.Enabled)
                {
                    f.Disable(_pm, _baseAddress, this);
                }
                f.Cleanup(_pm);
            }
            Console.WriteLine(T("[+] 所有修改点已还原", "[+] All patches restored"));
        }

        public void RunLoop()
        {
            string lastErr = "";
            while (true)
            {
                if (!_pm.IsGameRunning())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(T("\n[-] 游戏进程已退出，执行清理...", "\n[-] Game process exited. Cleaning up..."));
                    Console.ResetColor();
                    break;
                }

                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=========================================================================================");
                Console.WriteLine(T($"        PVZ Fusion 3.8.1 修改器 C# 原生版 (双语) - 已附加进程 PID: {_pm.GameProcess.Id}", 
                                    $"        PVZ Fusion 3.8.1 Trainer C# Native (Bilingual) - Attached PID: {_pm.GameProcess.Id}"));
                Console.WriteLine("=========================================================================================");
                Console.ResetColor();

                foreach (var f in _features)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write($" [{f.Key}] ");
                    Console.ResetColor();

                    string nameStr = f.Name.PadRight(Program.IsEnglish ? 30 : 26);
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
                Console.WriteLine(T(" [A] 开启全部功能", " [A] Enable all features"));
                Console.WriteLine(T(" [R] 还原所有修改", " [R] Restore all patches"));
                Console.WriteLine(T(" [Q] 还原并退出修改器", " [Q] Restore and Exit"));
                Console.WriteLine(T($" [L] 切换语言 / Switch Language (当前: {(IsEnglish ? "EN" : "CN")})", $" [L] Switch Language / 切换语言 (Current: {(IsEnglish ? "EN" : "CN")})"));
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

                Console.Write(T("请输入选项序号 (例如 1) 并按 Enter: ", "Please enter option number (e.g. 1) and press Enter: "));
                string choice = Console.ReadLine()?.Trim().ToUpper();

                if (choice == "Q") break;
                else if (choice == "L")
                {
                    IsEnglish = !IsEnglish;
                    Console.Title = T("PVZ Fusion 3.8.1 修改器 C# 原生版", "PVZ Fusion 3.8.1 Trainer C# Native");
                    lastErr = T("已切换语言为：中文", "Language switched to: English");
                }
                else if (choice == "R")
                {
                    Console.WriteLine(T("\n[*] 重置所有补丁...", "\n[*] Resetting all patches..."));
                    foreach (var f in _features)
                    {
                        if (f.Enabled) f.Disable(_pm, _baseAddress, this);
                    }
                    lastErr = T("所有补丁已重置", "All patches reset");
                }
                else if (choice == "A")
                {
                    Console.WriteLine(T("\n[*] 开启全部功能...", "\n[*] Enabling all features..."));
                    int success = 0;
                    foreach (var f in _features)
                    {
                        if (f.Key == "7") continue; // Skip speed regulation
                        if (!f.Enabled)
                        {
                            Console.WriteLine(T($"[*] 开启 {f.Name} ...", $"[*] Enabling {f.Name} ..."));
                            if (f.Enable(_pm, _baseAddress, this))
                            {
                                success++;
                                Console.WriteLine(T($"[+] {f.Name} 已开启", $"[+] {f.Name} enabled"));
                            }
                            else
                            {
                                Console.WriteLine(T($"[-] {f.Name} 开启失败", $"[-] {f.Name} failed to enable"));
                            }
                        }
                    }
                    lastErr = T($"全部开启完成，新激活 {success} 项功能。", $"All features enabled. Newly activated: {success}.");
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
                        lastErr = T("无效的指令，请重新输入", "Invalid command, please re-enter");
                    }
                }
            }
        }

        public static void Main(string[] args)
        {
            // Auto-detect OS language on startup
            string cultName = CultureInfo.CurrentUICulture.Name;
            IsEnglish = !cultName.StartsWith("zh", StringComparison.OrdinalIgnoreCase);

            // Configure Console properties
            Console.Title = T("PVZ Fusion 3.8.1 修改器 C# 原生版", "PVZ Fusion 3.8.1 Trainer C# Native");
            
            Program program = new Program();

            // Attempt initial attachment
            string status = program.AttachGame();

            if (status == "ACCESS_DENIED")
            {
                if (!IsAdmin())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(T("[!] 权限不足，请求 UAC 提升...", "[!] Insufficient privileges, requesting UAC elevation..."));
                    Console.ResetColor();
                    Thread.Sleep(1000);
                    ElevateUac();
                    return;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(T("[-] 已是管理员但仍无法打开游戏进程，可能被安全软件拦截。", "[-] Running as Admin but still cannot open game process. May be blocked by security software."));
                    Console.ResetColor();
                    Console.WriteLine(T("\n按任意键退出...", "\nPress any key to exit..."));
                    Console.ReadLine();
                    return;
                }
            }
            else if (status == "NOT_RUNNING")
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(T("[-] 游戏未运行，先启动 PlantsVsZombiesRH.exe 并进入关卡。", "[-] Game not running. Start PlantsVsZombiesRH.exe and enter a level first."));
                Console.ResetColor();
                Console.WriteLine(T("\n按任意键退出...", "\nPress any key to exit..."));
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
                Console.WriteLine(T($"[-] 修改器异常: {e.Message}", $"[-] Trainer exception: {e.Message}"));
                Console.ResetColor();
                Console.WriteLine(T("\n按任意键退出...", "\nPress any key to exit..."));
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
