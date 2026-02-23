namespace Handson.Core.Settings;

public class StorageSettings
{
    public string StoragePath { get; set; } = string.Empty;
    public List<string> FileName { get; set; } = [];
    public string OutputPath { get; set; } = string.Empty;
}
