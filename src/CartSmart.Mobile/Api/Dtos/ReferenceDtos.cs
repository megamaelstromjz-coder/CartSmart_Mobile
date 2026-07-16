namespace CartSmart.Mobile.Api.Dtos;

public record ReferenceVersionResponse(string Version);

public record ReferenceProductResponse(string ProductId, string Name, string Category, string DefaultUnit);
