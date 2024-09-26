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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Web;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlayerTransfer : Page
    {
        public PlayerTransfer()
        {
            this.InitializeComponent();
        }

        private string GetUrl(string pid, string dateTime)
        {
            string url = $"http://192.168.6.225:10001/cos/query_player_backup_req?PlayerID={pid}&ExpectTime={dateTime}";
            return HttpUtility.UrlEncode(url);
        }
    }
}
