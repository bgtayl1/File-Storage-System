// FileItem.cs
// This class represents a single file or folder.

namespace FileFlow // <-- This namespace must be 'FileFlow'.
{
    public class FileItem
    {
        public string? FileName { get; set; }
        public string? FullPath { get; set; }
        public bool IsFolder { get; set; }
        public string? Type { get; set; }
        public string? LastModified { get; set; }
    }
}
