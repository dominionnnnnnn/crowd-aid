using System.Net.Mail;
using System.Text.RegularExpressions;
using CrowdAid.Data.Exceptions;

namespace CrowdAid.Service.Validation;

/// <summary>
/// Collects field errors, then throws one 400 VALIDATION_ERROR with all of them
/// (the frontend shows them together in the error summary). Field keys match the request JSON.
/// </summary>
public partial class FormValidator
{
    private readonly Dictionary<string, string> _errors = new();

    public bool HasErrors => _errors.Count > 0;

    public void Add(string field, string message) => _errors.TryAdd(field, message);

    /// <summary>Trimmed value, or "" (with an error) when missing or too long.</summary>
    public string Required(string field, string? value, string message, int maxLength = 200)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0) { Add(field, message); return ""; }
        if (v.Length > maxLength) { Add(field, $"Keep this under {maxLength} characters."); return ""; }
        return v;
    }

    /// <summary>Trimmed value or null.</summary>
    public string? Optional(string field, string? value, int maxLength = 500)
    {
        var v = value?.Trim();
        if (string.IsNullOrEmpty(v)) return null;
        if (v.Length > maxLength) { Add(field, $"Keep this under {maxLength} characters."); return null; }
        return v;
    }

    /// <summary>Nigerian mobile: 0 or 234/+234 followed by 10 digits. Returns "+234XXXXXXXXXX".</summary>
    public string Phone(string field, string? value)
    {
        var raw = value?.Trim() ?? "";
        if (raw.Length == 0) { Add(field, "Enter a phone or WhatsApp number."); return ""; }

        var digits = NonDigits().Replace(raw, "");
        string? local = digits switch
        {
            { Length: 11 } when digits.StartsWith('0') => digits[1..],
            { Length: 13 } when digits.StartsWith("234") => digits[3..],
            _ => null,
        };
        if (local is null) { Add(field, "Enter a Nigerian number, e.g. 0803 123 4567."); return ""; }
        return "+234" + local;
    }

    /// <summary>Lower-cased email, or null when optional and empty.</summary>
    public string? Email(string field, string? value, bool required = true)
    {
        var v = value?.Trim() ?? "";
        if (v.Length == 0)
        {
            if (required) Add(field, "Enter your email address.");
            return null;
        }
        if (v.Length > 254 || !MailAddress.TryCreate(v, out var addr) || addr.Address != v || !v.Contains('.'))
        {
            Add(field, "Enter a valid email, e.g. you@example.com.");
            return null;
        }
        return v.ToLowerInvariant();
    }

    public string OneOf(string field, string? value, IReadOnlyCollection<string> allowed, string message)
    {
        var v = value?.Trim() ?? "";
        if (!allowed.Contains(v)) { Add(field, message); return ""; }
        return v;
    }

    /// <summary>Distinct values, all from <paramref name="allowed"/>. Errors when empty and atLeastOne.</summary>
    public List<string> Many(string field, IEnumerable<string>? values, IReadOnlyCollection<string> allowed,
        string message, bool atLeastOne = true)
    {
        var list = (values ?? []).Select(v => v?.Trim() ?? "").Where(v => v.Length > 0).Distinct().ToList();
        if ((atLeastOne && list.Count == 0) || list.Any(v => !allowed.Contains(v)))
        {
            Add(field, message);
            return new();
        }
        return list;
    }

    public void MustBeTrue(string field, bool? value, string message)
    {
        if (value != true) Add(field, message);
    }

    public void ThrowIfInvalid()
    {
        if (HasErrors) throw new ValidationFailedException(_errors);
    }

    [GeneratedRegex(@"\D")]
    private static partial Regex NonDigits();
}
