// SettingsView.xaml.cs
// This C# file contains the logic for the Settings view.

using System.Windows.Controls;

namespace FileFlow
{
    public partial class SettingsView : UserControl
    {
        /// <summary>
        /// This parameterless constructor is required for the XAML designer to work correctly.
        /// </summary>
        public SettingsView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This constructor is used by the application at runtime.
        /// </summary>
        public SettingsView(ApplicationState appState)
        {
            InitializeComponent();
            DataContext = new SettingsViewModel(appState);
        }
    }
}
