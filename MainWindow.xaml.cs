using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
//using System.Diagnostics;
//using System.Drawing;
//using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using GMTool.Data;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System.Text.RegularExpressions;
using GMTool.View;
using Windows.UI;
using Microsoft.UI.Xaml.Media.Imaging;
//using Microsoft.UI.Xaml.Media.Imaging;
//using NIdenticon;
//using Color = Windows.UI.Color;
//using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow
    {
        private int _buildVersion;
        internal static MainWindow CurrentMainWindow;

        public new AppWindow AppWindow
        {
            get
            {
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                var winId = Win32Interop.GetWindowIdFromWindow(hWnd);
                return AppWindow.GetFromWindowId(winId);
            }
        }

        public void InitializeObject(object obj)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(obj, hWnd);
        }


        public MainWindow()
        {
            this.InitializeComponent();
            CurrentMainWindow = this;
            this.Closed += OnClosed;
            Init();
        }

        private async void Init()
        {
            string iconPath = Path.Combine(Environment.CurrentDirectory, "Assets/GM.ico");
            if (File.Exists(iconPath))
            {
                TitleBarIcon.Source = new BitmapImage(new Uri(iconPath));
            }

            await LoadTheme();

            bool hasRootPath = await GetRootPath();
            if (hasRootPath)
            {
                CommonData.RepoUrlPath = await GetDirPath();
                CommonData.PrivilegeVisibility = Visibility.Visible;
            }
            else
            {
                CommonData.PrivilegeVisibility = Visibility.Collapsed;
            }

            LoadNavItem();

            await LoadRecentUpdate();
            await LoadAccountInfo();

            string selectedNavItem = "GMTool.GMCommandTool";
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string[] argList = args[1].Split('|');
                selectedNavItem = $"GMTool.{argList[0]}";
            }

            if (!string.IsNullOrEmpty(selectedNavItem))
            {
                foreach (var menuItem in navRoot.MenuItems)
                {
                    if (menuItem is NavigationViewItem navItem && !string.IsNullOrEmpty(navItem.Tag.ToString()) && navItem.Tag.ToString() == selectedNavItem)
                    {
                        DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                        {
                            navItem.IsSelected = true;
                        });
                        break;
                    }
                }
            }
            else
            {
                DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    navItemHomePage.IsSelected = true;
                });
            }
        }

        private async Task<bool> GetRootPath()
        {
            try
            {
                (bool success, string content) = await ExcelLockMgr.GetSvnCommandOutput("info --show-item wc-root");
                if (success && !string.IsNullOrEmpty(content) && Directory.Exists(content))
                {
                    CommonData.RepoRootPath = content;
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<string> GetDirPath()
        {
            try
            {
                string args = $"info --show-item relative-url \"{CommonData.RepoRootPath}\"";
                (bool success, string content) = await ExcelLockMgr.GetSvnCommandOutput(args);
                if (success && !string.IsNullOrEmpty(content))
                {
                    if (content.StartsWith("^/"))
                    {
                        content = content.Remove(0, 2);
                    }
                    return content;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return string.Empty;
        }

        private async Task LoadTheme()
        {
            CommonData.StartupTheme = gridRoot.ActualTheme;

            string themeSetting = await CommonData.GetLocalSetting("themeSetting");
            if (themeSetting != null && int.TryParse(themeSetting, out int value))
            {
                ElementTheme theme = (ElementTheme)value;
                gridRoot.RequestedTheme = theme;
            }

            CommonData.MainWindowGrid = gridRoot;
            CommonData.DefaultTheme = gridRoot.RequestedTheme;

            Version version = Environment.OSVersion.Version;
            _buildVersion = version.Build;
            if (_buildVersion >= 22000)
            {
                FrameworkElement content = Content as FrameworkElement;
                if (content != null)
                {
                    content.ActualThemeChanged += (_, _) => ModifyTitlebarTheme();
                }

                ModifyTitlebarTheme();
                gridRoot.Background = new SolidColorBrush(Colors.Transparent);

                AppTitleBar.Visibility = Visibility.Visible;
                AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
                AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                SetTitleBar(AppTitleBar);

                if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
                {
                    SystemBackdrop = new MicaBackdrop
                    {
                        Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.Base
                    };
                }
            }
            else
            {
                appTitleBarRow.Height = new GridLength(0);
            }
        }

        private void LoadNavItem()
        {
            Visibility privilegeVisibility = CommonData.PrivilegeVisibility;
            foreach (ToolGroupConfig groupConfig in CommonData.ToolGroupConfigs)
            {
                if (!groupConfig.Visible) continue;
                if (groupConfig.Privilege && privilegeVisibility == Visibility.Collapsed) continue;

                navRoot.MenuItems.Add(new NavigationViewItemSeparator());
                navRoot.MenuItems.Add(new NavigationViewItemHeader { Content = groupConfig.Name });

                foreach (ToolConfig toolConfig in groupConfig.ToolConfigs)
                {
                    if (!toolConfig.Visible) continue;
                    if (toolConfig.Privilege && privilegeVisibility == Visibility.Collapsed) continue;
                    navRoot.MenuItems.Add(new NavigationViewItem
                    {
                        Tag = toolConfig.PageName,
                        Content = toolConfig.Name,
                        Icon = new FontIcon
                        {
                            FontFamily = new FontFamily("Assets/Segoe Fluent Icons.ttf#Segoe Fluent Icons"),
                            Glyph = toolConfig.Icon,
                        },
                    });
                }
            }
        }

        private async Task LoadRecentUpdate()
        {
            tipRecentUpdate.Subtitle = CommonData.RecentUpdateInfo[0];
            tipRecentUpdate.Content = CommonData.RecentUpdateInfo[1];
            string version = await CommonData.GetLocalSetting("recentUpdateVersion");
            if (string.IsNullOrEmpty(version) || version != CommonData.RecentUpdateInfo[0])
            {
                tipRecentUpdate.IsLightDismissEnabled = false;
                navItemRecentUpdate.InfoBadge.Visibility = Visibility.Visible;
            }
            else
            {
                tipRecentUpdate.IsLightDismissEnabled = true;
                navItemRecentUpdate.InfoBadge.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadAccountInfo()
        {
            try
            {
                (bool success, string content) = await ExcelLockMgr.GetSvnCommandOutput("auth http://192.168.1.67");
                if (success && !string.IsNullOrEmpty(content))
                {
                    Regex regexAccountName = new("(?<=Username: )(.+)");
                    Match match = regexAccountName.Match(content);
                    if (match.Success)
                    {
                        string accountName = match.Groups[1].Value;
                        CommonData.AccountName = accountName;
                        ToolTipService.SetToolTip(navItemProfile, CommonData.AccountName);
                        navItemProfile.Content = $"ÓÃ»§£º{CommonData.AccountName}";
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }

            //if (!string.IsNullOrEmpty(CommonData.AccountName))
            //{
            //    if (!File.Exists(CommonData.IdenticonPath))
            //    {
            //        IdenticonGenerator generator = new IdenticonGenerator()
            //            .WithAlgorithm("MD5")
            //            .WithSize(100, 100)
            //            .WithBlocks(5, 5)
            //            .WithBackgroundColor(System.Drawing.Color.White)
            //            .WithBlockGenerators(IdenticonGenerator.DefaultBlockGeneratorsConfig);
            //        Bitmap bitmap = generator.Create(CommonData.AccountName);
            //        bitmap.Save(CommonData.IdenticonPath, ImageFormat.Png);
            //    }
            //    ImageIcon icon = new()
            //    {
            //        Width = 20,
            //        Height = 20,
            //        Source = new BitmapImage(new(CommonData.IdenticonPath, UriKind.Absolute))
            //    };
            //    navItemProfile.Icon = icon;
            //    //ppProfile.DisplayName = CommonData.AccountName;
            //    //ppProfile.ProfilePicture = new BitmapImage(new(CommonData.IdenticonPath, UriKind.Absolute));
            //}
            //else
            //{
            //    FontIcon icon = new()
            //    {
            //        Glyph = "\xe77B",
            //        FontFamily = new("/Assets/Segoe Fluent Icons.ttf#Segoe Fluent Icons")
            //    };
            //    navItemProfile.Icon = icon;
            //    //ppProfile.Visibility = Visibility.Collapsed;
            //}
        }

        private void ModifyTitlebarTheme()
        {
            var content = Content as FrameworkElement;
            if (content == null) return;
            var value = content.ActualTheme;

            var titleBar = AppWindow.TitleBar;
            if (value == ElementTheme.Light)
            {
                titleBar.ForegroundColor = Colors.Black;
                titleBar.BackgroundColor = Color.FromArgb(0, 243, 243, 243);
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.InactiveBackgroundColor = Color.FromArgb(0, 243, 243, 243);

                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 243, 243, 243);
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 243, 243, 243);

                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 180, 180, 180);
                titleBar.ButtonPressedForegroundColor = Colors.Black;
                titleBar.ButtonPressedBackgroundColor = Colors.White;
            }
            else if (value == ElementTheme.Dark)
            {
                titleBar.ForegroundColor = Colors.White;
                titleBar.BackgroundColor = Color.FromArgb(0, 32, 32, 32);
                titleBar.InactiveForegroundColor = Colors.Gray;
                titleBar.InactiveBackgroundColor = Color.FromArgb(0, 32, 32, 32);

                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonBackgroundColor = Color.FromArgb(0, 32, 32, 32);
                titleBar.ButtonInactiveForegroundColor = Colors.Gray;
                titleBar.ButtonInactiveBackgroundColor = Color.FromArgb(0, 32, 32, 32);

                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(255, 51, 51, 51);
                titleBar.ButtonPressedForegroundColor = Colors.White;
                titleBar.ButtonPressedBackgroundColor = Colors.Gray;
            }
        }

        private void NavRoot_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                Navigate("ÉèÖÃ", "GMTool.Settings");
            }
            else if (sender.SelectedItem is NavigationViewItem item)
            {
                Navigate((string)item.Content, (string)item.Tag);
            }
        }

        private void ButtonTool_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                Navigate((string)button.Tag, (string)button.Tag);
            }
        }

        public void Navigate(string title, string typeName)
        {
            string showTitle = $"{title}  -  {CommonData.RepoUrlPath}";
            Title = showTitle;
            if (_buildVersion >= 22000)
            {
                TitleBarName.Text = showTitle;
            }
            navContentFrame.Navigate(Type.GetType(typeName), null, new DrillInNavigationTransitionInfo());
        }

        internal void TryNavigateTo(string name)
        {
            foreach (var menuItem in navRoot.MenuItems)
            {
                if (menuItem is NavigationViewItem navItem && !string.IsNullOrEmpty(navItem.Tag.ToString()) && navItem.Tag.ToString() == name)
                {
                    DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                    {
                        navItem.IsSelected = true;
                    });
                    return;
                }
            }
        }

        private async void OnClosed(object sender, WindowEventArgs args)
        {
            await CommonData.SaveCommandHistory();
            await CommonData.SaveStarredExcel();
        }

        private void NavItemRecentUpdate_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (tipRecentUpdate.IsOpen)
            {
                tipRecentUpdate.IsOpen = false;
            }
            else
            {
                tipRecentUpdate.IsOpen = true;
            }
        }

        private async void TipRecentUpdate_OnCloseButtonClick(TeachingTip sender, object args)
        {
            tipRecentUpdate.IsLightDismissEnabled = true;
            navItemRecentUpdate.InfoBadge.Visibility = Visibility.Collapsed;
            await CommonData.SaveLocalSetting("recentUpdateVersion", CommonData.RecentUpdateInfo[0]);
        }
    }
}
