using CsvHelper.Configuration.Attributes;

namespace MoneyManager.Model.Import;

public class MintCSV
{
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    [Name("Original Description")] public string? OriginalDescription { get; set; }
    public decimal Amount { get; set; }
    [Name("Transaction Type")] public string? TransactionType { get; set; }
    public string Category { get; set; }
    [Name("Account Name")] public string? AccountName { get; set; }
    public string? Labels { get; set; }
    public string? Notes { get; set; }
}