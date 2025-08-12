// DashboardView.xaml.cs
// This file has been updated to handle navigating to the correct folder from search results.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System;

namespace FileFlow
{
    public partial class DashboardView : UserControl
    {
        private readonly ApplicationState? _appState;

        public DashboardView()
        {
            InitializeComponent();
        }

        public DashboardView(ApplicationState appState)
        {
            InitializeComponent();
            _appState = appState;
            DataContext = new DashboardViewModel(appState);
        }

        private void ProjectsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_appState != null && sender is ListView listView && listView.SelectedItem is SearchResultItem selectedItem)
            {
                string? folderPathToNavigate = null;

                // Case 1: The item is a folder found in a search. Navigate to its specific path.
                if (selectedItem.IsFolder && !string.IsNullOrEmpty(selectedItem.FilePath))
                {
                    folderPathToNavigate = selectedItem.FilePath;
                }
                // Case 2: The item is a file found in a search. Open the file directly.
                else if (!selectedItem.IsFolder && !string.IsNullOrEmpty(selectedItem.FilePath))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(selectedItem.FilePath) { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Could not open file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return; // Stop here, no folder navigation needed.
                }
                // Case 3: The item is a project from the initial list (no search term). Navigate to the project's root.
                else if (selectedItem.OriginalProject?.FullPath != null)
                {
                    folderPathToNavigate = selectedItem.OriginalProject.FullPath;
                }

                // If we have a valid folder path, navigate to it.
                if (folderPathToNavigate != null)
                {
                    if (Window.GetWindow(this) is MainWindow mainWindow)
                    {
                        mainWindow.NavigateTo(new FileBrowserView(_appState.ProjectFolderPath, folderPathToNavigate));
                    }
                }
            }
        }
    }
}




