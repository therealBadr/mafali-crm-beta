using ClosedXML.Excel;
using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public enum BonCommandeTemplateType
{
    Optique,
    Revendeur,
}

public class BonCommandeTemplateInfo
{
    public bool Exists { get; set; }
    public string FileName { get; set; } = string.Empty;
    public DateTime? LastModifiedUtc { get; set; }
}

public class BonCommandeTemplateService
{
    private readonly string _folder;
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    private readonly UserClock _clock;

    public BonCommandeTemplateService(IWebHostEnvironment env, IDbContextFactory<AppDbContext> dbFactory, UserClock clock)
    {
        _folder = Path.Combine(env.ContentRootPath, "AppData", "BonCommandeTemplates");
        Directory.CreateDirectory(_folder);
        _dbFactory = dbFactory;
        _clock = clock;
    }

    private string PathFor(BonCommandeTemplateType type) => Path.Combine(_folder, FileNameFor(type));

    private static string FileNameFor(BonCommandeTemplateType type) => type switch
    {
        BonCommandeTemplateType.Optique => "modele_bc_optique.xlsx",
        BonCommandeTemplateType.Revendeur => "modele_bc_revendeur.xlsx",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    public BonCommandeTemplateInfo GetInfo(BonCommandeTemplateType type)
    {
        var path = PathFor(type);
        var exists = File.Exists(path);
        return new BonCommandeTemplateInfo
        {
            Exists = exists,
            FileName = FileNameFor(type),
            LastModifiedUtc = exists ? File.GetLastWriteTimeUtc(path) : null,
        };
    }

    public async Task<byte[]?> GetBytesAsync(BonCommandeTemplateType type)
    {
        var path = PathFor(type);
        if (!File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path);
    }

    public async Task SaveAsync(BonCommandeTemplateType type, Stream content)
    {
        await using var fileStream = File.Create(PathFor(type));
        await content.CopyToAsync(fileStream);
    }

    public async Task<long> GetNextBonCommandeNumberAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return (await db.Database
            .SqlQuery<long>($"SELECT nextval('bon_commande_number_seq')")
            .ToListAsync())[0];
    }

    private static List<string> BuildAddressLines(FranceOptique client)
    {
        var lines = new List<string>
        {
            client.RaisonSociale ?? "",
            client.Rue ?? "",
        };
        if (!string.IsNullOrWhiteSpace(client.Localisation1))
        {
            lines.Add(client.Localisation1);
        }
        lines.Add($"{client.Cp} {client.Ville}".Trim());
        return lines;
    }

    public async Task<(byte[] Bytes, long BcNumber)> FillAsync(byte[] templateBytes, FranceOptique client, BonCommandeTemplateType type)
    {
        var bcNumber = await GetNextBonCommandeNumberAsync();

        using var input = new MemoryStream(templateBytes);
        using var workbook = new XLWorkbook(input);
        var sheet = workbook.Worksheets.First();

        var today = _clock.Today.ToDateTime(TimeOnly.MinValue);

        if (type == BonCommandeTemplateType.Optique)
        {
            FillOptique(sheet, client, bcNumber, today);
        }
        else
        {
            FillRevendeur(sheet, client, bcNumber, today);
        }

        using var output = new MemoryStream();
        workbook.SaveAs(output);
        return (output.ToArray(), bcNumber);
    }

    private static void FillOptique(IXLWorksheet sheet, FranceOptique client, long bcNumber, DateTime today)
    {
        sheet.Cell("E6").Value = client.Franchise ?? "";
        sheet.Cell("H6").Value = client.CleOpl;
        sheet.Cell("H6").Style.NumberFormat.Format = "0";
        sheet.Cell("B10").Value = bcNumber;
        sheet.Cell("B10").Style.NumberFormat.Format = "0";
        sheet.Cell("C10").Value = today;
        sheet.Cell("D10").Value = client.AssistanteCommercial ?? "";

        sheet.Cell("B12").Value = string.Join("\n", BuildAddressLines(client));

        var facturationLines = BuildAddressLines(client);
        if (!string.IsNullOrWhiteSpace(client.Tva))
        {
            facturationLines.Add("");
            facturationLines.Add($"TVA: {client.Tva}");
        }
        sheet.Cell("F12").Value = string.Join("\n", facturationLines);

        sheet.Cell("F19").Value = string.Join("\n", BuildAddressLines(client));

        sheet.Cell("C22").Value = client.ResponsableAchat ?? "";
        sheet.Cell("C23").Value = client.Telephone ?? "";
        sheet.Cell("C24").Value = client.Email ?? "";

        foreach (var coord in new[] { "C22", "C23", "C24" })
        {
            sheet.Cell(coord).Style.Alignment.Indent = 1;
        }
    }

    private static void FillRevendeur(IXLWorksheet sheet, FranceOptique client, long bcNumber, DateTime today)
    {
        sheet.Cell("H7").Value = client.CleOpl;
        sheet.Cell("H7").Style.NumberFormat.Format = "0";
        sheet.Cell("B11").Value = bcNumber;
        sheet.Cell("B11").Style.NumberFormat.Format = "0";
        sheet.Cell("C11").Value = today;
        sheet.Cell("D11").Value = client.AssistanteCommercial ?? "";

        sheet.Cell("B13").Value = string.Join("\n", BuildAddressLines(client));

        sheet.Cell("F13").Value = string.Join("\n", BuildAddressLines(client));

        sheet.Cell("F22").Value = string.Join("\n", BuildAddressLines(client));

        sheet.Cell("C25").Value = client.ResponsableAchat ?? "";
        sheet.Cell("C26").Value = client.Telephone ?? "";
        sheet.Cell("C27").Value = client.Email ?? "";

        foreach (var coord in new[] { "C25", "C26", "C27" })
        {
            sheet.Cell(coord).Style.Alignment.Indent = 1;
        }
    }
}
