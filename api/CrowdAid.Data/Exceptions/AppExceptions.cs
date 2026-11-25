namespace CrowdAid.Data.Exceptions;

// Throw these from services. The API's exception handler turns them into the spec's
// error shape and status code, so controllers stay thin.

public abstract class AppException(int statusCode, string code, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;
}

/// <summary>400 VALIDATION_ERROR with per-field messages (camelCase keys, matching the request JSON).</summary>
public class ValidationFailedException(IDictionary<string, string> fields,
    string message = "Please fix the highlighted fields.")
    : AppException(400, "VALIDATION_ERROR", message)
{
    public IDictionary<string, string> Fields { get; } = fields;

    public static ValidationFailedException For(string field, string message)
        => new(new Dictionary<string, string> { [field] = message });
}

/// <summary>401, e.g. INVALID_CREDENTIALS.</summary>
public class UnauthenticatedException(string code = "UNAUTHENTICATED",
    string message = "Please sign in.")
    : AppException(401, code, message);

/// <summary>403 FORBIDDEN.</summary>
public class ForbiddenException(string message = "You can't do this.")
    : AppException(403, "FORBIDDEN", message);

/// <summary>404 NOT_FOUND. Also used for "not published" and for mismatched reference + email.</summary>
public class NotFoundException(string message = "Not found.", string code = "NOT_FOUND")
    : AppException(404, code, message);

/// <summary>409, e.g. INVALID_TRANSITION, INSUFFICIENT_FUNDS, PAYOUT_DETAILS_REQUIRED.</summary>
public class ConflictException(string code, string message)
    : AppException(409, code, message);

/// <summary>410 TOKEN_EXPIRED / TOKEN_USED (activation links).</summary>
public class GoneException(string code, string message)
    : AppException(410, code, message);

/// <summary>502 PAYMENT_START_FAILED and other upstream failures.</summary>
public class UpstreamException(string code, string message)
    : AppException(502, code, message);
