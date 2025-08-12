// SearchIndexManager.cs
// Handles building, saving, loading, and querying a high-speed local search index.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FileFlow
{
    public static class SearchIndexManager
    {
        // The main list of all items.
        private static List<IndexItem> _allItems = new List<IndexItem>();
        // The high-speed inverted index. The key is a word/token, the value is a list of indices pointing to _allItems.
        private static Dictionary<string, List<int>> _invertedIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _indexLock = new object();

        private static readonly string AppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        private static readonly string AppFolder = Path.Combine(AppDataPath, "FileFlowMFG");
        private static readonly string IndexFile = Path.Combine(AppFolder, "search_index_v2.json");

        public static bool IsIndexLoaded { get; private set; } = false;

        /// <summary>
        /// Loads the search index from a local file on application startup.
        /// </summary>
        public static void LoadIndex()
        {
            try
            {
                if (File.Exists(IndexFile))
                {
                    var json = File.ReadAllText(IndexFile);
                    var loadedData = JsonSerializer.Deserialize<Tuple<List<IndexItem>, Dictionary<string, List<int>>>>(json);
                    if (loadedData != null)
                    {
                        _allItems = loadedData.Item1;
                        _invertedIndex = loadedData.Item2;
                        IsIndexLoaded = _allItems.Any();
                    }
                }
            }
            catch (Exception) { /* Silently fail if the index can't be loaded */ }
        }

        /// <summary>
        /// Saves the current in-memory index to a local file.
        /// </summary>
        private static void SaveIndex(List<IndexItem> items, Dictionary<string, List<int>> invertedIndex)
        {
            try
            {
                Directory.CreateDirectory(AppFolder);
                var dataToSave = new Tuple<List<IndexItem>, Dictionary<string, List<int>>>(items, invertedIndex);
                var json = JsonSerializer.Serialize(dataToSave);
                File.WriteAllText(IndexFile, json);
            }
            catch (Exception) { /* Silently fail if the index can't be saved */ }
        }

        /// <summary>
        /// Scans all project folders and builds a new search index.
        /// </summary>
        public static async Task BuildIndexAsync(IEnumerable<Project> projects, List<string> indexedPaths, IProgress<double> progress, CancellationToken cancellationToken)
        {
            await Task.Run(async () =>
            {
                // Build the new index in temporary collections to avoid disrupting ongoing searches.
                var newAllItems = new List<IndexItem>();
                var newInvertedIndex = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

                var projectList = projects.ToList();
                int totalProjects = projectList.Count;
                int projectsProcessed = 0;

                foreach (var project in projectList)
                {
                    if (cancellationToken.IsCancellationRequested) break;
                    if (string.IsNullOrEmpty(project.FullPath)) continue;

                    var foldersToScan = new Queue<string>();
                    foldersToScan.Enqueue(project.FullPath);

                    while (foldersToScan.Count > 0)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        var currentPath = foldersToScan.Dequeue();

                        foreach (var item in FastFileEnumerator.Enumerate(currentPath))
                        {
                            if (cancellationToken.IsCancellationRequested) break;

                            int itemIndex = newAllItems.Count;
                            newAllItems.Add(new IndexItem
                            {
                                FileName = item.FileName,
                                FilePath = item.FullPath,
                                IsFolder = item.IsFolder,
                                ProjectName = project.ProjectName ?? "Unknown"
                            });

                            // Tokenize the file name (as before)
                            var tokens = Tokenize(item.FileName);

                            // If it's a file in a specified path, get and tokenize its content
                            if (!item.IsFolder && item.FullPath != null && indexedPaths.Any(p => item.FullPath.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                            {
                                var content = FileContentExtractor.GetTextContent(item.FullPath);
                                if (!string.IsNullOrEmpty(content))
                                {
                                    tokens.AddRange(Tokenize(content));
                                }
                            }

                            // Add all unique tokens to the inverted index
                            foreach (var token in tokens.Distinct(StringComparer.OrdinalIgnoreCase))
                            {
                                if (!newInvertedIndex.ContainsKey(token))
                                {
                                    newInvertedIndex[token] = new List<int>();
                                }
                                newInvertedIndex[token].Add(itemIndex);
                            }

                            if (item.IsFolder && item.FullPath != null)
                            {
                                foldersToScan.Enqueue(item.FullPath);
                            }

                            // Throttle the loop to be less resource-intensive
                            await Task.Delay(1, cancellationToken);
                        }
                    }
                    projectsProcessed++;
                    progress.Report((double)projectsProcessed / totalProjects * 100);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    // Atomically swap the new index with the old one.
                    lock (_indexLock)
                    {
                        _allItems = newAllItems;
                        _invertedIndex = newInvertedIndex;
                        IsIndexLoaded = true;
                    }
                    SaveIndex(_allItems, _invertedIndex);
                }

            }, cancellationToken);
        }

        /// <summary>
        /// Performs a fast, in-memory search of the loaded index.
        /// </summary>
        public static List<IndexItem> Search(string searchTerm)
        {
            if (!IsIndexLoaded || string.IsNullOrWhiteSpace(searchTerm))
            {
                return new List<IndexItem>();
            }

            var finalResults = new List<IndexItem>();
            lock (_indexLock) // Ensure thread safety while searching.
            {
                var searchTokens = Tokenize(searchTerm);
                if (!searchTokens.Any())
                {
                    return new List<IndexItem>();
                }

                // Find the set of document indices for each search token
                var resultSets = new List<HashSet<int>>();
                foreach (var token in searchTokens)
                {
                    var matchingKeys = _invertedIndex.Keys
                        .Where(k => k.StartsWith(token, StringComparison.OrdinalIgnoreCase));

                    var indicesForToken = new HashSet<int>();
                    foreach (var key in matchingKeys)
                    {
                        foreach (var index in _invertedIndex[key])
                        {
                            indicesForToken.Add(index);
                        }
                    }

                    if (indicesForToken.Any())
                    {
                        resultSets.Add(indicesForToken);
                    }
                }

                if (!resultSets.Any())
                {
                    return new List<IndexItem>();
                }

                // Find the intersection of all sets (documents that contain ALL search tokens)
                var finalIndices = resultSets.First();
                foreach (var set in resultSets.Skip(1))
                {
                    finalIndices.IntersectWith(set);
                }

                foreach (var index in finalIndices)
                {
                    finalResults.Add(_allItems[index]);
                }
            }
            return finalResults;
        }

        private static List<string> Tokenize(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<string>();
            }
            return text.Split(new[] { ' ', '-', '_', '.', ',', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}