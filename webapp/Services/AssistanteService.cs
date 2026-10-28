using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class AssistanteInput
{
    public string PrenomNom { get; set; } = string.Empty;
    public string? Service { get; set; }
    public bool AllFiltres { get; set; }
    public string? Commentaire { get; set; }

    public static AssistanteInput FromEntity(Assistante a) => new()
    {
        PrenomNom = a.PrenomNom,
        Service = a.Service,
        AllFiltres = a.AllFiltres,
        Commentaire = a.Commentaire,
    };
}

public class AssistanteService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public AssistanteService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Assistante>> ListAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Assistantes
            .OrderBy(a => a.PrenomNom)
            .ToListAsync();
    }

    public async Task CreateAsync(string prenomNom, string? service, bool allFiltres, string? commentaire)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.Assistantes.Add(new Assistante
        {
            PrenomNom = prenomNom,
            Service = service,
            AllFiltres = allFiltres,
            Commentaire = commentaire,
        });
        await SaveOrThrowFriendly(db);
    }

    private static readonly Dictionary<string, string> AssistanteFieldLabels = new()
    {
        [nameof(AssistanteInput.PrenomNom)] = "Prénom Nom",
        [nameof(AssistanteInput.Service)] = "Service",
        [nameof(AssistanteInput.AllFiltres)] = "Accès à tous les Filtres",
        [nameof(AssistanteInput.Commentaire)] = "Commentaire",
    };

    public async Task<ApplyResult> UpdateWithConflictCheckAsync(string currentPrenomNom, AssistanteInput baseline, AssistanteInput current, bool force = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var fresh = await db.Assistantes.AsNoTracking().FirstOrDefaultAsync(a => a.PrenomNom == currentPrenomNom)
            ?? throw new InvalidOperationException("Assistante introuvable.");
        var merged = AssistanteInput.FromEntity(fresh);
        var conflicts = ConflictCheck.Apply(baseline, current, merged, force, AssistanteFieldLabels);

        if (conflicts.Count > 0)
        {
            return new ApplyResult { Success = false, Conflicts = conflicts };
        }

        int affected;
        try
        {
            affected = await db.Assistantes
                .Where(a => a.PrenomNom == currentPrenomNom)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(a => a.PrenomNom, merged.PrenomNom)
                    .SetProperty(a => a.Service, merged.Service)
                    .SetProperty(a => a.AllFiltres, merged.AllFiltres)
                    .SetProperty(a => a.Commentaire, merged.Commentaire));
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Cette assistante existe déjà.");
        }

        if (affected == 0)
        {
            throw new InvalidOperationException("Assistante introuvable.");
        }

        return new ApplyResult { Success = true };
    }

    public async Task DeleteAsync(string prenomNom)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Assistantes.FindAsync(prenomNom);
        if (entity is null) return;
        db.Assistantes.Remove(entity);
        await db.SaveChangesAsync();
    }

    private static async Task SaveOrThrowFriendly(AppDbContext db)
    {
        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Cette assistante existe déjà.");
        }
    }
}
