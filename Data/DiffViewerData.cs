using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace GMTool.Data;

public class DiffFileItem
{
    public int Index { get; }
    public string Action { get; }
    public string ActionName { get; }
    public Windows.UI.Color ActionColor { get; }
    public string FilePath { get; }

    public DiffFileItem(int index, string content)
    {
        Index = index;
        Action = content.Substring(0, 1);

        (ActionName, Color actionColor) = Action switch
        {
            "M" => ("修改", Color.RoyalBlue),
            "A" => ("新增", Color.Green),
            "D" => ("删除", Color.OrangeRed),
            "R" => ("移动", Color.BlueViolet),
            "L" => ("锁定", Color.Goldenrod),
            _ => (Action, Color.SlateGray),
        };
        ActionColor = GetColor(actionColor);
        FilePath = content.Substring(1).Trim();
    }

    private Windows.UI.Color GetColor(Color color)
    {
        return new Windows.UI.Color
        {
            R = color.R,
            G = color.G,
            B = color.B,
            A = color.A
        };
    }
}