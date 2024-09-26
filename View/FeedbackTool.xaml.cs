using GMTool.Data;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeedbackTool : Page
    {
        public FeedbackTool()
        {
            this.InitializeComponent();
        }

        private HttpClient _httpClient;
        private const string Url = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=b650e2f5-e66a-4442-8589-3c9db49bfce6";
        private long _sendTime = 0;

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (cbCatagory.SelectedIndex < 0)
            {
                ShowInfo(InfoBarSeverity.Error, "请选择类别");
                return;
            }

            if (string.IsNullOrEmpty(tbDetail.Text))
            {
                ShowInfo(InfoBarSeverity.Error, "请填写描述");
                return;
            }

            if (_sendTime > 0 && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _sendTime < 60)
            {
                ShowInfo(InfoBarSeverity.Warning, "发送过于频繁，请稍后再试。");
                return;
            }

            if (_httpClient != null)
            {
                _httpClient.CancelPendingRequests();
                _httpClient.Dispose();
                _httpClient = null;
            }

            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);

            FeedbackInfo feedbackInfo = new FeedbackInfo(CommonData.AccountName, cbCatagory.SelectedValue.ToString(), tbDetail.Text);
            string postData = JsonSerializer.Serialize(feedbackInfo, typeof(FeedbackInfo));
            HttpResponseMessage response = await _httpClient.PostAsync(Url, new StringContent(postData, Encoding.UTF8, "application/json"));
            if (response.IsSuccessStatusCode)
            {
                ShowInfo(InfoBarSeverity.Success, "反馈建议已发送。");
                _sendTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                ShowInfo(InfoBarSeverity.Error, $"反馈建议发送失败，请稍后再试。({response.StatusCode})");
            }
            btSubmit.IsEnabled = true;
        }

        private void ShowInfo(InfoBarSeverity severity, string content)
        {
            ibInfo.Severity = severity;
            ibInfo.Message = content;
            ibInfo.IsOpen = true;
        }
    }
}
