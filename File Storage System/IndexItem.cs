// IndexItem.cs
// This class represents a single file or folder entry in our local search index.

namespace FileFlow
{
    public class IndexItem
    {
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public bool IsFolder { get; set; }
        public string? ProjectName { get; set; }
    }
}