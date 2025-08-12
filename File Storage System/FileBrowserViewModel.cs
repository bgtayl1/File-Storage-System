// FileBrowserViewModel.cs
// This class now uses PdfiumViewer for direct, silent PDF printing.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PdfiumViewer; // Required for direct PDF printing

namespace FileFlow
{
    public class FileBrowserViewModel : INotifyPropertyChanged
    {
        private readonly string _rootPath;
        private CancellationTokenSource? _statsCts;
        private CancellationTokenSource? _navigateCts;

        public event Action? RequestNavigateToDashboard;

        private string? _currentPath;
        public string? CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;
                OnPropertyChanged(nameof(CurrentPath));
                ((RelayCommand)GoBackCommand).RaiseCanExecuteChanged();
            }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                _ = FilterItemsAsync();
            }
        }

        public ObservableCollection<FileItem> DisplayedItems { get; set; }
        public ICollectionView ItemsView { get; }

        public int TotalFolders { get; private set; }
        public long TotalFiles { get; private set; }
        public double StorageUsedGB { get; private set; }
        public string? StatusMessage { get; private set; }

        public ICommand PrintProConCommand { get; }
        public ICommand GoBackCommand { get; }

        public FileBrowserViewModel(string rootPath, string initialPath)
        {
            _rootPath = rootPath;
            DisplayedItems = new ObservableCollection<FileItem>();
            ItemsView = CollectionViewSource.GetDefaultView(DisplayedItems);

            PrintProConCommand = new RelayCommand(_ => PrintProConFile());
            GoBackCommand = new RelayCommand(_ => GoBack(), _ => CanGoBack());

            _ = NavigateToAsync(initialPath);
        }

        /// <summary>
        /// Finds and silently prints a PDF file containing "PRO+CON" in its name using PdfiumViewer.
        /// </summary>
        private void PrintProConFile()
        {
            if (CurrentPath == null) return;

            try
            {
                var proConFile = Directory.EnumerateFiles(CurrentPath, "*PRO+CON*.pdf", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (proConFile != null)
                {
                    // Load the PDF document using PdfiumViewer.
                    using (var document = PdfDocument.Load(proConFile))
                    {
                        // Create a standard print document from the PDF.
                        using (var printDocument = document.CreatePrintDocument())
                        {
                            // Send the document to the default printer.
                            printDocument.Print();
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No PDF file containing 'PRO+CON' was found in this folder.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not print file: {ex.Message}", "Print Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBack()
        {
            if (CurrentPath != null)
            {
                var parent = Directory.GetParent(CurrentPath);

                if (parent != null && string.Equals(parent.FullName, _rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    RequestNavigateToDashboard?.Invoke();
                }
                else if (parent != null)
                {
                    _ = NavigateToAsync(parent.FullName);
                }
            }
        }

        private bool CanGoBack()
        {
            return !string.Equals(CurrentPath, _rootPath, StringComparison.OrdinalIgnoreCase);
        }

        public async Task NavigateToAsync(string? path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;

            _navigateCts?.Cancel();
            _navigateCts = new CancellationTokenSource();
            var cancellationToken = _navigateCts.Token;

            CurrentPath = path;
            DisplayedItems.Clear();
            StatusMessage = null;
            OnPropertyChanged(nameof(StatusMessage));

            try
            {
                await Task.Run(() =>
                {
                    var items = new List<FileItem>();
                    try
                    {
                        foreach (var item in FastFileEnumerator.Enumerate(path))
                        {
                            if (cancellationToken.IsCancellationRequested) break;
                            App.Current.Dispatcher.Invoke(() => DisplayedItems.Add(item));
                            items.Add(item);
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            CacheManager.AddFolderContents(path, items);
                        }
                    }
                    catch (Exception)
                    {
                        App.Current.Dispatcher.Invoke(() => {
                            StatusMessage = "Access to this folder is denied.";
                            OnPropertyChanged(nameof(StatusMessage));
                        });
                    }
                }, cancellationToken);
            }
            catch (TaskCanceledException) { /* Expected */ }

            _ = CalculateFolderStatsAsync(path);
        }

        private async Task FilterItemsAsync()
        {
            await Task.Run(() =>
            {
                var sourceItems = CacheManager.GetFolderContents(CurrentPath ?? "") ?? new List<FileItem>();
                var filtered = string.IsNullOrWhiteSpace(SearchText)
                    ? sourceItems
                    : sourceItems.Where(item => item.FileName != null && item.FileName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);

                App.Current.Dispatcher.Invoke(() =>
                {
                    DisplayedItems.Clear();
                    foreach (var item in filtered)
                    {
                        DisplayedItems.Add(item);
                    }
                });
            });
        }

        public void Sort(string sortBy)
        {
            string? propertyName = sortBy switch
            {
                "Name" => nameof(FileItem.FileName),
                "Type" => nameof(FileItem.Type),
                "Last Modified" => nameof(FileItem.LastModified),
                _ => null
            };

            if (propertyName == null) return;

            var direction = ListSortDirection.Ascending;
            if (ItemsView.SortDescriptions.Count > 0 && ItemsView.SortDescriptions[0].PropertyName == propertyName)
            {
                direction = ItemsView.SortDescriptions[0].Direction == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
            }

            ItemsView.SortDescriptions.Clear();
            ItemsView.SortDescriptions.Add(new SortDescription(propertyName, direction));
        }

        private async Task CalculateFolderStatsAsync(string path)
        {
            _statsCts?.Cancel();
            _statsCts = new CancellationTokenSource();
            var cancellationToken = _statsCts.Token;

            var cachedStats = CacheManager.GetFolderStats(path);
            if (cachedStats.HasValue)
            {
                TotalFolders = cachedStats.Value.Item1;
                TotalFiles = cachedStats.Value.Item2;
                StorageUsedGB = cachedStats.Value.Item3;
                OnPropertyChanged(nameof(TotalFolders));
                OnPropertyChanged(nameof(TotalFiles));
                OnPropertyChanged(nameof(StorageUsedGB));
                return;
            }

            TotalFolders = 0;
            TotalFiles = 0;
            StorageUsedGB = 0;
            OnPropertyChanged(nameof(TotalFolders));
            OnPropertyChanged(nameof(TotalFiles));
            OnPropertyChanged(nameof(StorageUsedGB));

            try
            {
                await Task.Run(() =>
                {
                    if (!Directory.Exists(path)) return;
                    try
                    {
                        var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
                        if (cancellationToken.IsCancellationRequested) return;

                        TotalFolders = Directory.GetDirectories(path).Length;
                        TotalFiles = files.Length;
                        long totalBytes = files.Sum(f => { try { return new FileInfo(f).Length; } catch (FileNotFoundException) { return 0; } });
                        StorageUsedGB = Math.Round(totalBytes / (1024.0 * 1024.0 * 1024.0), 3);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            CacheManager.AddFolderStats(path, (TotalFolders, TotalFiles, StorageUsedGB));
                        }
                    }
                    catch (UnauthorizedAccessException) { /* Reset stats */ }

                    OnPropertyChanged(nameof(TotalFolders));
                    OnPropertyChanged(nameof(TotalFiles));
                    OnPropertyChanged(nameof(StorageUsedGB));

                }, cancellationToken);
            }
            catch (TaskCanceledException) { /* Expected */ }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}



