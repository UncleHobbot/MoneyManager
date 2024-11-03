using CsvHelper.Configuration.Attributes;

namespace MoneyManager.Model.Import;

public class RBCCSV
{
    [Name("Account Type")] public string? AccountType { get; set; }
    [Name("Account Number")] public string? AccountNumber { get; set; }
    [Name("Transaction Date")] public DateTime Date { get; set; }
    [Name("Cheque Number")] public string? ChequeNumber { get; set; }
    [Name("Description 1")] public string? Description1 { get; set; }
    [Name("Description 2")] public string? Description2 { get; set; }
    [Name("CAD$")] public decimal? AmountCAD { get; set; }
    [Name("USD$")] public decimal? AmountUSD { get; set; }
}