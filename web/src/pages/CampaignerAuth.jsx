import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { campaignerApi, USE_MOCKS } from "../api/campaigner";
import { setCampaignerToken } from "../api/campaignerSession";
import { requiredFields, validEmail } from "../components/constants";
import { useForm, useResource } from "../components/hooks";
import {
  ErrorSummary,
  Field,
  LoadError,
  Loading,
  Submit,
} from "../components/UI";
export function Login() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const f = useForm({ email: "", password: "" }, "login-");
  async function send(v) {
    const result = await campaignerApi.login(v);
    setCampaignerToken(result.token);
    navigate("/dashboard", { replace: true });
    return result;
  }
  return (
    <div className="p-auth-page p-card p-stack">
      <p className="p-eyebrow">Your CrowdAid account</p>
      <h1>Sign in</h1>
      <p className="p-muted">
        For applicants whose request has been approved. Use the link in your
        approval email to set your password first.
      </p>
      {USE_MOCKS && (
        <p className="p-warning">
          Mock campaigner demo. Use a fictional valid email and any non-empty
          password. This account is separate from admin.
        </p>
      )}
      {params.get("expired") && (
        <p className="p-error" role="alert">
          ⚠ Your session expired. Sign in again.
        </p>
      )}
      <form
        noValidate
        onSubmit={f.submit(send, (v) => ({
          ...requiredFields(v, ["email", "password"]),
          ...(!validEmail(v.email)
            ? { email: "Enter a valid email address." }
            : {}),
        }))}
      >
        <ErrorSummary error={f.error} prefix={f.prefix} />
        <Field
          label="Email *"
          {...f.bind("email")}
          type="email"
          autoComplete="username"
        />
        <Field
          label="Password *"
          {...f.bind("password")}
          type="password"
          autoComplete="current-password"
        />
        <Submit busy={f.busy} busyLabel="Signing in…">
          Sign in
        </Submit>
      </form>
      <Link to="/apply/status">
        Waiting for approval? Check application status
      </Link>
    </div>
  );
}
export function Activate() {
  const [params] = useSearchParams();
  const [token] = useState(() => params.get("token") || "");
  const navigate = useNavigate();
  useEffect(() => {
    const url = new URL(window.location.href);
    url.searchParams.delete("token");
    window.history.replaceState(
      window.history.state,
      "",
      url.pathname + url.search + url.hash,
    );
  }, []);
  const resource = useResource(
    () =>
      token
        ? campaignerApi.checkActivation(token)
        : Promise.reject(
            new Error(
              "This link is missing its activation token. Ask CrowdAid for a new link.",
            ),
          ),
    [token],
  );
  const f = useForm({ password: "", confirmPassword: "" }, "activate-");
  async function send(v) {
    const result = await campaignerApi.activate({
      token,
      password: v.password,
    });
    setCampaignerToken(result.token);
    navigate("/dashboard", { replace: true });
    return result;
  }
  return (
    <div className="p-auth-page p-card p-stack">
      <p className="p-eyebrow">Welcome to CrowdAid</p>
      <h1>Set up your account</h1>
      {USE_MOCKS && (
        <p className="p-warning">
          Mock activation. Demo tokens begin with “demo-”; “expired” and “used”
          show the recovery states. No real account password is changed.
        </p>
      )}
      {resource.loading ? (
        <Loading label="Checking your link…" />
      ) : resource.error ? (
        <>
          <LoadError resource={resource} />
          <Link to="/login">Sign in instead</Link>
        </>
      ) : (
        <>
          <p>
            Hello {resource.data.name}. Set a password to follow{" "}
            {resource.data.patientName}'s application.
          </p>
          <p className="p-reference">
            Reference <strong>{resource.data.applicationRef}</strong>
          </p>
          <p className="p-hint">Account email: {resource.data.emailMasked}</p>
          <form
            noValidate
            onSubmit={f.submit(send, (v) => ({
              ...requiredFields(v, ["password", "confirmPassword"]),
              ...(v.password.length < 8
                ? { password: "Use at least 8 characters." }
                : {}),
              ...(v.password !== v.confirmPassword
                ? { confirmPassword: "The passwords must match." }
                : {}),
            }))}
          >
            <ErrorSummary error={f.error} prefix={f.prefix} />
            <Field
              label="New password *"
              {...f.bind("password")}
              type="password"
              autoComplete="new-password"
              hint="At least 8 characters."
            />
            <Field
              label="Confirm password *"
              {...f.bind("confirmPassword")}
              type="password"
              autoComplete="new-password"
            />
            <Submit busy={f.busy} busyLabel="Setting up…">
              Set password and continue
            </Submit>
          </form>
        </>
      )}
    </div>
  );
}
