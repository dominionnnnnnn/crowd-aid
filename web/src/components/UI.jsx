import { useEffect, useRef } from "react";
import { Link } from "react-router-dom";
import { helpCategories } from "./constants";
export const money = (value) =>
  `₦${Number(value || 0).toLocaleString("en-NG")}`;
export const date = (value) =>
  new Date(
    value.length === 10 ? `${value}T12:00:00` : value,
  ).toLocaleDateString("en-NG", {
    day: "numeric",
    month: "short",
    year: "numeric",
  });
export function Icon({ name, ...props }) {
  return (
    <img
      src={`/assets/icons/${name}.svg`}
      alt=""
      width="24"
      height="24"
      {...props}
    />
  );
}
export function Field({
  label,
  name,
  id = name,
  error,
  hint,
  textarea,
  options,
  children,
  ...props
}) {
  const control = {
    ...props,
    id,
    name,
    "aria-invalid": !!error,
    "aria-describedby":
      [hint && `${id}-hint`, error && `${id}-error`]
        .filter(Boolean)
        .join(" ") || undefined,
  };
  return (
    <div className="p-field">
      <label htmlFor={id}>{label}</label>
      {children ||
        (options ? (
          <select {...control}>
            {!options.some((o) => typeof o !== "string" && o.value === "") && (
              <option value="">Select…</option>
            )}
            {options.map((o) => (
              <option
                key={typeof o === "string" ? o : o.value}
                value={typeof o === "string" ? o : o.value}
              >
                {typeof o === "string" ? o : o.label}
              </option>
            ))}
          </select>
        ) : textarea ? (
          <textarea {...control} />
        ) : (
          <input {...control} />
        ))}
      {hint && (
        <p className="p-hint" id={`${id}-hint`}>
          {hint}
        </p>
      )}
      {error && (
        <p className="p-field-error" id={`${id}-error`}>
          ⚠ {error}
        </p>
      )}
    </div>
  );
}
export function ErrorSummary({ error, prefix = "" }) {
  const ref = useRef(null);
  const hasError = !!error;
  useEffect(() => {
    if (hasError) ref.current?.focus();
  }, [hasError]);
  if (!error) return null;
  return (
    <div role="alert" tabIndex={-1} ref={ref} className="p-error">
      <strong>⚠ {error.message}</strong>
      {Object.entries(error.fields || {}).filter(([, v]) => v).length > 0 && (
        <ul>
          {Object.entries(error.fields)
            .filter(([, v]) => v)
            .map(([key, message]) => (
              <li key={key}>
                <a
                  href={`#${prefix}${key}`}
                  onClick={(e) => {
                    e.preventDefault();
                    document.getElementById(`${prefix}${key}`)?.focus();
                  }}
                >
                  {message}
                </a>
              </li>
            ))}
        </ul>
      )}
    </div>
  );
}
export function ChoiceGroup({
  label,
  name,
  values = [],
  options,
  onChange,
  error,
  radio = false,
  rich = false,
  needed = [],
  prefix = "",
}) {
  return (
    <fieldset
      className={`p-choices ${error ? "has-error" : ""}`}
      id={`${prefix}${name}`}
      tabIndex={-1}
      aria-describedby={error ? `${prefix}${name}-error` : undefined}
    >
      <legend>{label}</legend>
      <div className={rich ? "p-choice-grid" : "p-choice-options"}>
        {options.map((option) => {
          const value = typeof option === "string" ? option : option.value;
          const checked = radio ? values === value : values.includes(value);
          const meta = helpCategories.find((c) => c.value === value);
          return (
            <label
              key={value}
              className={`p-choice ${checked ? "is-selected" : ""} ${rich ? "rich-choice" : ""}`}
            >
              <input
                type={radio ? "radio" : "checkbox"}
                name={name}
                value={value}
                checked={checked}
                onChange={() =>
                  onChange(
                    radio
                      ? value
                      : checked
                        ? values.filter((v) => v !== value)
                        : [...values, value],
                  )
                }
              />
              {rich && meta && <Icon name={meta.icon} />}
              <span>
                <strong>
                  {typeof option === "string" ? option : option.label}
                </strong>
                {rich && meta && <small>{meta.description}</small>}
                {needed.includes(value) && (
                  <small className="p-needed">Needed for this campaign</small>
                )}
              </span>
            </label>
          );
        })}
      </div>
      {error && (
        <p className="p-field-error" id={`${prefix}${name}-error`}>
          ⚠ {error}
        </p>
      )}
    </fieldset>
  );
}
export function Consent({
  name,
  checked,
  onChange,
  error,
  children,
  prefix = "",
}) {
  return (
    <div>
      <label className={`p-consent ${error ? "has-error" : ""}`}>
        <input
          id={`${prefix}${name}`}
          name={name}
          type="checkbox"
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
          aria-invalid={!!error}
          aria-describedby={error ? `${prefix}${name}-error` : undefined}
        />
        <span>{children}</span>
      </label>
      {error && (
        <p className="p-field-error" id={`${prefix}${name}-error`}>
          ⚠ {error}
        </p>
      )}
    </div>
  );
}
export function Submit({ busy, children, busyLabel = "Sending…", ...props }) {
  return (
    <button className="button p-primary" disabled={busy} {...props}>
      {busy && <span className="p-spinner" aria-hidden="true" />}
      {busy ? busyLabel : children}
    </button>
  );
}
export function Loading({ label = "Loading…", cards = false }) {
  return (
    <div
      role="status"
      aria-label={label}
      className={cards ? "p-campaign-grid" : "p-loading"}
    >
      <span className="sr-only">{label}</span>
      {cards ? (
        <>
          <div className="p-skeleton" />
          <div className="p-skeleton" />
        </>
      ) : (
        label
      )}
    </div>
  );
}
export function LoadError({ resource }) {
  return (
    <div className="p-stack">
      <ErrorSummary error={resource.error} />
      <button className="button" onClick={resource.reload}>
        Try again
      </button>
    </div>
  );
}
export function Empty({ title, children }) {
  return (
    <section className="p-empty">
      <h2>{title}</h2>
      {children}
    </section>
  );
}
export function Funding({ raised, goal, supporters, large = false }) {
  const percent = goal > 0 ? Math.round((raised / goal) * 100) : 0;
  return (
    <div className={`p-funding ${large ? "large" : ""}`}>
      <div className="p-funding-numbers">
        <div>
          <span>Raised</span>
          <strong>{money(raised)}</strong>
        </div>
        <div>
          <span>Goal</span>
          <strong>{money(goal)}</strong>
        </div>
      </div>
      <div
        className="p-progress"
        role="progressbar"
        aria-label="Campaign funding"
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.min(100, Math.max(0, percent))}
        aria-valuetext={`${percent}% funded. ${money(raised)} raised of ${money(goal)}.`}
      >
        <span style={{ width: `${Math.min(100, Math.max(0, percent))}%` }} />
      </div>
      <div className="p-spread p-hint">
        <span>
          <strong>{percent}%</strong> funded
        </span>
        <span>
          {raised >= goal && goal > 0
            ? "Goal reached"
            : `${money(Math.max(0, goal - raised))} remaining`}
        </span>
      </div>
      {supporters != null && (
        <p className="p-hint">
          {supporters} supporter{supporters === 1 ? "" : "s"}
        </p>
      )}
    </div>
  );
}
export function CampaignTile({ campaign }) {
  return (
    <div className="p-campaign-tile">
      {campaign.imageUrl ? (
        <img className="p-campaign-photo" src={campaign.imageUrl} alt="" />
      ) : (
        <img src="/assets/logo/crowdaid-symbol.svg" alt="" />
      )}
      {campaign.reviewed && (
        <span className="p-reviewed">✓ Reviewed by CrowdAid</span>
      )}
    </div>
  );
}
export function CampaignCard({ campaign, featured = false }) {
  const href = `/campaigns/${encodeURIComponent(campaign.id)}`;
  return (
    <article className={`p-campaign-card ${featured ? "featured" : ""}`}>
      <Link to={href} tabIndex={-1} aria-hidden="true">
        <CampaignTile campaign={campaign} />
      </Link>
      <div className="p-card-content">
        <h2>
          <Link to={href}>{campaign.title}</Link>
        </h2>
        <p className="p-hint">
          {campaign.location} · {campaign.condition}
        </p>
        <p className="p-preview">{campaign.preview}</p>
        <Funding {...campaign} />
        <div className="p-two-buttons">
          <Link className="button p-outline" to={href}>
            {featured ? "View campaign" : "View details"}
          </Link>
          <Link
            className="button p-primary"
            to={`/donate?campaign=${encodeURIComponent(campaign.id)}`}
          >
            Donate
          </Link>
        </div>
      </div>
    </article>
  );
}
export function HelpCategories() {
  return (
    <section className="p-section p-stack">
      <h2>Help beyond donations</h2>
      <p className="p-muted">
        Not everyone can give money, and not every need is about money. Choose
        something you can offer and CrowdAid will follow up when it's needed.
      </p>
      <div className="p-help-grid">
        {helpCategories.map((c) => (
          <Link
            key={c.value}
            className="p-help-link"
            to={`/help?type=${encodeURIComponent(c.value)}`}
          >
            <span className="p-icon-tile">
              <Icon name={c.icon} />
            </span>
            <span>
              <strong>{c.value}</strong>
              <small>{c.description}</small>
            </span>
            <span aria-hidden="true">›</span>
          </Link>
        ))}
      </div>
      <p>
        Prefer to give your time?{" "}
        <Link to="/volunteer">Volunteer with CrowdAid</Link>
      </p>
    </section>
  );
}
export function ResultPanel({
  title,
  reference,
  status,
  children,
  tone = "success",
}) {
  const ref = useRef(null);
  useEffect(() => ref.current?.focus(), []);
  return (
    <section className={`p-result ${tone}`}>
      <div className="p-result-symbol" aria-hidden="true">
        {tone === "success" ? "✓" : tone === "pending" ? "◷" : "!"}
      </div>
      <h1 ref={ref} tabIndex={-1}>
        {title}
      </h1>
      {reference && (
        <p className="p-reference">
          Reference <strong>{reference}</strong>
        </p>
      )}
      {status && (
        <p>
          <strong>{status}</strong>
        </p>
      )}
      {children}
    </section>
  );
}
export function Modal({ title, onClose, busy, children }) {
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
      className="p-dialog"
      aria-labelledby="public-dialog-title"
      onCancel={(e) => {
        e.preventDefault();
        if (!busy) onClose();
      }}
    >
      <div className="p-dialog-inner">
        <div className="p-spread">
          <h2 id="public-dialog-title">{title}</h2>
          <button
            aria-label="Close dialog"
            disabled={busy}
            onClick={onClose}
            className="p-close"
          >
            ×
          </button>
        </div>
        {children}
      </div>
    </dialog>
  );
}
export function BackLink({ to = "/", children = "Home" }) {
  return (
    <div className="p-back">
      <div className="p-container">
        <Link to={to}>← {children}</Link>
      </div>
    </div>
  );
}
export function TestBadge({ testMode }) {
  return testMode ? (
    <span className="p-test-badge">Test mode, no real money</span>
  ) : null;
}

