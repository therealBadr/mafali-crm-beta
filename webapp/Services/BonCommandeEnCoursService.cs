using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class BonCommandeEnCoursService
{
    public const string StatusValue = "Vente (Bon de Commande)";

    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public BonCommandeEnCoursService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<FranceOptique>> ListAsync(string login)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.FranceOptiques.AsNoTracking()
            .Where(c => c.StatusVente == StatusValue
                        && c.AssistanteCommercial != null && EF.Functions.ILike(c.AssistanteCommercial, login))
            .OrderBy(c => c.DateSaisie)
            .ThenBy(c => c.HeureSaisie)
            .ToListAsync();
    }

    public async Task<int> CountAsync(string? login = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.FranceOptiques.Where(c => c.StatusVente == StatusValue);
        if (login is not null)
        {
            query = query.Where(c => c.AssistanteCommercial != null && EF.Functions.ILike(c.AssistanteCommercial, login));
        }
        return await query.CountAsync();
    }
}
