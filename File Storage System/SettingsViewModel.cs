// SettingsViewModel.cs
// This class now includes logic for managing content indexed paths.

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FileFlow
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationState _appState;
        private CancellationTokenSource? _indexCts;

        // This property will now only display the current path
        public string ProjectFolderPath => _appState.ProjectFolderPath;

        // This new property will be bound to the textbox for editing
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
            set
            {
                _isIndexing = value;
                OnPropertyChanged(nameof(IsIndexing));
            }
        }

        private double _indexProgress;
        public double IndexProgress
        {
            get => _indexProgress;
            set
            {
                _indexProgress = value;
                OnPropertyChanged(nameof(IndexProgress));
            }
        }

        public ObservableCollection<string> IndexedPaths { get; } = new ObservableCollection<string>();
        public string? NewPath { get; set; }


        public ICommand SaveAndReloadCommand { get; }
        public ICommand BuildIndexCommand { get; }
        public ICommand StopIndexCommand { get; }
        public ICommand AddPathCommand { get; }
        public ICommand RemovePathCommand { get; }

        public SettingsViewModel(ApplicationState appState)
        {
            _appState = appState;
            EditableProjectFolderPath = _appState.ProjectFolderPath; // Initialize with the current path

            SaveAndReloadCommand = new RelayCommand(async _ =>
            {
                if (_appState.ProjectFolderPath != EditableProjectFolderPath)
                {
                    _appState.ProjectFolderPath = EditableProjectFolderPath;
                    OnPropertyChanged(nameof(ProjectFolderPath)); // Notify UI of the change
                }
            });

            BuildIndexCommand = new RelayCommand(async _ => await BuildIndex(), _ => _appState.Projects.Any());
            StopIndexCommand = new RelayCommand(_ => StopIndexing());
            AddPathCommand = new RelayCommand(_ => AddPath());
            RemovePathCommand = new RelayCommand(path => RemovePath(path as string));

            _selectedTheme = ThemeManager.Theme.Dark;

            // Load saved paths
            foreach (var path in _appState.IndexedPaths)
            {
                IndexedPaths.Add(path);
            }

            _appState.Projects.CollectionChanged += (s, e) =>
            {
                ((RelayCommand)BuildIndexCommand).RaiseCanExecuteChanged();
            };
        }

        private void AddPath()
        {
            if (!string.IsNullOrWhiteSpace(NewPath) && !IndexedPaths.Contains(NewPath))
            {
                IndexedPaths.Add(NewPath);
                _appState.IndexedPaths = IndexedPaths.ToList();
                AppSettings.SaveIndexedPaths(_appState.IndexedPaths);
                OnPropertyChanged(nameof(NewPath)); // Clear the textbox
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

        private async Task BuildIndex()
        {
            if (!_appState.Projects.Any())
            {
                MessageBox.Show("There are no projects loaded to build an index from. Please check the project folder path and reload.", "No Projects Found", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            IsIndexing = true;
            IndexProgress = 0;
            _indexCts = new CancellationTokenSource();
            var progress = new Progress<double>(p => IndexProgress = p);

            try
            {
                await SearchIndexManager.BuildIndexAsync(_appState.Projects, _appState.IndexedPaths, progress, _indexCts.Token);
            }
            catch (OperationCanceledException) { /* Expected */ }
            finally
            {
                IsIndexing = false;
                _indexCts?.Dispose();
                _indexCts = null;
            }
        }

        private void StopIndexing()
        {
            _indexCts?.Cancel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



