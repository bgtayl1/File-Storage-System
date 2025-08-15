// SettingsViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FileFlow
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationState _appState;
        private CancellationTokenSource? _indexCts;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly DispatcherTimer _timer;

        public string ProjectFolderPath => _appState.ProjectFolderPath;
        public string EditableProjectFolderPath { get; set; }

        private ThemeManager.Theme _selectedTheme;
        public ThemeManager.Theme SelectedTheme
        {
            get => _selectedTheme;
            set
            {
                if (_selectedTheme != value)
                {
                    _selectedTheme = value;
                    OnPropertyChanged(nameof(SelectedTheme));
                    ThemeManager.ApplyTheme(_selectedTheme);
                }
            }
        }

        private bool _isIndexing;
        public bool IsIndexing
        {
            get => _isIndexing;
            set { _isIndexing = value; OnPropertyChanged(nameof(IsIndexing)); }
        }

        private double _indexProgress;
        public double IndexProgress
        {
            get => _indexProgress;
            set { _indexProgress = value; OnPropertyChanged(nameof(IndexProgress)); }
        }

        private string _apiStatusMessage;
        public string ApiStatusMessage
        {
            get => _apiStatusMessage;
            set { _apiStatusMessage = value; OnPropertyChanged(nameof(ApiStatusMessage)); }
        }

        public ObservableCollection<string> IndexedPaths { get; } = new ObservableCollection<string>();
        public string? NewPath { get; set; }

        public ICommand SaveAndReloadCommand { get; }
        public ICommand BuildIndexCommand { get; }
        public ICommand StopIndexCommand { get; }
        public ICommand AddPathCommand { get; }
        public ICommand RemovePathCommand { get; }

        // In SettingsViewModel.cs

        public SettingsViewModel(ApplicationState appState)
        {
            _appState = appState;
            EditableProjectFolderPath = _appState.ProjectFolderPath;

            // --- THIS BLOCK IS THE FIX ---
            // This handler tells the HttpClient to ignore self-signed certificate errors.
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            _httpClient = new HttpClient(handler);
            _httpClient.BaseAddress = new Uri("https://LPMSRV:7076/api/");
            // --- END OF FIX ---

            _apiStatusMessage = "Connecting to service...";

            SaveAndReloadCommand = new RelayCommand(_ =>
            {
                if (_appState.ProjectFolderPath != EditableProjectFolderPath)
                {
                    _appState.ProjectFolderPath = EditableProjectFolderPath;
                    OnPropertyChanged(nameof(ProjectFolderPath));
                }
            });

            BuildIndexCommand = new RelayCommand(async _ => await BuildIndex(), _ => false);
            StopIndexCommand = new RelayCommand(_ => StopIndexing(), _ => false);
            AddPathCommand = new RelayCommand(_ => AddPath());
            RemovePathCommand = new RelayCommand(path => RemovePath(path as string));

            _selectedTheme = ThemeManager.Theme.Dark;

            foreach (var path in _appState.IndexedPaths)
            {
                IndexedPaths.Add(path);
            }

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _timer.Tick += async (s, e) => await CheckApiStatus();
            _timer.Start();
            _ = CheckApiStatus();
        }

        private async Task CheckApiStatus()
        {
            try
            {
                var response = await _httpClient.GetAsync("status");
                if (response.IsSuccessStatusCode)
                {
                    var status = await response.Content.ReadFromJsonAsync<IndexingStatusModel>();
                    if (status != null)
                    {
                        ApiStatusMessage = status.StatusMessage;
                    }
                }
                else
                {
                    ApiStatusMessage = "Could not connect to indexing service.";
                }
            }
            catch (HttpRequestException)
            {
                ApiStatusMessage = "Service is offline.";
            }
        }

        private void AddPath()
        {
            if (!string.IsNullOrWhiteSpace(NewPath) && !IndexedPaths.Contains(NewPath))
            {
                IndexedPaths.Add(NewPath);
                _appState.IndexedPaths = IndexedPaths.ToList();
                AppSettings.SaveIndexedPaths(_appState.IndexedPaths);
                NewPath = string.Empty;
                OnPropertyChanged(nameof(NewPath));
            }
        }

        private void RemovePath(string? path)
        {
            if (path != null && IndexedPaths.Contains(path))
            {
                IndexedPaths.Remove(path);
                _appState.IndexedPaths = IndexedPaths.ToList();
                AppSettings.SaveIndexedPaths(_appState.IndexedPaths);
            }
        }

        private Task BuildIndex()
        {
            // This logic is now server-side. The button is disabled.
            return Task.CompletedTask;
        }

        private void StopIndexing()
        {
            // This logic is now server-side. The button is disabled.
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}