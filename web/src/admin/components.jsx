import { useEffect, useRef, useState } from "react";
import { asError } from "../components/hooks";
export const money = (value) =>
  value == null ? "Not provided" : `₦${Number(value).toLocaleString("en-NG")}`;
export const date = (value) =>
  value
    ? new Date(value).toLocaleDateString("en-NG", {
        day: "numeric",
        month: "short",
        year: "numeric",
      })
    : "—";
export const maskPhone = (value) =>
  value
    ? `${value.replace(/\s/g, "").slice(0, 4)} ${value.replace(/\s/g, "").slice(4, 7)} •••`
    : "Not provided";
export const dateTime = (value) =>
  value
    ? new Date(value).toLocaleString("en-NG", {
        day: "numeric",
        month: "short",
        year: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "—";
export const text = (value) =>
  Array.isArray(value) ? value.join(", ") : value || "—";
export const errorBody = asError;
export function ErrorBox({ error }) {
  if (!error) return null;
  return (
    <div role="alert" className="error-box">
      ⚠ {typeof error === "string" ? error : error.message}
      {error.fields && Object.keys(error.fields).length > 0 && (
        <ul>
          {Object.entries(error.fields).map(([key, value]) => (
            <li key={key}>
              {key}: {value}
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
export function Field({ label, name, error, textarea, children, ...props }) {
  const id = props.id || name;
  const controlProps = {
    ...props,
    id,
    name,
    "aria-invalid": !!error,
    "aria-describedby": error ? `${id}-error` : undefined,
  };
  return (
    <div className="field">
      <label htmlFor={id}>{label}</label>
      {children ||
        (textarea ? (
          <textarea {...controlProps} />
        ) : (
          <input {...controlProps} />
        ))}
      {error && (
        <span id={`${id}-error`} className="field-error">
          ⚠ {error}
        </span>
      )}
    </div>
  );
}
export function Pill({ status }) {
  const tone = [
    "Approved",
    "Published",
    "Verified",
    "Paid",
    "Activated",
  ].includes(status)
    ? "green"
    : ["Rejected", "Could not verify"].includes(status)
      ? "red"
      : ["More info requested", "Requested", "Link sent"].includes(status)
        ? "amber"
        : "neutral";
  return <span className={`pill ${tone}`}>{status}</span>;
}
export function Card({ title, children, className = "" }) {
  return (
    <section className={`card ${className}`}>
      {title && <h2>{title}</h2>}
      {children}
    </section>
  );
}
export function Details({ title, items }) {
  return (
    <div>
      <h3>{title}</h3>
      <dl>
        {items.map(([label, value]) => (
          <div key={label}>
            <dt>{label}</dt>
            <dd>{text(value)}</dd>
          </div>
        ))}
      </dl>
    </div>
  );
}
export function Modal({ title, children, onClose, busy = false }) {
  const ref = useRef(null);
  useEffect(() => {
    const trigger = document.activeElement;
    const dialog = ref.current;
    dialog.showModal();
    return () => {
      dialog.close();
      if (trigger?.isConnected) trigger.focus();
    };
  }, []);
  return (
    <dialog
      ref={ref}
      aria-labelledby="modal-title"
      onCancel={(event) => {
        event.preventDefault();
        if (!busy) onClose();
      }}
      onClick={(event) => {
        if (event.target === ref.current && !busy) onClose();
      }}
    >
      <div className="modal-inner">
        <div className="spread">
          <h2 id="modal-title">{title}</h2>
          <button
            className="icon-button"
            aria-label="Close dialog"
            disabled={busy}
            onClick={onClose}
          >
            ×
          </button>
        </div>
        {children}
      </div>
    </dialog>
  );
}
const actionCopy = {
  request_info: {
    title: "Request more information",
    description:
      "Record what is needed and the message you will send. Contact the applicant outside this app; this message is only logged.",
    first: "What information is needed?",
    key: "needed",
    second: true,
    button: "Request more info",
  },
  approve: {
    title: "Approve this application?",
    description:
      "This creates an account and an activation link. The system attempts to email the link. Approval keeps the application private until you publish separately.",
    first: "Note (optional)",
    key: "note",
    button: "Approve application",
  },
  reject: {
    title: "Reject this application?",
    description:
      "Record a private reason and the message you will send outside the app. This message is only logged.",
    first: "Reason (private)",
    key: "reason",
    second: true,
    button: "Reject application",
  },
  info_received: {
    title: "Mark information received?",
    description:
      "Return this application to In review. Review the new information before approving.",
    first: "Note (optional)",
    key: "note",
    button: "Mark info received",
  },
  publish: {
    title: "Publish this campaign?",
    description:
      "Only the public fields you reviewed will become visible to everyone. Private application details and documents stay private.",
    button: "Publish campaign",
  },
  withdrawal_approve: {
    title: "Approve demo withdrawal?",
    description: "Demo only. No money moves.",
    first: "Note (optional)",
    key: "note",
    button: "Approve withdrawal",
  },
  withdrawal_reject: {
    title: "Reject demo withdrawal?",
    description:
      "The message will be shown to the campaigner. The private reason is kept for admins.",
    first: "Reason (private)",
    key: "reason",
    second: true,
    button: "Reject withdrawal",
  },
  withdrawal_mark_paid: {
    title: "Mark demo withdrawal paid?",
    description: "This only changes the demo status. No bank transfer is made.",
    first: "Transfer reference (optional)",
    key: "transferReference",
    button: "Mark paid",
  },
  resend: {
    title: "Send a new activation link?",
    description:
      "This invalidates all earlier links. If the account is activated, the new link allows a password reset; the current password works until the new link is used.",
    button: "Send a new link",
  },
};
export function ReasonModal({
  action,
  reference,
  onClose,
  onConfirm,
  children,
  onError,
}) {
  const copy = actionCopy[action];
  const [values, setValues] = useState({});
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  async function submit(event) {
    event.preventDefault();
    setError(null);
    if (copy.second) {
      const fields = Object.fromEntries(
        [copy.key, "message"]
          .filter((key) => !values[key]?.trim())
          .map((key) => [key, "This field is required."]),
      );
      if (Object.keys(fields).length) {
        setError({ message: "Please fix the highlighted fields.", fields });
        return;
      }
    }
    setBusy(true);
    try {
      await onConfirm(values);
      onClose();
    } catch (e) {
      const body = errorBody(e);
      setError(body);
      onError?.(body);
    } finally {
      setBusy(false);
    }
  }
  return (
    <Modal title={copy.title} onClose={onClose} busy={busy}>
      <p className="mono muted">{reference}</p>
      <p>{copy.description}</p>
      {children}
      <form noValidate onSubmit={submit}>
        <ErrorBox error={error} />
        {copy.first && (
          <Field
            textarea
            rows={3}
            label={copy.first}
            name={copy.key}
            value={values[copy.key] || ""}
            required={copy.second}
            error={error?.fields?.[copy.key]}
            onChange={(e) =>
              setValues({ ...values, [copy.key]: e.target.value })
            }
          />
        )}
        {copy.second && (
          <Field
            textarea
            rows={3}
            name="message"
            label="Message to the applicant"
            value={values.message || ""}
            required
            error={error?.fields?.message}
            onChange={(e) => setValues({ ...values, message: e.target.value })}
          />
        )}
        <div className="actions modal-actions">
          <button type="button" disabled={busy} onClick={onClose}>
            Cancel
          </button>
          <button
            className={action.includes("reject") ? "danger" : "primary"}
            disabled={busy}
          >
            {busy ? "Saving…" : copy.button}
          </button>
        </div>
      </form>
    </Modal>
  );
}
