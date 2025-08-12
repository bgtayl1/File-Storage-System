// ApplicationState.cs
// This class now remembers the last used path and content indexed paths.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows; // Required for Application.Current

namespace FileFlow
{
    public class ApplicationState : INotifyPropertyChanged
    {
        public event EventHandler? ProjectsLoaded;
        private CancellationTokenSource? _loadCts;

        // A simple struct to hold progress updates for the stats calculation.
        private struct StatProgress
        {
            public long FileCount;
            public long TotalSize;
        }

        private string _projectFolderPath;

        public string ProjectFolderPath
        {
            get => _projectFolderPath;
            set
            {
                if (_projectFolderPath != value)
                {
                    _projectFolderPath = value;
                    OnPropertyChanged(nameof(ProjectFolderPath));
                    AppSettings.SaveLastPath(_projectFolderPath); // Save the new path
                    _ = SyncAndLoadProjectsAsync();
                }
            }
        }

        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();
        public List<string> IndexedPaths { get; set; } = new List<string>();


        public int TotalProjects { get; private set; }
        public long FilesManaged { get; private set; }
        public double StorageUsedGB { get; private set; }
        public string? StatusMessage { get; private set; }
        public bool IsLoading { get; private set; }

        public ApplicationState()
        {
            // Load the last used path on startup.
            _projectFolderPath = AppSettings.LoadLastPath();
            IndexedPaths = AppSettings.LoadIndexedPaths();
            SearchIndexManager.LoadIndex();
            this.ProjectsLoaded += OnProjectsLoadedForIndexing;
            _ = SyncAndLoadProjectsAsync();
        }

        private async void OnProjectsLoadedForIndexing(object? sender, EventArgs e)
        {
            // Wait for 10 seconds before starting the background index build
            await Task.Delay(10000);
            _ = SearchIndexManager.BuildIndexAsync(this.Projects, this.IndexedPaths, new Progress<double>(), CancellationToken.None);
        }

        public async Task SyncAndLoadProjectsAsync()
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var cancellationToken = _loadCts.Token;

            IsLoading = true;
            OnPropertyChanged(nameof(IsLoading));
            StatusMessage = "Loading projects...";
            OnPropertyChanged(nameof(StatusMessage));

            // Step 1: Load projects from the local cache.
            var cachedProjects = AppSettings.LoadProjectsCache();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Projects.Clear();
                foreach (var project in cachedProjects)
                {
                    if (string.IsNullOrEmpty(project.FullPath) && !string.IsNullOrEmpty(project.ProjectName))
                    {
                        project.FullPath = Path.Combine(ProjectFolderPath, project.ProjectName);
                    }
                    Projects.Add(project);
                }
            });

            // Step 2: Start the background sync with the network.
            try
            {
                await Task.Run(async () =>
                {
                    if (!Directory.Exists(ProjectFolderPath))
                    {
                        StatusMessage = "The specified folder does not exist.";
                        return;
                    }

                    StatusMessage = "Syncing with network...";
                    OnPropertyChanged(nameof(StatusMessage));

                    var liveProjectDirs = new DirectoryInfo(ProjectFolderPath).GetDirectories()
                        .Where(d => !d.Attributes.HasFlag(FileAttributes.Hidden) && !d.Attributes.HasFlag(FileAttributes.System))
                        .ToDictionary(d => d.Name, d => d.FullName);

                    var cachedProjectNames = new HashSet<string>(Projects.Select(p => p.ProjectName ?? ""));
                    var liveProjectNames = new HashSet<string>(liveProjectDirs.Keys);

                    // Find and add new projects
                    var newProjectNames = liveProjectNames.Except(cachedProjectNames);
                    foreach (var name in newProjectNames)
                    {
                        if (cancellationToken.IsCancellationRequested) return;
                        Application.Current.Dispatcher.Invoke(() => Projects.Add(new Project { ProjectName = name, FullPath = liveProjectDirs[name] }));
                    }

                    // Find and remove old projects
                    var removedProjectNames = cachedProjectNames.Except(liveProjectNames).ToList();
                    if (removedProjectNames.Any())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            foreach (var name in removedProjectNames)
                            {
                                var projectToRemove = Projects.FirstOrDefault(p => p.ProjectName == name);
                                if (projectToRemove != null) Projects.Remove(projectToRemove);
                            }
                        });
                    }

                    AppSettings.SaveProjectsCache(Projects);

                    StatusMessage = "Calculating statistics...";
                    OnPropertyChanged(nameof(StatusMessage));
                    await CalculateDirectoryStatsAsync(ProjectFolderPath, cancellationToken);

                }, cancellationToken);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsLoading = false;
                    StatusMessage = null;
                    OnPropertyChanged(nameof(IsLoading));
                    OnPropertyChanged(nameof(StatusMessage));
                    ProjectsLoaded?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private async Task CalculateDirectoryStatsAsync(string path, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(path)) return;

            // Explicitly reset stats to 0 before starting the scan.
            TotalProjects = Projects.Count;
            FilesManaged = 0;
            StorageUsedGB = 0;
            OnPropertyChanged(nameof(TotalProjects));
            OnPropertyChanged(nameof(FilesManaged));
            OnPropertyChanged(nameof(StorageUsedGB));

            // This progress reporter will receive updates from the background thread
            // and update the properties on the UI thread.
            IProgress<StatProgress> progress = new Progress<StatProgress>(p =>
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    FilesManaged = p.FileCount;
                    StorageUsedGB = Math.Round(p.TotalSize / (1024.0 * 1024.0 * 1024.0), 3);
                    OnPropertyChanged(nameof(FilesManaged));
                    OnPropertyChanged(nameof(StorageUsedGB));
                }
            });

            await Task.Run(() =>
            {
                long fileCount = 0;
                long totalSize = 0;
                var foldersToScan = new Queue<DirectoryInfo>();
                foldersToScan.Enqueue(new DirectoryInfo(path));

                while (foldersToScan.Count > 0)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    var currentDir = foldersToScan.Dequeue();
                    try
                    {
                        foreach (FileInfo file in currentDir.GetFiles())
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            fileCount++;
                            totalSize += file.Length;
                        }

                        // Report progress after scanning each folder.
                        progress.Report(new StatProgress { FileCount = fileCount, TotalSize = totalSize });

                        foreach (DirectoryInfo subDir in currentDir.GetDirectories())
                        {
                            if (cancellationToken.IsCancellationRequested) return;
                            if (!subDir.Attributes.HasFlag(FileAttributes.Hidden) && !subDir.Attributes.HasFlag(FileAttributes.System))
                            {
                                foldersToScan.Enqueue(subDir);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException) { continue; }
                }

            }, cancellationToken);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            // Check if the current thread has access to the UI.
            if (Application.Current?.Dispatcher.CheckAccess() ?? false)
            {
                // If it does, raise the event directly.
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            else
            {
                // If it doesn't, schedule the event to be raised on the UI thread.
                Application.Current?.Dispatcher.Invoke(() =>
                {
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                });
            }
        }
    }
}