namespace Application.Common.DTO;

/// <summary>Страница результата с метаданными пагинации.</summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);
