namespace CarRental.Api.Domain;

/// <summary>Unified vehicle category across all providers (spec.md §2).</summary>
public enum VehicleCategory
{
    Economy,
    Compact,
    Suv,
    Minivan
}

public enum CancellationPolicy
{
    /// <summary>Free cancellation up to 48 hours before pickup.</summary>
    FreeCancellation48h,
    NonRefundable
}

public enum InsuranceType
{
    /// <summary>Comprehensive insurance included in the quoted price.</summary>
    Comprehensive,
    Basic
}

public enum DocumentType
{
    Passport,
    NationalId
}
