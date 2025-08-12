// FastFileEnumerator.cs
// This class uses low-level Windows API calls (P/Invoke) to list files
// and folders with high performance, similar to Windows File Explorer.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.ComponentModel;

namespace FileFlow
{
    public static class FastFileEnumerator
    {
        /// <summary>
        /// Enumerates files and directories in a given path, yielding them one by one.
        /// </summary>
        public static IEnumerable<FileItem> Enumerate(string path)
        {
            var findData = new WIN32_FIND_DATA();
            var findHandle = FindFirstFile(Path.Combine(path, "*"), findData);

            if (findHandle.IsInvalid)
            {
                yield break;
            }

            try
            {
                do
                {
                    if (findData.cFileName == "." || findData.cFileName == "..")
                    {
                        continue;
                    }

                    var isDirectory = (findData.dwFileAttributes & FileAttributes.Directory) != 0;
                    var fullPath = Path.Combine(path, findData.cFileName);

                    yield return new FileItem
                    {
                        FileName = findData.cFileName,
                        FullPath = fullPath,
                        IsFolder = isDirectory,
                        Type = isDirectory ? "Folder" : "File"
                    };

                } while (FindNextFile(findHandle, findData));
            }
            finally
            {
                findHandle.Dispose();
            }
        }

        #region P/Invoke Definitions

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class WIN32_FIND_DATA
        {
            public FileAttributes dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName = "";
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName = "";
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstFile(string lpFileName, [In, Out] WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool FindNextFile(SafeFindHandle hFindFile, [In, Out] WIN32_FIND_DATA lpFindFileData);

        private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeFindHandle() : base(true) { }

            protected override bool ReleaseHandle()
            {
                return FindClose(handle);
            }

            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern bool FindClose(IntPtr hFindFile);
        }

        #endregion
    }
}
