using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class ClientValidationService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ClientValidationService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private static DateTime NowForDb() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

    public async Task<List<FranceOptique>> ListPendingAsync(string? addedBy = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.FranceOptiques.AsNoTracking()
            .Where(c => c.ValidationStatus == "pending");

        if (!string.IsNullOrWhiteSpace(addedBy))
            query = query.Where(c => c.AddedBy == addedBy);

        return await query
            .OrderBy(c => c.DateSaisie)
            .ThenBy(c => c.HeureSaisie)
            .ToListAsync();
    }

    public async Task<List<(string AddedBy, int Count)>> ListPendingAddersAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await db.FranceOptiques.AsNoTracking()
            .Where(c => c.ValidationStatus == "pending" && c.AddedBy != null)
            .GroupBy(c => c.AddedBy!)
            .Select(g => new { AddedBy = g.Key, Count = g.Count() })
            .OrderBy(g => g.AddedBy)
            .ToListAsync();

        return rows.Select(r => (r.AddedBy, r.Count)).ToList();
    }

    public async Task<List<FranceOptique>> ListByAdderAsync(string addedBy)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => c.AddedBy == addedBy)
            .OrderByDescending(c => c.DateSaisie)
            .ThenByDescending(c => c.HeureSaisie)
            .ThenByDescending(c => c.CleOpl)
            .ToListAsync();
    }

    public async Task<List<long>> ListIdsByAdderAsync(string addedBy)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques
            .Where(c => c.AddedBy == addedBy)
            .OrderByDescending(c => c.DateSaisie)
            .ThenByDescending(c => c.HeureSaisie)
            .ThenByDescending(c => c.CleOpl)
            .Select(c => c.CleOpl)
            .ToListAsync();
    }

    public async Task<FranceOptique?> GetIncludingRefusedAsync(long cleOpl)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques.AsNoTracking()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CleOpl == cleOpl);
    }

    public async Task<int> CountPendingAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques.CountAsync(c => c.ValidationStatus == "pending");
    }

    public async Task ValidateAsync(long cleOpl, ClientInput edited, string validatedBy)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FranceOptiques.FindAsync(cleOpl)
            ?? throw new InvalidOperationException("Client introuvable.");
        if (entity.ValidationStatus != "pending")
            throw new InvalidOperationException("Ce client a déjà été traité.");

        ClientService.ApplyInputTo(entity, edited);
        entity.ValidationStatus = "validated";
        entity.ValidatedBy = validatedBy;
        entity.ValidatedAt = NowForDb();

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out _))
        {
            throw new InvalidOperationException("Cette valeur doit correspondre à une référence existante.");
        }
    }

    public async Task RefuseAsync(long cleOpl, string validatedBy, string reason)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FranceOptiques.FindAsync(cleOpl)
            ?? throw new InvalidOperationException("Client introuvable.");
        if (entity.ValidationStatus != "pending")
            throw new InvalidOperationException("Ce client a déjà été traité.");

        entity.ValidationStatus = "refused";
        entity.ValidatedBy = validatedBy;
        entity.ValidatedAt = NowForDb();
        entity.RefusalReason = reason;
        await db.SaveChangesAsync();
    }
}
