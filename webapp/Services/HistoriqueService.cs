using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class HistoriqueInput
{
    public DateOnly? DateSaisie { get; set; }
    public TimeOnly? HeureSaisie { get; set; }
    public string? Operation { get; set; }
    public string? StatusVente { get; set; }
    public string? Note { get; set; }
    public DateOnly? DateRappel { get; set; }
    public TimeOnly? HeureRappel { get; set; }
    public string? Franchise { get; set; }
    public string? NAdherent { get; set; }
    public string? Franchise2 { get; set; }
    public string? NAdherent2 { get; set; }
    public string? Franchise3 { get; set; }
    public string? NAdherent3 { get; set; }
    public string? Franchise4 { get; set; }
    public string? NAdherent4 { get; set; }
    public string? RaisonSociale { get; set; }
    public string? Cp { get; set; }
    public string? Ville { get; set; }
    public string? AssistanteCommercial { get; set; }
    public string? MagasinPrincipal { get; set; }
    public string? StatutsClients { get; set; }
    public byte[]? FicStk { get; set; }
    public string? FicStkNom { get; set; }

    public static HistoriqueInput FromEntity(Historique h) => new()
    {
        DateSaisie = h.DateSaisie,
        HeureSaisie = h.HeureSaisie,
        Operation = h.Operation,
        StatusVente = h.StatusVente,
        Note = h.Note,
        DateRappel = h.DateRappel,
        HeureRappel = h.HeureRappel,
        Franchise = h.Franchise,
        NAdherent = h.NAdherent,
        Franchise2 = h.Franchise2,
        NAdherent2 = h.NAdherent2,
        Franchise3 = h.Franchise3,
        NAdherent3 = h.NAdherent3,
        Franchise4 = h.Franchise4,
        NAdherent4 = h.NAdherent4,
        RaisonSociale = h.RaisonSociale,
        Cp = h.Cp,
        Ville = h.Ville,
        AssistanteCommercial = h.AssistanteCommercial,
        MagasinPrincipal = h.MagasinPrincipal,
        StatutsClients = h.StatutsClients,
        FicStk = h.FicStk,
        FicStkNom = h.FicStkNom,
    };
}

