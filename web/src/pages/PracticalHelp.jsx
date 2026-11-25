import { Link, useSearchParams } from "react-router-dom";
import { publicApi } from "../api/public";
import {
  areas,
  availability,
  contactErrors,
  helpCategories,
  needToCategory,
  requiredFields,
} from "../components/constants";
import { useForm, useResource } from "../components/hooks";
import {
  BackLink,
  ChoiceGroup,
  Consent,
  ErrorSummary,
  Field,
  LoadError,
  Loading,
  ResultPanel,
  Submit,
} from "../components/UI";
export default function PracticalHelp() {
  const [params] = useSearchParams();
  const id = params.get("campaign");
  const type = params.get("type");
  const resource = useResource(
    (signal) => (id ? publicApi.campaign(id, signal) : Promise.resolve(null)),
    [id],
  );
  if (resource.loading)
    return (
      <div className="p-form-page">
        <Loading label="Loading help form…" />
      </div>
    );
  if (resource.error)
    return (
      <div className="p-form-page">
        <LoadError resource={resource} />
        <Link to="/help">Offer general help instead</Link>
      </div>
    );
  return (
    <OfferForm
      key={`${id || "general"}-${type || ""}`}
      campaign={resource.data}
      type={type}
    />
  );
}
function OfferForm({ campaign, type }) {
  const needed = [...new Set((campaign?.needs || []).map(needToCategory))];
  const preselected = helpCategories.some((c) => c.value === type)
    ? [type]
    : needed;
  const f = useForm(
    {
      campaignId: campaign?.id || null,
      types: preselected,
      details: "",
      availability: [],
      area: "",
      name: "",
      phone: "",
      contactConsent: false,
    },
    "offer-",
  );
  const v = f.values;
  const validate = (values) => ({
    ...contactErrors(values),
    ...requiredFields(values, [
      "types",
      "availability",
      "area",
      "contactConsent",
    ]),
  });
  const categories = [...helpCategories]
    .sort(
      (a, b) =>
        Number(needed.includes(b.value)) - Number(needed.includes(a.value)),
    )
    .map((c) => c.value);
  return (
    <>
      <BackLink
        to={campaign ? `/campaigns/${encodeURIComponent(campaign.id)}` : "/"}
      >
        {campaign ? "Back to campaign" : "Home"}
      </BackLink>
      <div className="p-form-page p-wide-form p-stack">
        {f.result ? (
          <ResultPanel
            title="Thank you for offering your support"
            reference={f.result.reference}
          >
            <p>
              {f.result.mode === "campaign"
                ? `Thank you for offering your support for ${f.result.campaignTitle}. CrowdAid has recorded your offer and may contact you if it is needed for this campaign.`
                : "CrowdAid may contact you when a relevant opportunity becomes available."}
            </p>
            <p className="p-info">
              Your offer has been recorded. It doesn't mean help has been
              arranged yet. CrowdAid does not automatically match supporters
              with patients.
            </p>
            <div className="p-actions">
              <Link
                className="button p-primary"
                to={
                  campaign
                    ? `/campaigns/${encodeURIComponent(campaign.id)}`
                    : "/"
                }
              >
                {campaign ? "Back to campaign" : "Back to home"}
              </Link>
              <Link
                to={
                  campaign
                    ? `/donate?campaign=${encodeURIComponent(campaign.id)}`
                    : "/campaigns"
                }
              >
                {campaign ? "Donate as well" : "Browse campaigns"}
              </Link>
            </div>
          </ResultPanel>
        ) : (
          <>
            <h1>Offer practical help</h1>
            <p className="p-hint">Fields marked * are required.</p>
            {campaign ? (
              <div className="p-info">
                <p className="p-eyebrow">For this campaign</p>
                <strong>{campaign.title}</strong>
                <p>Needs: {campaign.needs.join(", ") || "Practical support"}</p>
              </div>
            ) : (
              <p className="p-info">
                Tell us how you're willing to help. Your offer isn't linked to a
                specific campaign.
              </p>
            )}
            <form
              noValidate
              onSubmit={f.submit((values) => publicApi.offer(values), validate)}
            >
              <ErrorSummary error={f.error} prefix={f.prefix} />
              <p className="p-muted">
                Offering help doesn't commit you to anything yet. CrowdAid will
                contact you to confirm before anything is arranged.
              </p>
              <ChoiceGroup
                label="What can you offer? * · choose any"
                name="types"
                prefix={f.prefix}
                rich
                values={v.types}
                options={categories}
                needed={needed}
                onChange={(x) => f.set("types", x)}
                error={f.error?.fields?.types}
              />
              <Field
                label="Additional details · optional"
                {...f.bind("details")}
                textarea
                rows={3}
                maxLength={500}
              />
              <ChoiceGroup
                label="When are you usually available? *"
                name="availability"
                prefix={f.prefix}
                values={v.availability}
                options={availability}
                onChange={(x) => f.set("availability", x)}
                error={f.error?.fields?.availability}
              />
              <Field
                label="Your area in Abeokuta *"
                {...f.bind("area")}
                options={areas}
              />
              <Field
                label="Full name *"
                {...f.bind("name")}
                autoComplete="name"
                maxLength={120}
              />
              <Field
                label="Phone or WhatsApp number *"
                {...f.bind("phone")}
                autoComplete="tel"
                type="tel"
                hint="Accepts 0803 123 4567 or +234 803 123 4567"
              />
              <Consent
                name="contactConsent"
                prefix={f.prefix}
                checked={v.contactConsent}
                onChange={(x) => f.set("contactConsent", x)}
                error={f.error?.fields?.contactConsent}
              >
                * CrowdAid may contact me about this offer if needed. My details
                won't be shown publicly.
              </Consent>
              <Submit busy={f.busy} busyLabel="Sending offer…">
                Send offer
              </Submit>
            </form>
          </>
        )}
      </div>
    </>
  );
}
