namespace CarRental.Api.Domain;

public static class RentalPeriod
{
    /// <summary>Enumerates the rental nights: every date in [from, to).
    /// The checkout day is not a night (spec.md D1).</summary>
    public static IEnumerable<DateOnly> Nights(DateOnly from, DateOnly to)
    {
        for (var night = from; night < to; night = night.AddDays(1))
        {
            yield return night;
        }
    }

    public static int NightCount(DateOnly from, DateOnly to) => to.DayNumber - from.DayNumber;
}
