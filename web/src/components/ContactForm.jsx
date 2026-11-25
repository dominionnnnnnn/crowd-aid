import { useEffect } from "react";
import { Link } from "react-router-dom";
import { publicApi } from "../api/public";
import { contactErrors, requiredFields } from "./constants";
import { useForm } from "./hooks";
import { ErrorSummary, Field, ResultPanel, Submit } from "./UI";
export default function ContactForm({ campaign = null, onDone, onBusyChange }) {
  const f = useForm(
    { campaignId: campaign?.id || null, name: "", phone: "", message: "" },
    "contact-",
  );
  useEffect(() => {
    onBusyChange?.(f.busy);
  }, [f.busy, onBusyChange]);
  if (f.result)
    return (
      <div className="p-stack">
        <ResultPanel title="Message sent" reference={f.result.reference}>
          <p>The CrowdAid team will reply by phone or WhatsApp.</p>
        </ResultPanel>
        {onDone ? (
          <button onClick={onDone}>Done</button>
        ) : (
          <Link className="button p-primary" to="/">
            Back to home
          </Link>
        )}
      </div>
    );
  return (
    <form
      noValidate
      onSubmit={f.submit(
        (values) => publicApi.contact(values),
        (values) => ({
          ...contactErrors(values),
          ...requiredFields(values, ["message"]),
        }),
      )}
    >
      <p className="p-info">About: {campaign?.title || "General enquiry"}</p>
      <p>
        Your message goes to the CrowdAid team only. We reply by phone or
        WhatsApp.
      </p>
      <ErrorSummary error={f.error} prefix={f.prefix} />
      <Field
        label="Your name *"
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
      <Field
        label="Message *"
        {...f.bind("message")}
        textarea
        rows={5}
        maxLength={1000}
      />
      <div className="p-actions">
        {onDone && (
          <button type="button" onClick={onDone} disabled={f.busy}>
            Cancel
          </button>
        )}
        <Submit busy={f.busy}>Send message</Submit>
      </div>
    </form>
  );
}
