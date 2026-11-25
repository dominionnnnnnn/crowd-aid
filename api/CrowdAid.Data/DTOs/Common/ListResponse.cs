namespace CrowdAid.Data.DTOs.Common;

/// <summary>{ "data": [...] }</summary>
public record ListResponse<T>(IReadOnlyList<T> Data);

/// <summary>{ "data": [...], "total": n } (spec #27).</summary>
public record PagedResponse<T>(IReadOnlyList<T> Data, int Total);
