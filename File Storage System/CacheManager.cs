// CacheManager.cs
// This class provides a centralized, static cache for folder contents and statistics.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FileFlow
{
    public static class CacheManager
    {
        private static readonly Dictionary<string, List<FileItem>> _folderContentsCache = new();
        private static readonly Dictionary<string, (int TotalFolders, long TotalFiles, double StorageUsedGB)> _folderStatsCache = new();

        public static List<FileItem>? GetFolderContents(string path)
        {
            _folderContentsCache.TryGetValue(path, out var items);
            return items;
        }

        public static (int, long, double)? GetFolderStats(string path)
        {
            _folderStatsCache.TryGetValue(path, out var stats);
            return stats == default ? null : stats;
        }

        public static void AddFolderContents(string path, List<FileItem> items)
        {
            _folderContentsCache[path] = items;
        }

        public static void AddFolderStats(string path, (int, long, double) stats)
        {
            _folderStatsCache[path] = stats;
        }

        /// <summary>
        /// Scans all subdirectories of a root path and populates the cache, allowing for cancellation.
        /// </summary>
        public static async Task PreCacheAllAsync(string rootPath, IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                try
                {
                    var foldersToScan = new Queue<string>();
                    foldersToScan.Enqueue(rootPath);

                    var initialDirectories = Directory.GetDirectories(rootPath);
                    int total = initialDirectories.Length;
                    int processed = 0;

                    while (foldersToScan.Count > 0)
                    {
                        // Check for cancellation at the start of each loop.
                        cancellationToken.ThrowIfCancellationRequested();

                        var currentDir = foldersToScan.Dequeue();
                        var items = new List<FileItem>();
                        try
                        {
                            var subdirectories = Directory.GetDirectories(currentDir);
                            foreach (var subDir in subdirectories)
                            {
                                items.Add(new FileItem { FileName = Path.GetFileName(subDir), FullPath = subDir, IsFolder = true, Type = "Folder" });
                                foldersToScan.Enqueue(subDir);
                            }

                            foreach (var file in Directory.EnumerateFiles(currentDir))
                            {
                                items.Add(new FileItem { FileName = Path.GetFileName(file), FullPath = file, IsFolder = false, Type = "File" });
                            }
                            AddFolderContents(currentDir, items);
                        }
                        catch (UnauthorizedAccessException) { /* Skip inaccessible folders */ }

                        processed++;
                        if (total > 0)
                        {
                            progress.Report((double)processed / total * 100);
                        }
                    }
                }
                catch (UnauthorizedAccessException) { /* Can't access the root folder */ }
                // The OperationCanceledException will be caught by the calling method.
            }, cancellationToken);
        }
    }
}


