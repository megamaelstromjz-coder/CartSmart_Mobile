namespace CartSmart.Mobile.Api.Dtos;

public record AccountResponse(string Id, string Email, string AuthProvider, DateTimeOffset CreatedAt);

/// <summary>
/// Assumes an inline JSON export body (Section 6.5, Section 11.3 #10). If the backend instead
/// returns a signed download URL, this DTO and ExportAccountDataAsync need a download/polling
/// step rather than a direct parse.
/// </summary>
public record AccountExport(string Email, DateTimeOffset ExportedAt, object Data);
