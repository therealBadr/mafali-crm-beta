namespace MafaliCrm.Web.Services;

// Must match france_optique_magasin_ferme_check (migration 22). NULL means open and
// is deliberately absent. A literal that disagrees fails silently at runtime.
public static class MagasinFermeValues
{
    public const string Temporaire = "Fermé temporairement";
    public const string Definitif = "Fermé définitivement";

    public static readonly IReadOnlyList<string> All = new[] { Temporaire, Definitif };
}
