using System.Net;
using System.Text.Json;
using CartSmart.Mobile.Api.Dtos;
using RefitApiException = Refit.ApiException;

namespace CartSmart.Mobile.Api;

/// <summary>
/// Parses the {code, message} error envelope (Section 6.7) out of a Refit-thrown
/// <see cref="RefitApiException"/> and maps it to one of our typed exceptions, keyed off HTTP
/// status first and falling back to the envelope's `code` field.
/// </summary>
public static class ApiExceptionMapper
{
    public static async Task<ApiException> MapAsync(RefitApiException refitException)
    {
        var (code, message) = await TryReadEnvelopeAsync(refitException);

        return refitException.StatusCode switch
        {
            HttpStatusCode.Unauthorized => new AuthenticationException(code, message),
            HttpStatusCode.NotFound => new NotFoundException(code, message),
            HttpStatusCode.Conflict => new ConflictException(code, message),
            HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => new ValidationException(code, message),
            _ => new UnexpectedApiException(code, message),
        };
    }

    private static async Task<(string Code, string Message)> TryReadEnvelopeAsync(RefitApiException refitException)
    {
        try
        {
            var content = await refitException.GetContentAsAsync<ErrorEnvelope>();
            if (content is not null)
            {
                return (content.Code, content.Message);
            }
        }
        catch (JsonException)
        {
            // Body wasn't the expected envelope shape — fall through to the generic message below.
        }

        return ("UNKNOWN", refitException.Message);
    }
}
