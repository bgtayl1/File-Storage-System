// App.xaml.cs
// This file now initializes the default theme on startup.

using System.Windows;

namespace FileFlow
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // Apply the default dark theme when the application starts
            ThemeManager.ApplyTheme(ThemeManager.Theme.Dark);
        }
    }
}
