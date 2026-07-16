namespace CartSmart.Mobile.Api;

/// <summary>
/// Base for the typed exceptions Section 6.7 asks for, so ViewModels can catch specific
/// exception types instead of parsing the {code, message} envelope themselves.
/// </summary>
public abstract class ApiException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed class AuthenticationException(string code, string message) : ApiException(code, message);
public sealed class ConflictException(string code, string message) : ApiException(code, message);
public sealed class NotFoundException(string code, string message) : ApiException(code, message);
public sealed class ValidationException(string code, string message) : ApiException(code, message);

/// <summary>Fallback for status/code combinations not otherwise mapped.</summary>
public sealed class UnexpectedApiException(string code, string message) : ApiException(code, message);
