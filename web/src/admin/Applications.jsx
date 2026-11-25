import { useCallback, useEffect, useState } from "react";
import { adminApi } from "../api/admin";
import {
  Card,
  Details,
  ErrorBox,
  Field,
  Pill,
  ReasonModal,
  date,
  dateTime,
  errorBody,
  money,
} from "./components";
const statuses = [
  "Received",
  "In review",
  "More info requested",
  "Verified",
  "Approved",
  "Published",
  "Rejected",
];
const steps = ["Received", "In review", "Verified", "Approved", "Published"];
export default function Applications() {
  const [filter, setFilter] = useState("");
  const [rows, setRows] = useState([]);
  const [selected, setSelected] = useState(null);
  const [mobileDetail, setMobileDetail] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);
  const [version, setVersion] = useState(0);
  const changed = useCallback(() => setVersion((v) => v + 1), []);
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    adminApi
      .applications(filter)
      .then((result) => {
        if (active) {
          setRows(result.data);
          setSelected(
            (current) =>
              current ||
              (
                result.data.find((r) => r.status === "In review") ||
                result.data[0]
              )?.reference,
          );
        }
      })
      .catch((e) => {
        if (active) setError(errorBody(e));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [filter, version]);
  return (
    <div className={`application-layout ${mobileDetail ? "detail-open" : ""}`}>
      <aside className="card application-list" aria-label="Applications">
        <div className="list-filter">
          <Field label="Filter by status" name="status-filter">
            <select
              id="status-filter"
              value={filter}
              onChange={(e) => {
                setFilter(e.target.value);
                setSelected(null);
                setRows([]);
                setMobileDetail(false);
              }}
            >
              <option value="">All applications</option>
              {statuses.map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </Field>
          <div className="small muted" role="status">
            {loading
              ? "Loading applications…"
              : `${rows.length} application${rows.length === 1 ? "" : "s"}`}
          </div>
        </div>
        <ErrorBox error={error} />
        {error && <button onClick={changed}>Try again</button>}
        {!loading && !error && !rows.length && (
          <p className="empty">No applications with this status.</p>
        )}
        <div aria-busy={loading}>
          {rows.map((row) => (
            <button
              className={`application-row ${selected === row.reference ? "selected" : ""}`}
              key={row.reference}
              aria-pressed={selected === row.reference}
              onClick={() => {
                setSelected(row.reference);
                setMobileDetail(true);
              }}
            >
              <span className="spread">
                <span className="mono">{row.reference}</span>
                <Pill status={row.status} />
              </span>
              <strong>
                {row.applicant} <span className="muted">→</span> {row.patient}
              </strong>
              <span className="small muted">{row.treatment}</span>
              <span className="small muted">
                Evidence: {row.hasEvidence ? "Provided" : "None"} ·{" "}
                {date(row.receivedAt)}
              </span>
              {row.isNew && <span className="new-tag">New</span>}
            </button>
          ))}
        </div>
      </aside>
      <div className="application-detail">
        <button className="back-button" onClick={() => setMobileDetail(false)}>
          ← All applications
        </button>
        {selected ? (
          <ApplicationDetail
            key={selected}
            reference={selected}
            onChanged={changed}
          />
        ) : (
          <Card>
            <p className="empty">Select an application to review.</p>
          </Card>
        )}
      </div>
    </div>
  );
}
function ApplicationDetail({ reference, onChanged }) {
  const [app, setApp] = useState(null);
  const [verification, setVerification] = useState(null);
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  const [modal, setModal] = useState(null);
  const [account, setAccount] = useState(null);
  const [notice, setNotice] = useState("");
  const [retry, setRetry] = useState(0);
  useEffect(() => {
    let active = true;
    setError(null);
    adminApi
      .application(reference)
      .then((result) => {
        if (active) {
          setApp(result);
          setVerification(result.verification);
          onChanged();
        }
      })
      .catch((e) => {
        if (active) setError(errorBody(e));
      });
    return () => {
      active = false;
    };
  }, [reference, onChanged, retry]);
  async function refresh() {
    const next = await adminApi.application(reference);
    setApp(next);
    setVerification(next.verification);
    onChanged();
  }
  async function saveVerification(event) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setNotice("");
    try {
      await adminApi.verification(reference, verification);
      await refresh();
      setNotice("Verification saved.");
    } catch (e) {
      setError(errorBody(e));
    } finally {
      setBusy(false);
    }
  }
  async function confirm(values) {
    if (modal === "resend") {
      const response = await adminApi.activation(reference);
      setAccount(response);
      setApp((current) => ({
        ...current,
        account: {
          ...current.account,
          state: response.state,
          activationExpiresAt: response.expiresAt,
        },
      }));
      setNotice("New activation link created. Earlier links no longer work.");
      return;
    }
    const response = await adminApi.action(reference, {
      action: modal,
      ...values,
    });
    if (response.account) setAccount(response.account);
    await refresh();
    setNotice("Application updated.");
  }
  if (!app)
    return (
      <Card>
        <ErrorBox error={error} />
        {error ? (
          <button onClick={() => setRetry((v) => v + 1)}>Try again</button>
        ) : (
          <p role="status">Loading private application…</p>
        )}
      </Card>
    );
  const locked = ["Approved", "Published", "Rejected"].includes(app.status);
  const canDecide = ["In review", "Verified", "More info requested"].includes(
    app.status,
  );
  const stepIndex =
    app.status === "More info requested" ? 1 : steps.indexOf(app.status);
  return (
    <>
      <Card>
        <div className="spread">
          <h2 className="detail-title">
            {reference} · {app.name} for {app.patientName}
          </h2>
          <Pill status={app.status} />
        </div>
        <p className="small muted">
          Private application · Visible to the CrowdAid review team only
        </p>
        <ol className="tracker" aria-label="Application progress">
          {steps.map((s, i) => (
            <li
              key={s}
              className={stepIndex >= i ? "complete" : ""}
              aria-current={app.status === s ? "step" : undefined}
            >
              <span>{stepIndex > i ? "✓" : i + 1}</span>
              {s}
            </li>
          ))}
        </ol>
        {app.status === "Rejected" && (
          <p className="error-box">
            Application rejected. It cannot be published.
          </p>
        )}
        {app.status === "More info requested" && (
          <p className="warning-box">
            Waiting for more information from the applicant.
          </p>
        )}
        <div className="detail-grid">
          <Details
            title="1 · Applicant and patient"
            items={[
              ["Applicant", `${app.name} · ${app.relationship}`],
              ["Phone", app.phone],
              ["Email", app.email],
              ["Patient", `${app.patientName}, ${app.patientAge}`],
              ["Location", `${app.town} · ${app.lga}`],
            ]}
          />
          <Details
            title="2 · Healthcare"
            items={[
              ["Condition", app.condition],
              ["Treatment", app.treatment],
              ["Facility", app.facility || "Not provided"],
            ]}
          />
          <Details
            title="3 · Financial & practical"
            items={[
              ["Estimated cost", money(app.estimatedCost)],
              ["Help needed", money(app.amountNeeded)],
              ["Support types", app.supportTypes],
              [
                "Private review consent",
                app.privateReviewConsent ? "Given" : "Not given",
              ],
              ["Accuracy confirmed", app.accuracyConfirmed ? "Yes" : "No"],
            ]}
          />
          <div>
            <h3>Supporting evidence (private)</h3>
            {app.files.length ? (
              <>
                <ul className="file-list">
                  {app.files.map((file) => (
                    <li key={file}>
                      <span aria-hidden="true">▤</span> {file}
                    </li>
                  ))}
                </ul>
                <p className="small muted">
                  Filenames only. Secure document access is not available in
                  this API contract.
                </p>
              </>
            ) : (
              <p className="small muted">No documents submitted.</p>
            )}
          </div>
        </div>
        <div className="review-grid">
          <form onSubmit={saveVerification}>
            <h3>Verification (private)</h3>
            <Field name="verification-status" label="Verification status">
              <select
                id="verification-status"
                value={verification.status}
                disabled={locked || busy}
                onChange={(e) =>
                  setVerification({ ...verification, status: e.target.value })
                }
              >
                {[
                  "Not started",
                  "In progress",
                  "Verified",
                  "Could not verify",
                ].map((s) => (
                  <option key={s}>{s}</option>
                ))}
              </select>
            </Field>
            <Field
              textarea
              rows={3}
              name="verification-method"
              label="How was it checked? (private)"
              value={verification.method}
              disabled={locked || busy}
              onChange={(e) =>
                setVerification({ ...verification, method: e.target.value })
              }
            />
            {!locked && (
              <button disabled={busy}>
                {busy ? "Saving…" : "Save verification"}
              </button>
            )}
          </form>
          <div>
            <h3>Decision</h3>
            {canDecide ? (
              <>
                <div className="actions">
                  <button
                    className="primary"
                    disabled={
                      busy ||
                      app.status !== "Verified" ||
                      app.verification.status !== "Verified"
                    }
                    onClick={() => setModal("approve")}
                  >
                    Approve
                  </button>
                  {app.status === "More info requested" ? (
                    <button onClick={() => setModal("info_received")}>
                      Mark info received
                    </button>
                  ) : (
                    <button onClick={() => setModal("request_info")}>
                      Request more info
                    </button>
                  )}
                  <button
                    className="danger-outline"
                    onClick={() => setModal("reject")}
                  >
                    Reject
                  </button>
                </div>
                <p className="small muted">
                  Approve unlocks when saved verification is Verified. Approve
                  never publishes.
                </p>
              </>
            ) : (
              <p className="small muted">
                Decision recorded. Verification is locked.
              </p>
            )}
          </div>
        </div>
        <ErrorBox error={error} />
        <p role="status" className="success-text">
          {notice}
        </p>
        <div className="activity">
          <h3>Activity log</h3>
          {app.log.length ? (
            <ul>
              {[...app.log].reverse().map((entry, i) => (
                <li key={`${entry.at}-${i}`}>
                  <strong>{entry.action}</strong> · {entry.by} ·{" "}
                      {dateTime(entry.at)}
                  {entry.note && <p>{entry.note}</p>}
                </li>
              ))}
            </ul>
          ) : (
            <p className="small muted">No activity yet.</p>
          )}
        </div>
      </Card>
      {["Approved", "Published"].includes(app.status) && (
        <AccountPanel
          app={app}
          account={account}
          onResend={() => setModal("resend")}
        />
      )}
      <PublishPanel reference={reference} app={app} onPublished={refresh} />
      {modal && (
        <ReasonModal
          action={modal}
          reference={reference}
          onClose={() => setModal(null)}
          onConfirm={confirm}
        />
      )}
    </>
  );
}
function AccountPanel({ app, account, onResend }) {
  const [copied, setCopied] = useState(false);
  const [copyError, setCopyError] = useState(null);
  useEffect(() => {
    setCopied(false);
    setCopyError(null);
  }, [account?.activationLink]);
  async function copy() {
    try {
      await navigator.clipboard.writeText(account.activationLink);
      setCopied(true);
      setCopyError(null);
    } catch {
      setCopyError("Copy failed. Select and copy the link below manually.");
    }
  }
  return (
    <Card title="Account">
      <div className="spread">
        <p>{app.email}</p>
        <Pill status={account?.state || app.account.state} />
      </div>
      {account && typeof account.emailSent === "boolean" ? (
        <p className={account.emailSent ? "info-box" : "warning-box"}>
          {account.emailSent
            ? "Activation email sent."
            : "Activation email was not sent. Share the one-time link privately."}
        </p>
      ) : (
        <p className="small muted">
          Email delivery status is returned only when a link is created or
          resent.
        </p>
      )}
      {account?.activationLink ? (
        <>
          <Field
            name="activation-link"
            label="One-time activation link"
            readOnly
            value={account.activationLink}
          />
          <p className="small muted">
            Expires {dateTime(account.expiresAt)} · Single use · Available only in
            this view
          </p>
          <div className="actions">
            <button onClick={copy}>{copied ? "Copied ✓" : "Copy link"}</button>
            <a
              className="button"
              href={`https://wa.me/${app.phone.replace(/\D/g, "")}?text=${encodeURIComponent(`Set up your CrowdAid account: ${account.activationLink}`)}`}
              target="_blank"
              rel="noreferrer"
            >
              Send via WhatsApp ↗
            </a>
          </div>
          <ErrorBox error={copyError} />
        </>
      ) : (
        <p className="small muted">
          {app.account.state === "Activated"
            ? "The applicant has activated their account."
            : "The previous one-time link cannot be retrieved. Send a new link if needed."}
        </p>
      )}
      <button onClick={onResend}>Send a new link</button>
    </Card>
  );
}
const emptyPublication = {
  title: "",
  location: "",
  goal: "",
  organizer: "",
  story: "",
  consentRecorded: "",
  publicationConsent: false,
  contentReviewed: false,
};
function PublishPanel({ reference, app, onPublished }) {
  const [form, setForm] = useState(emptyPublication);
  const [modal, setModal] = useState(false);
  const [error, setError] = useState(null);
  function open(event) {
    event.preventDefault();
    const fields = Object.fromEntries(
      ["title", "location", "story", "consentRecorded"]
        .filter((key) => !form[key].trim())
        .map((key) => [key, "This field is required."]),
    );
    if (Object.keys(fields).length) {
      setError({ message: "Please fix the highlighted fields.", fields });
      return;
    }
    if (!Number.isInteger(Number(form.goal)) || Number(form.goal) <= 0) {
      setError({
        message: "Enter a positive whole naira amount.",
        fields: { goal: "Enter a positive whole number." },
      });
      return;
    }
    setError(null);
    setModal(true);
  }
  const field = (key, label, extra = {}) => (
    <Field
      key={key}
      name={`public-${key}`}
      label={label}
      value={form[key]}
      required={key !== "organizer"}
      error={error?.fields?.[key]}
      onChange={(e) => setForm({ ...form, [key]: e.target.value })}
      {...extra}
    />
  );
  return (
    <Card title="Publish campaign" className="publish-card">
      {app.status === "Published" ? (
        <p className="info-box">
          ✓ Published · {app.publication?.title || app.campaignId}
        </p>
      ) : app.status !== "Approved" ? (
        <p className="muted">
          {app.status === "Rejected"
            ? "This application was rejected and cannot be published."
            : "🔒 Unlocks when the status is Approved. Approving and publishing are separate steps."}
        </p>
      ) : (
        <form noValidate onSubmit={open}>
          <p className="small muted">
            Public information only. Fields start empty. Write only what the
            family has consented to share.
          </p>
          <ErrorBox error={error} />
          <div className="publish-grid">
            {field("title", "Public title *")}
            {field("location", "Public location *")}
            {field("goal", "Funding goal (₦) *", {
              type: "number",
              min: 1,
              step: 1,
              inputMode: "numeric",
            })}
            {field("organizer", "Organizer shown (optional)")}
          </div>
          {field("story", "Public story *", { textarea: true, rows: 4 })}
          {field("consentRecorded", "Consent recorded (date & how) *")}
          <label className="checkbox">
            <input
              type="checkbox"
              checked={form.publicationConsent}
              onChange={(e) =>
                setForm({ ...form, publicationConsent: e.target.checked })
              }
            />
            Publication consent obtained (separate from application consent)
          </label>
          <label className="checkbox">
            <input
              type="checkbox"
              checked={form.contentReviewed}
              onChange={(e) =>
                setForm({ ...form, contentReviewed: e.target.checked })
              }
            />
            Public content reviewed: no contact details, documents or
            unconsented medical details
          </label>
          <button
            className="primary"
            disabled={!form.publicationConsent || !form.contentReviewed}
          >
            Publish campaign…
          </button>
          <p className="small muted">
            Both consent and content review must be confirmed before publishing.
          </p>
        </form>
      )}
      {modal && (
        <ReasonModal
          action="publish"
          reference={reference}
          onClose={() => setModal(false)}
          onConfirm={async () => {
            await adminApi.publish(reference, {
              ...form,
              goal: Number(form.goal),
            });
            await onPublished();
          }}
        >
          <div className="info-box">
            <strong>{form.title}</strong>
            <br />
            {form.location} · {money(form.goal)}
          </div>
        </ReasonModal>
      )}
    </Card>
  );
}

