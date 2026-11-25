import { useEffect, useState } from "react";
import { adminApi, USE_MOCKS } from "../api/admin";
import { getToken, setToken } from "../api";
import { ErrorBox, Field, errorBody } from "./components";
import Applications from "./Applications";
import Inbox from "./Inbox";
const tabs = [
  ["applications", "Applications"],
  ["offers", "Practical help offers"],
  ["volunteers", "Volunteer registrations"],
  ["messages", "Messages"],
  ["withdrawals", "Withdrawals"],
];
export default function Admin() {
  const [token, updateToken] = useState(getToken());
  const [expired, setExpired] = useState(false);
  const [tab, setTab] = useState("applications");
  const [admin, setAdmin] = useState(null);
  const [restoring, setRestoring] = useState(!!getToken());
  const [sessionError, setSessionError] = useState(null);
  const [sessionRetry, setSessionRetry] = useState(0);
  useEffect(() => {
    if (!token) {
      setRestoring(false);
      setAdmin(null);
      return;
    }
    let active = true;
    setRestoring(true);
    setSessionError(null);
    adminApi
      .me()
      .then((user) => {
        if (!active) return;
        if (user.role !== "admin") {
          setToken(null);
          updateToken(null);
          setExpired(true);
          return;
        }
        setAdmin(user);
      })
      .catch((error) => {
        if (active && error.response?.status !== 401)
          setSessionError(errorBody(error));
      })
      .finally(() => {
        if (active) setRestoring(false);
      });
    return () => {
      active = false;
    };
  }, [token, sessionRetry]);
  useEffect(() => {
    const expire = () => {
      updateToken(null);
      setExpired(true);
    };
    window.addEventListener("crowdaid:session-expired", expire);
    return () => window.removeEventListener("crowdaid:session-expired", expire);
  }, []);
  const signOut = () => {
    setToken(null);
    updateToken(null);
    setExpired(false);
  };
  return (
    <div className="admin-shell">
      <header className="topbar">
        <div className="topbar-inner">
          <div className="brand">
            <img src="/crowdaid-symbol.svg" alt="" />
            <span>CrowdAid Admin</span>
          </div>
          {token && (
            <div className="actions">
              <span className="small muted">
                {admin?.name || "Restoring session…"}
              </span>
              <button onClick={signOut}>
                Sign out <span aria-hidden="true">↗</span>
              </button>
            </div>
          )}
        </div>
      </header>
      {!token ? (
        <SignIn
          expired={expired}
          onLogin={(result) => {
            setAdmin(result.admin);
            setToken(result.token);
            updateToken(result.token);
            setExpired(false);
          }}
        />
      ) : (
        <main className="workspace">
          <h1 className="sr-only">CrowdAid admin</h1>
          {restoring ? (
            <p role="status">Restoring your admin session…</p>
          ) : sessionError ? (
            <>
              <ErrorBox error={sessionError} />
              <button onClick={() => setSessionRetry((v) => v + 1)}>
                Try again
              </button>
            </>
          ) : (
            <>
              <nav className="tabs" aria-label="Admin sections">
                {tabs.map(([key, label]) => (
                  <button
                    key={key}
                    aria-current={tab === key ? "page" : undefined}
                    className={tab === key ? "active" : ""}
                    onClick={() => setTab(key)}
                  >
                    {label}
                    {key === "withdrawals" && (
                      <span className="tab-demo">Demo</span>
                    )}
                  </button>
                ))}
              </nav>
              {USE_MOCKS && (
                <p className="demo-note">
                  Fictional demo data. Changes last until this page is
                  refreshed. Emails and payments are simulated.
                </p>
              )}
              {tab === "applications" ? (
                <Applications />
              ) : (
                <Inbox key={tab} kind={tab} />
              )}
            </>
          )}
        </main>
      )}
      <footer className="admin-footer">
        CrowdAid · Healthcare, Together. <span>Private admin workspace</span>
      </footer>
    </div>
  );
}
function SignIn({ onLogin, expired }) {
  const [form, setForm] = useState({ email: "", password: "" });
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  async function submit(event) {
    event.preventDefault();
    setError(null);
    const fields = {};
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim()))
      fields.email = "Enter a valid email address.";
    if (!form.password) fields.password = "Enter your password.";
    if (Object.keys(fields).length) {
      setError({ message: "Please fix the highlighted fields.", fields });
      return;
    }
    setBusy(true);
    try {
      const result = await adminApi.login(form);
      onLogin(result);
    } catch (e) {
      setError(errorBody(e));
    } finally {
      setBusy(false);
    }
  }
  return (
    <main className="signin-wrap">
      <form className="card signin" noValidate onSubmit={submit}>
        <span className="eyebrow">CrowdAid review team</span>
        <h1>Sign in</h1>
        <p className="muted">For pre-created CrowdAid admin accounts.</p>
        {USE_MOCKS && (
          <p className="info-box">
            Demo: use any valid email and any non-empty password. Use fictional
            details.
          </p>
        )}
        {expired && <ErrorBox error="Your session expired. Sign in again." />}
        <ErrorBox error={error} />
        <Field
          name="email"
          label="Email"
          type="email"
          autoComplete="username"
          required
          value={form.email}
          error={error?.fields?.email}
          onChange={(e) => setForm({ ...form, email: e.target.value })}
        />
        <Field
          name="password"
          label="Password"
          type="password"
          autoComplete="current-password"
          required
          value={form.password}
          error={error?.fields?.password}
          onChange={(e) => setForm({ ...form, password: e.target.value })}
        />
        <button className="primary" disabled={busy}>
          {busy ? "Signing in…" : "Sign in"}
        </button>
        <p className="small muted">
          Only the review team can access applications and contact details.
        </p>
      </form>
    </main>
  );
}
