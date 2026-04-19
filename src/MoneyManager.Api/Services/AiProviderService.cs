using Microsoft.EntityFrameworkCore;
using MoneyManager.Api.Data;

namespace MoneyManager.Api.Services;

/// <summary>
/// Manages AI provider configurations for financial analysis.
/// </summary>
public class AiProviderService(IDbContextFactory<DataContext> contextFactory)
{
    /// <summary>
    /// Gets all configured AI providers.
    /// </summary>
    /// <returns>A list of <see cref="AiProvider"/> ordered by name.</returns>
    public async Task<List<AiProvider>> GetProvidersAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return await ctx.AiProviders.OrderBy(p => p.Name).ToListAsync();
    }

    /// <summary>
    /// Gets the default AI provider, or null if none is configured.
    /// </summary>
    /// <returns>The default <see cref="AiProvider"/>, or null.</returns>
    public async Task<AiProvider?> GetDefaultProviderAsync()
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return await ctx.AiProviders.FirstOrDefaultAsync(p => p.IsDefault)
               ?? await ctx.AiProviders.FirstOrDefaultAsync();
    }

    /// <summary>
    /// Gets an AI provider by ID.
    /// </summary>
    /// <param name="id">The provider ID.</param>
    /// <returns>The <see cref="AiProvider"/> if found, or null.</returns>
    public async Task<AiProvider?> GetProviderByIdAsync(int id)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        return await ctx.AiProviders.FindAsync(id);
    }

    /// <summary>
    /// Adds a new AI provider.
    /// </summary>
    /// <param name="provider">The provider to add.</param>
    /// <returns>The added <see cref="AiProvider"/> with generated ID.</returns>
    public async Task<AiProvider> AddProviderAsync(AiProvider provider)
    {
        var ctx = await contextFactory.CreateDbContextAsync();

        if (provider.IsDefault)
        {
            var others = await ctx.AiProviders.Where(p => p.IsDefault).ToListAsync();
            foreach (var other in others)
                other.IsDefault = false;
        }

        ctx.AiProviders.Add(provider);
        await ctx.SaveChangesAsync();
        return provider;
    }

    /// <summary>
    /// Updates an existing AI provider.
    /// </summary>
    /// <param name="provider">The provider with updated values.</param>
    /// <returns>The updated <see cref="AiProvider"/>, or null if not found.</returns>
    public async Task<AiProvider?> UpdateProviderAsync(AiProvider provider)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var existing = await ctx.AiProviders.FindAsync(provider.Id);
        if (existing == null) return null;

        existing.Name = provider.Name;
        existing.ProviderType = provider.ProviderType;
        existing.EncryptedApiKey = provider.EncryptedApiKey;
        existing.ApiUrl = provider.ApiUrl;
        existing.Model = provider.Model;

        if (provider.IsDefault)
        {
            var others = await ctx.AiProviders.Where(p => p.IsDefault && p.Id != provider.Id).ToListAsync();
            foreach (var other in others)
                other.IsDefault = false;
        }
        existing.IsDefault = provider.IsDefault;

        await ctx.SaveChangesAsync();
        return existing;
    }

    /// <summary>
    /// Deletes an AI provider by ID.
    /// </summary>
    /// <param name="id">The provider ID to delete.</param>
    /// <returns>True if the provider was deleted, false if not found.</returns>
    public async Task<bool> DeleteProviderAsync(int id)
    {
        var ctx = await contextFactory.CreateDbContextAsync();
        var provider = await ctx.AiProviders.FindAsync(id);
        if (provider == null) return false;

        ctx.AiProviders.Remove(provider);
        await ctx.SaveChangesAsync();
        return true;
    }
}
