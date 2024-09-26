using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;
using GMTool.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Settings : Page
    {
        public Settings()
        {
            this.InitializeComponent();

            btThemeLight.Checked += ThemeLight_OnChecked;
            btThemeDark.Checked += ThemeDark_OnChecked;
            btThemeDefault.Checked += ThemeDefault_OnChecked;

            LoadChangelog();
        }

        private async void ThemeLight_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button && button.IsChecked == true)
            {
                CommonData.DefaultTheme = ElementTheme.Light;
                CommonData.MainWindowGrid.RequestedTheme = ElementTheme.Light;
                await CommonData.SaveLocalSetting("themeSetting", ((int)ElementTheme.Light).ToString());
            }
        }

        private async void ThemeDark_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button && button.IsChecked == true)
            {
                CommonData.DefaultTheme = ElementTheme.Dark;
                CommonData.MainWindowGrid.RequestedTheme = ElementTheme.Dark;
                await CommonData.SaveLocalSetting("themeSetting", ((int)ElementTheme.Dark).ToString());
            }
        }

        private async void ThemeDefault_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button && button.IsChecked == true)
            {
                CommonData.DefaultTheme = ElementTheme.Default;
                CommonData.MainWindowGrid.RequestedTheme = CommonData.StartupTheme;
                await CommonData.SaveLocalSetting("themeSetting", ((int)ElementTheme.Default).ToString());
            }
        }

        private async void LoadChangelog()
        {
            if (CommonData.PrivilegeVisibility == Visibility.Visible)
            {
                if (File.Exists("Assets/Changelog.txt"))
                {
                    string content = await File.ReadAllTextAsync("Assets/Changelog.txt");
                    tbChangelog.Text = content;
                }
                xpChangelog.Visibility = Visibility.Visible;
            }
        }

        private void ThemeLight_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                if (CommonData.DefaultTheme is ElementTheme.Light)
                {
                    button.IsChecked = true;
                }
            }
        }

        private void ThemeDark_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                if (CommonData.DefaultTheme is ElementTheme.Dark)
                {
                    button.IsChecked = true;
                }
            }
        }

        public void ThemeDefault_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton button)
            {
                if (CommonData.DefaultTheme is ElementTheme.Default)
                {
                    button.IsChecked = true;
                }
            }
        }
    }
}
