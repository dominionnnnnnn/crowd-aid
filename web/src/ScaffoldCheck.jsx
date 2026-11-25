import { useEffect, useState } from "react";
import api from "./api";
import { asError } from "./components/hooks";

// CrowdAid scaffold page: proves React -> /api -> Postgres and API -> MailHog.
// Replace with the real app (routes, pages from the design) once hosting is confirmed.
export default function App() {
  const [health, setHealth] = useState(null);
  const [hello, setHello] = useState(null);
  const [error, setError] = useState("");
  const [to, setTo] = useState("");
  const [mail, setMail] = useState("");

  useEffect(() => {
    api
      .get("/api/health", { baseURL: "" })
      .then((r) => setHealth(r.data))
      .catch((e) => setError(asError(e).message));
    api
      .get("/api/hello", { baseURL: "" })
      .then((r) => setHello(r.data))
      .catch((e) => setError(asError(e).message));
  }, []);

  const sendTest = async (e) => {
    e.preventDefault();
    setMail("Sending…");
    try {
      const r = await api.post("/api/test-email", { to }, { baseURL: "" });
      setMail(`✅ Sent to ${r.data.to}. Open MailHog to see it.`);
    } catch (err) {
      const error = asError(err);
      setMail(`⚠ ${error.message}${error.fields.to ? ` ${error.fields.to}` : ""}`);
    }
  };

  return (
    <main style={s.page}>
      <h1 style={{ color: "#0B6B3A" }}>CrowdAid</h1>
      <p style={s.muted}>Healthcare, Together. · scaffold check</p>

      <section style={s.card}>
        <h2>1 · API + database</h2>
        {health && <p>Health: {health.status}</p>}
        {error && <p style={{ color: "#B42318" }}>❌ {error}</p>}
        {hello ? (
          <p style={s.big}>✅ {hello.message}</p>
        ) : (
          !error && <p>Loading…</p>
        )}
      </section>

      <section style={s.card}>
        <h2>2 · Email (MailHog)</h2>
        <form onSubmit={sendTest} style={{ display: "flex", gap: 8 }}>
          <input
            type="email"
            required
            value={to}
            onChange={(e) => setTo(e.target.value)}
            placeholder="any@email.com (nothing real is sent)"
            style={s.input}
          />
          <button style={s.btn}>Send test email</button>
        </form>
        {mail && <p>{mail}</p>}
        <p style={s.muted}>
          Open MailHog through the configured development port or SSH tunnel.
          See the project README for connection instructions.
        </p>
      </section>
    </main>
  );
}

const s = {
  page: {
    fontFamily: "'Plus Jakarta Sans', system-ui, sans-serif",
    maxWidth: 640,
    margin: "40px auto",
    padding: 16,
    color: "#14271E",
  },
  muted: { color: "#4A5C54" },
  card: {
    border: "1px solid #D5DFDA",
    borderRadius: 14,
    padding: 16,
    marginTop: 16,
    background: "#fff",
  },
  big: { fontSize: 20, fontWeight: 700 },
  input: {
    flex: 1,
    padding: 10,
    borderRadius: 10,
    border: "1px solid #7A8A83",
    minHeight: 44,
  },
  btn: {
    padding: "10px 16px",
    borderRadius: 10,
    border: 0,
    background: "#0B6B3A",
    color: "#fff",
    cursor: "pointer",
    minHeight: 44,
    fontWeight: 600,
  },
};
