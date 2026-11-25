import { Link } from "react-router-dom";
import { publicApi } from "../api/public";
import {
  contactErrors,
  lgas,
  relationships,
  requiredFields,
  supportTypes,
  wholeAmount,
} from "../components/constants";
import { useForm } from "../components/hooks";
import {
  BackLink,
  ChoiceGroup,
  Consent,
  ErrorSummary,
  Field,
  ResultPanel,
  Submit,
} from "../components/UI";
const initial = {
  who: "",
  name: "",
  phone: "",
  email: "",
  relationship: "",
  patientName: "",
  patientAge: "",
  town: "",
  lga: "",
  condition: "",
  treatment: "",
  facility: "",
  estimatedCost: "",
  amountNeeded: "",
  supportTypes: [],
  privateReviewConsent: false,
  accuracyConfirmed: false,
};
export default function Apply() {
  const f = useForm(initial, "apply-");
  const v = f.values;
  const validate = (values) => {
    const fields = {
      ...contactErrors(values, true),
      ...requiredFields(values, [
        "who",
        "patientAge",
        "town",
        "lga",
        "condition",
        "treatment",
        "supportTypes",
        "privateReviewConsent",
        "accuracyConfirmed",
      ]),
    };
    if (values.who === "someone_else")
      Object.assign(
        fields,
        requiredFields(values, ["relationship", "patientName"]),
      );
    if (!wholeAmount(values.patientAge) || Number(values.patientAge) > 120)
      fields.patientAge = "Enter an age in whole years between 0 and 120.";
    for (const key of ["estimatedCost", "amountNeeded"])
      if (values[key] !== "" && !wholeAmount(values[key]))
        fields[key] = "Enter a whole naira amount, or leave this empty.";
    return fields;
  };
  const send = (values) =>
    publicApi.apply({
      ...values,
      email: values.email.trim(),
      patientName: values.who === "myself" ? values.name : values.patientName,
      relationship: values.who === "myself" ? "Myself" : values.relationship,
      patientAge: Number(values.patientAge),
      estimatedCost:
        values.estimatedCost === "" ? null : Number(values.estimatedCost),
      amountNeeded:
        values.amountNeeded === "" ? null : Number(values.amountNeeded),
    });
  if (f.result)
    return (
      <>
        <BackLink />
        <div className="p-form-page">
          <ResultPanel
            title="Request received"
            reference={f.result.reference}
            status={`${f.result.status} · awaiting review`}
          >
            <p className="p-info">
              <strong>
                Your request will be reviewed by the CrowdAid team.
              </strong>{" "}
              This is not an approval, and no public campaign has been created.
            </p>
            <h2>What happens next</h2>
            <ol className="p-numbered">
              <li>CrowdAid reviews your application privately.</li>
              <li>We contact you only if we need more information.</li>
              <li>
                Approved applicants get a sign-in link by email to set up their
                account.
              </li>
              <li>
                We ask for separate consent before publishing. Support comes
                from the community, so we can't promise how much will be raised.
              </li>
            </ol>
            <div className="p-actions">
              <Link className="button p-primary" to="/">
                Back to home
              </Link>
              <Link to="/apply/status">Check application status</Link>
            </div>
          </ResultPanel>
        </div>
      </>
    );
  return (
    <>
      <BackLink />
      <div className="p-form-page p-stack">
        <div className="p-page-heading">
          <h1>Request healthcare assistance</h1>
          <p>
            For yourself or someone you care for. It's free and takes about 5
            minutes. Your request will be reviewed by the CrowdAid team.
            Approved applicants get a sign-in link by email.
          </p>
        </div>
        <details className="p-info">
          <summary>How CrowdAid handles your information</summary>
          <ul className="p-numbered">
            <li>Only the CrowdAid review team can see what you share here.</li>
            <li>
              Nothing is made public unless the request is approved and you give
              separate consent to publish a campaign.
            </li>
            <li>
              Phone numbers, addresses and documents are never shown publicly.
            </li>
          </ul>
        </details>
        <p className="p-hint">Fields marked * are required.</p>
        <form noValidate onSubmit={f.submit(send, validate)}>
          <ErrorSummary error={f.error} prefix={f.prefix} />
          <section className="p-form-section p-stack">
            <div className="p-form-section-heading">
              <span>1</span>
              <div>
                <p>Section 1 of 3</p>
                <h2>Applicant and patient</h2>
              </div>
            </div>
            <ChoiceGroup
              label="Who needs assistance? *"
              name="who"
              prefix={f.prefix}
              radio
              values={v.who}
              options={[
                { value: "myself", label: "Myself" },
                { value: "someone_else", label: "Someone else" },
              ]}
              onChange={(x) => f.set("who", x)}
              error={f.error?.fields?.who}
            />
            <Field
              label="Your full name (applicant) *"
              {...f.bind("name")}
              autoComplete="name"
              maxLength={120}
            />
            <Field
              label="Phone or WhatsApp number *"
              {...f.bind("phone")}
              type="tel"
              autoComplete="tel"
              hint="Accepts 0803 123 4567 or +234 803 123 4567"
            />
            <Field
              label="Your email (applicant) *"
              {...f.bind("email")}
              type="email"
              autoComplete="email"
              hint="We'll send your account link here if you're approved. Use your own email, even when applying for someone else."
            />
            {v.who === "someone_else" && (
              <>
                <Field
                  label="Your relationship to the patient *"
                  {...f.bind("relationship")}
                  options={relationships}
                />
                <Field
                  label="Patient's full name *"
                  {...f.bind("patientName")}
                  maxLength={120}
                />
              </>
            )}
            <div className="p-form-grid">
              <Field
                label="Patient's age (years) *"
                {...f.bind("patientAge")}
                type="number"
                min={0}
                max={120}
                step={1}
              />
              <Field label="Town *" {...f.bind("town")} maxLength={80} />
            </div>
            <Field
              label="Local government area (LGA) *"
              {...f.bind("lga")}
              options={lgas}
            />
          </section>
          <section className="p-form-section p-stack">
            <div className="p-form-section-heading">
              <span>2</span>
              <div>
                <p>Section 2 of 3</p>
                <h2>Healthcare information</h2>
              </div>
            </div>
            <Field
              label="Medical condition or healthcare problem *"
              {...f.bind("condition")}
              textarea
              rows={4}
              maxLength={2000}
              hint="Plain language is fine. No medical records are needed now."
            />
            <Field
              label="Treatment or assistance required *"
              {...f.bind("treatment")}
              textarea
              rows={3}
              maxLength={300}
            />
            <Field
              label="Hospital or healthcare facility · optional"
              {...f.bind("facility")}
              maxLength={200}
            />
            <div className="p-info">
              <strong>No documents needed now</strong>
              <p>
                You can still apply without documents; CrowdAid may request them
                privately later. Please don't post documents publicly.
              </p>
            </div>
          </section>
          <section className="p-form-section p-stack">
            <div className="p-form-section-heading">
              <span>3</span>
              <div>
                <p>Section 3 of 3</p>
                <h2>Financial and practical assistance</h2>
              </div>
            </div>
            <Field
              label="Estimated treatment cost (₦) · optional"
              {...f.bind("estimatedCost")}
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
            />
            <Field
              label="Financial assistance needed (₦) · optional"
              {...f.bind("amountNeeded")}
              type="number"
              min={0}
              step={1}
              inputMode="numeric"
            />
            <ChoiceGroup
              label="Type of assistance needed * · choose any"
              name="supportTypes"
              prefix={f.prefix}
              values={v.supportTypes}
              options={supportTypes}
              onChange={(x) => f.set("supportTypes", x)}
              error={f.error?.fields?.supportTypes}
            />
            {[
              [
                "privateReviewConsent",
                "I agree that CrowdAid may privately process and review this information.",
              ],
              [
                "accuracyConfirmed",
                "The information I've provided is accurate to the best of my knowledge.",
              ],
            ].map(([name, label]) => (
              <Consent
                key={name}
                name={name}
                prefix={f.prefix}
                checked={v[name]}
                onChange={(x) => f.set(name, x)}
                error={f.error?.fields?.[name]}
              >
                * {label}
              </Consent>
            ))}
            <p className="p-info">
              Submitting does not mean approval and does not create a public
              campaign. If approved, CrowdAid will ask for your separate consent
              before anything is published.
            </p>
          </section>
          <Submit busy={f.busy} busyLabel="Submitting…">
            Submit request
          </Submit>
        </form>
        <Link to="/apply/status">
          Already applied? Check application status
        </Link>
      </div>
    </>
  );
}
