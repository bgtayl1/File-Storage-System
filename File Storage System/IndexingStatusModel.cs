// IndexingStatusModel.cs
using System;

namespace FileFlow
{
    public class IndexingStatusModel
    {
        public bool IsIndexing { get; set; }
        public DateTime? LastIndexedUtc { get; set; }
        public string? StatusMessage { get; set; }
    }
}