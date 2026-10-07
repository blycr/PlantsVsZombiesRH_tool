using System.IO;
using System.Security.Cryptography;

namespace PvzFusionTrainer;

// The offsets and instruction signatures target this exact Windows x64 build.
public static class GameBuild
{
    public const string Version = "4.0.5";
    public const string TrainerVersion = "v10";
    public const string AssemblySha256 = "48096B917EE6AAF6E35C95C98E666A4399A9E0C3D5ADBAB727CEE941ECCF2D42";

    public static bool IsSupported(string assemblyPath)
    {
        using var stream = File.OpenRead(assemblyPath);
        return Convert.ToHexString(SHA256.HashData(stream)) == AssemblySha256;
    }
}
