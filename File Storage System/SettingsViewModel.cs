// SettingsViewModel.cs
// This class now includes logic for building the local search index.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace FileFlow
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationState _appState;
        private CancellationTokenSource? _cacheCts;
        private CancellationTokenSource? _indexCts;

        public string ProjectFolderPath
        {
            get => _appState.ProjectFolderPath;
            set
            {
                if (_appState.ProjectFolderPath != value)
                {
                    _appState.ProjectFolderPath = value;
                    OnPropertyChanged(nameof(ProjectFolderPath));
                }
            }
        }

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

        private bool _isCaching;
        public bool IsCaching
        {
            get => _isCaching;
            set
            {
                _isCaching = value;
                OnPropertyChanged(nameof(IsCaching));
            }
        }

        private double _cacheProgress;
        public double CacheProgress
        {
            get => _cacheProgress;
            set
            {
                _cacheProgress = value;
                OnPropertyChanged(nameof(CacheProgress));
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

        public ICommand SaveAndReloadCommand { get; }
        public ICommand PreCacheCommand { get; }
        public ICommand StopCacheCommand { get; }
        public ICommand BuildIndexCommand { get; }
        public ICommand StopIndexCommand { get; }

        public SettingsViewModel(ApplicationState appState)
        {
            _appState = appState;
            SaveAndReloadCommand = new RelayCommand(async _ => await _appState.SyncAndLoadProjectsAsync());
            PreCacheCommand = new RelayCommand(async _ => await PreCacheAll());
            StopCacheCommand = new RelayCommand(_ => StopCaching());
            BuildIndexCommand = new RelayCommand(async _ => await BuildIndex());
            StopIndexCommand = new RelayCommand(_ => StopIndexing());

            _selectedTheme = ThemeManager.Theme.Dark;
        }

        private async Task PreCacheAll()
        {
            IsCaching = true;
            CacheProgress = 0;
            _cacheCts = new CancellationTokenSource();
            var progress = new Progress<double>(p => CacheProgress = p);

            try
            {
                await CacheManager.PreCacheAllAsync(_appState.ProjectFolderPath, progress, _cacheCts.Token);
            }
            catch (OperationCanceledException) { /* Expected */ }
            finally
            {
                IsCaching = false;
                _cacheCts?.Dispose();
                _cacheCts = null;
            }
        }

        private void StopCaching()
        {
            _cacheCts?.Cancel();
        }

        private async Task BuildIndex()
        {
            IsIndexing = true;
            IndexProgress = 0;
            _indexCts = new CancellationTokenSource();
            var progress = new Progress<double>(p => IndexProgress = p);

            try
            {
                await SearchIndexManager.BuildIndexAsync(_appState.Projects, progress, _indexCts.Token);
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



