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
                ShowInfo(InfoBarSeverity.Error, "��ѡ�����");
                return;
            }

            if (string.IsNullOrEmpty(tbDetail.Text))
            {
                ShowInfo(InfoBarSeverity.Error, "����д����");
                return;
            }

            if (_sendTime > 0 && DateTimeOffset.UtcNow.ToUnixTimeSeconds() - _sendTime < 60)
            {
                ShowInfo(InfoBarSeverity.Warning, "���͹���Ƶ�������Ժ����ԡ�");
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
                ShowInfo(InfoBarSeverity.Success, "���������ѷ��͡�");
                _sendTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            else
            {
                ShowInfo(InfoBarSeverity.Error, $"�������鷢��ʧ�ܣ����Ժ����ԡ�({response.StatusCode})");
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
