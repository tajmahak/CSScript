using System;
using System.Runtime.InteropServices;

/// ВЗАИМОДЕЙСТВИЕ С WINDOWS (15.05.2021)

///// #using System;
///// #using System.Runtime.InteropServices;
/////
///// #namespace

// Утилиты для взаимодействия с API Windows
public static class WindowsUtils
{
    // Перемещение файла в корзину Windows
    public static bool MoveFileToRecycleBin(string path, bool silent = false) {
        __FileOperationFlags flags = silent ?
            (__FileOperationFlags.FOF_NOCONFIRMATION | __FileOperationFlags.FOF_NOERRORUI | __FileOperationFlags.FOF_SILENT) :
            (__FileOperationFlags.FOF_NOCONFIRMATION | __FileOperationFlags.FOF_WANTNUKEWARNING);
        try {
            if (IntPtr.Size == 8) {
                __SHFILEOPSTRUCT_x64 fs = new __SHFILEOPSTRUCT_x64 {
                    wFunc = __FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = __FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                __SHFileOperation_x64(ref fs);
            } else {
                __SHFILEOPSTRUCT_x86 fs = new __SHFILEOPSTRUCT_x86 {
                    wFunc = __FileOperationType.FO_DELETE,
                    pFrom = path + '\0' + '\0',
                    fFlags = __FileOperationFlags.FOF_ALLOWUNDO | flags
                };
                __SHFileOperation_x86(ref fs);
            }
            return true;
        } catch {
            return false;
        }
    }


    /// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

    [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation")]
    private static extern int __SHFileOperation_x86(ref __SHFILEOPSTRUCT_x86 FileOp);

    [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "SHFileOperation")]
    private static extern int __SHFileOperation_x64(ref __SHFILEOPSTRUCT_x64 FileOp);
}


/// --- ВНУТРЕННИЕ СУЩНОСТИ (НЕ ИСПОЛЬЗУЮТСЯ НАПРЯМУЮ) ---

internal enum __FileOperationType : uint
{
    FO_MOVE = 0x0001,
    FO_COPY = 0x0002,
    FO_DELETE = 0x0003,
    FO_RENAME = 0x0004,
}

[Flags]
internal enum __FileOperationFlags : ushort
{
    FOF_SILENT = 0x0004,
    FOF_NOCONFIRMATION = 0x0010,
    FOF_ALLOWUNDO = 0x0040,
    FOF_SIMPLEPROGRESS = 0x0100,
    FOF_NOERRORUI = 0x0400,
    FOF_WANTNUKEWARNING = 0x4000,
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 1)]
internal struct __SHFILEOPSTRUCT_x86
{
    public IntPtr hwnd;
    [MarshalAs(UnmanagedType.U4)]
    public __FileOperationType wFunc;
    public string pFrom;
    public string pTo;
    public __FileOperationFlags fFlags;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fAnyOperationsAborted;
    public IntPtr hNameMappings;
    public string lpszProgressTitle;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
internal struct __SHFILEOPSTRUCT_x64
{
    public IntPtr hwnd;
    [MarshalAs(UnmanagedType.U4)]
    public __FileOperationType wFunc;
    public string pFrom;
    public string pTo;
    public __FileOperationFlags fFlags;
    [MarshalAs(UnmanagedType.Bool)]
    public bool fAnyOperationsAborted;
    public IntPtr hNameMappings;
    public string lpszProgressTitle;
}
