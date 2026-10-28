using System.Reflection;
using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class ClientInput
{
    public string RaisonSociale { get; set; } = string.Empty;
    public string? Complement { get; set; }
    public string? Famille { get; set; }
    public string? Franchise { get; set; }
    public string? NAdherent { get; set; }
    public string? Rue { get; set; }
    public string? Localisation1 { get; set; }
    public string? Localisation2 { get; set; }
    public string? Cp { get; set; }
    public string? Ville { get; set; }
    public string? Pays { get; set; }
    public string? Telephone { get; set; }
    public string? Portable { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Email2 { get; set; }
    public string? ResponsableAchat { get; set; }
    public string? Representant { get; set; }
    public string? AssistanteCommercial { get; set; }
    public string? EtatClient { get; set; }

    public static ClientInput FromEntity(FranceOptique c) => new()
    {
        RaisonSociale = c.RaisonSociale ?? string.Empty,
        Complement = c.Complement,
        Famille = c.Famille,
        Franchise = c.Franchise,
        NAdherent = c.NAdherent,
        Rue = c.Rue,
        Localisation1 = c.Localisation1,
        Localisation2 = c.Localisation2,
        Cp = c.Cp,
        Ville = c.Ville,
        Pays = c.Pays,
        Telephone = c.Telephone,
        Portable = c.Portable,
        Fax = c.Fax,
        Email = c.Email,
        Email2 = c.Email2,
        ResponsableAchat = c.ResponsableAchat,
        Representant = c.Representant,
        AssistanteCommercial = c.AssistanteCommercial,
        EtatClient = c.EtatClient,
    };

}

public class ParcoursClientInput
{
    public string? Famille { get; set; }
    public string? RaisonSociale { get; set; }
    public string? Complement { get; set; }
    public string? Franchise { get; set; }
    public string? Franchise2 { get; set; }
    public string? Franchise3 { get; set; }
    public string? Franchise4 { get; set; }
    public string? NAdherent { get; set; }
    public string? NAdherent2 { get; set; }
    public string? NAdherent3 { get; set; }
    public string? NAdherent4 { get; set; }
    public string? Rue { get; set; }
    public string? Localisation1 { get; set; }
    public string? Localisation2 { get; set; }
    public string? Cp { get; set; }
    public string? Ville { get; set; }
    public string? Pays { get; set; }
    public string? Telephone { get; set; }
    public string? TelBis { get; set; }
    public string? Portable { get; set; }
    public string? Fax { get; set; }
    public string? Email { get; set; }
    public string? Email2 { get; set; }
    public string? AssistanteCommercial { get; set; }
    public string? ResponsableAchat { get; set; }
    public string? OpEnCours { get; set; }
    public string? StatusVente { get; set; }
    public string? StatutsClients { get; set; }
    public string? EtatClient { get; set; }
    public string? Production { get; set; }
    public string? MagasinPrincipal { get; set; }
    public DateOnly? DateSaisie { get; set; }
    public TimeOnly? HeureSaisie { get; set; }
    public DateOnly? DateRappel { get; set; }
    public TimeOnly? HeureRappel { get; set; }
    public string? Note { get; set; }
    public bool Bloque { get; set; }
    public bool RappelRdv { get; set; }
    public string? Siret { get; set; }
    public string? Siren { get; set; }
    public string? Tva { get; set; }
    public bool FacturationElectronique { get; set; }
    public bool PasEmailing { get; set; }
    public string? MagasinFerme { get; set; }

    public static ParcoursClientInput FromEntity(FranceOptique c) => new()
    {
        Famille = c.Famille,
        RaisonSociale = c.RaisonSociale,
        Complement = c.Complement,
        Franchise = c.Franchise,
        Franchise2 = c.Franchise2,
        Franchise3 = c.Franchise3,
        Franchise4 = c.Franchise4,
        NAdherent = c.NAdherent,
        NAdherent2 = c.NAdherent2,
        NAdherent3 = c.NAdherent3,
        NAdherent4 = c.NAdherent4,
        Rue = c.Rue,
        Localisation1 = c.Localisation1,
        Localisation2 = c.Localisation2,
        Cp = c.Cp,
        Ville = c.Ville,
        Pays = c.Pays,
        Telephone = c.Telephone,
        TelBis = c.TelBis,
        Portable = c.Portable,
        Fax = c.Fax,
        Email = c.Email,
        Email2 = c.Email2,
        AssistanteCommercial = c.AssistanteCommercial,
        ResponsableAchat = c.ResponsableAchat,
        OpEnCours = c.OpEnCours,
        StatusVente = c.StatusVente,
        StatutsClients = c.StatutsClients,
        EtatClient = c.EtatClient,
        Production = c.Production,
        MagasinPrincipal = c.MagasinPrincipal,
        DateSaisie = c.DateSaisie,
        HeureSaisie = c.HeureSaisie,
        DateRappel = c.DateRappel,
        HeureRappel = c.HeureRappel,
        Note = c.Note,
        Bloque = c.Bloque,
        RappelRdv = c.RappelRdv,
        Siret = c.Siret,
        Siren = c.Siren,
        Tva = c.Tva,
        FacturationElectronique = c.FacturationElectronique,
        PasEmailing = c.PasEmailing,
        MagasinFerme = c.MagasinFerme,
    };
}

public class ClientService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private readonly UserClock _clock;

    public ClientService(IDbContextFactory<AppDbContext> dbFactory, UserClock clock)
    {
        _dbFactory = dbFactory;
        _clock = clock;
    }

    // Used positionally by Parcours Client, so this ORDER BY must stay identical to
    // ClientGridQueryService's, or "next client" stops meaning the next one on screen.
    public async Task<List<long>> ListIdsAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques
            .OrderBy(c => c.RaisonSociale)
            .ThenBy(c => c.CleOpl)
            .Select(c => c.CleOpl)
            .ToListAsync();
    }

    public async Task<FranceOptique?> GetAsync(long cleOpl)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CleOpl == cleOpl);
    }

    public async Task<int> CountAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques.CountAsync();
    }

    public async Task<FranceOptique> CreateAsync(
        ClientInput input,
        string? addedBy = null,
        string validationStatus = "validated")
    {
        await using var db = await _dbFactory.CreateDbContextAsync();

        var cleOpl = (await db.Database
            .SqlQuery<long>($"SELECT nextval('france_optique_cle_opl_seq')")
            .ToListAsync())[0];

        var now = _clock.LocalNow;
        var entity = new FranceOptique
        {
            CleOpl = cleOpl,
            AddedBy = addedBy,
            ValidationStatus = validationStatus,
            DateSaisie = DateOnly.FromDateTime(now),
            HeureSaisie = TimeOnly.FromDateTime(now),
        };
        ApplyInputTo(entity, input);
        db.FranceOptiques.Add(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out var constraintName))
        {
            throw new InvalidOperationException(FriendlyForeignKeyMessage(constraintName));
        }

        return entity;
    }

    public async Task UpdateAsync(long cleOpl, ClientInput input)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FranceOptiques.FindAsync(cleOpl)
            ?? throw new InvalidOperationException("Client introuvable.");
        ApplyInputTo(entity, input);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out var constraintName))
        {
            throw new InvalidOperationException(FriendlyForeignKeyMessage(constraintName));
        }
    }

    private static readonly Dictionary<string, string> ClientFieldLabels = new()
    {
        [nameof(ClientInput.RaisonSociale)] = "Raison Sociale",
        [nameof(ClientInput.Complement)] = "Complément",
        [nameof(ClientInput.Famille)] = "Famille",
        [nameof(ClientInput.Franchise)] = "Franchise",
        [nameof(ClientInput.NAdherent)] = "N° Adhérent",
        [nameof(ClientInput.Rue)] = "Rue",
        [nameof(ClientInput.Localisation1)] = "Localisation 1",
        [nameof(ClientInput.Localisation2)] = "Localisation 2",
        [nameof(ClientInput.Cp)] = "Code Postal",
        [nameof(ClientInput.Ville)] = "Ville",
        [nameof(ClientInput.Pays)] = "Pays",
        [nameof(ClientInput.Telephone)] = "Téléphone",
        [nameof(ClientInput.Portable)] = "Portable",
        [nameof(ClientInput.Fax)] = "Fax",
        [nameof(ClientInput.Email)] = "Email",
        [nameof(ClientInput.Email2)] = "Email 2",
        [nameof(ClientInput.ResponsableAchat)] = "Responsable Achat",
        [nameof(ClientInput.Representant)] = "Représentant",
        [nameof(ClientInput.AssistanteCommercial)] = "Assistante Commerciale",
        [nameof(ClientInput.EtatClient)] = "Etat Client",
    };

    public async Task<ApplyResult> UpdateWithConflictCheckAsync(long cleOpl, ClientInput baseline, ClientInput current, bool force = false)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FranceOptiques.FindAsync(cleOpl)
            ?? throw new InvalidOperationException("Client introuvable.");
        var merged = ClientInput.FromEntity(entity);
        var conflicts = ConflictCheck.Apply(baseline, current, merged, force, ClientFieldLabels);

        if (conflicts.Count > 0)
        {
            return new ApplyResult { Success = false, Conflicts = conflicts };
        }

        ApplyInputTo(entity, merged);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out var constraintName))
        {
            throw new InvalidOperationException(FriendlyForeignKeyMessage(constraintName));
        }

        return new ApplyResult { Success = true };
    }

    private static readonly Dictionary<string, string> ParcoursFieldLabels = new()
    {
        [nameof(ParcoursClientInput.Famille)] = "Famille",
        [nameof(ParcoursClientInput.RaisonSociale)] = "Raison Sociale",
        [nameof(ParcoursClientInput.Complement)] = "Complément",
        [nameof(ParcoursClientInput.Franchise)] = "Franchise",
        [nameof(ParcoursClientInput.Franchise2)] = "Franchise 2",
        [nameof(ParcoursClientInput.Franchise3)] = "Franchise 3",
        [nameof(ParcoursClientInput.Franchise4)] = "Franchise 4",
        [nameof(ParcoursClientInput.NAdherent)] = "N° Adhérent",
        [nameof(ParcoursClientInput.NAdherent2)] = "N° Adhérent 2",
        [nameof(ParcoursClientInput.NAdherent3)] = "N° Adhérent 3",
        [nameof(ParcoursClientInput.NAdherent4)] = "N° Adhérent 4",
        [nameof(ParcoursClientInput.Rue)] = "Rue",
        [nameof(ParcoursClientInput.Localisation1)] = "Localisation 1",
        [nameof(ParcoursClientInput.Localisation2)] = "Localisation 2",
        [nameof(ParcoursClientInput.Cp)] = "Code Postal",
        [nameof(ParcoursClientInput.Ville)] = "Ville",
        [nameof(ParcoursClientInput.Pays)] = "Pays",
        [nameof(ParcoursClientInput.Telephone)] = "Téléphone",
        [nameof(ParcoursClientInput.TelBis)] = "Téléphone Bis",
        [nameof(ParcoursClientInput.Portable)] = "Portable",
        [nameof(ParcoursClientInput.Fax)] = "Fax",
        [nameof(ParcoursClientInput.Email)] = "Email",
        [nameof(ParcoursClientInput.Email2)] = "Email 2",
        [nameof(ParcoursClientInput.AssistanteCommercial)] = "Assistante Commerciale",
        [nameof(ParcoursClientInput.ResponsableAchat)] = "Responsable Achat",
        [nameof(ParcoursClientInput.OpEnCours)] = "Opération en Cours",
        [nameof(ParcoursClientInput.StatusVente)] = "Status de la Vente",
        [nameof(ParcoursClientInput.StatutsClients)] = "Statuts Clients",
        [nameof(ParcoursClientInput.EtatClient)] = "Etat Client",
        [nameof(ParcoursClientInput.Production)] = "Production",
        [nameof(ParcoursClientInput.MagasinPrincipal)] = "Magasin Principal",
        [nameof(ParcoursClientInput.DateRappel)] = "Date Rappel",
        [nameof(ParcoursClientInput.HeureRappel)] = "Heure Rappel",
        [nameof(ParcoursClientInput.Note)] = "Note",
        [nameof(ParcoursClientInput.Bloque)] = "Bloqué",
        [nameof(ParcoursClientInput.RappelRdv)] = "Rappel RDV",
        [nameof(ParcoursClientInput.Siret)] = "SIRET",
        [nameof(ParcoursClientInput.Siren)] = "SIREN",
        [nameof(ParcoursClientInput.Tva)] = "TVA",
        [nameof(ParcoursClientInput.FacturationElectronique)] = "Facturation Électronique",
        [nameof(ParcoursClientInput.PasEmailing)] = "Pas d'Emailing",
        [nameof(ParcoursClientInput.MagasinFerme)] = "État du Magasin",
    };

    private static readonly HashSet<string> ExcludedFromConflictCheck = new()
    {
        nameof(ParcoursClientInput.DateSaisie),
        nameof(ParcoursClientInput.HeureSaisie),
    };

    public async Task<ApplyResult> UpdateFullWithConflictCheckAsync(long cleOpl, ParcoursClientInput baseline, ParcoursClientInput current, bool force = false, HashSet<string>? excludeFromConflictCheck = null)
    {
        excludeFromConflictCheck ??= ExcludedFromConflictCheck;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FranceOptiques.FindAsync(cleOpl)
            ?? throw new InvalidOperationException("Client introuvable.");
        var merged = ParcoursClientInput.FromEntity(entity);
        var conflicts = ConflictCheck.Apply(baseline, current, merged, force, ParcoursFieldLabels, excludeFromConflictCheck);

        if (conflicts.Count > 0)
        {
            return new ApplyResult { Success = false, Conflicts = conflicts };
        }

        entity.Famille = merged.Famille;
        entity.RaisonSociale = merged.RaisonSociale;
        entity.Complement = merged.Complement;
        entity.Franchise = merged.Franchise;
        entity.Franchise2 = merged.Franchise2;
        entity.Franchise3 = merged.Franchise3;
        entity.Franchise4 = merged.Franchise4;
        entity.NAdherent = merged.NAdherent;
        entity.NAdherent2 = merged.NAdherent2;
        entity.NAdherent3 = merged.NAdherent3;
        entity.NAdherent4 = merged.NAdherent4;
        entity.Rue = merged.Rue;
        entity.Localisation1 = merged.Localisation1;
        entity.Localisation2 = merged.Localisation2;
        entity.Cp = merged.Cp;
        entity.Ville = merged.Ville;
        entity.Pays = merged.Pays;
        entity.Telephone = merged.Telephone;
        entity.TelBis = merged.TelBis;
        entity.Portable = merged.Portable;
        entity.Fax = merged.Fax;
        entity.Email = merged.Email;
        entity.Email2 = merged.Email2;
        entity.AssistanteCommercial = merged.AssistanteCommercial;
        entity.ResponsableAchat = merged.ResponsableAchat;
        entity.OpEnCours = merged.OpEnCours;
        entity.StatusVente = merged.StatusVente;
        entity.StatutsClients = merged.StatutsClients;
        entity.EtatClient = merged.EtatClient;
        entity.Production = merged.Production;
        entity.MagasinPrincipal = merged.MagasinPrincipal;
        entity.DateSaisie = merged.DateSaisie;
        entity.HeureSaisie = merged.HeureSaisie;
        entity.DateRappel = merged.DateRappel;
        entity.HeureRappel = merged.HeureRappel;
        entity.Note = merged.Note;
        entity.Bloque = merged.Bloque;
        entity.RappelRdv = merged.RappelRdv;
        entity.Siret = merged.Siret;
        entity.Siren = merged.Siren;
        entity.Tva = merged.Tva;
        entity.FacturationElectronique = merged.FacturationElectronique;
        entity.PasEmailing = merged.PasEmailing;
        entity.MagasinFerme = merged.MagasinFerme;

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out var constraintName))
        {
            throw new InvalidOperationException(FriendlyForeignKeyMessage(constraintName));
        }

        return new ApplyResult { Success = true };
    }

    public async Task DeleteAsync(long cleOpl)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.FranceOptiques.FindAsync(cleOpl);
        if (entity is null) return;
        db.FranceOptiques.Remove(entity);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out _))
        {
            throw new InvalidOperationException(
                "Ce client a des interactions ou du chiffre d'affaires enregistrés et ne peut pas être supprimé.");
        }
    }

    internal static void ApplyInputTo(FranceOptique entity, ClientInput input)
    {
        entity.RaisonSociale = input.RaisonSociale;
        entity.Complement = input.Complement;
        entity.Famille = input.Famille;
        entity.Franchise = input.Franchise;
        entity.NAdherent = input.NAdherent;
        entity.Rue = input.Rue;
        entity.Localisation1 = input.Localisation1;
        entity.Localisation2 = input.Localisation2;
        entity.Cp = input.Cp;
        entity.Ville = input.Ville;
        entity.Pays = input.Pays;
        entity.Telephone = input.Telephone;
        entity.Portable = input.Portable;
        entity.Fax = input.Fax;
        entity.Email = input.Email;
        entity.Email2 = input.Email2;
        entity.ResponsableAchat = input.ResponsableAchat;
        entity.Representant = input.Representant;
        entity.AssistanteCommercial = input.AssistanteCommercial;
        entity.EtatClient = input.EtatClient;
    }

    private static string FriendlyForeignKeyMessage(string? constraintName) =>
        constraintName?.Contains("pays") == true
            ? "Pays doit correspondre à un pays existant."
            : "Cette valeur doit correspondre à une référence existante.";
}
