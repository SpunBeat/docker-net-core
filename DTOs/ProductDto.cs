namespace MyAPI.DTOs;

public record ProductDto(int Id, string Name, decimal Price, string Brand);

public record CreateProductDto(string Name, decimal Price, string Brand);

public record UpdateProductDto(string Name, decimal Price, string Brand);

public record PagedResponse<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages
);
