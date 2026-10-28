using System.Globalization;
using ClosedXML.Excel;
using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public record ImportResult(
    int Added,
    int Modified,
    int Unchanged,
    List<string> SkippedRows,
    Dictionary<string, int> InvalidReferenceCounts);

public class ClientImportExportService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public ClientImportExportService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    private sealed class ColumnDef
    {
        public required string Header { get; init; }
        public required Func<FranceOptique, string> ToText { get; init; }
        public required Action<FranceOptique, string> FromText { get; init; }
        public Func<string, string> Normalize { get; init; } = s => s;
        public string? ReferenceGroup { get; init; }
    }

    private static string? Blank(string s) => s.Length == 0 ? null : s;

    private static bool ParseBool(string s) => s.Trim().ToUpperInvariant() is "VRAI" or "1" or "OUI";

    private static string BoolText(bool b) => b ? "VRAI" : "FAUX";

    private static DateOnly? ParseDate(string s) =>
        DateOnly.TryParseExact(s.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    private static string DateText(DateOnly? d) => d?.ToString("yyyyMMdd") ?? "";

    private static TimeOnly? ParseTime(string s) =>
        TimeOnly.TryParseExact(s.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;

    private static string TimeText(TimeOnly? t) => t?.ToString("HH:mm") ?? "";

    private static readonly List<ColumnDef> Columns = new()
    {
        new() { Header = "Famille", ReferenceGroup = "Famille",
            ToText = c => c.Famille ?? "", FromText = (c, v) => c.Famille = Blank(v) },
        new() { Header = "Raison Sociale",
            ToText = c => c.RaisonSociale ?? "", FromText = (c, v) => c.RaisonSociale = Blank(v) },
        new() { Header = "Complement",
            ToText = c => c.Complement ?? "", FromText = (c, v) => c.Complement = Blank(v) },
        new() { Header = "Franchise", ReferenceGroup = "Franchise",
            ToText = c => c.Franchise ?? "", FromText = (c, v) => c.Franchise = Blank(v) },
        new() { Header = "Franchise 2", ReferenceGroup = "Franchise",
            ToText = c => c.Franchise2 ?? "", FromText = (c, v) => c.Franchise2 = Blank(v) },
        new() { Header = "Franchise 3", ReferenceGroup = "Franchise",
            ToText = c => c.Franchise3 ?? "", FromText = (c, v) => c.Franchise3 = Blank(v) },
        new() { Header = "Franchise 4", ReferenceGroup = "Franchise",
            ToText = c => c.Franchise4 ?? "", FromText = (c, v) => c.Franchise4 = Blank(v) },
        new() { Header = "Rue",
            ToText = c => c.Rue ?? "", FromText = (c, v) => c.Rue = Blank(v) },
        new() { Header = "Localisation 1",
            ToText = c => c.Localisation1 ?? "", FromText = (c, v) => c.Localisation1 = Blank(v) },
        new() { Header = "Localisation 2",
            ToText = c => c.Localisation2 ?? "", FromText = (c, v) => c.Localisation2 = Blank(v) },
        new() { Header = "Code Postal",
            ToText = c => c.Cp ?? "", FromText = (c, v) => c.Cp = Blank(v) },
        new() { Header = "Ville",
            ToText = c => c.Ville ?? "", FromText = (c, v) => c.Ville = Blank(v) },
        new() { Header = "Pays", ReferenceGroup = "Pays",
            ToText = c => c.Pays ?? "", FromText = (c, v) => c.Pays = Blank(v) },
        new() { Header = "Telephone",
            ToText = c => c.Telephone ?? "", FromText = (c, v) => c.Telephone = Blank(v) },
        new() { Header = "Telephone Bis",
            ToText = c => c.TelBis ?? "", FromText = (c, v) => c.TelBis = Blank(v) },
        new() { Header = "Portable",
            ToText = c => c.Portable ?? "", FromText = (c, v) => c.Portable = Blank(v) },
        new() { Header = "Email",
            ToText = c => c.Email ?? "", FromText = (c, v) => c.Email = Blank(v) },
        new() { Header = "Assistante Commerciale", ReferenceGroup = "Assistante",
            ToText = c => c.AssistanteCommercial ?? "", FromText = (c, v) => c.AssistanteCommercial = Blank(v) },
        new() { Header = "Responsable Achat",
            ToText = c => c.ResponsableAchat ?? "", FromText = (c, v) => c.ResponsableAchat = Blank(v) },
        new() { Header = "Representant",
            ToText = c => c.Representant ?? "", FromText = (c, v) => c.Representant = Blank(v) },
        new() { Header = "Operation En Cours",
            ToText = c => c.OpEnCours ?? "", FromText = (c, v) => c.OpEnCours = Blank(v) },
        new() { Header = "Status Vente",
            ToText = c => c.StatusVente ?? "", FromText = (c, v) => c.StatusVente = Blank(v) },
        new() { Header = "Statuts Clients",
            ToText = c => c.StatutsClients ?? "", FromText = (c, v) => c.StatutsClients = Blank(v) },
        new() { Header = "Etat Client",
            ToText = c => c.EtatClient ?? "", FromText = (c, v) => c.EtatClient = Blank(v) },
        new() { Header = "Production",
            ToText = c => c.Production ?? "", FromText = (c, v) => c.Production = Blank(v) },
        new() { Header = "Magasin Principal",
            ToText = c => c.MagasinPrincipal ?? "", FromText = (c, v) => c.MagasinPrincipal = Blank(v) },
        new() { Header = "Date Saisie",
            ToText = c => DateText(c.DateSaisie), FromText = (c, v) => c.DateSaisie = ParseDate(v),
            Normalize = v => DateText(ParseDate(v)) },
        new() { Header = "Heure Saisie",
            ToText = c => TimeText(c.HeureSaisie), FromText = (c, v) => c.HeureSaisie = ParseTime(v),
            Normalize = v => TimeText(ParseTime(v)) },
        new() { Header = "Date Rappel",
            ToText = c => DateText(c.DateRappel), FromText = (c, v) => c.DateRappel = ParseDate(v),
            Normalize = v => DateText(ParseDate(v)) },
        new() { Header = "Heure Rappel",
            ToText = c => TimeText(c.HeureRappel), FromText = (c, v) => c.HeureRappel = ParseTime(v),
            Normalize = v => TimeText(ParseTime(v)) },
        new() { Header = "Note",
            ToText = c => c.Note ?? "", FromText = (c, v) => c.Note = Blank(v) },
        new() { Header = "Bloque",
            ToText = c => BoolText(c.Bloque), FromText = (c, v) => c.Bloque = ParseBool(v),
            Normalize = v => BoolText(ParseBool(v)) },
        new() { Header = "Rappel RDV",
            ToText = c => BoolText(c.RappelRdv), FromText = (c, v) => c.RappelRdv = ParseBool(v),
            Normalize = v => BoolText(ParseBool(v)) },
        new() { Header = "SIRET",
            ToText = c => c.Siret ?? "", FromText = (c, v) => c.Siret = Blank(v) },
        new() { Header = "SIREN",
            ToText = c => c.Siren ?? "", FromText = (c, v) => c.Siren = Blank(v) },
        new() { Header = "Facturation Electronique",
            ToText = c => BoolText(c.FacturationElectronique), FromText = (c, v) => c.FacturationElectronique = ParseBool(v),
            Normalize = v => BoolText(ParseBool(v)) },
        new() { Header = "Pas d'Emailing",
            ToText = c => BoolText(c.PasEmailing), FromText = (c, v) => c.PasEmailing = ParseBool(v),
            Normalize = v => BoolText(ParseBool(v)) },
        new() { Header = "État du Magasin", ReferenceGroup = MagasinFermeGroup,
            ToText = c => c.MagasinFerme ?? "",
            FromText = (c, v) => c.MagasinFerme = CanonicalMagasinFerme(v),
            Normalize = v => CanonicalMagasinFerme(v) ?? "" },
        new() { Header = "N° Adhérent",
            ToText = c => c.NAdherent ?? "", FromText = (c, v) => c.NAdherent = Blank(v) },
        new() { Header = "N° Adhérent 2",
            ToText = c => c.NAdherent2 ?? "", FromText = (c, v) => c.NAdherent2 = Blank(v) },
        new() { Header = "N° Adhérent 3",
            ToText = c => c.NAdherent3 ?? "", FromText = (c, v) => c.NAdherent3 = Blank(v) },
        new() { Header = "N° Adhérent 4",
            ToText = c => c.NAdherent4 ?? "", FromText = (c, v) => c.NAdherent4 = Blank(v) },
        new() { Header = "Email 2",
            ToText = c => c.Email2 ?? "", FromText = (c, v) => c.Email2 = Blank(v) },
    };

    private const string MagasinFermeGroup = "État du Magasin";

    private static string? CanonicalMagasinFerme(string v) =>
        MagasinFermeValues.All.FirstOrDefault(x => string.Equals(x, v, StringComparison.OrdinalIgnoreCase));

    public byte[] Export(IEnumerable<FranceOptique> clients)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Clients");

        sheet.Cell(1, 1).Value = "Cle Opl";
        for (var i = 0; i < Columns.Count; i++)
        {
            sheet.Cell(1, i + 2).Value = Columns[i].Header;
        }
        sheet.Row(1).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(1);

        var row = 2;
        foreach (var c in clients)
        {
            sheet.Cell(row, 1).Value = c.CleOpl;
            for (var i = 0; i < Columns.Count; i++)
            {
                sheet.Cell(row, i + 2).Value = Columns[i].ToText(c);
            }
            row++;
        }

        sheet.Columns().AdjustToContents(1, 1);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public async Task<ImportResult> ImportAsync(Stream fileStream)
    {
        using var workbook = new XLWorkbook(fileStream);
        var sheet = workbook.Worksheets.First();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var existing = await db.FranceOptiques.ToDictionaryAsync(c => c.CleOpl);

        var validByGroup = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Famille"] = (await db.TypeFamilles.Select(f => f.NomFamille).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            ["Franchise"] = (await db.TypeFranchises.Select(f => f.NomFranchise).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            ["Pays"] = (await db.Pays.Select(p => p.NomPays).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            ["Assistante"] = (await db.Assistantes.Select(a => a.PrenomNom).ToListAsync()).ToHashSet(StringComparer.OrdinalIgnoreCase),
            [MagasinFermeGroup] = MagasinFermeValues.All.ToHashSet(StringComparer.OrdinalIgnoreCase),
        };

        int added = 0, modified = 0, unchanged = 0;
        var invalidCounts = new Dictionary<string, int>
            { ["Famille"] = 0, ["Franchise"] = 0, ["Pays"] = 0, ["Assistante"] = 0, [MagasinFermeGroup] = 0 };
        var skipped = new List<string>();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = sheet.Row(rowNum);
            if (row.IsEmpty()) continue;

            var cleOplText = row.Cell(1).GetString().Trim();
            if (!long.TryParse(cleOplText, out var cleOpl) || cleOpl <= 0)
            {
                skipped.Add($"Ligne {rowNum} : Cle Opl manquant ou invalide (\"{cleOplText}\")");
                continue;
            }

            var isNew = !existing.TryGetValue(cleOpl, out var entity);
            if (isNew)
            {
                entity = new FranceOptique { CleOpl = cleOpl };
            }

            var changed = false;
            for (var i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                var raw = row.Cell(i + 2).GetString().Trim();

                if (col.ReferenceGroup is not null && raw.Length > 0 && !validByGroup[col.ReferenceGroup].Contains(raw))
                {
                    invalidCounts[col.ReferenceGroup]++;
                    continue;
                }

                if (col.ToText(entity!) != col.Normalize(raw))
                {
                    col.FromText(entity!, raw);
                    changed = true;
                }
            }

            if (isNew)
            {
                db.FranceOptiques.Add(entity!);
                existing[cleOpl] = entity!;
                added++;
            }
            else if (changed)
            {
                modified++;
            }
            else
            {
                unchanged++;
            }
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (Exception ex) when (FriendlyError.IsForeignKeyViolation(ex, out _))
        {
            throw new InvalidOperationException(
                "Une valeur du fichier ne correspond à aucune référence existante (Pays, Famille…).");
        }

        return new ImportResult(added, modified, unchanged, skipped, invalidCounts);
    }
}
