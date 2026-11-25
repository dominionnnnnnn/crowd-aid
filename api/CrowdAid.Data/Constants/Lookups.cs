namespace CrowdAid.Data.Constants;

// Allowed values from api-spec-v3.md §3 (confirmed from Prototype v2).
// Use these for server-side validation. The frontend keeps its own copy as constants.
public static class Lookups
{
    public static readonly string[] ApplicantRelationships =
    [
        "Parent", "Child", "Spouse", "Sibling", "Aunt or uncle", "Grandparent", "Friend",
        "Neighbour", "Community or religious leader", "Other",
    ];

    public static readonly string[] OgunLgas =
    [
        "Abeokuta North", "Abeokuta South", "Ado-Odo/Ota", "Ewekoro", "Ifo", "Ijebu East",
        "Ijebu North", "Ijebu North East", "Ijebu Ode", "Ikenne", "Imeko Afon", "Ipokia",
        "Obafemi Owode", "Odeda", "Odogbolu", "Ogun Waterside", "Remo North", "Sagamu",
        "Yewa North", "Yewa South",
    ];

    /// <summary>Application section 3 checkboxes.</summary>
    public static readonly string[] SupportTypes =
    [
        "Financial support", "Transportation", "Medical equipment", "Food",
        "Blood donor coordination", "Professional support",
    ];

    /// <summary>Practical help offer categories.</summary>
    public static readonly string[] HelpCategories =
    [
        "Transportation", "Blood donor coordination", "Medical equipment", "Food support",
        "Professional assistance", "Other support",
    ];

    public static readonly string[] CampaignNeeds =
    [
        "Transport to hospital", "Blood donors", "Meals for family", "Professional support",
        "Medical equipment",
    ];

    public static readonly string[] Availability = ["Weekdays", "Weekends", "Flexible"];

    public static readonly string[] AbeokutaAreas =
    [
        "Adatan", "Asero", "Camp", "Ibara", "Isale-Igbein", "Kuto", "Lafenwa", "Obantoko",
        "Oke-Ilewo", "Onikolobo", "Panseke", "Somewhere else",
    ];

    public static readonly string[] OutreachRoles =
    [
        "Collecting donations", "Sharing campaign stories", "Setup & logistics", "Wherever needed",
    ];

    /// <summary>"all" = "Yes, all of it", "part" = "Part of it".</summary>
    public static readonly string[] OutreachStay = ["all", "part"];

    public static readonly string[] CommunityInterests =
    [
        "Collecting donations at outreaches", "Sharing campaign stories", "Transport and logistics",
        "Healthcare or professional skills", "Admin and coordination",
    ];

    public static readonly int[] DonationPresets = [1000, 2000, 5000, 10000, 20000];

    public const int MinimumDonation = 500;
    public const int MinimumWithdrawal = 1000; // OPEN in the spec; change here only.
    public const int MinimumPasswordLength = 8;
    public static readonly TimeSpan ActivationLinkLifetime = TimeSpan.FromHours(72);
}
