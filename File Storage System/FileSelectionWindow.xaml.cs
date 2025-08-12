// FileSelectionWindow.xaml.cs
// This file contains the logic for the file selection dialog.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FileFlow
{
    public partial class FileSelectionWindow : Window
    {
        public string? SelectedFile { get; private set; }
        private readonly List<string> _fullFilePaths;

        public List<string> FileNames { get; } = new List<string>();

        public FileSelectionWindow(List<string> filePaths)
        {
            InitializeComponent();
            _fullFilePaths = filePaths;
            FileNames = _fullFilePaths.Select(Path.GetFileName).ToList()!;
            DataContext = this;
        }

        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedItem != null)
            {
                SelectedFile = _fullFilePaths[FileListBox.SelectedIndex];
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Please select a file to print.", "No File Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void FileListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PrintButton_Click(sender, e);
        }
    }
}

