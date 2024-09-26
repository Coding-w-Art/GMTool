using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.UI.Xaml;

namespace GMTool.Data;

internal class CfgLanguageInfo
{
    public string FileName { get; }
    public int LineNo { get; }
    public string ID { get; }
    public string NewID { get; }
    public string zh_CN { get; }
    public string en { get; }
    public string zh_TW { get; }
    public string pt { get; }
    public string fr { get; }
    public string th { get; }
    public string id { get; }
    public int Index { get; set; }

    public Visibility OldTagVisibility { get; }
    public Visibility NewTagVisibility { get; }

    private string _queryId { get; }
    private string _queryContent { get; }

    public CfgLanguageInfo(string fileName, int lineNo, string id, string newId, params string[] content)
    {
        FileName = fileName;
        LineNo = lineNo;
        ID = $"{id}";
        NewID = $"{newId}";

        zh_CN = content[0];
        this.en = content[1];
        zh_TW = content[2];
        this.pt = content[3];
        this.fr = content[4];
        this.th = content[5];
        this.id = content[6];

        OldTagVisibility = string.IsNullOrEmpty(ID) ? Visibility.Collapsed : Visibility.Visible;
        NewTagVisibility = string.IsNullOrEmpty(NewID) ? Visibility.Collapsed : Visibility.Visible;
        _queryId = $"{id}|{newId}";
        _queryContent = $"{string.Join("|", content)}";
    }

    public bool? Match(string query, bool caseSensitive, bool wholeWord, bool searchId, bool searchContent)
    {
        try
        {
            if (searchId)
            {
                if (Regex.IsMatch(_queryId,
                        wholeWord ? $"^{query}[^a-zA-Z0-9+]|[^a-zA-Z0-9+]{query}[^a-zA-Z0-9+]|[^a-zA-Z0-9+]{query}$" : query,
                        caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            if (searchContent)
            {
                if (Regex.IsMatch(_queryContent,
                        wholeWord ? $"^{query}[^a-zA-Z0-9+]|[^a-zA-Z0-9+]{query}[^a-zA-Z0-9+]|[^a-zA-Z0-9+]{query}$" : query,
                        caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
        }
        catch (Exception)
        {
            return null;
        }

        return false;
    }
}

internal class CfgLanguageFileInfo
{
    public string FileName { get; set; }
    public List<CfgLanguageInfo> CfgLanguageInfoList { get; set; }

    public CfgLanguageFileInfo(string fileName)
    {
        FileName = fileName;
        CfgLanguageInfoList = new List<CfgLanguageInfo>();
    }

    public void Add(int lineNo, string id, string newId, params string[] content)
    {
        CfgLanguageInfoList.Add(new CfgLanguageInfo(FileName, lineNo, id, newId, content));
    }
}