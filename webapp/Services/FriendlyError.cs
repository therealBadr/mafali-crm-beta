using Npgsql;

namespace MafaliCrm.Web.Services;

public static class FriendlyError
{
    public static bool IsUniqueViolation(Exception ex) =>
        ex is PostgresException { SqlState: "23505" } ||
        ex.InnerException is PostgresException { SqlState: "23505" };

    public static bool IsForeignKeyViolation(Exception ex, out string? constraintName)
    {
        var pgEx = ex as PostgresException ?? ex.InnerException as PostgresException;
        if (pgEx is { SqlState: "23503" })
        {
            constraintName = pgEx.ConstraintName;
            return true;
        }
        constraintName = null;
        return false;
    }

    public static string ToUserMessage(Exception ex) => ex switch
    {
        InvalidOperationException => ex.Message,
        UnauthorizedAccessException => ex.Message,
        FormatException => "Le format saisi n'est pas reconnu. Vérifiez les valeurs saisies et réessayez.",
        OverflowException => "Une valeur saisie est trop grande ou invalide.",
        _ when IsUniqueViolation(ex) => "Cette valeur existe déjà.",
        _ when IsForeignKeyViolation(ex, out _) => "Cette valeur doit correspondre à une référence existante.",
        _ => "Une erreur inattendue s'est produite. Veuillez réessayer ou contacter le support si le problème persiste.",
    };
}
