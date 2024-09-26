using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using Windows.Storage;
using Windows.System;
using GMTool.Data;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Match = System.Text.RegularExpressions.Match;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ResourceCheckTool : Page
    {
        private readonly Regex _regexPath = new("\"[^,\"/]+\":\"([^,\"]+)/([^,\"]+)\"");
        private readonly Regex _regexId = new("(?!:)[0-9]+(?=,)");
        private readonly Regex _regexField = new("(?!\")[^:]+(?=\")");

        private readonly string _resourcePath;

        private readonly ObservableCollection<CfgItemInfo> _cfgItemList;
        private readonly ObservableCollection<ResourceCheckData> _cfgResultList;
        private int _resultIndex;

        private ResourceCheckData _curSelectedData;

        private ErrorReason _sortByErrorReason = ErrorReason.None;

        private readonly Dictionary<string, string> _ignoreDict = new()
        {
            {"CfgActivity", "ActivityRemark"},
        };

        public ResourceCheckTool()
        {
            this.InitializeComponent();

            _cfgItemList = new ObservableCollection<CfgItemInfo>();
            _cfgResultList = new ObservableCollection<ResourceCheckData>();

            string configPath = Path.Combine(CommonData.RepoRootPath, "Resources\\LogicData\\Dev\\client_json");
            _resourcePath = Path.Combine(CommonData.RepoRootPath, "Client\\Assets\\XResources");

            DirectoryInfo dirInfo = new DirectoryInfo(configPath);
            if (!dirInfo.Exists)
            {
                cbCfgList.IsEnabled = false;
                btCheck.IsEnabled = false;
                tbCheckResult.Text = "未读取到表格配置。";
                return;
            }

            _cfgItemList.Add(new CfgItemInfo());
            FileInfo[] files = dirInfo.GetFiles("*.json", SearchOption.TopDirectoryOnly);
            foreach (FileInfo fileInfo in files)
            {
                string filePath = fileInfo.FullName;

                string fileName = Path.GetFileNameWithoutExtension(filePath);

                if (fileName.StartsWith("CfgLanguage"))
                {
                    continue;
                }

                if (fileName.StartsWith("CfgBlockWord"))
                {
                    continue;
                }

                if (fileName == "CfgNewbieGuide" || fileName == "CfgWeakGuide")
                {
                    continue;
                }

                _cfgItemList.Add(new CfgItemInfo(fileName, filePath));
            }

            cbCfgList.ItemsSource = _cfgItemList;
            cbCfgList.SelectedIndex = 0;
        }

        private async void BtCheck_OnClick(object sender, RoutedEventArgs e)
        {
            if (cbCfgList.SelectedItem is CfgItemInfo cfgItemInfo)
            {
                lvResults.ItemsSource = null;
                _cfgResultList.Clear();
                _resultIndex = 0;

                btCheck.IsEnabled = false;
                lvResults.IsEnabled = false;
                spProgress.Visibility = Visibility.Visible;

                if (cfgItemInfo.CheckAll)
                {
                    prgCheck.IsIndeterminate = false;
                    for (int i = 1; i < _cfgItemList.Count; i++)
                    {
                        prgCheck.Value = i * 100d / _cfgItemList.Count;
                        tbProgressInfo.Text = $"{_cfgItemList[i].FileName}...";
                        await CheckCfg(_cfgItemList[i]);
                    }
                    SortResult();
                    tbCheckResult.Text = $"共发现 {_cfgResultList.Count} 条错误资源配置";
                }
                else
                {
                    prgCheck.IsIndeterminate = true;
                    tbProgressInfo.Text = $"{cfgItemInfo.FileName}...";
                    await CheckCfg(cfgItemInfo);
                    SortResult();
                    tbCheckResult.Text = $"{cfgItemInfo.FileName} 中发现 {_cfgResultList.Count} 条错误资源配置";
                }

                btCheck.IsEnabled = true;
                lvResults.IsEnabled = true;
                spProgress.Visibility = Visibility.Collapsed;
                lvResults.ItemsSource = _cfgResultList;

                tbCheckResult.Text = cfgItemInfo.CheckAll
                    ? $"共发现 {_cfgResultList.Count} 条错误资源配置"
                    : $"{cfgItemInfo.FileName} 中发现 {_cfgResultList.Count} 条错误资源配置";
            }
        }

        private async Task CheckCfg(CfgItemInfo info)
        {
            if (!File.Exists(info.FilePath))
                return;

            string[] lines = await File.ReadAllLinesAsync(info.FilePath);
            foreach (string line in lines)
            {
                MatchCollection matches = _regexPath.Matches(line);
                if (matches.Count > 0)
                {
                    string id = string.Empty;
                    Match matchId = _regexId.Match(line);
                    if (matchId.Success)
                    {
                        id = matchId.Value;
                    }

                    foreach (Match match in matches)
                    {
                        string content = match.Value;
                        MatchCollection field = _regexField.Matches(content);
                        if (field.Count == 2)
                        {
                            string column = field[0].Value;
                            string path = field[1].Value;

                            if (_ignoreDict.TryGetValue(info.FileName, out string value) &&
                                value == column)
                            {
                                continue;
                            }

                            string[] pathSegs = path.Split(";");
                            foreach (string seg in pathSegs)
                            {
                                (bool result, ErrorReason season) = IsFileExists(seg);
                                if (!result)
                                {
                                    _cfgResultList.Add(new ResourceCheckData(_resultIndex, info.FileName, id, column, seg, season));
                                    _resultIndex++;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void SortResult()
        {
            if (_sortByErrorReason == ErrorReason.None) return;

            ObservableCollection<ResourceCheckData> sortList1 = new ObservableCollection<ResourceCheckData>();
            ObservableCollection<ResourceCheckData> sortList2 = new ObservableCollection<ResourceCheckData>();
            ObservableCollection<ResourceCheckData> sortList3 = new ObservableCollection<ResourceCheckData>();

            foreach (ResourceCheckData data in _cfgResultList)
            {
                if (data.ErrorReason == ErrorReason.DirNotExists)
                {
                    sortList1.Add(data);
                }
                else if (data.ErrorReason == ErrorReason.FileNotExists)
                {
                    sortList2.Add(data);
                }
                else if (data.ErrorReason == ErrorReason.CasesNotMatch)
                {
                    sortList3.Add(data);
                }
            }

            _cfgResultList.Clear();
            if (_sortByErrorReason == ErrorReason.DirNotExists)
            {
                foreach (ResourceCheckData data in sortList1)
                {
                    _cfgResultList.Add(data);
                }
                foreach (ResourceCheckData data in sortList2)
                {
                    _cfgResultList.Add(data);
                }
                foreach (ResourceCheckData data in sortList3)
                {
                    _cfgResultList.Add(data);
                }
            }
            else if (_sortByErrorReason == ErrorReason.FileNotExists)
            {
                foreach (ResourceCheckData data in sortList2)
                {
                    _cfgResultList.Add(data);
                }
                foreach (ResourceCheckData data in sortList1)
                {
                    _cfgResultList.Add(data);
                }
                foreach (ResourceCheckData data in sortList3)
                {
                    _cfgResultList.Add(data);
                }
            }
            else if (_sortByErrorReason == ErrorReason.CasesNotMatch)
            {
                foreach (ResourceCheckData data in sortList3)
                {
                    _cfgResultList.Add(data);
                }
                foreach (ResourceCheckData data in sortList1)
                {
                    _cfgResultList.Add(data);
                }
                foreach (ResourceCheckData data in sortList2)
                {
                    _cfgResultList.Add(data);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int GetLongPathName(string path, StringBuilder longPath, int longPathLength);

        private (bool, ErrorReason) IsFileExists(string path)
        {
            try
            {
                string fullPath = Path.GetFullPath(Path.Combine(_resourcePath, path));
                string dirPath = Path.GetDirectoryName(fullPath);

                DirectoryInfo dir = new DirectoryInfo(dirPath);
                if (!dir.Exists)
                {
                    return (false, ErrorReason.DirNotExists);
                }

                string fileName = Path.GetFileNameWithoutExtension(path);
                FileInfo[] files = dir.GetFiles($"{fileName}.*", SearchOption.TopDirectoryOnly);
                if (files.Length != 2) // itself and its .meta file
                {
                    return (false, ErrorReason.FileNotExists);
                }

                StringBuilder longPath = new StringBuilder(255);
                GetLongPathName(dirPath, longPath, longPath.Capacity);
                string realPath = longPath.ToString();
                if (!string.Equals(realPath, dirPath, StringComparison.Ordinal))
                {
                    return (false, ErrorReason.CasesNotMatch);
                }

                string path1 = Path.GetFileNameWithoutExtension(Path.GetFullPath(fullPath));
                string path2 = Path.GetFileNameWithoutExtension(Path.GetFullPath(files[0].FullName));
                if (!string.Equals(path1, path2, StringComparison.Ordinal))
                {
                    return (false, ErrorReason.CasesNotMatch);
                }

                return (true, ErrorReason.None);
            }
            catch (Exception)
            {
                return (false, ErrorReason.FileNotExists);
            }
        }

        private void More_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button)
            {
                int index = (int)button.Tag;
                if (_cfgResultList.Count > index)
                {
                    _curSelectedData = _cfgResultList[index];
                    string dirPath = Path.GetDirectoryName(_curSelectedData.Path);
                    string fullPath = Path.GetFullPath(Path.Combine(_resourcePath, dirPath));
                    tbFolderPath.Text = fullPath;
                    tipDetailButton.IsEnabled = Directory.Exists(fullPath);

                    tipDetail.Target = button;
                    tipDetail.IsOpen = true;
                }
            }
        }

        async void TipDetail_OnActionButtonClick(object sender, RoutedEventArgs args)
        {
            if (_curSelectedData == null) return;
            string dirPath = Path.GetDirectoryName(_curSelectedData.Path);
            string fullPath = Path.GetFullPath(Path.Combine(_resourcePath, dirPath));
            if (Directory.Exists(fullPath))
            {
                try
                {
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(fullPath);
                    if (folder != null)
                    {
                        await Launcher.LaunchFolderAsync(folder);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            _curSelectedData = null;
            tipDetail.IsOpen = false;
        }

        private void SortResult1_OnClick(object sender, RoutedEventArgs e)
        {
            if (tgSortResult1.IsChecked)
            {
                if (tgSortResult2.IsChecked)
                {
                    tgSortResult2.IsChecked = false;
                }
                if (tgSortResult3.IsChecked)
                {
                    tgSortResult3.IsChecked = false;
                }
                _sortByErrorReason = ErrorReason.DirNotExists;
            }
            else
            {
                _sortByErrorReason = ErrorReason.None;
            }
            SortResult();
        }

        private void SortResult2_OnClick(object sender, RoutedEventArgs e)
        {
            if (tgSortResult2.IsChecked)
            {
                if (tgSortResult1.IsChecked)
                {
                    tgSortResult1.IsChecked = false;
                }
                if (tgSortResult3.IsChecked)
                {
                    tgSortResult3.IsChecked = false;
                }
                _sortByErrorReason = ErrorReason.FileNotExists;
            }
            else
            {
                _sortByErrorReason = ErrorReason.None;
            }
            SortResult();
        }

        private void SortResult3_OnClick(object sender, RoutedEventArgs e)
        {
            if (tgSortResult3.IsChecked)
            {
                if (tgSortResult1.IsChecked)
                {
                    tgSortResult1.IsChecked = false;
                }
                if (tgSortResult2.IsChecked)
                {
                    tgSortResult2.IsChecked = false;
                }
                _sortByErrorReason = ErrorReason.CasesNotMatch;
            }
            else
            {
                _sortByErrorReason = ErrorReason.None;
            }
            SortResult();
        }
    }
}
