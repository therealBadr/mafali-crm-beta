using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public record DueRappel(FranceOptique Client, DateTimeOffset Moment);

public class RappelService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public RappelService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public static string OwnerKey(string value) => value.Trim().ToLower();

    public async Task<List<FranceOptique>> ListDueAsync(DateOnly today, string login)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var needle = OwnerKey(login);
        return await db.FranceOptiques.AsNoTracking()
            .Where(c => c.RappelRdv && c.DateRappel != null && c.DateRappel <= today
                        && c.AssistanteCommercial != null && c.AssistanteCommercial.Trim().ToLower() == needle)
            .OrderBy(c => c.DateRappel)
            .ThenBy(c => c.HeureRappel)
            .ToListAsync();
    }

    public async Task<int> CountDueAsync(DateOnly today, string? login = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var query = db.FranceOptiques
            .Where(c => c.RappelRdv && c.DateRappel != null && c.DateRappel <= today);
        if (login is not null)
        {
            var needle = OwnerKey(login);
            query = query.Where(c => c.AssistanteCommercial != null && c.AssistanteCommercial.Trim().ToLower() == needle);
        }
        return await query.CountAsync();
    }

    public async Task<List<FranceOptique>> ListRappelCandidatesAsync(DateTimeOffset now)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var (from, to) = RappelDueTime.CandidateDateRange(now);

        return await db.FranceOptiques.AsNoTracking()
            .Where(c => c.RappelRdv
                        && c.DateRappel != null && c.DateRappel >= from && c.DateRappel <= to
                        && c.HeureRappel != null)
            .OrderBy(c => c.DateRappel).ThenBy(c => c.HeureRappel).ThenBy(c => c.CleOpl)
            .ToListAsync();
    }
}
