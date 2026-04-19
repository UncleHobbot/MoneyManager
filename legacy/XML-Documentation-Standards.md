# XML Documentation Compliance Guide

## Important: AI Agent Instructions

**As an AI coding agent working on this codebase, you MUST strictly adhere to the following XML documentation guidelines:**

## Core Principles

1. **ALWAYS Add XML Documentation**: Every public class, method, property, and event MUST have XML documentation comments (`///`).

2. **Be Detailed and Meaningful**: Comments must be comprehensive, not generic. Include:
   - What the code does (purpose)
   - How it works (implementation details)
   - When to use it (usage scenarios)
   - Important notes, warnings, or gotchas
   - Examples where helpful

3. **Follow Standard Format**:
   ```csharp
   /// <summary>
   /// Brief description of what this element does.
   /// </summary>
   /// <remarks>
   /// Additional context, implementation details, or usage notes.
   /// </remarks>
   /// <param name="parameterName">Description of the parameter.</param>
   /// <returns>
   /// Description of what is returned.
   /// </returns>
   /// <value>
   /// For properties: description of the value.
   /// </value>
   /// <example>
   /// Optional: code example showing usage.
   /// </example>
   /// <exception cref="ExceptionType">
   /// When this exception might be thrown.
   /// </exception>
   /// <list type="bullet|number|table">
   ///   <item><description>Item description</description></item>
   /// </list>
   ```

## Required Elements

### Classes
```csharp
/// <summary>
/// Clear description of the class's purpose and responsibility.
/// </summary>
/// <remarks>
/// Additional context about:
/// - When to use this class
/// - How it relates to other classes
/// - Key design patterns used
/// - Important implementation details
/// </remarks>
public class MyClass { }
```

### Methods
```csharp
/// <summary>
/// What this method does and why it's needed.
/// </summary>
/// <param name="param1">Description of parameter and its valid range/values.</param>
/// <param name="param2">Description of parameter.</param>
/// <returns>
/// What the method returns and what the return value means.
/// </returns>
/// <remarks>
/// Additional implementation details:
/// - Algorithm used
/// - Performance considerations
/// - Edge cases handled
/// - Dependencies on other methods
/// </remarks>
/// <example>
/// <code>
/// var result = MyMethod("input", 123);
/// Console.WriteLine(result);
/// </code>
/// </example>
public ReturnType MyMethod(string param1, int param2)
{
    // Implementation
}
```

### Properties
```csharp
/// <summary>
/// What this property represents and its purpose.
/// </summary>
/// <value>
/// Description of the valid values and what they mean.
/// </value>
/// <remarks>
/// Additional notes about:
/// - Validation rules
/// - Default values
/// - Side effects of setting this property
/// - Relationship to other properties
/// </remarks>
public string MyProperty { get; set; }
```

### Enums
```csharp
/// <summary>
/// Purpose of this enumeration.
/// </summary>
/// <remarks>
/// When this enum is used and what each value represents.
/// </remarks>
public enum MyEnum
{
    /// <summary>
    /// Description of this enum value.
    /// </summary>
    /// <remarks>
    /// Additional context about when to use this value.
    /// </remarks>
    Value1,
    /// <summary>
    /// Description of this enum value.
    /// </summary>
    Value2
}
```

## Best Practices

### 1. Use Present Tense
- ✅ Good: "Gets the user's balance"
- ❌ Bad: "Getting the user's balance"

### 2. Be Specific
- ✅ Good: "Calculates the monthly average based on the last 12 months"
- ❌ Bad: "Gets the average"

### 3. Document Edge Cases
- ✅ Good: "Returns null if account is not found in database"
- ❌ Bad: "Returns the account"

### 4. Include Examples for Complex Logic
- ✅ Good: Code example showing how to use the method with proper parameters
- ❌ Bad: No examples for non-obvious usage

### 5. Document All Overloads
- ✅ Good: Each overload has its own XML documentation
- ❌ Bad: One documentation block covering all overloads

### 6. Use `<see>` for References
```csharp
/// <summary>
/// Updates the <see cref="Account"/> in the database.
/// </summary>
```

