// DashboardViewModel.cs
// This is the stable version before the StatItem changes.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Specialized;

namespace FileFlow
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationState _appState;
        private readonly WindowsSearchService _searchService;
        private CancellationTokenSource? _searchCts;

        public ObservableCollection<SearchResultItem> DisplayedResults { get; } = new ObservableCollection<SearchResultItem>();

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _ = FilterProjectsAsync();
            }
        }

        public bool IsSearching { get; private set; }

        public int TotalProjects => _appState.TotalProjects;
        public long FilesManaged => _appState.FilesManaged;
        public double StorageUsedGB => _appState.StorageUsedGB;
        public string? StatusMessage { get; private set; }

        public DashboardViewModel(ApplicationState appState)
        {
            _appState = appState;
            _searchService = new WindowsSearchService();
            _appState.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            _appState.Projects.CollectionChanged += OnProjectsCollectionChanged;
            LoadInitialProjects();
        }

        private void OnProjectsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    LoadInitialProjects();
                }
            });
        }

        private void LoadInitialProjects()
        {
            DisplayedResults.Clear();
            foreach (var project in _appState.Projects)
            {
                DisplayedResults.Add(new SearchResultItem
                {
                    ProjectName = project.ProjectName,
                    ProjectCode = project.ProjectCode,
                    ProjectClient = project.Client,
                    ProjectStatus = project.Status,
                    ProjectLastUpdate = project.LastUpdate,
                    OriginalProject = project
                });
            }
        }

        private async Task FilterProjectsAsync()
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var cancellationToken = _searchCts.Token;

            try
            {
                await Task.Delay(100, cancellationToken);

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    LoadInitialProjects();
                    IsSearching = false;
                    StatusMessage = null;
                    OnPropertyChanged(nameof(StatusMessage));
                    return;
                }

                IsSearching = true;
                StatusMessage = null;
                OnPropertyChanged(nameof(StatusMessage));
                DisplayedResults.Clear();

                var results = new List<SearchResultItem>();

                await Task.Run(() =>
                {
                    var indexResults = SearchIndexManager.Search(SearchText);

                    foreach (var item in indexResults)
                    {
                        if (cancellationToken.IsCancellationRequested) break;

                        var parentProject = _appState.Projects.FirstOrDefault(p => p.ProjectName == item.ProjectName);
                        if (parentProject != null)
                        {
                            results.Add(new SearchResultItem
                            {
                                FileName = item.FileName,
                                FilePath = item.FilePath,
                                IsFolder = item.IsFolder,
                                ProjectName = parentProject.ProjectName,
                                ProjectClient = parentProject.Client,
                                OriginalProject = parentProject
                            });
                        }
                    }
                }, cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    var sortedResults = results
                        .OrderByDescending(r => r.IsFolder)
                        .ThenBy(r => r.FileName);

                    foreach (var result in sortedResults)
                    {
                        DisplayedResults.Add(result);
                    }

                    if (DisplayedResults.Count == 0)
                    {
                        StatusMessage = "No matching items found in the index. Try building the index from the Settings menu.";
                        OnPropertyChanged(nameof(StatusMessage));
                    }
                }
            }
            catch (TaskCanceledException) { /* Expected */ }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsSearching = false;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string? propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}