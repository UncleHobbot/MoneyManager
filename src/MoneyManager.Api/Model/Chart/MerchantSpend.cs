namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// Total spend at one merchant over a period. The merchant is the transaction
/// <c>Description</c> (the Rules-normalized display label — see CONTEXT.md
/// "Merchant / Payee"); there is no separate Merchant entity.
/// </summary>
/// <param name="Name">The merchant label (transaction <c>Description</c>).</param>
/// <param name="Amount">Total spend (positive); net of any same-merchant credits.</param>
/// <param name="Count">Number of transactions at this merchant.</param>
public sealed record MerchantSpend(string Name, decimal Amount, int Count);
