using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public sealed record ClientGridPage(List<FranceOptique> Rows, int Total);

public sealed record ClientGridFilter(string Column, string Text);

public class ClientGridQueryService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ClientGridQueryService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public static readonly IReadOnlySet<string> FilterableColumns = new HashSet<string>
    {
        "cle_opl", "famille", "raison_sociale", "complement",
        "franchise", "franchise2", "franchise3", "franchise4",
        "n_adherent", "n_adherent2", "n_adherent3", "n_adherent4",
        "rue", "localisation_1", "localisation_2", "cp", "ville", "pays",
        "telephone", "portable", "email", "email2", "assistante_commercial",
        "magasin_principal", "statuts_clients", "representant",
        "responsable_achat", "op_en_cours", "status_vente", "note_perm",
        "etat_client", "validation_status",
    };

    public async Task<ClientGridPage> GetPageAsync(
        IReadOnlyCollection<ClientGridFilter> filters,
        string? sortColumn,
        bool descending,
        int skip,
        int take)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = BuildQuery(db, filters);

        var total = await query.CountAsync();

        var rows = await ApplySort(query, sortColumn, descending)
            .Skip(skip).Take(take).ToListAsync();

        return new ClientGridPage(rows, total);
    }

    public async Task<List<FranceOptique>> GetAllMatchingAsync(
        IReadOnlyCollection<ClientGridFilter> filters,
        string? sortColumn,
        bool descending)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await ApplySort(BuildQuery(db, filters), sortColumn, descending).ToListAsync();
    }

    private static IQueryable<FranceOptique> BuildQuery(
        AppDbContext db, IReadOnlyCollection<ClientGridFilter> filters)
    {
        IQueryable<FranceOptique> query = db.FranceOptiques.AsNoTracking();

        foreach (var filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Text)) continue;
            query = ApplyFilter(query, filter.Column, filter.Text);
        }

        return query;
    }

    // Matches the text the grid RENDERS, not the stored value: etat_client stores
    // "1"/legacy junk, validation_status stores "pending". Hence the CASE mappings.
    private static IQueryable<FranceOptique> ApplyFilter(
        IQueryable<FranceOptique> query, string column, string text)
    {
        var pattern = $"%{EscapeLike(text)}%";

        return column switch
        {
            "cle_opl" => query.Where(c => EF.Functions.ILike(c.CleOpl.ToString(), pattern, LikeEscape)),

            "famille" => query.Where(c => c.Famille != null && EF.Functions.ILike(c.Famille, pattern, LikeEscape)),
            "raison_sociale" => query.Where(c => c.RaisonSociale != null && EF.Functions.ILike(c.RaisonSociale, pattern, LikeEscape)),
            "complement" => query.Where(c => c.Complement != null && EF.Functions.ILike(c.Complement, pattern, LikeEscape)),
            "franchise" => query.Where(c => c.Franchise != null && EF.Functions.ILike(c.Franchise, pattern, LikeEscape)),
            "franchise2" => query.Where(c => c.Franchise2 != null && EF.Functions.ILike(c.Franchise2, pattern, LikeEscape)),
            "franchise3" => query.Where(c => c.Franchise3 != null && EF.Functions.ILike(c.Franchise3, pattern, LikeEscape)),
            "franchise4" => query.Where(c => c.Franchise4 != null && EF.Functions.ILike(c.Franchise4, pattern, LikeEscape)),
            "n_adherent" => query.Where(c => c.NAdherent != null && EF.Functions.ILike(c.NAdherent, pattern, LikeEscape)),
            "n_adherent2" => query.Where(c => c.NAdherent2 != null && EF.Functions.ILike(c.NAdherent2, pattern, LikeEscape)),
            "n_adherent3" => query.Where(c => c.NAdherent3 != null && EF.Functions.ILike(c.NAdherent3, pattern, LikeEscape)),
            "n_adherent4" => query.Where(c => c.NAdherent4 != null && EF.Functions.ILike(c.NAdherent4, pattern, LikeEscape)),
            "rue" => query.Where(c => c.Rue != null && EF.Functions.ILike(c.Rue, pattern, LikeEscape)),
            "localisation_1" => query.Where(c => c.Localisation1 != null && EF.Functions.ILike(c.Localisation1, pattern, LikeEscape)),
            "localisation_2" => query.Where(c => c.Localisation2 != null && EF.Functions.ILike(c.Localisation2, pattern, LikeEscape)),
            "cp" => query.Where(c => c.Cp != null && EF.Functions.ILike(c.Cp, pattern, LikeEscape)),
            "ville" => query.Where(c => c.Ville != null && EF.Functions.ILike(c.Ville, pattern, LikeEscape)),
            "pays" => query.Where(c => c.Pays != null && EF.Functions.ILike(c.Pays, pattern, LikeEscape)),
            "telephone" => query.Where(c => c.Telephone != null && EF.Functions.ILike(c.Telephone, pattern, LikeEscape)),
            "portable" => query.Where(c => c.Portable != null && EF.Functions.ILike(c.Portable, pattern, LikeEscape)),
            "email" => query.Where(c => c.Email != null && EF.Functions.ILike(c.Email, pattern, LikeEscape)),
            "email2" => query.Where(c => c.Email2 != null && EF.Functions.ILike(c.Email2, pattern, LikeEscape)),
            "assistante_commercial" => query.Where(c => c.AssistanteCommercial != null && EF.Functions.ILike(c.AssistanteCommercial, pattern, LikeEscape)),
            "magasin_principal" => query.Where(c => c.MagasinPrincipal != null && EF.Functions.ILike(c.MagasinPrincipal, pattern, LikeEscape)),
            "statuts_clients" => query.Where(c => c.StatutsClients != null && EF.Functions.ILike(c.StatutsClients, pattern, LikeEscape)),
            "representant" => query.Where(c => c.Representant != null && EF.Functions.ILike(c.Representant, pattern, LikeEscape)),
            "responsable_achat" => query.Where(c => c.ResponsableAchat != null && EF.Functions.ILike(c.ResponsableAchat, pattern, LikeEscape)),
            "op_en_cours" => query.Where(c => c.OpEnCours != null && EF.Functions.ILike(c.OpEnCours, pattern, LikeEscape)),
            "status_vente" => query.Where(c => c.StatusVente != null && EF.Functions.ILike(c.StatusVente, pattern, LikeEscape)),
            "note_perm" => query.Where(c => c.NotePerm != null && EF.Functions.ILike(c.NotePerm, pattern, LikeEscape)),

            "etat_client" => query.Where(c =>
                EF.Functions.ILike(c.EtatClient == "1" ? "Client" : "Prospect", pattern, LikeEscape)),

            "validation_status" => query.Where(c =>
                EF.Functions.ILike(c.ValidationStatus == "pending" ? "Non validé" : "", pattern, LikeEscape)),

            "date_saisie" or "heure_saisie" or "date_rappel" or "heure_rappel" =>
                throw new NotSupportedException(
                    $"Le filtre sur la colonne '{column}' n'est pas encore pris en charge côté base. " +
                    "Les colonnes de date et d'heure s'affichent via un format .NET (dd/MM/yyyy, HH:mm) " +
                    "qui n'a pas d'équivalent SQL direct ici."),

            _ => throw new NotSupportedException($"Colonne de filtre inconnue : '{column}'."),
        };
    }

    private static IQueryable<FranceOptique> ApplySort(
        IQueryable<FranceOptique> query, string? column, bool descending)
    {
        if (string.IsNullOrEmpty(column))
        {
            return query.OrderBy(c => c.RaisonSociale).ThenBy(c => c.CleOpl);
        }

        var ordered = column switch
        {
            "cle_opl" => descending
                ? query.OrderByDescending(c => c.CleOpl)
                : query.OrderBy(c => c.CleOpl),
            "famille" => Order(query, c => c.Famille, descending),
            "raison_sociale" => Order(query, c => c.RaisonSociale, descending),
            "complement" => Order(query, c => c.Complement, descending),
            "franchise" => Order(query, c => c.Franchise, descending),
            "franchise2" => Order(query, c => c.Franchise2, descending),
            "franchise3" => Order(query, c => c.Franchise3, descending),
            "franchise4" => Order(query, c => c.Franchise4, descending),
            "n_adherent" => Order(query, c => c.NAdherent, descending),
            "n_adherent2" => Order(query, c => c.NAdherent2, descending),
            "n_adherent3" => Order(query, c => c.NAdherent3, descending),
            "n_adherent4" => Order(query, c => c.NAdherent4, descending),
            "rue" => Order(query, c => c.Rue, descending),
            "localisation_1" => Order(query, c => c.Localisation1, descending),
            "localisation_2" => Order(query, c => c.Localisation2, descending),
            "cp" => Order(query, c => c.Cp, descending),
            "ville" => Order(query, c => c.Ville, descending),
            "pays" => Order(query, c => c.Pays, descending),
            "telephone" => Order(query, c => c.Telephone, descending),
            "portable" => Order(query, c => c.Portable, descending),
            "email" => Order(query, c => c.Email, descending),
            "email2" => Order(query, c => c.Email2, descending),
            "assistante_commercial" => Order(query, c => c.AssistanteCommercial, descending),
            "magasin_principal" => Order(query, c => c.MagasinPrincipal, descending),
            "statuts_clients" => Order(query, c => c.StatutsClients, descending),
            "representant" => Order(query, c => c.Representant, descending),
            "responsable_achat" => Order(query, c => c.ResponsableAchat, descending),
            "op_en_cours" => Order(query, c => c.OpEnCours, descending),
            "status_vente" => Order(query, c => c.StatusVente, descending),
            "note_perm" => Order(query, c => c.NotePerm, descending),

            "etat_client" => Order(query, c => c.EtatClient == "1" ? "Client" : "Prospect", descending),
            "validation_status" => Order(query, c => c.ValidationStatus == "pending" ? "Non validé" : "", descending),

            "date_saisie" => Order(query, c => c.DateSaisie, descending),
            "heure_saisie" => Order(query, c => c.HeureSaisie, descending),
            "date_rappel" => Order(query, c => c.DateRappel, descending),
            "heure_rappel" => Order(query, c => c.HeureRappel, descending),

            _ => throw new NotSupportedException($"Colonne de tri inconnue : '{column}'."),
        };

        return ordered.ThenBy(c => c.CleOpl);
    }

    private static IOrderedQueryable<FranceOptique> Order<TKey>(
        IQueryable<FranceOptique> query,
        System.Linq.Expressions.Expression<Func<FranceOptique, TKey>> key,
        bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);

    private const string LikeEscape = "\\";

    private static string EscapeLike(string input) => input
        .Replace("\\", "\\\\")
        .Replace("%", "\\%")
        .Replace("_", "\\_");
}