public class HistoriqueService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public HistoriqueService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<Historique>> GetForClientAsync(long numClient)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Historiques.AsNoTracking()
            .Where(h => h.NumClient == numClient && h.DeletedAt == null)
            .OrderByDescending(h => h.DateSaisie)
            .ThenByDescending(h => h.HeureSaisie)
            .Select(WithoutFicStk)
            .ToListAsync();
    }

    public async Task<List<Historique>> GetPagedAsync(long? fromId, int count)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.Historiques.AsNoTracking().Where(h => h.DeletedAt == null);
        if (fromId is not null)
        {
            query = query.Where(h => h.IdHisto <= fromId.Value);
        }
        return await query
            .OrderByDescending(h => h.IdHisto)
            .Take(count)
            .Select(WithoutFicStk)
            .ToListAsync();
    }

    // Projection behind every grid read. A property added to Historique but missing
    // here reads as null in the UI while sitting correctly in the database.
    private static readonly System.Linq.Expressions.Expression<Func<Historique, Historique>> WithoutFicStk = h => new Historique
    {
        IdHisto = h.IdHisto,
        NumClient = h.NumClient,
        DateSaisie = h.DateSaisie,
        HeureSaisie = h.HeureSaisie,
        AssistanteCommercial = h.AssistanteCommercial,
        DateRappel = h.DateRappel,
        HeureRappel = h.HeureRappel,
        Operation = h.Operation,
        StatusVente = h.StatusVente,
        Note = h.Note,
        Franchise = h.Franchise,
        NAdherent = h.NAdherent,
        Franchise2 = h.Franchise2,
        NAdherent2 = h.NAdherent2,
        Franchise3 = h.Franchise3,
        NAdherent3 = h.NAdherent3,
        Franchise4 = h.Franchise4,
        NAdherent4 = h.NAdherent4,
        RaisonSociale = h.RaisonSociale,
        Cp = h.Cp,
        Ville = h.Ville,
        StatutsClients = h.StatutsClients,
        MagasinPrincipal = h.MagasinPrincipal,
        FicStkNom = h.FicStkNom,
        DeletedAt = h.DeletedAt,
    };

    public async Task<int> GetCountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Historiques.AsNoTracking().CountAsync(h => h.DeletedAt == null);
    }

    public async Task<Historique?> GetByIdAsync(long idHisto)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.Historiques.AsNoTracking()
            .FirstOrDefaultAsync(h => h.IdHisto == idHisto && h.DeletedAt == null);
    }

    public async Task<long> CreateAsync(long numClient, HistoriqueInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = new Historique { NumClient = numClient };
        ApplyInput(entity, input);
        db.Historiques.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out _))
        {
            throw new InvalidOperationException("Ce numéro de client n'existe pas.");
        }

        return entity.IdHisto;
    }

    public async Task UpdateAsync(long idHisto, HistoriqueInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Historiques.FirstOrDefaultAsync(h => h.IdHisto == idHisto && h.DeletedAt == null)
            ?? throw new InvalidOperationException("Historique introuvable.");
        ApplyInput(entity, input);
        await db.SaveChangesAsync();
    }

    private static readonly Dictionary<string, string> HistoriqueFieldLabels = new()
    {
        [nameof(HistoriqueInput.DateSaisie)] = "Date de Saisie",
        [nameof(HistoriqueInput.HeureSaisie)] = "Heure de Saisie",
        [nameof(HistoriqueInput.Operation)] = "Opération",
        [nameof(HistoriqueInput.StatusVente)] = "Status Vente",
        [nameof(HistoriqueInput.Note)] = "Notes",
        [nameof(HistoriqueInput.DateRappel)] = "Date de Rappel",
        [nameof(HistoriqueInput.HeureRappel)] = "Heure de Rappel",
        [nameof(HistoriqueInput.Franchise)] = "Franchise",
        [nameof(HistoriqueInput.NAdherent)] = "N° Adhérent",
        [nameof(HistoriqueInput.Franchise2)] = "Franchise 2",
        [nameof(HistoriqueInput.NAdherent2)] = "N° Adhérent 2",
        [nameof(HistoriqueInput.Franchise3)] = "Franchise 3",
        [nameof(HistoriqueInput.NAdherent3)] = "N° Adhérent 3",
        [nameof(HistoriqueInput.Franchise4)] = "Franchise 4",
        [nameof(HistoriqueInput.NAdherent4)] = "N° Adhérent 4",
        [nameof(HistoriqueInput.RaisonSociale)] = "Raison Sociale",
        [nameof(HistoriqueInput.Cp)] = "Code Postal",
        [nameof(HistoriqueInput.Ville)] = "Ville",
        [nameof(HistoriqueInput.AssistanteCommercial)] = "Assistante Commerciale",
        [nameof(HistoriqueInput.MagasinPrincipal)] = "Magasin",
        [nameof(HistoriqueInput.StatutsClients)] = "Statut Client",
    };

    public async Task<ApplyResult> UpdateWithConflictCheckAsync(long idHisto, HistoriqueInput baseline, HistoriqueInput current, bool force = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Historiques.FirstOrDefaultAsync(h => h.IdHisto == idHisto && h.DeletedAt == null)
            ?? throw new InvalidOperationException("Historique introuvable.");
        var merged = HistoriqueInput.FromEntity(entity);
        var conflicts = ConflictCheck.Apply(baseline, current, merged, force, HistoriqueFieldLabels);

        if (conflicts.Count > 0)
        {
            return new ApplyResult { Success = false, Conflicts = conflicts };
        }

        ApplyInput(entity, merged);
        await db.SaveChangesAsync();
        return new ApplyResult { Success = true };
    }

    public async Task DeleteAsync(long idHisto)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.Historiques.FirstOrDefaultAsync(h => h.IdHisto == idHisto && h.DeletedAt == null);
        if (entity is null) return;
        entity.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    private static void ApplyInput(Historique entity, HistoriqueInput input)
    {
        entity.DateSaisie = input.DateSaisie;
        entity.HeureSaisie = input.HeureSaisie;
        entity.Operation = input.Operation;
        entity.StatusVente = input.StatusVente;
        entity.Note = input.Note;
        entity.DateRappel = input.DateRappel;
        entity.HeureRappel = input.HeureRappel;
        entity.Franchise = input.Franchise;
        entity.NAdherent = input.NAdherent;
        entity.Franchise2 = input.Franchise2;
        entity.NAdherent2 = input.NAdherent2;
        entity.Franchise3 = input.Franchise3;
        entity.NAdherent3 = input.NAdherent3;
        entity.Franchise4 = input.Franchise4;
        entity.NAdherent4 = input.NAdherent4;
        entity.RaisonSociale = input.RaisonSociale;
        entity.Cp = input.Cp;
        entity.Ville = input.Ville;
        entity.AssistanteCommercial = input.AssistanteCommercial;
        entity.MagasinPrincipal = input.MagasinPrincipal;
        entity.StatutsClients = input.StatutsClients;
        entity.FicStk = input.FicStk;
        entity.FicStkNom = input.FicStkNom;
    }
}
