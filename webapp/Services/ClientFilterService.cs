using System.Linq.Dynamic.Core;
using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public enum FieldType { Text, Number, Date, Time, Boolean }

public record FilterField(string Field, string Label, FieldType Type);

public enum Operator { Eq, Ne, Gt, Gte, Lt, Lte, Between, StartsWith, NotStartsWith, Contains, NotContains }

public class Condition
{
    public string Field { get; set; } = string.Empty;
    public Operator Operator { get; set; }
    public List<string> Values { get; set; } = new();
    public string? Liaison { get; set; }
    public string? GroupLiaison { get; set; }
}

public class ClientFilterService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private readonly UserClock _clock;

    public ClientFilterService(IDbContextFactory<AppDbContext> dbFactory, UserClock clock)
    {
        _dbFactory = dbFactory;
        _clock = clock;
    }

    public static readonly List<FilterField> Fields = new()
    {
        new("famille", "Famille", FieldType.Text),
        new("raison_sociale", "Raison Sociale", FieldType.Text),
        new("complement", "Complément", FieldType.Text),
        new("franchise", "Franchise", FieldType.Text),
        new("franchise2", "Franchise 2", FieldType.Text),
        new("franchise3", "Franchise 3", FieldType.Text),
        new("franchise4", "Franchise 4", FieldType.Text),
        new("n_adherent", "N° Adhérent", FieldType.Text),
        new("n_adherent2", "N° Adhérent 2", FieldType.Text),
        new("n_adherent3", "N° Adhérent 3", FieldType.Text),
        new("n_adherent4", "N° Adhérent 4", FieldType.Text),
        new("rue", "Rue", FieldType.Text),
        new("localisation_1", "Localisation 1", FieldType.Text),
        new("localisation_2", "Localisation 2", FieldType.Text),
        new("cp", "Code Postal", FieldType.Text),
        new("ville", "Ville", FieldType.Text),
        new("pays", "Pays", FieldType.Text),
        new("telephone", "Téléphone", FieldType.Text),
        new("tel_bis", "Téléphone Bis", FieldType.Text),
        new("portable", "Portable", FieldType.Text),
        new("email", "Email", FieldType.Text),
        new("email2", "Email 2", FieldType.Text),
        new("assistante_commercial", "Assistante Commerciale", FieldType.Text),
        new("responsable_achat", "Responsable Achat", FieldType.Text),
        new("representant", "Représentant", FieldType.Text),
        new("op_en_cours", "Opération en Cours", FieldType.Text),
        new("status_vente", "Status Vente", FieldType.Text),
        new("statuts_clients", "Statuts Clients", FieldType.Text),
        new("etat_client", "État Client", FieldType.Text),
        new("production", "Production", FieldType.Text),
        new("magasin_principal", "Magasin Principal", FieldType.Text),
        new("date_saisie", "Date Saisie", FieldType.Date),
        new("heure_saisie", "Heure Saisie", FieldType.Time),
        new("date_rappel", "Date Rappel", FieldType.Date),
        new("heure_rappel", "Heure Rappel", FieldType.Time),
        new("note", "Note", FieldType.Text),
        new("note_perm", "Note Permanente", FieldType.Text),
        new("bloque", "Bloqué", FieldType.Boolean),
        new("rappel_rdv", "Rappel RDV", FieldType.Boolean),
        new("pas_emailing", "Pas d'Emailing", FieldType.Boolean),
        new("magasin_ferme", "État du Magasin", FieldType.Text),
        new("cle_opl", "Cle Opl", FieldType.Number),
    };

    internal static readonly Dictionary<string, string> PropertyName = new()
    {
        ["famille"] = "Famille",
        ["raison_sociale"] = "RaisonSociale",
        ["complement"] = "Complement",
        ["franchise"] = "Franchise",
        ["franchise2"] = "Franchise2",
        ["franchise3"] = "Franchise3",
        ["franchise4"] = "Franchise4",
        ["n_adherent"] = "NAdherent",
        ["n_adherent2"] = "NAdherent2",
        ["n_adherent3"] = "NAdherent3",
        ["n_adherent4"] = "NAdherent4",
        ["rue"] = "Rue",
        ["localisation_1"] = "Localisation1",
        ["localisation_2"] = "Localisation2",
        ["cp"] = "Cp",
        ["ville"] = "Ville",
        ["pays"] = "Pays",
        ["telephone"] = "Telephone",
        ["tel_bis"] = "TelBis",
        ["portable"] = "Portable",
        ["email"] = "Email",
        ["email2"] = "Email2",
        ["assistante_commercial"] = "AssistanteCommercial",
        ["responsable_achat"] = "ResponsableAchat",
        ["representant"] = "Representant",
        ["op_en_cours"] = "OpEnCours",
        ["status_vente"] = "StatusVente",
        ["statuts_clients"] = "StatutsClients",
        ["etat_client"] = "EtatClient",
        ["production"] = "Production",
        ["magasin_principal"] = "MagasinPrincipal",
        ["date_saisie"] = "DateSaisie",
        ["heure_saisie"] = "HeureSaisie",
        ["date_rappel"] = "DateRappel",
        ["heure_rappel"] = "HeureRappel",
        ["note"] = "Note",
        ["note_perm"] = "NotePerm",
        ["bloque"] = "Bloque",
        ["rappel_rdv"] = "RappelRdv",
        ["pas_emailing"] = "PasEmailing",
        ["magasin_ferme"] = "MagasinFerme",
        ["cle_opl"] = "CleOpl",
    };

    public static readonly Dictionary<FieldType, Operator[]> OperatorsByType = new()
    {
        [FieldType.Text] = new[] { Operator.Eq, Operator.Ne, Operator.StartsWith, Operator.NotStartsWith, Operator.Contains, Operator.NotContains, Operator.Gt, Operator.Gte, Operator.Lt, Operator.Lte, Operator.Between },
        [FieldType.Number] = new[] { Operator.Eq, Operator.Ne, Operator.Gt, Operator.Gte, Operator.Lt, Operator.Lte, Operator.Between },
        [FieldType.Date] = new[] { Operator.Eq, Operator.Ne, Operator.Gt, Operator.Gte, Operator.Lt, Operator.Lte, Operator.Between },
        [FieldType.Time] = new[] { Operator.Eq, Operator.Ne, Operator.Gt, Operator.Gte, Operator.Lt, Operator.Lte, Operator.Between },
        [FieldType.Boolean] = new[] { Operator.Eq, Operator.Ne },
    };

    public static readonly Dictionary<Operator, string> SecondValueLiaison = new()
    {
        [Operator.Eq] = "ou",
        [Operator.Ne] = "et",
        [Operator.StartsWith] = "ou",
        [Operator.NotStartsWith] = "et",
        [Operator.Contains] = "ou",
        [Operator.NotContains] = "et",
    };

    internal static readonly Dictionary<string, string> LegacyFieldName = new()
    {
        ["famille"] = "Famille",
        ["raison_sociale"] = "Raison_Sociale",
        ["complement"] = "Complement",
        ["franchise"] = "Franchise",
        ["franchise2"] = "Franchise2",
        ["franchise3"] = "Franchise3",
        ["franchise4"] = "Franchise4",
        ["n_adherent"] = "N_Adherent",
        ["n_adherent2"] = "N_Adherent2",
        ["n_adherent3"] = "N_Adherent3",
        ["n_adherent4"] = "N_Adherent4",
        ["rue"] = "Rue",
        ["localisation_1"] = "Localisation_1",
        ["localisation_2"] = "Localisation_2",
        ["cp"] = "CP",
        ["ville"] = "Ville",
        ["pays"] = "Pays",
        ["telephone"] = "Telephone",
        ["tel_bis"] = "Tel_Bis",
        ["portable"] = "Portable",
        ["email"] = "Email",
        ["email2"] = "Email2",
        ["assistante_commercial"] = "Assistante_Commerciale",
        ["responsable_achat"] = "Responsable_Achat",
        ["representant"] = "Représentant",
        ["op_en_cours"] = "Op_En_Cours",
        ["status_vente"] = "Status_Vente",
        ["statuts_clients"] = "Statuts_Clients",
        ["etat_client"] = "Etat_Client",
        ["production"] = "Production",
        ["magasin_principal"] = "Magasin_Principal",
        ["date_saisie"] = "Date_Saisie",
        ["heure_saisie"] = "Heure_Saisie",
        ["date_rappel"] = "Date_Rappel",
        ["heure_rappel"] = "Heure_Rappel",
        ["note"] = "Note",
        ["note_perm"] = "Note_Perm",
        ["bloque"] = "Bloque",
        ["pas_emailing"] = "Pas_Emailing",
        ["magasin_ferme"] = "Magasin_Ferme",
        ["rappel_rdv"] = "Rappel_RDV",
        ["cle_opl"] = "Cle_Opl",
    };

    private static readonly Dictionary<Operator, string> LegacyOperatorTemplate = new()
    {
        [Operator.Eq] = " ='%1'",
        [Operator.Ne] = " <>'%1'",
        [Operator.Lt] = " <'%1'",
        [Operator.Lte] = " <='%1'",
        [Operator.Gt] = " >'%1'",
        [Operator.Gte] = " >='%1'",
        [Operator.Contains] = " LIKE '%%1%'",
        [Operator.StartsWith] = " LIKE '%1%'",
        [Operator.NotStartsWith] = " NOT LIKE '%1%'",
        [Operator.NotContains] = " NOT LIKE '%%1%'",
        [Operator.Between] = " BETWEEN '%1' AND '%2'",
    };

    public static string BuildLegacyFiltreReel(List<Condition> conditions)
    {
        var fieldClauses = conditions.GroupBy(c => c.Field).Select(group =>
        {
            var list = group.ToList();
            if (list.Count == 1) return BuildSingleConditionClause(list[0]);

            var combined = BuildSingleConditionClause(list[0]);
            for (var i = 1; i < list.Count; i++)
            {
                var liaison = list[i].GroupLiaison ?? "ou";
                combined = $"{combined} {liaison} {BuildSingleConditionClause(list[i])}";
            }
            return $"({combined})";
        });
        return string.Join(" et ", fieldClauses);
    }

    private static string BuildSingleConditionClause(Condition c)
    {
        var name = LegacyFieldName[c.Field];

        if (c.Operator == Operator.Between)
        {
            var clause = $"{name}{ApplyTemplate(c.Operator, c.Values[0])}".Replace("%2", c.Values[1]);
            return $"({clause})";
        }

        if (c.Values.Count > 1 && SecondValueLiaison.ContainsKey(c.Operator))
        {
            var liaison = c.Liaison ?? SecondValueLiaison[c.Operator];
            var parts = c.Values.Select(v => $"{name}{ApplyTemplate(c.Operator, v)}");
            return $"({string.Join($" {liaison} ", parts)})";
        }

        return $"({name}{ApplyTemplate(c.Operator, c.Values[0])})";
    }

    private static string ApplyTemplate(Operator op, string value) =>
        LegacyOperatorTemplate[op].Replace("%1", value);

    public async Task<List<string>> SuggestValuesAsync(string field, string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix)) return new();

        await using var db = await _dbFactory.CreateDbContextAsync();
        IQueryable<string?> query = field switch
        {
            "famille" => db.FranceOptiques.Select(c => c.Famille),
            "raison_sociale" => db.FranceOptiques.Select(c => c.RaisonSociale),
            "complement" => db.FranceOptiques.Select(c => c.Complement),
            "franchise" => db.FranceOptiques.Select(c => c.Franchise),
            "franchise2" => db.FranceOptiques.Select(c => c.Franchise2),
            "franchise3" => db.FranceOptiques.Select(c => c.Franchise3),
            "franchise4" => db.FranceOptiques.Select(c => c.Franchise4),
            "n_adherent" => db.FranceOptiques.Select(c => c.NAdherent),
            "n_adherent2" => db.FranceOptiques.Select(c => c.NAdherent2),
            "n_adherent3" => db.FranceOptiques.Select(c => c.NAdherent3),
            "n_adherent4" => db.FranceOptiques.Select(c => c.NAdherent4),
            "rue" => db.FranceOptiques.Select(c => c.Rue),
            "localisation_1" => db.FranceOptiques.Select(c => c.Localisation1),
            "localisation_2" => db.FranceOptiques.Select(c => c.Localisation2),
            "cp" => db.FranceOptiques.Select(c => c.Cp),
            "ville" => db.FranceOptiques.Select(c => c.Ville),
            "pays" => db.FranceOptiques.Select(c => c.Pays),
            "magasin_principal" => db.FranceOptiques.Select(c => c.MagasinPrincipal),
            "telephone" => db.FranceOptiques.Select(c => c.Telephone),
            "portable" => db.FranceOptiques.Select(c => c.Portable),
            "tel_bis" => db.FranceOptiques.Select(c => c.TelBis),
            "email" => db.FranceOptiques.Select(c => c.Email),
            "email2" => db.FranceOptiques.Select(c => c.Email2),
            "assistante_commercial" => db.FranceOptiques.Select(c => c.AssistanteCommercial),
            "responsable_achat" => db.FranceOptiques.Select(c => c.ResponsableAchat),
            "note" => db.FranceOptiques.Select(c => c.Note),
            _ => Enumerable.Empty<string?>().AsQueryable(),
        };

        return await query
            .Where(v => v != null && EF.Functions.ILike(v!, $"%{prefix}%"))
            .Distinct()
            .OrderBy(v => v)
            .Take(20)
            .Select(v => v!)
            .ToListAsync();
    }

    public async Task<(List<FranceOptique> Rows, int Count)> SearchAsync(List<Condition> conditions)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        IQueryable<FranceOptique> query = db.FranceOptiques.AsNoTracking();

        foreach (var fieldGroup in conditions.GroupBy(c => c.Field))
        {
            query = fieldGroup.Key == FranchiseAnyField
                ? ApplyFranchiseAnyConditions(query, fieldGroup.ToList(), _clock.Today)
                : ApplyFieldConditions(query, fieldGroup.ToList(), _clock.Today);
        }

        var rows = await query.OrderBy(c => c.CleOpl).ToListAsync();
        return (rows, rows.Count);
    }

    public async Task<(List<FranceOptique> Rows, int Count)> SearchByFiltreReelAsync(string filtreReel)
    {
        var (predicate, args) = FiltreReelParser.Parse(filtreReel, _clock.Today);
        await using var db = await _dbFactory.CreateDbContextAsync();
        var rows = await db.FranceOptiques.AsNoTracking()
            .Where(predicate, args)
            .OrderBy(c => c.CleOpl)
            .ToListAsync();
        return (rows, rows.Count);
    }

    public const string FranchiseAnyField = "franchise_any";

    private static readonly string[] FranchiseProps = { "Franchise", "Franchise2", "Franchise3", "Franchise4" };

    private static IQueryable<FranceOptique> ApplyFranchiseAnyConditions(IQueryable<FranceOptique> query, List<Condition> conditions, DateOnly today)
    {
        var args = new List<object>();
        var valueClauses = new List<string>();

        foreach (var c in conditions)
        {
            var idx = args.Count;
            args.Add(CastValue(FieldType.Text, c.Values[0], today));
            var perColumn = FranchiseProps.Select(p => OperatorTemplate(p, Operator.Eq, $"@{idx}", FieldType.Text, args));
            valueClauses.Add($"({string.Join(" || ", perColumn)})");
        }

        return query.Where(string.Join(" || ", valueClauses), args.ToArray());
    }

    private static IQueryable<FranceOptique> ApplyFieldConditions(IQueryable<FranceOptique> query, List<Condition> sameFieldConditions, DateOnly today)
    {
        var meta = Fields.First(f => f.Field == sameFieldConditions[0].Field);
        var prop = PropertyName[sameFieldConditions[0].Field];
        var args = new List<object>();
        var clauses = new List<string>();

        foreach (var c in sameFieldConditions)
        {
            clauses.Add(BuildConditionClause(meta, prop, c, args, today));
        }

        var combined = clauses[0];
        for (var i = 1; i < clauses.Count; i++)
        {
            var joiner = (sameFieldConditions[i].GroupLiaison ?? "ou") == "ou" ? "||" : "&&";
            combined = $"({combined}) {joiner} ({clauses[i]})";
        }

        return query.Where(combined, args.ToArray());
    }

    private static string BuildConditionClause(FilterField meta, string prop, Condition c, List<object> args, DateOnly today)
    {
        if (c.Operator == Operator.Between)
        {
            var i1 = args.Count; args.Add(CastValue(meta.Type, c.Values[0], today));
            var i2 = args.Count; args.Add(CastValue(meta.Type, c.Values[1], today));
            return $"{prop} >= @{i1} && {prop} <= @{i2}";
        }

        if (c.Values.Count > 1 && SecondValueLiaison.ContainsKey(c.Operator))
        {
            var liaison = c.Liaison ?? SecondValueLiaison[c.Operator];
            var joiner = liaison == "ou" ? "||" : "&&";
            var parts = c.Values.Select(v =>
            {
                var idx = args.Count; args.Add(CastValue(meta.Type, v, today));
                return $"({OperatorTemplate(prop, EffectiveOperator(c.Operator, v), $"@{idx}", meta.Type, args)})";
            });
            return string.Join($" {joiner} ", parts);
        }

        var index = args.Count; args.Add(CastValue(meta.Type, c.Values[0], today));
        return OperatorTemplate(prop, EffectiveOperator(c.Operator, c.Values[0]), $"@{index}", meta.Type, args);
    }

    internal static Operator EffectiveOperator(Operator op, string raw) =>
        op == Operator.Lt && raw == "SYSDATE" ? Operator.Lte : op;

    private static readonly (string From, string To)[] AccentFolds =
    {
        ("é", "e"), ("è", "e"), ("ê", "e"), ("ë", "e"),
        ("à", "a"), ("â", "a"), ("ä", "a"),
        ("ù", "u"), ("û", "u"), ("ü", "u"),
        ("ô", "o"), ("ö", "o"),
        ("î", "i"), ("ï", "i"),
        ("ç", "c"),
        ("œ", "oe"),
        ("ñ", "n"),
    };

    private static string Fold(string expr, List<object> args)
    {
        var result = $"{expr}.ToLower()";
        foreach (var (from, to) in AccentFolds)
        {
            var fromIndex = args.Count; args.Add(from);
            var toIndex = args.Count; args.Add(to);
            result = $"{result}.Replace(@{fromIndex}, @{toIndex})";
        }
        return result;
    }

    internal static string OperatorTemplate(string prop, Operator op, string placeholder, FieldType type, List<object> args)
    {
        var lhs = type == FieldType.Text ? Fold(prop, args) : prop;
        var rhs = type == FieldType.Text ? Fold(placeholder, args) : placeholder;

        return op switch
        {
            Operator.Eq => $"({lhs} != null && {lhs} == {rhs})",
            Operator.Ne => $"({lhs} == null || {lhs} != {rhs})",
            Operator.Gt => $"({lhs} != null && {lhs} > {rhs})",
            Operator.Gte => $"({lhs} != null && {lhs} >= {rhs})",
            Operator.Lt => $"({lhs} == null || {lhs} < {rhs})",
            Operator.Lte => $"({lhs} == null || {lhs} <= {rhs})",
            Operator.StartsWith => $"({prop} != null && {lhs}.StartsWith({rhs}))",
            Operator.NotStartsWith => $"({prop} == null || !{lhs}.StartsWith({rhs}))",
            Operator.Contains => $"({prop} != null && {lhs}.Contains({rhs}))",
            Operator.NotContains => $"({prop} == null || !{lhs}.Contains({rhs}))",
            _ => throw new InvalidOperationException($"Unsupported operator: {op}"),
        };
    }

    private static object CastValue(FieldType type, string raw, DateOnly today) =>
        (type, raw) switch
        {
            (FieldType.Date, "SYSDATE") => today,
            (FieldType.Number, _) => long.Parse(raw),
            (FieldType.Date, _) => DateOnly.Parse(raw),
            (FieldType.Time, _) => TimeOnly.Parse(raw),
            (FieldType.Boolean, _) => bool.Parse(raw),
            _ => raw,
        };
}
