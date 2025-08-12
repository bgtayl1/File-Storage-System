// FileItem.cs
// This class represents a single file or folder. It now supports live updates.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileFlow
{
    public class FileItem : INotifyPropertyChanged // <-- Added interface
    {
        public string? FileName { get; set; }
        public string? FullPath { get; set; }
        public bool IsFolder { get; set; }
        public string? Type { get; set; }

        // --- This is the modified section ---
        private DateTime _lastModified;
        public DateTime LastModified // <-- Changed from string to DateTime
        {
            get => _lastModified;
            set
            {
                if (_lastModified != value)
                {
                    _lastModified = value;
                    OnPropertyChanged(); // Notifies the UI to update this property
                }
            }
        }
        // ------------------------------------

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
