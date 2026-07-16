namespace CartSmart.Mobile.Api.Dtos;

/// <summary>Uniform error body returned by every endpoint on failure (spec Section 6.7).</summary>
public record ErrorEnvelope(string Code, string Message);
