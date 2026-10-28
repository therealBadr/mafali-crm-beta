using MafaliCrm.Web.Data;
using MafaliCrm.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace MafaliCrm.Web.Services;

public class FranchiseService
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public FranchiseService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<List<string>> ListAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.TypeFranchises
            .OrderBy(f => f.NomFranchise)
            .Select(f => f.NomFranchise)
            .ToListAsync();
    }

    public async Task CreateAsync(string nomFranchise)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        db.TypeFranchises.Add(new TypeFranchise { NomFranchise = nomFranchise });
        await SaveOrThrowFriendly(db);
    }

    public async Task RenameAsync(string currentName, string newName)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        int affected;
        try
        {
            affected = await db.TypeFranchises
                .Where(f => f.NomFranchise == currentName)
                .ExecuteUpdateAsync(setters => setters.SetProperty(f => f.NomFranchise, newName));
        }
        catch (Exception ex) when (FriendlyError.IsUniqueViolation(ex))
        {
            throw new InvalidOperationException("Cette franchise existe déjà.");
        }

        if (affected == 0)
        {
            throw new InvalidOperationException("Franchise introuvable.");
        }
    }

    public async Task DeleteAsync(string nomFranchise)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var entity = await db.TypeFranchises.FindAsync(nomFranchise);
        if (entity is null) return;
        db.TypeFranchises.Remove(entity);
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
            throw new InvalidOperationException("Cette franchise existe déjà.");
        }
    }
}
