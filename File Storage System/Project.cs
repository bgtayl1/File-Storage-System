// Project.cs
// This class defines the data structure for a single project.

namespace FileFlow;

public class Project
{
    public string? ProjectName { get; set; }
    public string? ProjectCode { get; set; }
    public string? Client { get; set; }
    public string? Status { get; set; }
    public string? LastUpdate { get; set; }
    public string? FullPath { get; set; } // Added to store the full folder path
}
