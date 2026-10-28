using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class CaInput
{
    public int Annee { get; set; }
    public int? Montant { get; set; }

    public static CaInput FromEntity(Ca c) => new()
    {
        Annee = c.Annee,
        Montant = c.Ca1,
    };
}

public class CaService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public CaService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Ca>> ListByClientAsync(long cleOpl)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Cas.AsNoTracking()
            .Where(c => c.CleOpl == cleOpl)
            .OrderByDescending(c => c.Annee)
            .ToListAsync();
    }

    public async Task CreateAsync(long cleOpl, int annee, int? montant)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Cas.Add(new Ca { CleOpl = cleOpl, Annee = annee, Ca1 = montant });
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Veuillez saisir une autre année.");
        }
    }

    private static readonly Dictionary<string, string> CaFieldLabels = new()
    {
        [nameof(CaInput.Annee)] = "Année",
        [nameof(CaInput.Montant)] = "Chiffre d'affaire",
    };

    public async Task<ApplyResult> UpdateWithConflictCheckAsync(long cleOpl, int originalAnnee, CaInput baseline, CaInput current, bool force = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fresh = await db.Cas.AsNoTracking().FirstOrDefaultAsync(c => c.CleOpl == cleOpl && c.Annee == originalAnnee)
            ?? throw new InvalidOperationException("Enregistrement de chiffre d'affaire introuvable.");
        var merged = CaInput.FromEntity(fresh);
        var conflicts = ConflictCheck.Apply(baseline, current, merged, force, CaFieldLabels);

        if (conflicts.Count > 0)
        {
            return new ApplyResult { Success = false, Conflicts = conflicts };
        }

        int affected;
        try
        {
            affected = await db.Cas
                .Where(c => c.CleOpl == cleOpl && c.Annee == originalAnnee)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.Annee, merged.Annee)
                    .SetProperty(c => c.Ca1, merged.Montant));
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Veuillez saisir une autre année.");
        }

        if (affected == 0)
        {
            throw new InvalidOperationException("Enregistrement de chiffre d'affaire introuvable.");
        }

        return new ApplyResult { Success = true };
    }

    public async Task DeleteAsync(long cleOpl, int annee)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Cas.FindAsync(cleOpl, annee);
        if (entity is null) return;
        db.Cas.Remove(entity);
        await db.SaveChangesAsync();
    }
}
