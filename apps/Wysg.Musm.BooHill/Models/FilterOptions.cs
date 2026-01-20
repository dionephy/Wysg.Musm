using System;

namespace Wysg.Musm.BooHill;

public sealed class FilterOptions
{
    public int? ClusterId { get; set; }
    public string? BuildingNumber { get; set; }
    public string? UnitNumber { get; set; }
    public string? Area { get; set; }
    public bool ShowSold { get; set; }
    public double? MinValue { get; set; }
    public double? MaxValue { get; set; }
    public double? MinRank { get; set; }
    public double? MaxRank { get; set; }
    public SortField SortField { get; set; } = SortField.Default;
    public SortDirection SortDirection { get; set; } = SortDirection.Descending;
}

public enum SortField
{
    Default,
    Building,
    PriceRange
}

public enum SortDirection
{
    Ascending,
    Descending
}
