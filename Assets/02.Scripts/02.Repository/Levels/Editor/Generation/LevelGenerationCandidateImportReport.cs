using System.Collections.Generic;
using System.Text;

internal sealed class LevelGenerationCandidateImportReport
{
    private readonly List<string> _infoList = new List<string>();
    private readonly List<string> _warningList = new List<string>();
    private readonly List<string> _errorList = new List<string>();
    private string _inputPath = string.Empty;
    private string _createdAssetPath = string.Empty;
    private string _reportAssetPath = string.Empty;

    public bool HasError => _errorList.Count > 0;
    public bool IsSaved => !string.IsNullOrEmpty(_createdAssetPath);
    public string InputPath => _inputPath;
    public string CreatedAssetPath => _createdAssetPath;
    public string ReportAssetPath => _reportAssetPath;

    public void SetInputPath(string inputPath)
    {
        _inputPath = inputPath ?? string.Empty;
    }

    public void SetCreatedAssetPath(string createdAssetPath)
    {
        _createdAssetPath = createdAssetPath ?? string.Empty;
    }

    public void SetReportAssetPath(string reportAssetPath)
    {
        _reportAssetPath = reportAssetPath ?? string.Empty;
    }

    public void AddInfo(string message)
    {
        _infoList.Add(message ?? string.Empty);
    }

    public void AddWarning(string message)
    {
        _warningList.Add(message ?? string.Empty);
    }

    public void AddError(string message)
    {
        _errorList.Add(message ?? string.Empty);
    }

    public string GetDialogMessage()
    {
        if (IsSaved)
        {
            return $"생성 후보 import 완료\n\n{_createdAssetPath}";
        }

        if (HasError)
        {
            return $"생성 후보 import 실패\n\n{GetFirstErrorMessage()}";
        }

        return "생성 후보 import가 완료되지 않았습니다. Console 로그를 확인해주세요.";
    }

    public string ToText()
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("DeadLock LevelGenerationCandidate Import Report");
        builder.AppendLine($"Input: {_inputPath}");
        builder.AppendLine($"Saved: {(IsSaved ? _createdAssetPath : "None")}");
        builder.AppendLine($"Report: {(string.IsNullOrEmpty(_reportAssetPath) ? "None" : _reportAssetPath)}");
        builder.AppendLine();
        AppendSection(builder, "Info", _infoList);
        AppendSection(builder, "Warnings", _warningList);
        AppendSection(builder, "Errors", _errorList);
        return builder.ToString();
    }

    private string GetFirstErrorMessage()
    {
        if (_errorList.Count == 0)
        {
            return string.Empty;
        }

        return _errorList[0];
    }

    private static void AppendSection(StringBuilder builder, string title, IReadOnlyList<string> messageList)
    {
        builder.AppendLine($"[{title}]");

        if (messageList.Count == 0)
        {
            builder.AppendLine("- None");
            builder.AppendLine();
            return;
        }

        for (int i = 0; i < messageList.Count; i++)
        {
            builder.AppendLine($"- {messageList[i]}");
        }

        builder.AppendLine();
    }
}
