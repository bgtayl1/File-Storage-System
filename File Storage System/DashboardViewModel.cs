// DashboardViewModel.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Specialized;

namespace FileFlow
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationState _appState;
        private CancellationTokenSource? _searchCts;
        private readonly HttpClient _httpClient = new HttpClient();

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

        // In DashboardViewModel.cs

        public DashboardViewModel(ApplicationState appState)
        {
            _appState = appState;
            _appState.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
            _appState.Projects.CollectionChanged += OnProjectsCollectionChanged;

            // --- THIS BLOCK IS THE FIX ---
            // This handler tells the HttpClient to ignore self-signed certificate errors.
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri("https://LPMSRV:7076/api/");
            // --- END OF FIX ---

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
            var items = new List<SearchResultItem>();
            if (!string.IsNullOrEmpty(_appState.ProjectFolderPath))
            {
                foreach (var item in FastFileEnumerator.Enumerate(_appState.ProjectFolderPath))
                {
                    if (item.FullPath == null) continue;
                    var fileInfo = new FileInfo(item.FullPath);
                    items.Add(new SearchResultItem
                    {
                        FileName = item.FileName,
                        FilePath = item.FullPath,
                        IsFolder = item.IsFolder,
                        Type = item.IsFolder ? "Folder" : "File",
                        LastModified = fileInfo.LastWriteTime.ToString("g")
                    });
                }
            }

            var sortedItems = items
                .OrderByDescending(r => r.IsFolder)
                .ThenBy(r => r.FileName);

            DisplayedResults.Clear();
            foreach (var item in sortedItems)
            {
                DisplayedResults.Add(item);
            }
        }

        private async Task FilterProjectsAsync()
        {
            _searchCts?.Cancel();
            _searchCts = new CancellationTokenSource();
            var cancellationToken = _searchCts.Token;

            try
            {
                // --- CHANGE IS HERE ---
                // Increased delay to 300ms to prevent API calls on every keystroke.
                await Task.Delay(300, cancellationToken);
                // --- END OF CHANGE ---

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    LoadInitialProjects();
                    IsSearching = false;
                    StatusMessage = null;
                    OnPropertyChanged(nameof(IsSearching));
                    OnPropertyChanged(nameof(StatusMessage));
                    return;
                }

                IsSearching = true;
                StatusMessage = null;
                OnPropertyChanged(nameof(IsSearching));
                OnPropertyChanged(nameof(StatusMessage));
                DisplayedResults.Clear();

                var results = new List<SearchResultItem>();
                var response = await _httpClient.GetAsync($"search?term={SearchText}", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var indexResults = await response.Content.ReadFromJsonAsync<List<IndexItem>>(cancellationToken: cancellationToken);
                    if (indexResults != null)
                    {
                        foreach (var item in indexResults)
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            var parentProject = _appState.Projects.FirstOrDefault(p => p.ProjectName == item.ProjectName);
                            if (parentProject != null && item.FilePath != null)
                            {
                                var fileInfo = new FileInfo(item.FilePath);
                                results.Add(new SearchResultItem
                                {
                                    FileName = item.FileName,
                                    FilePath = item.FilePath,
                                    IsFolder = item.IsFolder,
                                    ProjectName = parentProject.ProjectName,
                                    OriginalProject = parentProject,
                                    Type = item.IsFolder ? "Folder" : "File",
                                    LastModified = fileInfo.LastWriteTime.ToString("g")
                                });
                            }
                        }
                    }
                }
                else
                {
                    StatusMessage = "Error searching. Please try again later.";
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    var sortedResults = results
                        .OrderByDescending(r => r.IsFolder)
                        .ThenBy(r => r.FileName);

                    foreach (var result in sortedResults)
                    {
                        DisplayedResults.Add(result);
                    }

                    if (DisplayedResults.Count == 0 && string.IsNullOrEmpty(StatusMessage))
                    {
                        StatusMessage = "No matching items found.";
                    }
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
            catch (TaskCanceledException) { /* Expected */ }
            catch (HttpRequestException)
            {
                StatusMessage = "Could not connect to the search service.";
                OnPropertyChanged(nameof(StatusMessage));
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IsSearching = false;
                    OnPropertyChanged(nameof(IsSearching));
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