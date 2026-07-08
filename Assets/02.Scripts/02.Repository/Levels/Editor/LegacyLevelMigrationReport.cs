using System.Collections.Generic;
using System.Text;

internal sealed class LegacyLevelMigrationReport
{
    private readonly List<string> _lineList = new();
    private readonly List<KeyValuePair<LegacyColorKey, int>> _colorMappingList = new();

    private int _sourceAssetCount;
    public int SourceAssetCount => _sourceAssetCount;

    private int _convertedAssetCount;
    public int ConvertedAssetCount => _convertedAssetCount;

    private int _failedAssetCount;
    public int FailedAssetCount => _failedAssetCount;

    public void SetSourceAssetCount(int sourceAssetCount)
    {
        _sourceAssetCount = sourceAssetCount;
    }

    public void AddConvertedAsset()
    {
        _convertedAssetCount++;
    }

    public void AddFailedAsset()
    {
        _failedAssetCount++;
    }

    public void AddColorMapping(LegacyColorKey colorKey, int colorId)
    {
        _colorMappingList.Add(new KeyValuePair<LegacyColorKey, int>(colorKey, colorId));
    }

    public void AddInfo(string message)
    {
        _lineList.Add("[Info] " + message);
    }

    public void AddWarning(string message)
    {
        _lineList.Add("[Warning] " + message);
    }

    public void AddError(string message)
    {
        _lineList.Add("[Error] " + message);
    }

    public string ToText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# Legacy Level Migration Report");
        builder.AppendLine();
        builder.AppendLine($"Source assets: {_sourceAssetCount}");
        builder.AppendLine($"Converted assets: {_convertedAssetCount}");
        builder.AppendLine($"Failed assets: {_failedAssetCount}");
        builder.AppendLine();
        builder.AppendLine("## Color Mapping");

        for (int i = 0; i < _colorMappingList.Count; i++)
        {
            KeyValuePair<LegacyColorKey, int> pair = _colorMappingList[i];
            builder.AppendLine($"ColorId {pair.Value}: {pair.Key.ToHexString()}");
        }

        builder.AppendLine();
        builder.AppendLine("## Messages");

        for (int i = 0; i < _lineList.Count; i++)
        {
            builder.AppendLine(_lineList[i]);
        }

        return builder.ToString();
    }
}
