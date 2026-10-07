using System.Diagnostics;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using C = pvz_fusion_cheats_cs;
using G = pvz_fusion_cheats_wpf;

// A minimal localization stub lets the GUI patch code run without opening WPF.
namespace pvz_fusion_cheats_wpf
{
    public static class MainWindow
    {
        public static bool IsEnglish => true;
        public static string T(string zh, string en) => en;
    }
}

internal static class TrainerVerification
{
    private static readonly (int Rva, int Length)[] Targets =
    [
        (0x957550, 6), (0x72DD30, 6), (0x9FFAC4, 6), (0xA0D401, 6),
        (0x9D8850, 5), (0x9DF552, 6), (0x9DF593, 6),
        (0x46EAC0, 7), (0x467D50, 14), (0x65D080, 7), (0x64E280, 9),
        (0x46C48B, 9), (0x4865B0, 9), (0xA0CD30, 6)
    ];
    private static readonly List<object> Captured = [];

    public static void Main(string[] args)
    {
        if (args.Length < 1) throw new ArgumentException("Pass the supported GameAssembly.dll path.");
        Check(PvzFusionTrainer.GameBuild.IsSupported(args[0]), "supported build hash");
        Check(!PvzFusionTrainer.GameBuild.IsSupported(Environment.ProcessPath!), "reject unrelated binary");
        using var reader = new PEReader(File.OpenRead(args[0]));
        var headers = reader.PEHeaders;
        byte[] image = new byte[headers.PEHeader!.SizeOfImage];
        foreach (var section in headers.SectionHeaders)
        {
            reader.GetEntireImage().GetContent(section.PointerToRawData, section.SizeOfRawData)
                .CopyTo(image, section.VirtualAddress);
        }
        TestBackend(typeof(C.NativeMemory), typeof(C.CheatFeature), image, "console");
        TestBackend(typeof(G.NativeMemory), typeof(G.CheatFeature), image, "gui");
        if (args.Length > 1) File.WriteAllText(args[1], JsonSerializer.Serialize(Captured));
        Console.WriteLine("PASS: both backends; seven features; byte guards; simultaneous enable; reuse; exact restoration; speed reset.");
    }

