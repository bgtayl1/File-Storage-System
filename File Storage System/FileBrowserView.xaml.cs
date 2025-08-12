// FileBrowserView.xaml.cs
// This file now handles the event to navigate back to the dashboard.

using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System;

namespace FileFlow
{
    public partial class FileBrowserView : UserControl
    {
        private readonly FileBrowserViewModel _viewModel;

        public FileBrowserView(string rootPath, string initialPath)
        {
            InitializeComponent();
            _viewModel = new FileBrowserViewModel(rootPath, initialPath);
            // Subscribe to the event that signals a request to go back to the dashboard.
            _viewModel.RequestNavigateToDashboard += OnRequestNavigateToDashboard;
            DataContext = _viewModel;
        }

        /// <summary>
        /// When the ViewModel requests it, tell the MainWindow to navigate to the dashboard.
        /// </summary>
        private void OnRequestNavigateToDashboard()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.NavigateToDashboard();
            }
        }

        private void ItemsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListView listView && listView.SelectedItem is FileItem selectedItem && selectedItem.FullPath != null)
            {
                if (selectedItem.IsFolder)
                {
                    _ = _viewModel.NavigateToAsync(selectedItem.FullPath);
                }
                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(selectedItem.FullPath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Handles clicks on the column headers to trigger sorting.
        /// </summary>
        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader headerClicked)
            {
                string? sortBy = headerClicked.Column.Header as string;
                if (sortBy != null)
                {
                    _viewModel.Sort(sortBy);
                }
            }
        }
    }
}




