namespace CrowdAid.Data.DTOs.Common;

/// <summary>{ "error": { "code", "message", "fields" } } — the one error shape (spec §1).</summary>
public record ErrorResponse(ErrorBody Error)
{
    public static ErrorResponse Of(string code, string message, IDictionary<string, string>? fields = null)
        => new(new ErrorBody(code, message, fields));
}

public record ErrorBody(string Code, string Message, IDictionary<string, string>? Fields);