    private static void TestBackend(Type memoryType, Type featureType, byte[] image, string backend)
    {
        // Copy the actual PE into disposable executable memory in this test process.
        // No game process is started, attached to, or modified by these tests.
        using var pm = (IDisposable)Activator.CreateInstance(memoryType)!;
        Set(pm, "ProcessHandle", C.NativeMemory.OpenProcess(C.NativeMemory.PROCESS_ALL_ACCESS, false, Environment.ProcessId));
        Set(pm, "GameProcess", Process.GetCurrentProcess());
        IntPtr baseAddress = C.NativeMemory.VirtualAllocEx(Get<IntPtr>(pm, "ProcessHandle"), IntPtr.Zero,
            (uint)image.Length, C.NativeMemory.MEM_COMMIT | C.NativeMemory.MEM_RESERVE, C.NativeMemory.PAGE_EXECUTE_READWRITE);
        Check(baseAddress != IntPtr.Zero, "allocate test image");
        Set(pm, "BaseAddress", baseAddress);
        Set(pm, "ModuleSize", image.Length);
        Marshal.Copy(image, 0, baseAddress, image.Length);
        // Time.set_timeScale is replaced with a test stub that records xmm0.
        // This prevents executing Unity code when Disable runs its remote thread.
        const int setter = 0x26C5460, recordedFloat = setter + 0x100;
        byte[] stub = [0xF3, 0x0F, 0x11, 0x05, .. BitConverter.GetBytes(recordedFloat - (setter + 8)), 0xC3];
        Marshal.Copy(stub, 0, baseAddress + setter, stub.Length);
        object[] features = new[] { "CooldownFeature", "SunFeature", "PlacementFeature", "InvincibleFeature",
            "OneHitKillFeature", "AccelerateFeature", "SpeedFeature" }
            .Select(n => Activator.CreateInstance(featureType.Assembly.GetType(featureType.Namespace + "." + n)!)!).ToArray();
        var modifier = backend == "console" ? new C.Program() : null;
        bool Toggle(object feature, string method) => (bool)feature.GetType().GetMethod(method)!.Invoke(feature,
            backend == "console" ? [pm, baseAddress, modifier] : [pm, baseAddress])!;
        var heldCaves = new HashSet<long>();
        try
        {
            // A mismatched entry must fail before committing any patch.
            Marshal.WriteByte(baseAddress + Targets[0].Rva, 0xCC);
            Check(!Toggle(features[0], "Enable"), backend + " rejects changed entry");
            Marshal.WriteByte(baseAddress + Targets[0].Rva, image[Targets[0].Rva]);
            for (int cycle = 0; cycle < 2; cycle++)
            {
                foreach (object feature in features)
                {
                    Check(Toggle(feature, "Enable"), backend + " enable " + feature.GetType().Name);
                    Check(Get<bool>(feature, "Enabled"), "feature enabled state");
                    var patches = (System.Collections.IEnumerable)featureType.GetField("Patches", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(feature)!;
                    foreach (object patch in patches)
                    {
                        Type pt = patch.GetType();
                        IntPtr address = (IntPtr)pt.GetField("Address")!.GetValue(patch)!;
                        byte[] original = (byte[])pt.GetField("OriginalBytes")!.GetValue(patch)!;
                        byte[] patched = (byte[])pt.GetField("PatchedBytes")!.GetValue(patch)!;
                        int rva = checked((int)(address.ToInt64() - baseAddress.ToInt64()));
                        Check(Targets.Contains((rva, original.Length)), "known patch boundary");
                        Check(original.AsSpan().SequenceEqual(image.AsSpan(rva, original.Length)), "original bytes match game PE");
                        byte[] actual = Read(address, patched.Length);
                        Check(actual.SequenceEqual(patched), "patch bytes written");
                        if (actual[0] == 0xE9 && cycle == 0)
                        {
                            IntPtr cave = address + 5 + BitConverter.ToInt32(actual, 1);
                            Captured.Add(new { backend, feature = feature.GetType().Name, rva,
                                baseAddress = baseAddress.ToInt64(), address = cave.ToInt64(), bytes = Convert.ToHexString(Read(cave, 256)) });
                        }
                    }
                    var caves = (List<IntPtr>)featureType.GetField("Caves", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(feature)!;
                    foreach (var cave in caves)
                    {
                        if (cycle == 0) Check(heldCaves.Add(cave.ToInt64()), "distinct code caves");
                        else Check(heldCaves.Contains(cave.ToInt64()), "reuse code caves");
                    }
                }
                Check(Read(baseAddress + 0x9DF593, 6).SequenceEqual(Convert.FromHexString("0F849F000000")), "failed fusion jumps to normal planting");
                foreach (object feature in features.Reverse()) Check(Toggle(feature, "Disable"), backend + " disable");
                foreach (var target in Targets)
                    Check(Read(baseAddress + target.Rva, target.Length).AsSpan().SequenceEqual(image.AsSpan(target.Rva, target.Length)), "restore every original byte");
                Check(BitConverter.ToSingle(Read(baseAddress + recordedFloat, 4)) == 1.0f, "timeScale reset to 1.0");
            }
        }
        finally
        {
            foreach (object feature in features) feature.GetType().GetMethod("Cleanup")!.Invoke(feature, [pm]);
            C.NativeMemory.VirtualFreeEx(Get<IntPtr>(pm, "ProcessHandle"), baseAddress, 0, C.NativeMemory.MEM_RELEASE);
        }
    }

    private static byte[] Read(IntPtr address, int length)
    {
        byte[] bytes = new byte[length];
        Marshal.Copy(address, bytes, 0, length);
        return bytes;
    }
    private static T Get<T>(object obj, string property) => (T)obj.GetType().GetProperty(property)!.GetValue(obj)!;
    private static void Set(object obj, string property, object value) => obj.GetType().GetProperty(property)!.SetValue(obj, value);
    private static void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException("FAIL: " + description);
    }
}
