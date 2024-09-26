using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Foundation;
using Windows.Foundation.Collections;
using GMTool.Data;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GMTool
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConditionEditor : Page
    {
        private Regex _eciRegex = new ("(?<=\\t| )ECI_.+(?=\r|\n)");
        private Regex _idRegex = new("(?!=)[0-9]+(?=;)");
        private Regex _paramRegex = new("(?<=\\[).+(?=\\])");
        private Regex _commentRegex = new("(?<=//)[^ ]+(?= )");

        private string _filePath = Path.Combine(Environment.CurrentDirectory, "..\\..\\Resources\\PbDefs\\game\\def\\common_def.proto");

        private readonly ObservableCollection<Condition> _conditionList;
        private List<ReachMode> _reachModeList = new() { new(1, "等于"), new(2, "大于"), new(3, "小于"), new(4, "大于等于"), new(5, "小于等于") };

        public ConditionEditor()
        {
            this.InitializeComponent();

            _conditionList = new ObservableCollection<Condition>();
            cbConditionId.ItemsSource = _conditionList;
            cbCompareType.ItemsSource = _reachModeList;

            _filePath = "C:\\Projects\\GrandMaisonHRG\\Resources\\PbDefs\\game\\def\\common_def.proto";
            LoadConditions();
        }

         private async void LoadConditions()
        {
            if (!File.Exists(_filePath)) return;
            string content = await File.ReadAllTextAsync(_filePath);
            if (string.IsNullOrEmpty(content)) return;

            MatchCollection collection = _eciRegex.Matches(content);
            if (collection.Count > 0)
            {
                foreach (Match match in collection)
                {
                    string line = match.Value;
                    line.ReplaceLineEndings();
                    Match idMatch = _idRegex.Match(line);
                    Match commentMatch = _commentRegex.Match(line);
                    if (idMatch.Success && commentMatch.Success)
                    {
                        _conditionList.Add(new Condition(idMatch.Value, commentMatch.Value));
                    }
                }
            }
        }

         private void CbConditionId_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
         {
             cbCompareType.IsEnabled = true;
             tbTargetValue.IsEnabled = true;
         }
    }
}
