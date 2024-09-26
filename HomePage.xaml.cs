// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using GMTool.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        public HomePage()
        {
            this.InitializeComponent();

            Visibility privilegeVisibility = CommonData.PrivilegeVisibility;
            ObservableCollection<ToolConfig> itemList = new ObservableCollection<ToolConfig>();
            foreach (ToolGroupConfig groupConfig in CommonData.ToolGroupConfigs)
            {
                if (!groupConfig.Visible) continue;
                if (groupConfig.Privilege && privilegeVisibility == Visibility.Collapsed) continue;
                foreach (ToolConfig toolConfig in groupConfig.ToolConfigs)
                {
                    if (!toolConfig.Visible) continue;
                    if (toolConfig.Privilege && privilegeVisibility == Visibility.Collapsed) continue;
                    itemList.Add(toolConfig);
                }
            }

            gridView.ItemsSource = itemList;
        }

        private void ButtonTool_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                try
                {
                    if (MainWindow.CurrentMainWindow != null)
                    {
                        MainWindow.CurrentMainWindow.TryNavigateTo((string)button.Tag);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
