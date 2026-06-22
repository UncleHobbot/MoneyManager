namespace MoneyManager.Api.Model.Chart;

/// <summary>
/// A node in the cash-flow Sankey. <see cref="Kind"/> is one of
/// <c>income</c> / <c>hub</c> / <c>expense</c> / <c>uncategorized</c> /
/// <c>other</c> / <c>savings</c> / <c>deficit</c> and drives color and drill
/// behavior on the frontend. <see cref="CategoryId"/> is set only for nodes that
/// map to a single category (income/expense), enabling a category drill-down.
/// </summary>
public sealed record SankeyNode(string Name, int? CategoryId, string Kind);

/// <summary>A weighted Sankey flow from one node to another (value always positive).</summary>
public sealed record SankeyLink(string Source, string Target, decimal Value);

/// <summary>
/// Cash-flow Sankey for a period: income category nodes flow into a single
/// "Total Income" hub, which flows out to expense category nodes plus a "Savings"
/// node. A surplus produces the Savings flow; a deficit (expenses &gt; income) adds
/// a "Deficit" source feeding the hub so inflow and outflow balance.
/// </summary>
public sealed record CashFlowChart(
    IReadOnlyList<SankeyNode> Nodes,
    IReadOnlyList<SankeyLink> Links);
