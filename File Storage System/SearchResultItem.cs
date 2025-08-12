// SearchResultItem.cs
// This class represents a single item in the search results list.
// It can be either a project or a specific file found within a project.

namespace FileFlow
{
    public class SearchResultItem
    {
        // Properties of the found file or folder
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public bool IsFolder { get; set; }
        public string? Type { get; set; }
        public string? LastModified { get; set; }


        // Properties of the project the file belongs to
        public string? ProjectName { get; set; }
        public string? ProjectCode { get; set; }


        // A reference to the original project object for navigation
        public Project? OriginalProject { get; set; }

        // A new property to provide a consistent name for display and searching.
        public string DisplayName => FileName ?? ProjectName ?? "";
    }
}

