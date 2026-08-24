namespace CarRental.Api.Domain;

/// <summary>Document rules (spec.md §3): international pickup requires a passport;
/// domestic pickup accepts a national ID or a passport (assumption A4).</summary>
public static class DocumentPolicy
{
    public static bool IsValid(Location pickup, DocumentType document) =>
        !pickup.IsInternational || document == DocumentType.Passport;

    public static string MismatchMessage(Location pickup) =>
        $"International pickup '{pickup.Name}' requires a Passport.";
}
