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

        public CooldownFeature() : base(
            "1",
            "即时冷却 ×100", "Instant Cooldown x100",
            "卡牌和手套冷却立即完成", "Seed packets, gloves, and hammers cool down instantly"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
        {
            if (Enabled) return true;

            _targetAddr = (IntPtr)((long)baseAddress + 0x7A3519);

            try
            {
                byte[] verify = pm.ReadBytes(_targetAddr, 5);
                byte[] origBytes = { 0xE8, 0x52, 0x23, 0x4F, 0x01 }; // call GameAssembly.dll + 0x1C95870
                for (int i = 0; i < verify.Length; i++)
                {
                    if (verify[i] != origBytes[i]) return false;
                }
            }
            catch
            {
                return false;
            }

            if (_float100Addr == IntPtr.Zero)
            {
                _float100Addr = pm.FindFloat100();
                if (_float100Addr == IntPtr.Zero) return false;
            }

            if (_caveAddr == IntPtr.Zero)
            {
                try
                {
                    _caveAddr = pm.GetCave(18, _targetAddr);
                    Caves.Add(_caveAddr);
                }
                catch
                {
                    return false;
                }

                // Compile cave bytes
                IntPtr callTarget = (IntPtr)((long)baseAddress + 0x1C95870);
                byte[] callCode = MakeCall(_caveAddr, callTarget);

                byte[] mulCode = { 0xF3, 0x0F, 0x59, 0x05, 0x00, 0x00, 0x00, 0x00 };
                int mulOffset = (int)((long)_float100Addr - ((long)_caveAddr + 5 + 8));
                Array.Copy(BitConverter.GetBytes(mulOffset), 0, mulCode, 4, 4);

                IntPtr backAddr = (IntPtr)((long)_targetAddr + 5);
                byte[] jmpCode = MakeJmp((IntPtr)((long)_caveAddr + 13), backAddr);

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
                _getSunAddr = pm.FindPattern(getsunPattern, 0x7DAF00, 0x7DCF00);
                if (_getSunAddr == IntPtr.Zero) return false;
            }

            if (_useSunAddr == IntPtr.Zero)
            {
                byte[] usesunPattern = { 0x29, 0x83, 0x08, 0x01, 0x00, 0x00 };
                _useSunAddr = pm.FindPattern(usesunPattern, 0x7E8100, 0x7EA100);
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

                pm.WriteBytes(_cave1Addr, caveCode1);
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

                pm.WriteBytes(_cave2Addr, caveCode2);
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
            "解除地形限制，可在水面重叠种植，保留植物自动融合", "Plant anywhere including water/roof, overlapping compatible plants fuses them"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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
            byte[] patch3 = { 0x0F, 0x84, 0x9E, 0x00, 0x00, 0x00 };

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

        public InvincibleFeature() : base(
            "4",
            "植物无敌", "Invincible Plants",
            "植物免疫啃食/秒杀/碾压/落水，不影响铲除与爆炸自毁", "Plants immune to chewing/instant kills, shovel-up & explosions still destroy them"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
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

                pm.WriteBytes(_caveAddr, caveCode);
            }

            byte[] patchTakedamage = { 0xC3, 0x90, 0x90, 0x90, 0x90, 0x90, 0x90 };
            
            byte[] patchDie = new byte[14];
            byte[] jmpToCave = MakeJmp(_dieAddr, _caveAddr);
            Array.Copy(jmpToCave, 0, patchDie, 0, 5);
            for (int i = 5; i < 14; i++) patchDie[i] = 0x90;

            byte[] origTakedamageVerify = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x10 };
            byte[] origDieVerify2 = { 0x48, 0x8B, 0xC4, 0x48, 0x89, 0x58, 0x18, 0x89, 0x50, 0x10, 0x48, 0x89, 0x48, 0x08 };

            Patches.Add(new PatchRecord(_takedamageAddr, origTakedamageVerify, patchTakedamage));
            Patches.Add(new PatchRecord(_dieAddr, origDieVerify2, patchDie));

            pm.WriteBytes(_takedamageAddr, patchTakedamage);
            pm.WriteBytes(_dieAddr, patchDie);

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
        private IntPtr _caveAddr = IntPtr.Zero;

        public OneHitKillFeature() : base(
            "5",
            "僵尸一击必杀", "One-Hit Kill Zombies",
            "僵尸受到任何伤害立即死亡", "All zombies die immediately upon taking any damage"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
        {
            if (Enabled) return true;

            _targetAddr = (IntPtr)((long)baseAddress + 0x564120);

            try
            {
                byte[] verify = pm.ReadBytes(_targetAddr, 14);
                byte[] origBytes = { 0x40, 0x56, 0x41, 0x54, 0x41, 0x55, 0x41, 0x56, 0x41, 0x57, 0x48, 0x83, 0xEC, 0x60 };

                for (int i = 0; i < 14; i++)
                {
                    if (verify[i] != origBytes[i]) return false;
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
                    _caveAddr = pm.GetCave(32, _targetAddr);
                    Caves.Add(_caveAddr);
                }
                catch
                {
                    return false;
                }

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
            for (int i = 5; i < 14; i++) patchBytes[i] = 0x90;

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

        public AccelerateFeature() : base(
            "6",
            "特定植物状态加速", "Specific Plant Speedup",
            "大嘴花咀嚼与土豆地雷准备等状态加速 20 倍（非瞬爆，保留正常动作）", "Chomper chewing and Potato Mine arming runs 20x faster, retaining animations"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
        {
            if (Enabled) return true;

            _chewHookAddr = (IntPtr)((long)baseAddress + 0x3F00F4);
            _riseHookAddr = (IntPtr)((long)baseAddress + 0x40EE00);

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

                byte[] chewCode = new byte[87];
                byte[] chewHeader = {
                    0x50, 0x8B, 0x87, 0x8C, 0x01, 0x00, 0x00, 0x83, 0xF8, 0x05, 0x74, 0x23,
                    0x3D, 0x62, 0x01, 0x00, 0x00, 0x74, 0x1C, 0x3D, 0x64, 0x01, 0x00, 0x00, 0x74, 0x15,
                    0x3D, 0x70, 0x01, 0x00, 0x00, 0x74, 0x0E, 0x3D, 0x84, 0x03, 0x00, 0x00, 0x7C, 0x1D,
                    0x3D, 0x7D, 0x05, 0x00, 0x00, 0x7F, 0x16,
                    0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00,
                    0xF3, 0x0F, 0x59, 0x35, 0x14, 0x00, 0x00, 0x00,
                    0x58
                };
                Array.Copy(chewHeader, 0, chewCode, 0, chewHeader.Length);
                IntPtr backChew = (IntPtr)((long)_chewHookAddr + 8);
                byte[] jmpChew1 = MakeJmp((IntPtr)((long)_chewCaveAddr + 64), backChew);
                Array.Copy(jmpChew1, 0, chewCode, 64, 5);

                byte[] chewNoAcc = {
                    0xF3, 0x0F, 0x10, 0xB7, 0x4C, 0x01, 0x00, 0x00,
                    0x58
                };
                Array.Copy(chewNoAcc, 0, chewCode, 69, chewNoAcc.Length);
                byte[] jmpChew2 = MakeJmp((IntPtr)((long)_chewCaveAddr + 78), backChew);
                Array.Copy(jmpChew2, 0, chewCode, 78, 5);

                Array.Copy(BitConverter.GetBytes(20.0f), 0, chewCode, 83, 4);

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
            patchChew[5] = 0x90; patchChew[6] = 0x90; patchChew[7] = 0x90;

            byte[] patchRise = new byte[9];
            byte[] jmpRiseHook = MakeJmp(_riseHookAddr, _riseCaveAddr);
            Array.Copy(jmpRiseHook, 0, patchRise, 0, 5);
            for (int i = 5; i < 9; i++) patchRise[i] = 0x90;

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

        public SpeedFeature() : base(
            "7",
            "游戏速率调节", "Game Speed Controller",
            "调节游戏整体运行速率（支持加速/减速，默认 1.0x）", "Adjust global game speed from 0.1x to 10.0x (default 1.0x)"
        ) { }

        public override bool Enable(NativeMemory pm, IntPtr baseAddress)
        {
            return SetSpeed(pm, baseAddress, Speed);
        }

        public override bool Disable(NativeMemory pm, IntPtr baseAddress)
        {
            if (SetSpeed(pm, baseAddress, 1.0))
            {
                Speed = 1.0;
                Enabled = false;
                return true;
            }
            return false;
        }

        public bool SetSpeed(NativeMemory pm, IntPtr baseAddress, double speed)
        {
            IntPtr setTimeScaleAddr = (IntPtr)((long)baseAddress + 0x1C95A90);
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
                
                byte[] tail = {
                    0xFF, 0xD0,
                    0x48, 0x83, 0xC4, 0x28,
                    0xC3,
                    0x90, 0x90, 0x90
                };
                Array.Copy(tail, 0, shellcode, 22, tail.Length);
                Array.Copy(BitConverter.GetBytes((float)speed), 0, shellcode, 32, 4);

                pm.WriteBytes(cave, shellcode);

                if (pm.StartThread(cave, out IntPtr threadHandle))
                {
                    NativeMemory.WaitForSingleObject(threadHandle, 500);
                    NativeMemory.CloseHandle(threadHandle);
                }

                pm.Free(cave);
                Speed = speed;
                Enabled = (speed != 1.0);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
