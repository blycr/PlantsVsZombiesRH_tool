using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace pvz_fusion_cheats_wpf
{
    public class NativeMemory : IDisposable
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        public const uint PROCESS_ALL_ACCESS = 0x001F0FFF;
        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint PAGE_EXECUTE_READWRITE = 0x40;
        public const uint INFINITE = 0xFFFFFFFF;

        public IntPtr ProcessHandle { get; private set; } = IntPtr.Zero;
        public Process GameProcess { get; private set; } = null;
        public IntPtr BaseAddress { get; private set; } = IntPtr.Zero;
        public int ModuleSize { get; private set; } = 0;

        public bool Attach(string processName, string moduleName)
        {
            Process[] processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0) return false;

            GameProcess = processes[0];
            ProcessHandle = OpenProcess(PROCESS_ALL_ACCESS, false, GameProcess.Id);
            if (ProcessHandle == IntPtr.Zero)
            {
                GameProcess = null!;
                return false;
            }

            try
            {
                foreach (ProcessModule module in GameProcess.Modules)
                {
                    if (module.ModuleName == moduleName)
                    {
                        BaseAddress = module.BaseAddress;
                        ModuleSize = module.ModuleMemorySize;
                        return true;
                    }
                }
            }
            catch
            {
                // Permission denied during module enumeration — fall through to cleanup.
            }

            // OpenProcess succeeded but module was not found / not readable: do not leak the handle.
            CloseHandle(ProcessHandle);
            ProcessHandle = IntPtr.Zero;
            GameProcess = null!;
            BaseAddress = IntPtr.Zero;
            ModuleSize = 0;
            return false;
        }

        public bool IsGameRunning()
        {
            if (ProcessHandle == IntPtr.Zero) return false;
            return GetExitCodeProcess(ProcessHandle, out uint exitCode) && exitCode == 259; // 259 = STILL_ACTIVE
        }

        public byte[] ReadBytes(IntPtr address, int size)
        {
            if (size <= 0)
                throw new ArgumentOutOfRangeException(nameof(size));
            if (ProcessHandle == IntPtr.Zero)
                throw new InvalidOperationException("Not attached to a process.");

            byte[] buffer = new byte[size];
            if (!ReadProcessMemory(ProcessHandle, address, buffer, size, out int bytesRead) || bytesRead != size)
                throw new InvalidOperationException($"ReadProcessMemory failed at 0x{address.ToInt64():X} (wanted {size}, got {bytesRead}).");
            return buffer;
        }

        public bool WriteBytes(IntPtr address, byte[] data)
        {
            if (data == null || data.Length == 0 || ProcessHandle == IntPtr.Zero)
                return false;
            return WriteProcessMemory(ProcessHandle, address, data, data.Length, out int written) && written == data.Length;
        }

        public IntPtr Allocate(uint size, IntPtr preferredAddress = default)
        {
            if (preferredAddress != IntPtr.Zero)
            {
                long baseAddr = (long)preferredAddress;
                long[] offsets = { 0x3000000, 0x4000000, 0x5000000, 0x8000000, -0x1000000, -0x2000000, -0x3000000, -0x4000000 };
                foreach (long offset in offsets)
                {
                    IntPtr target = (IntPtr)(baseAddr + offset);
                    if ((long)target < 0x10000) continue;
                    IntPtr allocated = VirtualAllocEx(ProcessHandle, target, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
                    if (allocated != IntPtr.Zero)
                    {
                        long diff = Math.Abs((long)allocated - baseAddr);
                        if (diff < 0x7F000000) return allocated;
                        VirtualFreeEx(ProcessHandle, allocated, 0, MEM_RELEASE);
                    }
                }
            }
            return VirtualAllocEx(ProcessHandle, IntPtr.Zero, size, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
        }

        public bool Free(IntPtr address)
        {
            return VirtualFreeEx(ProcessHandle, address, 0, MEM_RELEASE);
        }

        public IntPtr FindCodeCave(IntPtr startAddress, int minSize, int scanSize)
        {
            byte[] data = ReadBytes(startAddress, scanSize);
            int currentLen = 0;
            int startIdx = -1;

            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x90 || data[i] == 0xCC)
                {
                    if (startIdx == -1) startIdx = i;
                    currentLen++;
                    if (currentLen >= minSize)
                    {
                        return (IntPtr)((long)startAddress + startIdx);
                    }
                }
                else
                {
                    startIdx = -1;
                    currentLen = 0;
                }
            }
            return IntPtr.Zero;
        }

        public IntPtr GetCave(int size, IntPtr nearAddress)
        {
            long[] scanOffsets = { 0x80, 0x1000, 0x2000, 0x4000, -0x4000, -0x8000 };
            foreach (long offset in scanOffsets)
            {
                IntPtr scanStart = (IntPtr)((long)nearAddress + offset);
                if ((long)scanStart < (long)BaseAddress) continue;
                IntPtr cave = FindCodeCave(scanStart, size, 0x4000);
                if (cave != IntPtr.Zero) return cave;
            }

            IntPtr alloc = Allocate((uint)size, nearAddress);
            if (alloc != IntPtr.Zero) return alloc;

            throw new Exception($"Cannot find or allocate code cave near 0x{nearAddress.ToInt64():X}");
        }

        public IntPtr FindPattern(byte[] pattern, int startOffset, int endOffset)
        {
            int size = endOffset - startOffset;
            byte[] data = ReadBytes((IntPtr)((long)BaseAddress + startOffset), size);
            for (int i = 0; i <= data.Length - pattern.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (data[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return (IntPtr)((long)BaseAddress + startOffset + i);
            }
            return IntPtr.Zero;
        }

        public IntPtr FindFloat100()
        {
            byte[] pattern = { 0x00, 0x00, 0xC8, 0x42 }; // 100.0f
            int chunkSize = 1024 * 1024;
            for (int offset = 0; offset < ModuleSize; offset += chunkSize)
            {
                int readSize = Math.Min(chunkSize + pattern.Length - 1, ModuleSize - offset);
                if (readSize < pattern.Length) break;
                byte[] data = ReadBytes((IntPtr)((long)BaseAddress + offset), readSize);
                for (int i = 0; i <= data.Length - pattern.Length; i++)
                {
                    bool found = true;
                    for (int j = 0; j < pattern.Length; j++)
                    {
                        if (data[i + j] != pattern[j])
                        {
                            found = false;
                            break;
                        }
                    }
                    if (found) return (IntPtr)((long)BaseAddress + offset + i);
                }
            }
            return IntPtr.Zero;
        }

        public bool StartThread(IntPtr address, out IntPtr threadHandle)
        {
            uint threadId;
            threadHandle = CreateRemoteThread(ProcessHandle, IntPtr.Zero, 0, address, IntPtr.Zero, 0, out threadId);
            return threadHandle != IntPtr.Zero;
        }

        public void Dispose()
        {
            if (ProcessHandle != IntPtr.Zero)
            {
                CloseHandle(ProcessHandle);
                ProcessHandle = IntPtr.Zero;
            }
        }
    }
}
