using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using Windows.System;
using GMTool.Data;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LanguageSearchTool : Page
    {

        private readonly List<CfgLanguageFileInfo> _cfgLanguageFileList;
        private readonly ObservableCollection<CfgLanguageInfo> _queryResults;

        private string _curQuery = string.Empty;
        private readonly string _configPath;
        private readonly string _sourcePath;

        private bool _isCaseSensitive = false;
        private bool _isWholeWord = false;
        private bool _searchId = true;
        private bool _searchContent = true;

        private int _totalCount = 0;
        private CfgLanguageInfo _curLanguageInfo;

        public LanguageSearchTool()
        {
            this.InitializeComponent();
            _cfgLanguageFileList = new List<CfgLanguageFileInfo>();
            _queryResults = new ObservableCollection<CfgLanguageInfo>();
            _configPath = Path.GetFullPath(Path.Combine(CommonData.RepoRootPath, "Resources\\LogicData\\Dev\\client_json"));
            _sourcePath = Path.GetFullPath(Path.Combine(CommonData.RepoRootPath, "Excels\\Dev"));
            lvResults.ItemsSource = _queryResults;
            //dgResults.DataContext = _queryResults;
            _ = Init();
        }

        private async Task Init()
        {
            if (!await LoadCfgLanguages())
            {
                tbTotalCount.Text = "未读取到多语言配置。";
                tbQuery.IsEnabled = false;
                btQuery.IsEnabled = false;
                btCheck.IsEnabled = false;
            }
        }

        private async Task<bool> LoadCfgLanguages()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(_configPath);
            if (!dirInfo.Exists)
                return false;

            _totalCount = 0;
            _cfgLanguageFileList.Clear();
            FileInfo[] languageFiles = dirInfo.GetFiles("CfgLanguage*.json", SearchOption.TopDirectoryOnly);
            if (languageFiles.Length == 0) return false;

            try
            {
                foreach (FileInfo fileInfo in languageFiles)
                {
                    string fileName = Path.GetFileNameWithoutExtension(fileInfo.Name);
                    CfgLanguageFileInfo cfgLanguageFileInfo = new CfgLanguageFileInfo(fileName.Substring(3));
                    JArray o = JArray.Parse(await File.ReadAllTextAsync(fileInfo.FullName));
                    int lineNo = 3;
                    foreach (JToken line in o)
                    {
                        lineNo++;
                        string key = (string)line.SelectToken("ID") ?? string.Empty;
                        string newKey = (string)line.SelectToken("NewID") ?? string.Empty;
                        if (!string.IsNullOrWhiteSpace(key) || !string.IsNullOrWhiteSpace(newKey))
                        {
                            _totalCount++;
                            string zh_CN = (string)line.SelectToken("zh-CN", false) ?? string.Empty;
                            string en = (string)line.SelectToken("en", false) ?? string.Empty;
                            string zh_TW = (string)line.SelectToken("zh-TW", false) ?? string.Empty;
                            string pt = (string)line.SelectToken("pt", false) ?? string.Empty;
                            string fr = (string)line.SelectToken("fr", false) ?? string.Empty;
                            string th = (string)line.SelectToken("th", false) ?? string.Empty;
                            string id = (string)line.SelectToken("id", false) ?? string.Empty;
                            cfgLanguageFileInfo.Add(lineNo, key, newKey, zh_CN, en, zh_TW, pt, fr, th, id);
                        }
                    }
                    _cfgLanguageFileList.Add(cfgLanguageFileInfo);
                }
                tbTotalCount.Text = $"共 {_totalCount} 条多语言配置";
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        private void BtQuery_OnClick(object sender, RoutedEventArgs e)
        {
            string query = tbQuery.Text;
            if (string.IsNullOrEmpty(query)) return;
            _curQuery = Regex.Escape(query);
            _queryResults.Clear();

            int index = 0;
            foreach (CfgLanguageFileInfo fileInfo in _cfgLanguageFileList)
            {
                foreach (CfgLanguageInfo info in fileInfo.CfgLanguageInfoList)
                {
                    bool? result = info.Match(_curQuery, _isCaseSensitive, _isWholeWord, _searchId, _searchContent);
                    if (result == true)
                    {
                        info.Index = index;
                        _queryResults.Add(info);
                        index++;
                    }
                }
            }

            tbqueryCount.Text = $"查询到 {_queryResults.Count} 条结果";
        }

        private void BtCheck_OnClick(object sender, RoutedEventArgs e)
        {
            _queryResults.Clear();
            _curQuery = string.Empty;

            int index = 0;
            Dictionary<string, CfgLanguageInfo> fullList = new Dictionary<string, CfgLanguageInfo>();
            foreach (CfgLanguageFileInfo fileInfo in _cfgLanguageFileList)
            {
                foreach (CfgLanguageInfo info in fileInfo.CfgLanguageInfoList)
                {
                    if (fullList.TryGetValue(info.ID, out CfgLanguageInfo old))
                    {
                        if (old.Index < 0)
                        {
                            old.Index = index;
                            _queryResults.Add(old);
                            index++;
                        }

                        info.Index = index;
                        _queryResults.Add(info);
                        index++;
                    }
                    else
                    {
                        info.Index = -1;
                        fullList.Add(info.ID, info);
                    }
                }
            }
            tbqueryCount.Text = $"查询到 {index} 条重复项";
        }

        private void TbQuery_OnKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                BtQuery_OnClick(null, null);
            }
        }

        private void HyperLinkButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button)
            {
                int index = (int)button.Tag;
                _curLanguageInfo = _queryResults[index];

                if (string.IsNullOrEmpty(_curLanguageInfo.NewID))
                {
                    tipDetail.Title = $"ID: {_curLanguageInfo.ID}";
                }
                else
                {
                    tipDetail.Title = $"ID: {_curLanguageInfo.ID}\nNewID: {_curLanguageInfo.NewID}";
                }

                tipDetail.Subtitle = $"表格: {_curLanguageInfo.FileName} (第{_curLanguageInfo.LineNo}行)";

                tbLanguage1.Text = _curLanguageInfo.zh_CN;
                tbLanguage2.Text = _curLanguageInfo.en;
                tbLanguage3.Text = _curLanguageInfo.zh_TW;
                tbLanguage4.Text = _curLanguageInfo.pt;
                tbLanguage5.Text = _curLanguageInfo.fr;

                tipDetail.Target = button;
                tipDetail.IsOpen = true;
            }
        }

        private void OpenExcelButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button)
            {
                int index = (int)button.Tag;
                _curLanguageInfo = _queryResults[index];
                TipDetail_OnActionButtonClick(null, null);
            }
        }

        private async void TipDetail_OnActionButtonClick(TeachingTip sender, object args)
        {
            if (_curLanguageInfo == null) return;
            string fileName = _curLanguageInfo.FileName;
            if (!string.IsNullOrEmpty(fileName))
            {
                string filePath = Path.GetFullPath($"{_sourcePath}\\{fileName}.xlsx");
                if (File.Exists(filePath))
                {
                    try
                    {
                        StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
                        if (file != null)
                        {
                            await Launcher.LaunchFileAsync(file);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }

            _curLanguageInfo = null;
            tipDetail.IsOpen = false;
        }

        private void cbCase_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                _isCaseSensitive = cb.IsChecked ?? false;
            }
        }

        private void cbWholeWord_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                _isWholeWord = cb.IsChecked ?? false;
            }
        }

        private void cbId_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                _searchId = cb.IsChecked ?? false;
            }
        }

        private void cbContent_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb)
            {
                _searchContent = cb.IsChecked ?? false;
            }
        }

        private async void ButtonReload_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                button.IsEnabled = false;
                await Init();
                button.IsEnabled = true;
            }
        }
    }
}
