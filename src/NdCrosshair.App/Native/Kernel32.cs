using System.Runtime.InteropServices;

namespace NdCrosshair.App.Native;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct PROCESSENTRY32W
{
    public uint DwSize;
    public uint CntUsage;
    public uint Th32ProcessID;
    public nuint Th32DefaultHeapID;
    public uint Th32ModuleID;
    public uint CntThreads;
    public uint Th32ParentProcessID;
    public int PcPriClassBase;
    public uint DwFlags;
    public fixed char SzExeFile[260];
}

internal static unsafe partial class Kernel32
{
    public const uint TH32CS_SNAPPROCESS = 0x00000002;
    public static readonly nint INVALID_HANDLE_VALUE = -1;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial nint CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [LibraryImport("kernel32.dll", EntryPoint = "Process32FirstW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Process32First(nint hSnapshot, PROCESSENTRY32W* lppe);

    [LibraryImport("kernel32.dll", EntryPoint = "Process32NextW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool Process32Next(nint hSnapshot, PROCESSENTRY32W* lppe);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CloseHandle(nint hObject);
}
