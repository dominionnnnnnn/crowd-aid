using CrowdAid.Data;
using CrowdAid.Data.Constants;
using CrowdAid.Data.DTOs.Public;
using CrowdAid.Data.Entities;
using CrowdAid.Data.Exceptions;
using CrowdAid.Data.IRepo;
using CrowdAid.Service.Validation;
using Microsoft.EntityFrameworkCore;

namespace CrowdAid.Service.Services;

/// <summary>Public side of applications: submit (spec #3) and status check (spec #34).</summary>
public class ApplicationService(AppDbContext db, IReferenceGenerator refs) : IApplicationService
{
    public async Task<ApplicationCreatedResponse> SubmitAsync(CreateApplicationRequest r, CancellationToken ct = default)
    {
        var v = new FormValidator();

        var who = v.OneOf("who", r.Who, [ApplicantWho.Myself, ApplicantWho.SomeoneElse],
            "Tell us who the help is for.");
        var name = v.Required("name", r.Name, "Enter your full name.", 120);
        var phone = v.Phone("phone", r.Phone);
        var email = v.Email("email", r.Email);

        string relationship = ApplicantWho.MyselfRelationship;
        string patientName = name;
        if (who == ApplicantWho.SomeoneElse)
        {
            relationship = v.OneOf("relationship", r.Relationship, Lookups.ApplicantRelationships,
                "Choose your relationship to the patient.");
            patientName = v.Required("patientName", r.PatientName, "Enter the patient's full name.", 120);
        }

        if (r.PatientAge is not { } age || age < 0 || age > 120)
            v.Add("patientAge", "Enter the patient's age in years.");

        var town = v.Required("town", r.Town, "Enter the town.", 80);
        var lga = v.OneOf("lga", r.Lga, Lookups.OgunLgas, "Choose the local government area.");
        var condition = v.Required("condition", r.Condition, "Describe the medical condition or healthcare problem.", 2000);
        var treatment = v.Required("treatment", r.Treatment, "Tell us the treatment or help needed.", 300);
        var facility = v.Optional("facility", r.Facility, 200);

        if (r.EstimatedCost is < 0) v.Add("estimatedCost", "Enter an amount in naira, or leave it empty.");
        if (r.AmountNeeded is < 0) v.Add("amountNeeded", "Enter an amount in naira, or leave it empty.");

        var supportTypes = v.Many("supportTypes", r.SupportTypes, Lookups.SupportTypes,
            "Choose at least one type of help.");

        v.MustBeTrue("privateReviewConsent", r.PrivateReviewConsent,
            "Tick this box so CrowdAid can review your information.");
        v.MustBeTrue("accuracyConfirmed", r.AccuracyConfirmed,
            "Tick this box to confirm the information is accurate.");

        v.ThrowIfInvalid();

        var application = new Application
        {
            Reference = await refs.NextAsync(ReferencePrefix.Application, ct),
            Who = who,
            Name = name,
            Phone = phone,
            Email = email!,
            Relationship = relationship,
            PatientName = patientName,
            PatientAge = r.PatientAge!.Value,
            Town = town,
            Lga = lga,
            Condition = condition,
            Treatment = treatment,
            Facility = facility,
            EstimatedCost = r.EstimatedCost,
            AmountNeeded = r.AmountNeeded,
            SupportTypes = supportTypes,
            PrivateReviewConsent = true,
            AccuracyConfirmed = true,
            Status = ApplicationStatus.Received,
        };
        application.Log.Add(new ApplicationLog { Action = "Received", By = "Applicant" });

        db.Applications.Add(application);
        await db.SaveChangesAsync(ct);

        // Always "Received": a submitted request is never shown as approved.
        return new ApplicationCreatedResponse(application.Reference, application.Status);
    }

    public async Task<ApplicationStatusResponse> GetStatusAsync(string? reference, string? email, CancellationToken ct = default)
    {
        var refValue = reference?.Trim().ToUpperInvariant() ?? "";
        var emailValue = email?.Trim().ToLowerInvariant() ?? "";
        var notFound = new NotFoundException("We couldn't find an application with that reference and email.");
        if (refValue.Length == 0 || emailValue.Length == 0) throw notFound;

        var status = await db.Applications.AsNoTracking()
            .Where(a => a.Reference == refValue && a.Email == emailValue)
            .Select(a => a.Status)
            .FirstOrDefaultAsync(ct)
            ?? throw notFound; // same message whichever one is wrong

        return new ApplicationStatusResponse(refValue, status, StatusMessage(status));
    }

    /// <summary>One fixed message per status. Never notes, reasons or verification details.</summary>
    private static string StatusMessage(string status) => status switch
    {
        ApplicationStatus.Received => "We've received your application and will review it soon.",
        ApplicationStatus.InReview => "We are reviewing your application.",
        ApplicationStatus.MoreInfoRequested => "We need a little more information. CrowdAid will contact you.",
        ApplicationStatus.Verified => "Your details have been checked. A decision is coming soon.",
        ApplicationStatus.Approved => "Your application was approved. Check your email for a link to set up your account.",
        ApplicationStatus.Published => "Your campaign is live.",
        ApplicationStatus.Rejected => "We couldn't take your application forward. CrowdAid will contact you.",
        _ => "We are reviewing your application.",
    };
}
