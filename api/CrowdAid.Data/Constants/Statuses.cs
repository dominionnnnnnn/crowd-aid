namespace CrowdAid.Data.Constants;

// String constants, not C# enums: several spec values contain spaces ("In review",
// "More info requested") and must appear in JSON and the DB exactly as written in
// api-spec-v3.md §3. Store and return these strings as-is.

public static class ApplicationStatus
{
    public const string Received = "Received";
    public const string InReview = "In review";
    public const string MoreInfoRequested = "More info requested";
    public const string Verified = "Verified";
    public const string Approved = "Approved";
    public const string Published = "Published";
    public const string Rejected = "Rejected";

    public static readonly string[] All =
        [Received, InReview, MoreInfoRequested, Verified, Approved, Published, Rejected];
}

public static class VerificationStatus
{
    public const string NotStarted = "Not started";
    public const string InProgress = "In progress";
    public const string Verified = "Verified";
    public const string CouldNotVerify = "Could not verify";

    public static readonly string[] All = [NotStarted, InProgress, Verified, CouldNotVerify];
}

public static class OfferStatus
{
    public const string New = "New";
    public const string Contacted = "Contacted";
    public const string NotNeeded = "Not needed";

    public static readonly string[] All = [New, Contacted, NotNeeded];
}

/// <summary>CrowdAid's own donation status (spec #6 maps Paystack statuses onto these).</summary>
public static class DonationStatus
{
    public const string Pending = "pending";
    public const string Success = "success";
    public const string Failed = "failed";
}

public static class WithdrawalStatus
{
    public const string Requested = "Requested";
    public const string Approved = "Approved";
    public const string Paid = "Paid";
    public const string Rejected = "Rejected";
}

public static class UserRole
{
    public const string Admin = "admin";
    public const string Campaigner = "campaigner";
}

/// <summary>Shown to admins on the application detail (spec #14).</summary>
public static class AccountState
{
    public const string NotCreated = "Not created";
    public const string LinkSent = "Link sent";
    public const string Activated = "Activated";
}

public static class VolunteerKind
{
    public const string Outreach = "Outreach";
    public const string Community = "Community";
}

public static class ApplicantWho
{
    public const string Myself = "myself";
    public const string SomeoneElse = "someone_else";
    /// <summary>Stored as the relationship when who = myself (spec #3).</summary>
    public const string MyselfRelationship = "Myself";
}

public static class PaymentMethod
{
    public const string Card = "card";
    public const string Transfer = "transfer";
    public const string Ussd = "ussd";

    public static readonly string[] All = [Card, Transfer, Ussd];

    /// <summary>Paystack "channels" value for each method (spec #5).</summary>
    public static string ToPaystackChannel(string method) => method switch
    {
        Card => "card",
        Transfer => "bank_transfer",
        Ussd => "ussd",
        _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
    };
}

/// <summary>Reference prefixes (spec §1). Each one has its own Postgres sequence.</summary>
public static class ReferencePrefix
{
    public const string Application = "CA-REQ";
    public const string Donation = "CA-DON";
    public const string Offer = "CA-OFF";
    public const string Volunteer = "CA-VOL";
    public const string Message = "CA-MSG";
    public const string Withdrawal = "CA-WDR";
}
