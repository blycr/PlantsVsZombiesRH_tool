using System;
using System.Collections.Generic;

namespace pvz_fusion_cheats_wpf
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
        public string Name => MainWindow.IsEnglish ? NameEn : NameZh;
        public string Description => MainWindow.IsEnglish ? DescriptionEn : DescriptionZh;
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
            return Enabled ? MainWindow.T("[ 已激活 ]", "[  Active ]") : MainWindow.T("[ 已关闭 ]", "[  Closed ]");
        }

        public abstract bool Enable(NativeMemory pm, IntPtr baseAddress);

        public virtual bool Disable(NativeMemory pm, IntPtr baseAddress)
        {
            if (Patches.Count == 0)
            {
                Enabled = false;
                return true;
            }

            for (int i = Patches.Count - 1; i >= 0; i--)
            {
                var patch = Patches[i];
                try
                {
                    pm.WriteBytes(patch.Address, patch.OriginalBytes);
                }
                catch
                {
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
            long offsetLong = (long)to - ((long)from + 5);
            if (offsetLong > int.MaxValue || offsetLong < int.MinValue)
                throw new InvalidOperationException($"Jump too far for rel32: 0x{from.ToInt64():X} -> 0x{to.ToInt64():X}");
            byte[] code = new byte[5];
            code[0] = 0xE9;
            Array.Copy(BitConverter.GetBytes((int)offsetLong), 0, code, 1, 4);
            return code;
        }

        protected static byte[] MakeCall(IntPtr from, IntPtr to)
        {
            long offsetLong = (long)to - ((long)from + 5);
            if (offsetLong > int.MaxValue || offsetLong < int.MinValue)
                throw new InvalidOperationException($"Call too far for rel32: 0x{from.ToInt64():X} -> 0x{to.ToInt64():X}");
            byte[] code = new byte[5];
            code[0] = 0xE8;
            Array.Copy(BitConverter.GetBytes((int)offsetLong), 0, code, 1, 4);
            return code;
        }

        /// <summary>Write all patches; on failure attempt to restore any successful writes in reverse.</summary>
        protected bool CommitWrites(NativeMemory pm, params (IntPtr Address, byte[] Data)[] writes)
        {
            var done = new List<(IntPtr Address, byte[] Original)>();
            try
            {
                foreach (var (address, data) in writes)
                {
                    byte[] original = pm.ReadBytes(address, data.Length);
                    if (!pm.WriteBytes(address, data))
                        throw new InvalidOperationException($"WriteProcessMemory failed at 0x{address.ToInt64():X}");
                    done.Add((address, original));
                }
                return true;
            }
            catch
            {
                for (int i = done.Count - 1; i >= 0; i--)
                {
                    try { pm.WriteBytes(done[i].Address, done[i].Original); } catch { }
                }
                return false;
            }
        }
    }

    // ============================================================================
    // 功能 1：极速冷却 ×100
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

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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
                    if (v1[i] != origCard[i] || v2[i] != origTool[i]) return false;
                }
            }
            catch
            {
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
                catch
                {
                    return false;
                }

                List<byte> cardCode = new List<byte>();
                // mov eax, [rcx+0x48] (8B 41 48)
                cardCode.AddRange(new byte[] { 0x8B, 0x41, 0x48 });
                // mov [rcx+0x44], eax (89 41 44)
                cardCode.AddRange(new byte[] { 0x89, 0x41, 0x44 });
                // push rbx; sub rsp, 40h (40 53 48 83 EC 40)
                cardCode.AddRange(new byte[] { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x30 });
                // jmp back
                IntPtr backCard = (IntPtr)((long)_cardCdAddr + 6);
                cardCode.AddRange(MakeJmp((IntPtr)((long)_cardCave + cardCode.Count), backCard));

                if (!pm.WriteBytes(_cardCave, cardCode.ToArray())) return false;
            }

            // Allocate InGameTool cave
            if (_toolCave == IntPtr.Zero)
            {
                try
                {
                    _toolCave = pm.GetCave(32, _toolCdAddr);
                    Caves.Add(_toolCave);
                }
                catch
                {
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

                if (!pm.WriteBytes(_toolCave, toolCode.ToArray())) return false;
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

            if (!CommitWrites(pm, (_cardCdAddr, patchCard), (_toolCdAddr, patchTool)))


                return false;


            Patches.Add(new PatchRecord(_cardCdAddr, origCard, patchCard));


            Patches.Add(new PatchRecord(_toolCdAddr, origTool, patchTool));
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
            "捡阳光或种植时，阳光都会 100 倍增加", "Picking up or consuming sun increases sun by 100x"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
        {
            if (Enabled) return true;

            if (_getSunAddr == IntPtr.Zero)
            {
                byte[] getsunPattern = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
                _getSunAddr = pm.FindPattern(getsunPattern, 0x88C000, 0x88D000);
                if (_getSunAddr == IntPtr.Zero) return false;
            }

            if (_useSunAddr == IntPtr.Zero)
            {
                byte[] usesunPattern = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 };
                _useSunAddr = pm.FindPattern(usesunPattern, 0x89A000, 0x89B000);
                if (_useSunAddr == IntPtr.Zero) return false;
            }

            try
            {
                byte[] v1 = pm.ReadBytes(_getSunAddr, 6);
                byte[] v2 = pm.ReadBytes(_useSunAddr, 6);
                byte[] origGet = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
                byte[] origUse = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 };

                for (int i = 0; i < 6; i++)
                {
                    if (v1[i] != origGet[i] || v2[i] != origUse[i]) return false;
                }
            }
            catch
            {
                return false;
            }

            if (_cave1Addr == IntPtr.Zero)
            {
                try
                {
                    _cave1Addr = pm.GetCave(14, _getSunAddr);
                    Caves.Add(_cave1Addr);
                }
                catch
                {
                    return false;
                }

                byte[] caveCode1 = new byte[14];
                byte[] instr = { 0x6B, 0xC0, 0x64, 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
                Array.Copy(instr, 0, caveCode1, 0, 9);
                byte[] jmp = MakeJmp((IntPtr)((long)_cave1Addr + 9), (IntPtr)((long)_getSunAddr + 6));
                Array.Copy(jmp, 0, caveCode1, 9, 5);

                if (!pm.WriteBytes(_cave1Addr, caveCode1)) return false;
            }

            if (_cave2Addr == IntPtr.Zero)
            {
                try
                {
                    _cave2Addr = pm.GetCave(14, _useSunAddr);
                    Caves.Add(_cave2Addr);
                }
                catch
                {
                    return false;
                }

                byte[] caveCode2 = new byte[14];
                byte[] instr = { 0x6B, 0xC0, 0x64, 0x01, 0x83, 0x08, 0x01, 0x00, 0x00 };
                Array.Copy(instr, 0, caveCode2, 0, 9);
                byte[] jmp = MakeJmp((IntPtr)((long)_cave2Addr + 9), (IntPtr)((long)_useSunAddr + 6));
                Array.Copy(jmp, 0, caveCode2, 9, 5);

                if (!pm.WriteBytes(_cave2Addr, caveCode2)) return false;
            }

            byte[] patchGet = new byte[6];
            byte[] jmpGet = MakeJmp(_getSunAddr, _cave1Addr);
            Array.Copy(jmpGet, 0, patchGet, 0, 5);
            patchGet[5] = 0x90;

            byte[] patchUse = new byte[6];
            byte[] jmpUse = MakeJmp(_useSunAddr, _cave2Addr);
            Array.Copy(jmpUse, 0, patchUse, 0, 5);
            patchUse[5] = 0x90;

            byte[] origGetVerify = { 0x01, 0x86, 0x08, 0x01, 0x00, 0x00 };
            byte[] origUseVerify = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 };

            if (!CommitWrites(pm, (_getSunAddr, patchGet), (_useSunAddr, patchUse)))


                return false;


            Patches.Add(new PatchRecord(_getSunAddr, origGetVerify, patchGet));


            Patches.Add(new PatchRecord(_useSunAddr, origUseVerify, patchUse));
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
            "解除地形限制，可在水面重叠种植，保留植物自动融合", "Plant anywhere including water/roof, overlapping compatible plants fuses them"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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
                    if (v1[i] != orig1[i]) return false;
                }

                for (int i = 0; i < 6; i++)
                {
                    if (v2[i] != orig2[i] || v3[i] != orig3[i]) return false;
                }
            }
            catch
            {
                return false;
            }

            byte[] patch1 = { 0xB0, 0x01, 0xC3 };
            byte[] patch2 = { 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
            byte[] patch3 = { 0x0F, 0x84, 0x9F, 0x00, 0x00, 0x00 };

            byte[] orig1V = { 0x48, 0x8B, 0xC4 };
            byte[] orig2V = { 0x0F, 0x85, 0xE0, 0x00, 0x00, 0x00 };
            byte[] orig3V = { 0x0F, 0x84, 0x26, 0xFF, 0xFF, 0xFF };

            if (!CommitWrites(pm, (_chkboxAddr, patch1), (_skipAddr, patch2), (_failAddr, patch3)))


                return false;


            Patches.Add(new PatchRecord(_chkboxAddr, orig1V, patch1));


            Patches.Add(new PatchRecord(_skipAddr, orig2V, patch2));


            Patches.Add(new PatchRecord(_failAddr, orig3V, patch3));
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
            "植物免疫啃食/秒杀/碾压/落水，不影响铲除与爆炸自毁", "Plants immune to chewing/instant kills, shovel-up & explosions still destroy them"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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
                    if (v1[i] != orig1[i]) return false;
                }

                for (int i = 0; i < 14; i++)
                {
                    if (v2[i] != origDieVerify[i]) return false; // wait, using helper variable
                }
            }
            catch
            {
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(25, _dieAddr);
                    Caves.Add(_caveAddr);
                }
                catch
                {
                    return false;
                }

                byte[] caveCode = new byte[25];
                byte[] cmpInstruction = { 0x83, 0xFA, 0x0B, 0x7D, 0x13 };
                byte[] origDie = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };
                
                Array.Copy(cmpInstruction, 0, caveCode, 0, 5);
                Array.Copy(origDie, 0, caveCode, 5, 14);

                IntPtr jmpBackDest = (IntPtr)((long)_dieAddr + 14);
                byte[] jmpCode = MakeJmp((IntPtr)((long)_caveAddr + 19), jmpBackDest);
                Array.Copy(jmpCode, 0, caveCode, 19, 5);
                caveCode[24] = 0xC3;

                if (!pm.WriteBytes(_caveAddr, caveCode)) return false;
            }

            byte[] patchTakedamage = { 0xC3, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
            
            byte[] patchDie = new byte[14];
            byte[] jmpToCave = MakeJmp(_dieAddr, _caveAddr);
            Array.Copy(jmpToCave, 0, patchDie, 0, 5);
            for (int i = 5; i < 14; i++) patchDie[i] = 0x90;

            byte[] origTakedamageVerify = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x10 };
            byte[] origDieVerify2 = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };

            if (!CommitWrites(pm, (_takedamageAddr, patchTakedamage), (_dieAddr, patchDie)))


                return false;


            Patches.Add(new PatchRecord(_takedamageAddr, origTakedamageVerify, patchTakedamage));


            Patches.Add(new PatchRecord(_dieAddr, origDieVerify2, patchDie));
            Enabled = true;
            return true;
        }

        private static readonly byte[] origDieVerify = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };
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
            "僵尸受到任何伤害立即死亡", "All zombies die immediately upon taking any damage"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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
                    if (verify[i] != origBytes[i]) return false;
                for (int i = 0; i < 9; i++)
                    if (verifyApply[i] != origApply[i]) return false;
            }
            catch
            {
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(48, _targetAddr);
                    Caves.Add(_caveAddr);
                }
                catch
                {
                    return false;
                }

                byte[] caveCode = new byte[20];
                byte[] movEdx = { 0xBA, 0x40, 0x42, 0x0F, 0x00 };
                Array.Copy(movEdx, 0, caveCode, 0, 5);
                Array.Copy(origBytes, 0, caveCode, 5, 10);
                Array.Copy(MakeJmp((IntPtr)((long)_caveAddr + 15), (IntPtr)((long)_targetAddr + 10)), 0, caveCode, 15, 5);
                if (!pm.WriteBytes(_caveAddr, caveCode)) return false;
            }

            if (_applyCave == IntPtr.Zero)
            {
                try
                {
                    _applyCave = pm.GetCave(32, _applyAddr);
                    Caves.Add(_applyCave);
                }
                catch
                {
                    return false;
                }

                byte[] applyCode = new byte[20];
                byte[] movR8d = { 0x41, 0xB8, 0x40, 0x42, 0x0F, 0x00 };
                Array.Copy(movR8d, 0, applyCode, 0, 6);
                Array.Copy(origApply, 0, applyCode, 6, 9);
                Array.Copy(MakeJmp((IntPtr)((long)_applyCave + 15), (IntPtr)((long)_applyAddr + 9)), 0, applyCode, 15, 5);
                if (!pm.WriteBytes(_applyCave, applyCode)) return false;
            }

            byte[] patchTd = new byte[10];
            Array.Copy(MakeJmp(_targetAddr, _caveAddr), 0, patchTd, 0, 5);
            for (int i = 5; i < 10; i++) patchTd[i] = 0x90;

            byte[] patchAp = new byte[9];
            Array.Copy(MakeJmp(_applyAddr, _applyCave), 0, patchAp, 0, 5);
            for (int i = 5; i < 9; i++) patchAp[i] = 0x90;

            if (!CommitWrites(pm, (_targetAddr, patchTd), (_applyAddr, patchAp)))


                return false;


            Patches.Add(new PatchRecord(_targetAddr, origBytes, patchTd));


            Patches.Add(new PatchRecord(_applyAddr, origApply, patchAp));
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

        public AccelerateFeature() : base(
            "6",
            "特定植物状态加速", "Specific Plant Speedup",
            "大嘴花咀嚼与土豆地雷准备等状态加速 20 倍（非瞬爆，保留正常动作）", "Chomper chewing and Potato Mine arming runs 20x faster, retaining animations"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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
                    if (v1[i] != origChew[i]) return false;
                }

                for (int i = 0; i < 9; i++)
                {
                    if (v2[i] != origRise[i]) return false;
                }
            }
            catch
            {
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(256, _chewHookAddr);
                    Caves.Add(_caveAddr);
                }
                catch
                {
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

                if (!pm.WriteBytes(_chewCaveAddr, chewCode)) return false;
                if (!pm.WriteBytes(_riseCaveAddr, riseCode)) return false;
            }

            byte[] patchChew = new byte[8];
            byte[] jmpChew = MakeJmp(_chewHookAddr, _chewCaveAddr);
            Array.Copy(jmpChew, 0, patchChew, 0, 5);
            patchChew[5] = 0x90; patchChew[6] = 0x90; patchChew[7] = 0x90;

            byte[] patchRise = new byte[9];
            byte[] jmpRiseHook = MakeJmp(_riseHookAddr, _riseCaveAddr);
            Array.Copy(jmpRiseHook, 0, patchRise, 0, 5);
            for (int i = 5; i < 9; i++) patchRise[i] = 0x90;

            byte[] origChewVerify = { 0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00 };
            byte[] origRiseVerify = { 0x40, 0x53, 0x48, 0x81, 0xEC, 0x90, 0x00, 0x00, 0x00 };

            if (!CommitWrites(pm, (_chewHookAddr, patchChew), (_riseHookAddr, patchRise)))


                return false;


            Patches.Add(new PatchRecord(_chewHookAddr, origChewVerify, patchChew));


            Patches.Add(new PatchRecord(_riseHookAddr, origRiseVerify, patchRise));
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
            "游戏速率调节", "Game Speed Controller",
            "调节游戏整体运行速率（支持加速/减速，过关后保持）", "Adjust global game speed from 0.1x to 10.0x (persists across levels)"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
        {
            return SetSpeed(pm, baseAddress, Speed != 1.0 ? Speed : 2.0);
        }

        public override bool Disable(NativeMemory pm, IntPtr baseAddress)
        {
            base.Disable(pm, baseAddress);
            Speed = 1.0;
            Enabled = false;
            OneShotSetTimeScale(pm, baseAddress, 1.0);
            return true;
        }

        public bool SetSpeed(NativeMemory pm, IntPtr baseAddress, double speed)
        {
            // 设为 1.0 时卸 hook 并立刻还原
            if (Math.Abs(speed - 1.0) < 1e-6 && Patches.Count > 0)
            {
                return Disable(pm, baseAddress);
            }

            _boardUpdateAddr = (IntPtr)((long)baseAddress + 0x899A20);
            IntPtr setTs = (IntPtr)((long)baseAddress + 0x1E7C290);
            byte[] orig = { 0x40, 0x53, 0x48, 0x83, 0xEC, 0x40 };

            try
            {
                if (Patches.Count == 0)
                {
                    byte[] verify = pm.ReadBytes(_boardUpdateAddr, 6);
                    for (int i = 0; i < 6; i++)
                        if (verify[i] != orig[i]) return false;
                }
            }
            catch
            {
                return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(128, _boardUpdateAddr);
                    Caves.Add(_caveAddr);
                }
                catch
                {
                    return false;
                }

                List<byte> code = new List<byte>();
                code.AddRange(new byte[] { 0x51 });
                code.AddRange(new byte[] { 0x52 });
                code.AddRange(new byte[] { 0x41, 0x50 });
                code.AddRange(new byte[] { 0x41, 0x51 });
                code.AddRange(new byte[] { 0x41, 0x52 });
                code.AddRange(new byte[] { 0x41, 0x53 });
                code.AddRange(new byte[] { 0x48, 0x83, 0xEC, 0x28 });
                int movssOff = code.Count;
                code.AddRange(new byte[] { 0xF3, 0x0F, 0x10, 0x05, 0x00, 0x00, 0x00, 0x00 });
                code.AddRange(new byte[] { 0x48, 0xB8 });
                code.AddRange(BitConverter.GetBytes(setTs.ToInt64()));
                code.AddRange(new byte[] { 0xFF, 0xD0 });
                code.AddRange(new byte[] { 0x48, 0x83, 0xC4, 0x28 });
                code.AddRange(new byte[] { 0x41, 0x5B });
                code.AddRange(new byte[] { 0x41, 0x5A });
                code.AddRange(new byte[] { 0x41, 0x59 });
                code.AddRange(new byte[] { 0x41, 0x58 });
                code.AddRange(new byte[] { 0x5A });
                code.AddRange(new byte[] { 0x59 });
                code.AddRange(orig);
                code.AddRange(MakeJmp((IntPtr)((long)_caveAddr + code.Count), (IntPtr)((long)_boardUpdateAddr + 6)));

                while (code.Count % 4 != 0) code.Add(0x90);
                int floatOff = code.Count;
                code.AddRange(BitConverter.GetBytes((float)speed));

                int disp = floatOff - (movssOff + 8);
                byte[] dispBytes = BitConverter.GetBytes(disp);
                for (int i = 0; i < 4; i++) code[movssOff + 4 + i] = dispBytes[i];

                _speedFloatAddr = (IntPtr)((long)_caveAddr + floatOff);
                if (!pm.WriteBytes(_caveAddr, code.ToArray())) return false;
            }
            else if (_speedFloatAddr != IntPtr.Zero)
            {
                if (!pm.WriteBytes(_speedFloatAddr, BitConverter.GetBytes((float)speed))) return false;
            }

            if (Patches.Count == 0)
            {
                byte[] patch = new byte[6];
                Array.Copy(MakeJmp(_boardUpdateAddr, _caveAddr), 0, patch, 0, 5);
                patch[5] = 0x90;
                if (!CommitWrites(pm, (_boardUpdateAddr, patch)))
                    return false;
                Patches.Add(new PatchRecord(_boardUpdateAddr, orig, patch));
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
                if (!pm.WriteBytes(cave, shellcode)) return false;

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
}
