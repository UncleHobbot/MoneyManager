namespace MoneyManager.Model.Import;

public class CIBCCSV
{
    [CsvHelper.Configuration.Attributes.Index(0)]
    public DateTime Date { get; set; }
    [CsvHelper.Configuration.Attributes.Index(1)]
    public string Description { get; set; }
    [CsvHelper.Configuration.Attributes.Index(2)]
    public decimal? AmountDebit { get; set; }
    [CsvHelper.Configuration.Attributes.Index(3)]
    public decimal? AmountCredit { get; set; }
    [CsvHelper.Configuration.Attributes.Index(4)]
    public string AccountNumber { get; set; }
}