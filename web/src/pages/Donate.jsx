import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { publicApi } from "../api/public";
import {
  requiredFields,
  validEmail,
  wholeAmount,
} from "../components/constants";
import { useForm, useResource } from "../components/hooks";
import {
  BackLink,
  ChoiceGroup,
  Consent,
  Empty,
  ErrorSummary,
  Field,
  Funding,
  LoadError,
  Loading,
  Submit,
  TestBadge,
  money,
} from "../components/UI";
const DRAFT_KEY = "crowdaid.donation.draft";
export function readDonationDraft(id) {
  try {
    const saved = JSON.parse(sessionStorage.getItem(DRAFT_KEY));
    return saved?.campaignId === id ? saved : null;
  } catch {
    return null;
  }
}
export function clearDonationDraft() {
  try {
    sessionStorage.removeItem(DRAFT_KEY);
  } catch {
    /* Restricted storage. */
  }
}
export default function Donate() {
  const [params] = useSearchParams();
  const id = params.get("campaign");
  const resource = useResource(
    (signal) =>
      id ? publicApi.campaign(id, signal) : publicApi.campaigns({}, signal),
    [id],
  );
  if (resource.loading)
    return (
      <div className="p-form-page">
        <Loading label="Loading donation…" />
      </div>
    );
  if (resource.error)
    return (
      <div className="p-form-page">
        <LoadError resource={resource} />
        <Link to="/campaigns">See all campaigns</Link>
      </div>
    );
  if (!id)
    return (
      <div className="p-form-page p-stack">
        <h1>Choose a campaign to support</h1>
        {resource.data.data.length ? (
          resource.data.data.map((c) => (
            <Link
              className="p-help-link"
              key={c.id}
              to={`/donate?campaign=${encodeURIComponent(c.id)}`}
            >
              {c.title}
              <span>›</span>
            </Link>
          ))
        ) : (
          <Empty title="No published campaigns yet">
            <Link to="/apply">Request support</Link>
          </Empty>
        )}
      </div>
    );
  return <DonationForm key={id} campaign={resource.data} />;
}
function DonationForm({ campaign: c }) {
  const f = useForm(
    () =>
      readDonationDraft(c.id) || {
        campaignId: c.id,
        amount: "5000",
        name: "",
        anonymous: false,
        email: "",
        method: "",
      },
    "donation-",
  );
  const v = f.values;
  const [redirecting, setRedirecting] = useState(false);
  useEffect(() => {
    try {
      sessionStorage.setItem(DRAFT_KEY, JSON.stringify(v));
    } catch {
      /* Form still works without storage. */
    }
  }, [v]);
  const validate = (values) => ({
    ...requiredFields(values, ["email", "method"]),
    ...(!validEmail(values.email)
      ? { email: "Enter a valid email address for your receipt." }
      : {}),
    ...(!wholeAmount(values.amount, 500) || Number(values.amount) > 100000000
      ? { amount: "Enter a whole naira amount between ₦500 and ₦100,000,000." }
      : {}),
  });
  async function checkout() {
    try {
      const url = new URL(f.result.authorizationUrl);
      if (url.protocol !== "https:")
        throw new Error(
          "The payment provider did not return a secure checkout URL.",
        );
      setRedirecting(true);
      window.location.assign(url.href);
    } catch (e) {
      f.setError({ message: e.message, fields: {} });
      setRedirecting(false);
    }
  }
  return (
    <>
      <BackLink to={`/campaigns/${encodeURIComponent(c.id)}`}>
        Back to campaign
      </BackLink>
      <div className="p-form-page p-stack">
        <h1>Donate</h1>
        <div className="p-card p-stack">
          <h2>{c.title}</h2>
          <Funding {...c} />
        </div>
        {f.result ? (
          <section className="p-card p-stack">
            <TestBadge testMode={f.result.testMode} />
            <h2>Review your donation</h2>
            <FeeSummary
              amount={f.result.amount}
              fee={f.result.platformFee}
              total={f.result.total}
            />
            <p className="p-reference">
              Reference <strong>{f.result.reference}</strong>
            </p>
            <p className="p-info">
              You'll finish paying on Paystack's secure checkout. CrowdAid never
              sees your card or bank details.
            </p>
            <ErrorSummary error={f.error} />
            <Submit
              busy={redirecting}
              busyLabel="Opening secure checkout…"
              onClick={checkout}
            >
              Continue to secure checkout
            </Submit>
            <Link
              to={`/donate/result?ref=${encodeURIComponent(f.result.reference)}`}
            >
              Already completed checkout? Check status
            </Link>
          </section>
        ) : (
          <form
            noValidate
            onSubmit={f.submit(
              (values) =>
                publicApi.donate({
                  ...values,
                  amount: Number(values.amount),
                  name: values.anonymous ? null : values.name || null,
                  email: values.email.trim(),
                }),
              validate,
            )}
          >
            <p className="p-info">
              Payments use Paystack test checkout. Do not use real card or bank
              details. Only confirmed payments count towards the campaign total.
            </p>
            <ErrorSummary error={f.error} prefix={f.prefix} />
            <section className="p-form-section p-stack">
              <h2>1. Amount</h2>
              <div
                className="p-amount-presets"
                role="group"
                aria-label="Donation amount presets"
              >
                {[1000, 2000, 5000, 10000, 20000].map((amount) => (
                  <button
                    type="button"
                    key={amount}
                    aria-pressed={Number(v.amount) === amount}
                    className={Number(v.amount) === amount ? "is-selected" : ""}
                    onClick={() => f.set("amount", String(amount))}
                  >
                    {money(amount)}
                  </button>
                ))}
              </div>
              <Field
                label="Or enter another amount (₦)"
                {...f.bind("amount")}
                type="number"
                inputMode="numeric"
                min={500}
                step={1}
                hint="Minimum ₦500. Whole naira only."
              />
            </section>
            <section className="p-form-section p-stack">
              <h2>2. Your details</h2>
              <Field
                label="Your name · optional"
                {...f.bind("name")}
                autoComplete="name"
                maxLength={80}
                disabled={v.anonymous}
              />
              <Consent
                name="anonymous"
                prefix={f.prefix}
                checked={v.anonymous}
                onChange={(x) => f.set("anonymous", x)}
              >
                Donate anonymously
              </Consent>
              <Field
                label="Email for your receipt *"
                {...f.bind("email")}
                type="email"
                autoComplete="email"
              />
            </section>
            <section className="p-form-section p-stack">
              <h2>3. Payment</h2>
              <ChoiceGroup
                label="How would you like to pay? *"
                name="method"
                prefix={f.prefix}
                radio
                values={v.method}
                options={[
                  { value: "card", label: "Card" },
                  { value: "transfer", label: "Bank transfer" },
                  { value: "ussd", label: "USSD" },
                ]}
                onChange={(x) => f.set("method", x)}
                error={f.error?.fields?.method}
              />
              <p className="p-hint">
                You'll finish paying on the payment provider's secure page.
                CrowdAid never sees your card or bank details.
              </p>
            </section>
            <FeeSummary
              amount={Number(v.amount) || 0}
              fee={0}
              total={Number(v.amount) || 0}
            />
            <Submit busy={f.busy} busyLabel="Preparing checkout…">
              Review donation{" "}
              {wholeAmount(v.amount, 500) ? money(v.amount) : ""}
            </Submit>
          </form>
        )}
      </div>
    </>
  );
}
function FeeSummary({ amount, fee, total }) {
  return (
    <div className="p-summary p-stack">
      <h2>Summary</h2>
      <div>
        <span>Donation to campaign</span>
        <strong>{money(amount)}</strong>
      </div>
      <div>
        <span>CrowdAid platform fee</span>
        <strong>{money(fee)}</strong>
      </div>
      <div className="p-total">
        <strong>Total</strong>
        <strong>{money(total)}</strong>
      </div>
    </div>
  );
}
