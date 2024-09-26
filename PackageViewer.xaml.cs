using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using Windows.System;
using GMTool.Data;
using QRCoder;
using System.IO;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.WinUI.Controls;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PackageViewer : Page
    {
        private readonly string _apkListUrl = "https://bundles.hrgame.com.cn/GrandMaison/jenkins_home/jobs/App_AndroidBuild/builds/";
        private readonly string _ipaListUrl = "https://bundles.hrgame.com.cn/GrandMaison/jenkins_home/jobs/App_iOSBuild/builds/";
        private readonly string _svnCmdArgs = "log http://192.168.1.67/svn/GrandMaisonHRG/ -v";

        private readonly string[] _regionType = {"BetaCh", "Talkweb", "bureau" };
        private string PkgListUrl => _packageTypeIndex switch { 1 => _ipaListUrl, _ => _apkListUrl };
        private string PkgExt => _packageTypeIndex switch { 1 => ".ipa", _ => ".apk" };

        private readonly Regex _listIndexRegex = new("(?<=>)[0-9]+(?=(/</a>))");
        private readonly Regex _indexRegex = new("(?<=>).+(?=(</a>))");
        private readonly Regex _ipaPkgPathRegex = new("(?<=<a href=\").+(?=(\">))");
        private readonly Regex _dateTimeRegex = new("[0-9]+-.+-[0-9]+ +[0-9]+:[0-9]+");
        private readonly Regex _revisionRegex = new("(?<=\\.)[0-9]{4,}(?=\\.)");
        private readonly Regex _repoRegex = new("(?<=/)trunk(?=/)|(?<=/)branches/[^/|\r]*");

        private readonly ObservableCollection<PackageItem> _packageItemList;
        private HttpClient _httpClient = null;

        private string _packageName = string.Empty;
        private string _packageTime = string.Empty;
        private string _fullUrl = string.Empty;
        private string _packageUrl = string.Empty;
        private int _packageTypeIndex = 0;

        public PackageViewer()
        {
            this.InitializeComponent();

            _packageItemList = new ObservableCollection<PackageItem>();
            PackageListView.ItemsSource = _packageItemList;
            Init();
        }

        private async void Init()
        {
            string setting = await CommonData.GetLocalSetting("PackageTypeIndex");
            if (!string.IsNullOrEmpty(setting) && int.TryParse(setting, out int index))
            {
                _packageTypeIndex = index;
            }

            sgType.SelectedIndex = _packageTypeIndex;
            sgType.SelectionChanged += Selector_OnSelectionChanged;
            //sbType.SelectedItem = sbType.Items[_packageTypeIndex];
            //sbType.SelectionChanged += Selector_OnSelectionChanged;
            LoadPackageList();

            string logoPath = Path.Combine(Environment.CurrentDirectory, "Assets/Icon.png");
            if (File.Exists(logoPath))
            {
                imgLogo.Source = new BitmapImage(new Uri(logoPath));
            }
            else
            {
                imgLogo.Visibility = Visibility.Collapsed;
            }
        }

        private async void LoadPackageList()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            try
            {
                HttpResponseMessage message = await _httpClient.GetAsync(PkgListUrl, HttpCompletionOption.ResponseContentRead);
                message.EnsureSuccessStatusCode();
                string content = await message.Content.ReadAsStringAsync();

                List<PackageItem> tmpList = new List<PackageItem>();
                if (!string.IsNullOrEmpty(content))
                {
                    string[] lines = content.Split("\n");
                    foreach (string line in lines)
                    {
                        MatchCollection indexMatch = _listIndexRegex.Matches(line);
                        MatchCollection dateTimeMatch = _dateTimeRegex.Matches(line);

                        if (indexMatch.Count == 1 && dateTimeMatch.Count == 1)
                        {
                            string index = indexMatch[0].Value;
                            string dateTime = dateTimeMatch[0].Value;
                            if (int.TryParse(index, out int id))
                            {
                                tmpList.Add(new PackageItem(id, _packageTypeIndex, dateTime));
                            }
                        }
                    }
                }

                tmpList.Sort();

                _httpClient.Dispose();
                _httpClient = null;

                foreach (PackageItem item in tmpList)
                {
                    _packageItemList.Add(item);
                }

                if (_packageItemList.Count > 0)
                {
                    PackageListView.SelectedIndex = 0;
                    return;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            _httpClient?.Dispose();
            _httpClient = null;
        }

        private void Reload()
        {
            _fullUrl = string.Empty;
            _packageUrl = string.Empty;
            _packageName = string.Empty;

            tbPackageName.Text = string.Empty;
            tbPackageTime.Text = string.Empty;
            imageQRCode.Source = null;
            spFailed.Visibility = Visibility.Collapsed;
            spSuccess.Visibility = Visibility.Collapsed;

            if (_httpClient != null)
            {
                _httpClient.CancelPendingRequests();
                _httpClient.Dispose();
                _httpClient = null;
            }

            _packageItemList.Clear();
            LoadPackageList();
        }

        private void BtRefresh_OnClick(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private async void BtDownload_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_packageUrl))
            {
                await Launcher.LaunchUriAsync(new Uri(_packageUrl));
            }
        }

        private void PackageListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PackageListView.SelectedItem is PackageItem item)
            {
                LoadPackageInfo(item);
            }
        }

        private async void LoadPackageInfo(PackageItem item)
        {
            bool result = false;
            string region = string.Empty;
            if (_packageTypeIndex == 0)
            {
                foreach (string r in _regionType)
                {
                    result = await LoadPackageInfo(item.ID, r);
                    if (result)
                    {
                        region = r;
                        break;
                    }
                }
            }
            else
            {
                result = await LoadPackageInfo(item.ID, string.Empty);
                region = string.Empty;
            }

            if (result)
            {
                if (!item.ExtReady)
                {
                    long size = await LoadPackageSize();
                    BitmapImage image = await GenerateQrCode(item.PackageType);
                    string repo = await LoadRepoInfo();
                    item.SetExtInfo(size, repo, region, image);
                }

                tbPackageRegion.Text = item.Region;
                tbPackageTime.Text = _packageTime;
                tbPackageSize.Text = item.Size;
                tbPackageRepo.Text = item.Repo;
                imageQRCode.Source = item.Image;
                if (item.PackageType == 0)
                {
                    QRCodeFrame.Width = 320;
                    QRCodeFrame.Height = 320;
                }
                else
                {
                    QRCodeFrame.Width = 368;
                    QRCodeFrame.Height = 368;
                }
                imageQRCodeLogo.Visibility = Visibility.Visible;

                spSuccess.Visibility = Visibility.Visible;
                spFailed.Visibility = Visibility.Collapsed;
            }
            else
            {
                spSuccess.Visibility = Visibility.Collapsed;
                spFailed.Visibility = Visibility.Visible;
            }
        }

        private async Task<bool> LoadPackageInfo(int id, string region)
        {
            switch (_packageTypeIndex)
            {
                case 1:
                    _fullUrl = await LoadIpaPackageInfo(id);
                    break;
                default:
                    _fullUrl = $"{PkgListUrl}{id}/archive/{region}/";
                    break;
            }

            if (string.IsNullOrEmpty(_fullUrl))
            {
                return false;
            }
            return await LoadPackageInfo();
        }

        private async Task<bool> LoadPackageInfo()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            try
            {
                HttpResponseMessage message = await _httpClient.GetAsync(_fullUrl, HttpCompletionOption.ResponseContentRead);
                message.EnsureSuccessStatusCode();
                string content = await message.Content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(content))
                {
                    string[] lines = content.Split("\n");
                    foreach (string line in lines)
                    {
                        MatchCollection nameMatches = _indexRegex.Matches(line);
                        if (nameMatches.Count > 0)
                        {
                            foreach (Match match in nameMatches)
                            {
                                string value = match.Value;
                                if (value.EndsWith(PkgExt))
                                {
                                    MatchCollection dateTimeMatch = _dateTimeRegex.Matches(line);
                                    if (dateTimeMatch.Count > 0)
                                    {
                                        _packageTime = dateTimeMatch[0].Value;
                                    }
                                    _packageName = value;
                                    _packageUrl = _fullUrl + value;
                                    break;
                                }
                            }
                        }
                        if (!string.IsNullOrEmpty(_packageName)) break;
                    }

                    MatchCollection matches = _indexRegex.Matches(content);

                    if (matches.Count >= 2)
                    {
                        foreach (Match match in matches)
                        {
                            string value = match.Value;
                            if (value.EndsWith(PkgExt))
                            {
                                _packageName = value;
                                _packageUrl = _fullUrl + value;
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(_packageName))
                {
                    tbPackageName.Text = _packageName;
                    _httpClient.Dispose();
                    _httpClient = null;
                    return true;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            _httpClient?.Dispose();
            _httpClient = null;
            return false;
        }

        private async Task<string> LoadIpaPackageInfo(int id)
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            try
            {
                string url = $"{PkgListUrl}{id}/archive/";
                HttpResponseMessage message = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseContentRead);
                message.EnsureSuccessStatusCode();
                string content = await message.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(content)) throw null;
                string[] lines = content.Split("\n");

                string pkgPath = null;
                foreach (string line in lines)
                {
                    MatchCollection nameMatches = _ipaPkgPathRegex.Matches(line);
                    if (nameMatches.Count > 0)
                    {
                        foreach (Match match in nameMatches)
                        {
                            string value = match.Value;
                            if (value != "../")
                            {
                                pkgPath = value;
                                break;
                            }
                        }
                    }
                    if (!string.IsNullOrEmpty(pkgPath)) break;
                }

                if (string.IsNullOrEmpty(pkgPath)) throw null;

                _httpClient.Dispose();
                _httpClient = null;
                return $"{url}{pkgPath}development/";
            }
            catch (Exception)
            {
                // ignored
            }
            _httpClient?.Dispose();
            _httpClient = null;
            return string.Empty;
        }

        private async Task<string> LoadRepoInfo()
        {
            try
            {
                Match matchRevision = _revisionRegex.Match(_packageName);
                if (!matchRevision.Success)
                {
                    return string.Empty;
                }

                string strRevision = matchRevision.Value;
                if (!int.TryParse(strRevision, out int revision))
                {
                    return string.Empty;
                }

                ProcessStartInfo processInfo = new ProcessStartInfo("svn", $"{_svnCmdArgs} -r {revision}")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };

                Process process = Process.Start(processInfo);
                if (process == null)
                {
                    return string.Empty;
                }

                process.EnableRaisingEvents = true;
                process.ErrorDataReceived += delegate
                {
                    process.Kill();
                    process.Close();
                };

                string repo = string.Empty;
                string result = await process.StandardOutput.ReadToEndAsync();
                Match matchRepo = _repoRegex.Match(result);
                if (matchRepo.Success)
                {
                    repo = matchRepo.Value;
                }
                process.Kill();
                process.Close();
                return repo;
            }
            catch (Exception)
            {
                // ignored
            }
            return string.Empty;
        }

        private async Task<long> LoadPackageSize()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Head, _packageUrl);
                HttpResponseMessage message = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                message.EnsureSuccessStatusCode();
                long? length = message.Content?.Headers?.ContentLength;
                _httpClient.Dispose();
                _httpClient = null;
                if (length is > 0)
                {
                    return length.Value;
                }
            }
            catch (Exception)
            {
                // ignored
            }
            _httpClient?.Dispose();
            _httpClient = null;
            return 0;
        }

        private async Task<BitmapImage> GenerateQrCode(int packageType)
        {
            string qrCodeUrl;
            if (packageType == 0)
            {
                qrCodeUrl = _packageUrl;
            }
            else
            {
                qrCodeUrl = $"itms-services://?action=download-manifest&url={Path.ChangeExtension(_packageUrl, ".plist")}";
            }
            QRCodeGenerator generator = new QRCodeGenerator();
            QRCodeData qrCodeData = generator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.L);

            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeAsBitmapBytes = qrCode.GetGraphic(12,
                new[] { (byte)0x00, (byte)0x00, (byte)0x00, (byte)0xFF },
                new[] { (byte)0xFF, (byte)0xFF, (byte)0xFF, (byte)0xFF });
            string file = Path.GetTempFileName();
            await File.WriteAllBytesAsync(file, qrCodeAsBitmapBytes);
            BitmapImage image = new BitmapImage(new Uri(file));
            return image;
        }

        private void BtCopyUrl_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_packageUrl))
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.RequestedOperation = DataPackageOperation.Copy;
                dataPackage.SetText(_packageUrl);
                Clipboard.SetContent(dataPackage);
            }
        }

        private async void BtOpenInBrowser_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_fullUrl))
            {
                await Launcher.LaunchUriAsync(new Uri(_fullUrl));
            }
        }

        private async void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is Segmented segmented)
            {
                if (_packageTypeIndex != segmented.SelectedIndex)
                {
                    _packageTypeIndex = segmented.SelectedIndex;
                    await CommonData.SaveLocalSetting("PackageTypeIndex", _packageTypeIndex.ToString());
                    Reload();
                }
            }
        }

        //private async void Selector_OnSelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs e)
        //{
        //        SelectorBarItem selectedItem = sender.SelectedItem;
        //        int currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        //        if (_packageTypeIndex != currentSelectedIndex)
        //        {
        //            _packageTypeIndex = currentSelectedIndex;
        //            await CommonData.SaveLocalSetting("PackageTypeIndex", _packageTypeIndex.ToString());
        //            Reload();
        //        }
        //}
    }
}
