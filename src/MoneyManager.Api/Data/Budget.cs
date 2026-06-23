using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoneyManager.Api.Data;

/// <summary>
/// An opt-in recurring monthly spending limit for a (top-level) category. One row
/// per category in v1; per-month history is a future extension via effective-dated
/// rows (do not add constraints that block that). See CONTEXT.md ("Budget") and
/// ADR-0007.
/// </summary>
public class Budget
{
    /// <summary>Primary key (auto-generated).</summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    /// <summary>The category this limit applies to (intended to be a top-level/parent category).</summary>
    public Category Category { get; set; } = null!;

    /// <summary>The recurring monthly limit, in dollars.</summary>
    public decimal Amount { get; set; }
}
