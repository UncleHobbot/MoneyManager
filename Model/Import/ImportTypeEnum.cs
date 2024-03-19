namespace MoneyManager.Model.Import;

public enum ImportTypeEnum
{
    MintCSV,
    RBCCSV
}

public class ImportFileParams(ImportTypeEnum importType, string fileName)
{
    public string FileName { get; } = fileName;
    public ImportTypeEnum ImportType { get; } = importType;
}