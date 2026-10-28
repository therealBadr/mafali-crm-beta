using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class FiltreOperatriceInput
{
    public string NomOperateur { get; set; } = string.Empty;
    public string NomFiltre { get; set; } = string.Empty;
    public string FiltreReel { get; set; } = string.Empty;

    public static FiltreOperatriceInput FromEntity(FiltreOperatrice f) => new()
    {
        NomOperateur = f.NomOperateur,
        NomFiltre = f.NomFiltre,
        FiltreReel = f.FiltreReel ?? string.Empty,
    };
}

public class FiltreOperatriceService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly CurrentUserService _currentUser;

    public FiltreOperatriceService(IDbContextFactory<AppDbContext> dbFactory, CurrentUserService currentUser)
    {
        _dbFactory = dbFactory;
        _currentUser = currentUser;
    }

    public async Task<List<FiltreOperatrice>> ListAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FiltreOperatrices
            .OrderBy(f => f.NomOperateur).ThenBy(f => f.NomFiltre)
            .ToListAsync();
    }

    public async Task<List<FiltreOperatrice>> ListByOperatorAsync(string nomOperateur)
    {
        var needle = nomOperateur.Trim().ToLower();
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FiltreOperatrices
            .Where(f => f.NomOperateur.Trim().ToLower() == needle)
            .OrderBy(f => f.NomFiltre)
            .ToListAsync();
    }

    public async Task CreateAsync(string nomOperateur, string nomFiltre, string filtreReel)
    {
        if (!await _currentUser.IsAdminAsync())
        {
            throw new InvalidOperationException("Seuls les administrateurs peuvent enregistrer de nouveaux filtres.");
        }

        await using var db = await _dbFactory.CreateDbContextAsync();
        db.FiltreOperatrices.Add(new FiltreOperatrice
        {
            NomOperateur = nomOperateur,
            NomFiltre = nomFiltre,
            FiltreReel = filtreReel,
        });

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Un filtre avec ce nom existe déjà pour cet opérateur.");
        }
    }

    public async Task<FiltreOperatrice?> GetAsync(string nomOperateur, string nomFiltre)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FiltreOperatrices.AsNoTracking()
            .FirstOrDefaultAsync(f => f.NomOperateur == nomOperateur && f.NomFiltre == nomFiltre);
    }

    private static readonly Dictionary<string, string> FiltreFieldLabels = new()
    {
        [nameof(FiltreOperatriceInput.NomOperateur)] = "Assistante Commerciale",
        [nameof(FiltreOperatriceInput.NomFiltre)] = "Nom du Filtre",
        [nameof(FiltreOperatriceInput.FiltreReel)] = "Filtre Réel",
    };

    public async Task<ApplyResult> UpdateWithConflictCheckAsync(string originalNomOperateur, string originalNomFiltre, FiltreOperatriceInput baseline, FiltreOperatriceInput current, bool force = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fresh = await db.FiltreOperatrices.AsNoTracking()
            .FirstOrDefaultAsync(f => f.NomOperateur == originalNomOperateur && f.NomFiltre == originalNomFiltre)
            ?? throw new InvalidOperationException("Filtre introuvable.");
        var merged = FiltreOperatriceInput.FromEntity(fresh);
        var conflicts = ConflictCheck.Apply(baseline, current, merged, force, FiltreFieldLabels);

        if (conflicts.Count > 0)
        {
            return new ApplyResult { Success = false, Conflicts = conflicts };
        }

        int affected;
        try
        {
            affected = await db.FiltreOperatrices
                .Where(f => f.NomOperateur == originalNomOperateur && f.NomFiltre == originalNomFiltre)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(f => f.NomOperateur, merged.NomOperateur)
                    .SetProperty(f => f.NomFiltre, merged.NomFiltre)
                    .SetProperty(f => f.FiltreReel, merged.FiltreReel));
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Un filtre avec ce nom existe déjà pour cet opérateur.");
        }

        if (affected == 0)
        {
            throw new InvalidOperationException("Filtre introuvable.");
        }

        return new ApplyResult { Success = true };
    }

    public async Task DeleteAsync(string nomOperateur, string nomFiltre)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FiltreOperatrices.FindAsync(nomOperateur, nomFiltre);
        if (entity is null) return;
        db.FiltreOperatrices.Remove(entity);
        await db.SaveChangesAsync();
    }
}
