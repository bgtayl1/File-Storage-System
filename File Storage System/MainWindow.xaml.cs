// MainWindow.xaml.cs
// This file now includes a dedicated method for navigating to the dashboard.

using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FileFlow
{
    public partial class MainWindow : Window
    {
        private readonly ApplicationState _appState = new ApplicationState();
        private readonly Stack<UserControl> _navigationHistory = new Stack<UserControl>();

        public MainWindow()
        {
            InitializeComponent();
            NavigateTo(new DashboardView(_appState));
        }

        public void NavigateTo(UserControl view)
        {
            if (MainContent.Content is UserControl currentView)
            {
                _navigationHistory.Push(currentView);
            }
            MainContent.Content = view;
        }

        public void NavigateBack()
        {
            if (_navigationHistory.Any())
            {
                MainContent.Content = _navigationHistory.Pop();
            }
        }

        /// <summary>
        /// Navigates to the dashboard view, clearing any previous navigation history.
        /// </summary>
        public void NavigateToDashboard()
        {
            _navigationHistory.Clear();
            MainContent.Content = new DashboardView(_appState);
        }

        private void NavButton_Click(object sender, RoutedEventArgs e)
        {
            _navigationHistory.Clear();
            if (sender is RadioButton button)
            {
                switch (button.Name)
                {
                    case "DashboardButton":
                        MainContent.Content = new DashboardView(_appState);
                        break;
                    case "SettingsButton":
                        MainContent.Content = new SettingsView(_appState);
                        break;
                    default:
                        MainContent.Content = new DashboardView(_appState);
                        break;
                }
            }
        }
    }
}
