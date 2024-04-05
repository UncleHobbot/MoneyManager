namespace MoneyManager.Model.Import;

public enum ImportTypeEnum
{
    Mint_CSV, RBC_CSV, CIBC_CSV
}

public class ImportFileParams(ImportTypeEnum importType, string fileName)
{
    public string FileName { get; } = fileName;
    public ImportTypeEnum ImportType { get; } = importType;
}