### 7. Use `<list>` for Multiple Items
```csharp
/// <remarks>
/// Supported account types:
/// <list type="bullet">
/// <item><description>Cash - Physical currency</description></item>
/// <item><description>Checking - Bank checking account</description></item>
/// </list>
/// </remarks>
```

### 8. Document Computed Properties
```csharp
/// <summary>
/// Gets the extended amount representing the signed value.
/// </summary>
/// <value>
/// Positive for income, negative for expenses. Calculated from <see cref="IsDebit"/> and <see cref="Amount"/>.
/// </value>
/// <remarks>
/// This is a computed property, not mapped to the database.
/// </remarks>
public decimal AmountExt => IsDebit ? -Amount : Amount;
```

### 9. Document NotMapped Properties
```csharp
/// <summary>
/// Gets the Fluent UI icon for this category.
/// </summary>
/// <remarks>
/// This property is not mapped to the database and is computed dynamically.
/// </remarks>
[NotMapped]
public Icon Icon => GetIcon(Type);
```

### 10. Document Required vs Optional Fields
```csharp
/// <summary>
/// Gets or sets the account name.
/// </summary>
/// <value>
/// The name is required and cannot be null.
/// </value>
public string Name { get; set; } = null!;
```

## What to Document

### Always Document:
- **Public classes**: Purpose, responsibilities, relationships
- **Public methods**: What it does, parameters, return value, side effects
- **Public properties**: What they represent, valid values, constraints
- **Public events**: When they fire, what data they provide
- **Interfaces**: Contract and purpose
- **Enums**: Meaning of each value
- **Delegates**: Purpose and signature

### Consider Documenting:
- **Internal/protected members**: If they're part of a public API surface
- **Private fields**: If they have complex semantics
- **Exceptions**: Under what conditions they're thrown

## Common Mistakes to Avoid

1. **Missing `<returns>` on methods that return values**
2. **Missing `<param>` tags for all parameters**
3. **Vague descriptions**: "Does the thing" vs "Calculates the monthly average"
4. **Missing `<remarks>` for important implementation details**
5. **No examples for non-obvious usage**
6. **Not documenting side effects** (e.g., "This method also updates the cache")
7. **Not mentioning thread safety** for concurrent access
8. **Not documenting nullability** (can a parameter or return value be null?)

## Quality Checklist

Before considering code complete, verify:

- [ ] All public classes have XML documentation
- [ ] All public methods have XML documentation
- [ ] All public properties have XML documentation
- [ ] All public events have XML documentation
- [ ] All parameters are documented with `<param>`
- [ ] All return values are documented with `<returns>`
- [ ] Complex logic has `<remarks>` or `<example>`
- [ ] Enum values have individual documentation
- [ ] References to other types use `<see cref="">`
- [ ] Nullable vs non-nullable is documented
- [ ] Default values are mentioned
- [ ] Thread safety is addressed if applicable
- [ ] Performance characteristics are noted if relevant

## Examples from This Codebase

See the following files for reference examples of high-quality XML documentation:

- `/Data/Account.cs` - Data entity with enums and helper class
- `/Data/Transaction.cs` - Entity with computed properties and DTO pattern
- `/Data/Category.cs` - Complex hierarchy with multiple types and helper methods
- `/Data/Rule.cs` - Enum documentation with examples
- `/Data/Balance.cs` - Simple entity with clear property documentation
- `/Data/DBContext.cs` - Context with interceptor documentation

## Enforcement

**THIS IS MANDATORY**: When you write or modify C# code in this codebase:

1. Add XML documentation for ALL public members
2. Review existing documentation style for consistency
3. Update this guide if new patterns emerge
4. Run `dotnet build` to verify XML documentation compiles
5. Ensure IntelliSense shows useful information in IDE

## Why This Matters

Good XML documentation:
- Enables IntelliSense to show helpful tooltips
- Generates readable API documentation
- Helps new developers understand code quickly
- Reduces bugs through clear contracts
- Documents decisions and constraints
- Facilitates maintenance and refactoring

Poor or missing documentation:
- Causes confusion about API usage
- Leads to incorrect usage patterns
- Increases debugging time
- Makes onboarding difficult
- Results in duplicated code

**Treat documentation as part of the implementation, not an afterthought.**
