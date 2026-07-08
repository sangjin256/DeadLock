using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

internal sealed class LevelEditorColorMap
{
    private const string DefaultReportPath = "Assets/02.Scripts/02.Repository/Levels/Migrated/LegacyLevelMigrationReport.txt";

    private static readonly Regex ColorLineRegex = new Regex(
        @"^ColorId\s+(?<id>\d+):\s+(?<hex>#[0-9A-Fa-f]{8})$",
        RegexOptions.Compiled);

    private readonly Dictionary<int, Color> _colorByIdDict = new();
    public IReadOnlyDictionary<int, Color> ColorByIdDict => _colorByIdDict;

    private readonly Dictionary<int, string> _legacyHexByIdDict = new();

    public void Reload()
    {
        _colorByIdDict.Clear();
        _legacyHexByIdDict.Clear();

        if (!File.Exists(DefaultReportPath))
        {
            return;
        }

        string[] lineArray = File.ReadAllLines(DefaultReportPath);

        for (int i = 0; i < lineArray.Length; i++)
        {
            Match match = ColorLineRegex.Match(lineArray[i].Trim());

            if (!match.Success)
            {
                continue;
            }

            int colorId = int.Parse(match.Groups["id"].Value);
            string hex = match.Groups["hex"].Value;

            if (ColorUtility.TryParseHtmlString(hex, out Color color))
            {
                _colorByIdDict[colorId] = color;
                _legacyHexByIdDict[colorId] = hex;
            }
        }
    }

    public Color GetColor(int colorId)
    {
        if (_colorByIdDict.TryGetValue(colorId, out Color color))
        {
            return color;
        }

        return GetFallbackColor(colorId);
    }

    public string GetLegacyHex(int colorId)
    {
        if (_legacyHexByIdDict.TryGetValue(colorId, out string hex))
        {
            return hex;
        }

        return "자동색";
    }

    private static Color GetFallbackColor(int colorId)
    {
        if (colorId <= 0)
        {
            return new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        float hue = Mathf.Abs((colorId * 0.173f) % 1f);
        return Color.HSVToRGB(hue, 0.55f, 0.9f);
    }
}
