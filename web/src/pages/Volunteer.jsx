import { useRef } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { publicApi } from "../api/public";
import {
  areas,
  availability,
  contactErrors,
  interests,
  outreachRoles,
  requiredFields,
  validEmail,
} from "../components/constants";
import { useForm, useResource } from "../components/hooks";
import {
  BackLink,
  ChoiceGroup,
  Consent,
  Empty,
  ErrorSummary,
  Field,
  LoadError,
  Loading,
  ResultPanel,
  Submit,
  date,
} from "../components/UI";
export default function Volunteer() {
  const [params, setParams] = useSearchParams();
  const path = params.get("path") === "community" ? "community" : "outreach";
  const resource = useResource((signal) => publicApi.outreach(signal), []);
  return (
    <>
      <BackLink />
      <div className="p-form-page p-wide-form p-stack">
        <div className="p-page-heading">
          <h1>Volunteer with CrowdAid</h1>
          <p>
            CrowdAid organises community outreaches in Abeokuta to raise funds
            and share campaigns in person. Volunteers help on the day. You don't
            need to organise anything.
          </p>
        </div>
        <div className="p-path-picker">
          {[
            [
              "outreach",
              "Register for an outreach",
              "Sign up to help at a specific upcoming outreach day.",
            ],
            [
              "community",
              "Join the volunteer community",
              "Tell us how you'd like to help, and we'll contact you when there's a need or an outreach.",
            ],
          ].map(([value, title, copy]) => (
            <button
              key={value}
              className={path === value ? "is-selected" : ""}
              aria-pressed={path === value}
              onClick={() => setParams({ path: value })}
            >
              <strong>{title}</strong>
              <span>{copy}</span>
            </button>
          ))}
        </div>
        {path === "community" ? (
          <CommunityForm />
        ) : resource.loading ? (
          <Loading label="Loading upcoming outreach…" />
        ) : resource.error ? (
          <LoadError resource={resource} />
        ) : resource.data.data.length ? (
          <OutreachForm events={resource.data.data} />
        ) : (
          <Empty title="No upcoming outreaches right now">
            <p>
              Join the volunteer community and we'll let you know when the next
              one is planned.
            </p>
            <button
              className="button p-primary"
              onClick={() => setParams({ path: "community" })}
            >
              Join the volunteer community
            </button>
            <Link to="/campaigns">Browse campaigns</Link>
          </Empty>
        )}
      </div>
    </>
  );
}
function ContactFields({ f }) {
  return (
    <>
      <Field
        label="Full name *"
        {...f.bind("name")}
        autoComplete="name"
        maxLength={120}
      />
      <Field
        label="Phone or WhatsApp number *"
        {...f.bind("phone")}
        type="tel"
        autoComplete="tel"
      />
      <Field label="Your area *" {...f.bind("area")} options={areas} />
    </>
  );
}
function OutreachForm({ events }) {
  const f = useForm(
    {
      outreachId: "",
      name: "",
      phone: "",
      area: "",
      roles: [],
      stay: "",
      guidelinesConsent: false,
    },
    "outreach-",
  );
  const formRef = useRef(null);
  const v = f.values;
  if (f.result)
    return (
      <ResultPanel title="You're registered" reference={f.result.reference}>
        <p>
          CrowdAid will contact you by phone or WhatsApp before the outreach
          with the meeting point and what to expect.
        </p>
        <div className="p-info">
          <strong>{f.result.outreach.title}</strong>
          <p>
            {date(f.result.outreach.date)} · {f.result.outreach.time}
          </p>
        </div>
        <Link className="button p-primary" to="/">
          Back to home
        </Link>
      </ResultPanel>
    );
  return (
    <>
      <h2>Upcoming outreach</h2>
      <div className="p-stack">
        {events.map((event) => (
          <article key={event.id} className="p-card p-outreach-card">
            <div className="p-date-tile">
              <span>
                {new Date(`${event.date}T12:00:00`).toLocaleDateString(
                  "en-NG",
                  { month: "short" },
                )}
              </span>
              <strong>{new Date(`${event.date}T12:00:00`).getDate()}</strong>
            </div>
            <div className="p-stack">
              <h3>{event.title}</h3>
              <p>
                {date(event.date)} · {event.time}
              </p>
              <p className="p-muted">{event.venue}</p>
              {event.supportingCampaign && (
                <p>
                  Supporting:{" "}
                  <Link
                    to={`/campaigns/${encodeURIComponent(event.supportingCampaign.id)}`}
                  >
                    {event.supportingCampaign.title}
                  </Link>
                </p>
              )}
              <button
                className="button p-outline"
                onClick={() => {
                  f.set("outreachId", event.id);
                  formRef.current?.scrollIntoView({
                    behavior: "smooth",
                    block: "start",
                  });
                }}
              >
                Register for this outreach ↓
              </button>
            </div>
          </article>
        ))}
      </div>
      <form
        ref={formRef}
        className="p-form-section"
        noValidate
        onSubmit={f.submit(
          (values) => publicApi.volunteerOutreach(values),
          (values) => ({
            ...contactErrors(values),
            ...requiredFields(values, [
              "outreachId",
              "area",
              "stay",
              "guidelinesConsent",
            ]),
          }),
        )}
      >
        <h2>Register for the outreach</h2>
        <p className="p-hint">Fields marked * are required.</p>
        <ErrorSummary error={f.error} prefix={f.prefix} />
        <Field
          label="Outreach *"
          {...f.bind("outreachId")}
          options={events.map((e) => ({
            value: e.id,
            label: `${e.title} · ${date(e.date)}`,
          }))}
        />
        <ContactFields f={f} />
        <ChoiceGroup
          label="How would you like to help? · optional"
          name="roles"
          prefix={f.prefix}
          values={v.roles}
          options={outreachRoles}
          onChange={(x) => f.set("roles", x)}
          error={f.error?.fields?.roles}
        />
        <ChoiceGroup
          label="Can you stay for the whole outreach? *"
          name="stay"
          prefix={f.prefix}
          radio
          values={v.stay}
          options={[
            { value: "all", label: "Yes, all of it" },
            { value: "part", label: "Part of it" },
          ]}
          onChange={(x) => f.set("stay", x)}
          error={f.error?.fields?.stay}
        />
        <details className="p-info">
          <summary>Volunteer guidelines</summary>
          <p>
            Follow the CrowdAid team's briefing, respect each family's privacy,
            and only collect donations through the channels the team provides.
            Contact CrowdAid if you need help or cannot attend.
          </p>
        </details>
        <Consent
          name="guidelinesConsent"
          prefix={f.prefix}
          checked={v.guidelinesConsent}
          onChange={(x) => f.set("guidelinesConsent", x)}
          error={f.error?.fields?.guidelinesConsent}
        >
          * I agree to follow CrowdAid's volunteer guidelines, and CrowdAid may
          contact me about this outreach.
        </Consent>
        <Submit busy={f.busy} busyLabel="Registering…">
          Register for outreach
        </Submit>
      </form>
    </>
  );
}
function CommunityForm() {
  const f = useForm(
    {
      name: "",
      phone: "",
      email: "",
      area: "",
      interests: [],
      availability: [],
      contactConsent: false,
    },
    "community-",
  );
  const v = f.values;
  if (f.result)
    return (
      <ResultPanel
        title="Welcome to the CrowdAid volunteer community"
        reference={f.result.reference}
      >
        <p>
          CrowdAid will contact you by phone or WhatsApp when there's an
          outreach or a need that matches what you offered. You can ask us to
          stop at any time.
        </p>
        <div className="p-actions">
          <Link className="button p-primary" to="/">
            Back to home
          </Link>
          <Link to="/volunteer?path=outreach">See upcoming outreach</Link>
        </div>
      </ResultPanel>
    );
  return (
    <form
      className="p-form-section"
      noValidate
      onSubmit={f.submit(
        (values) =>
          publicApi.volunteerCommunity({
            ...values,
            email: values.email || null,
          }),
        (values) => ({
          ...contactErrors(values),
          ...requiredFields(values, [
            "area",
            "interests",
            "availability",
            "contactConsent",
          ]),
          ...(values.email && !validEmail(values.email)
            ? { email: "Enter a valid email address, or leave it empty." }
            : {}),
        }),
      )}
    >
      <h2>Join the volunteer community</h2>
      <p className="p-hint">Fields marked * are required.</p>
      <ErrorSummary error={f.error} prefix={f.prefix} />
      <ContactFields f={f} />
      <Field
        label="Email · optional"
        {...f.bind("email")}
        type="email"
        autoComplete="email"
      />
      <ChoiceGroup
        label="How would you like to help? * · choose any"
        name="interests"
        prefix={f.prefix}
        values={v.interests}
        options={interests}
        onChange={(x) => f.set("interests", x)}
        error={f.error?.fields?.interests}
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
      <Consent
        name="contactConsent"
        prefix={f.prefix}
        checked={v.contactConsent}
        onChange={(x) => f.set("contactConsent", x)}
        error={f.error?.fields?.contactConsent}
      >
        * CrowdAid may contact me by phone or WhatsApp about volunteering. I can
        ask to stop at any time.
      </Consent>
      <Submit busy={f.busy} busyLabel="Saving…">
        Join volunteer community
      </Submit>
    </form>
  );
}
