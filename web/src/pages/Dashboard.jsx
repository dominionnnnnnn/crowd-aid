import { useEffect, useState } from "react";
import {
  Link,
  NavLink,
  Navigate,
  useLocation,
  useNavigate,
} from "react-router-dom";
import { campaignerApi, DASHBOARD_MOCKS } from "../api/campaigner";
import {
  getCampaignerToken,
  setCampaignerToken,
} from "../api/campaignerSession";
import { requiredFields, wholeAmount } from "../components/constants";
import { useForm, useResource } from "../components/hooks";
import {
  Empty,
  ErrorSummary,
  Field,
  Funding,
  LoadError,
  Loading,
  ResultPanel,
  Submit,
  date,
  money,
} from "../components/UI";
export default function Dashboard() {
  const [token, setToken] = useState(getCampaignerToken());
  const navigate = useNavigate();
  useEffect(() => {
    const expired = () => {
      setToken(null);
      navigate("/login?expired=1", { replace: true });
    };
    window.addEventListener("crowdaid:campaigner-expired", expired);
    return () =>
      window.removeEventListener("crowdaid:campaigner-expired", expired);
  }, [navigate]);
  if (!token) return <Navigate to="/login" replace />;
  return (
    <DashboardContent
      onSignOut={() => {
        setCampaignerToken(null);
        setToken(null);
        navigate("/login", { replace: true });
      }}
    />
  );
}
function DashboardContent({ onSignOut }) {
  const location = useLocation();
  const view = location.pathname.endsWith("/offers")
    ? "offers"
    : location.pathname.endsWith("/withdraw")
      ? "withdraw"
      : "overview";
  const resource = useResource(
    () =>
      Promise.all([
        campaignerApi.me(),
        campaignerApi.campaign(),
        campaignerApi.donations(),
        campaignerApi.offers(),
        campaignerApi.payout(),
        campaignerApi.withdrawals(),
      ]).then(([user, overview, donations, offers, payout, withdrawals]) => ({
        user,
        overview,
        donations,
        offers,
        payout,
        withdrawals,
      })),
    [],
  );
  const [snapshot, setSnapshot] = useState(null);
  useEffect(() => {
    if (resource.data) setSnapshot(resource.data);
  }, [resource.data]);
  if (!snapshot)
    return (
      <div className="p-container p-page">
        {resource.error ? (
          <LoadError resource={resource} />
        ) : (
          <Loading label="Loading your dashboard…" />
        )}
      </div>
    );
  const { user, overview, donations, offers, payout, withdrawals } = snapshot;
  const c = overview.campaign;
  const steps = ["Received", "In review", "Verified", "Approved", "Published"];
  const current = steps.indexOf(overview.application.status);
  return (
    <div className="p-container p-page p-stack">
      <div className="p-spread">
        <div className="p-stack">
          <p className="p-eyebrow">Your CrowdAid account</p>
          <h1>Hello, {user.name.split(" ")[0]}</h1>
          <p className="p-muted">
            Follow your campaign and the support from your community.
          </p>
        </div>
        <button onClick={onSignOut}>Sign out</button>
      </div>
      {DASHBOARD_MOCKS && (
        <p className="p-warning">
          <strong>Demo: no real money moves.</strong> You are signed in to your
          real account. The campaign figures, offers and withdrawals below are
          fictional examples while dashboard data is being connected.
        </p>
      )}
      <nav className="p-dashboard-nav" aria-label="Your account">
        <NavLink to="/dashboard" end>
          Overview
        </NavLink>
        <NavLink to="/dashboard/offers">Practical help</NavLink>
        <NavLink to="/dashboard/withdraw">Withdraw</NavLink>
      </nav>
      {resource.error && <LoadError resource={resource} />}
      <section className="p-card p-stack">
        <div className="p-spread">
          <h2>
            {c?.title || `Application for ${overview.application.patientName}`}
          </h2>
          <span className="p-pill">{overview.application.status}</span>
        </div>
        <p className="p-reference">
          Reference <strong>{overview.application.reference}</strong>
        </p>
        <ol className="p-status-steps">
          {steps.map((step, i) => (
            <li
              key={step}
              className={i <= current ? "done" : ""}
              aria-current={i === current ? "step" : undefined}
            >
              <span>{i < current ? "✓" : i + 1}</span>
              {step}
            </li>
          ))}
        </ol>
        {!c && (
          <p className="p-info">
            Your application has been approved and is still private. CrowdAid
            will ask for your separate publication consent before your campaign
            goes public.
          </p>
        )}
      </section>
      {view === "overview" ? (
        <>
          <section className="p-card p-stack">
            <h2>Campaign progress</h2>
            {c ? (
              <>
                <Funding {...c} large />
                <div className="p-balance-grid">
                  <div>
                    <span>Withdrawn</span>
                    <strong>{money(c.withdrawn)}</strong>
                  </div>
                  <div>
                    <span>Pending withdrawals</span>
                    <strong>{money(c.pendingWithdrawals)}</strong>
                  </div>
                  <div>
                    <span>Available</span>
                    <strong>{money(c.available)}</strong>
                  </div>
                </div>
                <Link className="button p-outline" to="/dashboard/withdraw">
                  Request a demo withdrawal
                </Link>
              </>
            ) : (
              <p>
                No public campaign yet. Your funding progress will appear after
                publication.
              </p>
            )}
          </section>
          <section className="p-card p-stack">
            <h2>Donations received</h2>
            <p className="p-hint">
              Confirmed donations only. Supporter contact details are never
              shown here.
            </p>
            {donations.data.length ? (
              <div className="p-table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th>Reference</th>
                      <th>Supporter</th>
                      <th>Amount</th>
                      <th>Date</th>
                    </tr>
                  </thead>
                  <tbody>
                    {donations.data.map((d) => (
                      <tr key={d.reference}>
                        <td>{d.reference}</td>
                        <td>{d.displayName}</td>
                        <td>{money(d.amount)}</td>
                        <td>{date(d.createdAt)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p>No confirmed donations yet.</p>
            )}
          </section>
          {c?.updates?.length > 0 && (
            <section className="p-card p-stack">
              <h2>Campaign updates</h2>
              {c.updates.map((u, i) => (
                <article key={i}>
                  <p className="p-hint">{date(u.date)}</p>
                  <h3>{u.title}</h3>
                  <p>{u.text}</p>
                </article>
              ))}
            </section>
          )}
        </>
      ) : view === "offers" ? (
        <section className="p-card p-stack">
          <h2>Practical help offered</h2>
          <p className="p-info">
            CrowdAid records offers and follows up with supporters. Offers are
            not automatically matched or assigned to patients. Names and phone
            numbers stay with the review team.
          </p>
          {offers.data.length ? (
            <div className="p-help-grid">
              {offers.data.map((o) => (
                <article className="p-card p-stack" key={o.reference}>
                  <span className="p-pill">{o.status}</span>
                  <h3>{o.types.join(", ")}</h3>
                  <p>{o.details}</p>
                  <p className="p-muted">
                    {o.availability.join(", ")} · {o.area}
                  </p>
                  <p className="p-reference">
                    Reference <strong>{o.reference}</strong>
                  </p>
                </article>
              ))}
            </div>
          ) : (
            <p>No practical help offers yet.</p>
          )}
        </section>
      ) : (
        <>
          <section className="p-card p-stack">
            <h2>Withdraw support funds</h2>
            <p className="p-warning">
              <strong>Demo: no real money moves.</strong> Requests and payment
              statuses are simulated.
            </p>
            <div className="p-available">
              <span>Available balance</span>
              <strong>{money(withdrawals.available)}</strong>
            </div>
            {!c ? (
              <p>
                Your campaign must be published before you can request a
                withdrawal.
              </p>
            ) : (
              <WithdrawalForm
                available={withdrawals.available}
                payoutSet={overview.payoutSet}
                onChanged={resource.reload}
              />
            )}
          </section>
          <section className="p-card p-stack">
            <h2>Payout details (private)</h2>
            {payout && (
              <p className="p-info">
                {payout.bankName} · {payout.accountNumber} ·{" "}
                {payout.accountName}
              </p>
            )}
            <PayoutForm payout={payout} onChanged={resource.reload} />
          </section>
          <section className="p-card p-stack">
            <h2>Withdrawal history</h2>
            {withdrawals.data.length ? (
              <div className="p-table-scroll">
                <table>
                  <thead>
                    <tr>
                      <th>Reference</th>
                      <th>Amount</th>
                      <th>Status</th>
                      <th>Requested</th>
                      <th>Note</th>
                    </tr>
                  </thead>
                  <tbody>
                    {withdrawals.data.map((w) => (
                      <tr key={w.reference}>
                        <td>{w.reference}</td>
                        <td>{money(w.amount)}</td>
                        <td>
                          <span className="p-pill">{w.status}</span>
                        </td>
                        <td>{date(w.requestedAt)}</td>
                        <td>{w.note || "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <p>No withdrawals requested.</p>
            )}
          </section>
        </>
      )}
    </div>
  );
}
function WithdrawalForm({ available, payoutSet, onChanged }) {
  const f = useForm({ amount: "" }, "withdraw-");
  return (
    <form
      noValidate
      onSubmit={f.submit(
        async (v) => {
          const result = await campaignerApi.withdraw(Number(v.amount));
          onChanged();
          return result;
        },
        (v) =>
          !wholeAmount(v.amount, 1000)
            ? { amount: "Enter a whole naira amount of at least ₦1,000." }
            : Number(v.amount) > available
              ? { amount: "Enter no more than your available balance." }
              : {},
      )}
    >
      <ErrorSummary error={f.error} prefix={f.prefix} />
      {!payoutSet && (
        <p className="p-warning">
          Save your payout details below before making a request.
        </p>
      )}
      <Field
        label="Withdrawal amount (₦) *"
        {...f.bind("amount")}
        type="number"
        min={1000}
        step={1}
        max={available}
        inputMode="numeric"
      />
      <Submit
        busy={f.busy}
        busyLabel="Requesting…"
        disabled={f.busy || !payoutSet || available < 1000}
      >
        Request demo withdrawal
      </Submit>
      {f.result && (
        <div className="p-info" role="status">
          <strong>Demo withdrawal requested</strong>
          <p>
            Reference {f.result.reference} · {f.result.status}
          </p>
          <p>Available: {money(f.result.available)}. No real money moves.</p>
        </div>
      )}
    </form>
  );
}
function PayoutForm({ payout, onChanged }) {
  const f = useForm(
    {
      bankName: payout?.bankName || "",
      accountNumber: "",
      accountName: payout?.accountName || "",
    },
    "payout-",
  );
  return (
    <form
      noValidate
      onSubmit={f.submit(
        async (v) => {
          const result = await campaignerApi.savePayout(v);
          f.set("accountNumber", "");
          onChanged();
          return result;
        },
        (v) => ({
          ...requiredFields(v, ["bankName", "accountNumber", "accountName"]),
          ...(!/^\d{10}$/.test(v.accountNumber)
            ? { accountNumber: "Enter exactly 10 digits." }
            : {}),
        }),
      )}
    >
      <ErrorSummary error={f.error} prefix={f.prefix} />
      <p className="p-hint">
        Demo only: use fictional bank details. Enter the full number to update
        it; saved numbers are displayed masked.
      </p>
      <div className="p-form-grid">
        <Field label="Bank name *" {...f.bind("bankName")} />
        <Field
          label="Account number *"
          {...f.bind("accountNumber")}
          inputMode="numeric"
          maxLength={10}
        />
      </div>
      <Field label="Account name *" {...f.bind("accountName")} />
      <Submit busy={f.busy} busyLabel="Saving…">
        Save demo payout details
      </Submit>
      {f.result && (
        <p className="p-info" role="status">
          Demo payout details saved.
        </p>
      )}
    </form>
  );
}
