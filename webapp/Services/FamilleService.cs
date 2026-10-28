using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class FamilleService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FamilleService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<string>> ListAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.TypeFamilles
            .OrderBy(f => f.NomFamille)
            .Select(f => f.NomFamille)
            .ToListAsync();
    }

    public async Task CreateAsync(string nomFamille)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.TypeFamilles.Add(new TypeFamille { NomFamille = nomFamille });
        await SaveOrThrowFriendly(db);
    }

    public async Task RenameAsync(string currentName, string newName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        int affected;
        try
        {
            affected = await db.TypeFamilles
                .Where(f => f.NomFamille == currentName)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.NomFamille, newName));
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Cette famille existe déjà.");
        }

        if (affected == 0)
        {
            throw new InvalidOperationException("Famille introuvable.");
        }
    }

    public async Task DeleteAsync(string nomFamille)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.TypeFamilles.FindAsync(nomFamille);
        if (entity is null) return;
        db.TypeFamilles.Remove(entity);
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
            throw new InvalidOperationException("Cette famille existe déjà.");
        }
    }
}
