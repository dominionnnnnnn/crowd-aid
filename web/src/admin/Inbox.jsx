import { useEffect, useState } from "react";
import { adminApi } from "../api/admin";
import { publicApi } from "../api/public";
import { useResource } from "../components/hooks";
import {
  Card,
  Details,
  ErrorBox,
  Field,
  Modal,
  Pill,
  ReasonModal,
  date,
  errorBody,
  maskPhone,
  money,
  text,
} from "./components";
export default function Inbox({ kind }) {
  const [rows, setRows] = useState([]);
  const [filter, setFilter] = useState("");
  const [selected, setSelected] = useState(null);
  const [modal, setModal] = useState(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(null);
  const [error, setError] = useState(null);
  const [version, setVersion] = useState(0);
  const [notice, setNotice] = useState("");
  const [offerErrorRef, setOfferErrorRef] = useState(null);
  const outreaches = useResource(
    (signal) =>
      kind === "volunteers"
        ? publicApi.outreach(signal)
        : Promise.resolve({ data: [] }),
    [kind],
  );
  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(null);
    const params =
      filter === "Community"
        ? { kind: "Community" }
        : filter
          ? { outreachId: filter }
          : {};
    const call =
      kind === "volunteers"
        ? adminApi.volunteers(params)
        : kind === "withdrawals"
          ? adminApi.withdrawals(filter)
          : adminApi[kind]();
    call
      .then((result) => {
        if (active) setRows(result.data);
      })
      .catch((e) => {
        if (active)
          setError(
            kind === "withdrawals" &&
              e.response?.status === 404 &&
              !e.response?.data?.error
              ? {
                  message:
                    "The withdrawals endpoint is not available on this backend yet.",
                }
              : errorBody(e),
          );
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [kind, filter, version]);
  async function updateOffer(row, status) {
    setBusy(row.reference);
    setError(null);
    setNotice("");
    setOfferErrorRef(null);
    try {
      const updated = await adminApi.offerStatus(row.reference, status);
      setRows((current) =>
        current.map((r) => (r.reference === row.reference ? updated : r)),
      );
      setNotice(`${row.reference} marked ${status}.`);
    } catch (e) {
      setError(errorBody(e));
      setOfferErrorRef(row.reference);
    } finally {
      setBusy(null);
    }
  }
  const person = (row) => (
    <>
      <button className="person-button" onClick={() => setSelected(row)}>
        {row.name}
      </button>
      <span className="muted block">{maskPhone(row.phone)}</span>
    </>
  );
  const ref = (row) => (
    <>
      <span className="mono">{row.reference}</span>
      {row.isNew && <span className="new-tag block">New</span>}
    </>
  );
  return (
    <div className="inbox">
      <div className="spread">
        <h2>
          {
            {
              offers: "Practical help offers",
              volunteers: "Volunteer registrations",
              messages: "Messages",
              withdrawals: "Withdrawals",
            }[kind]
          }
        </h2>
        <span className="small muted" role="status">
          {loading ? "Loading…" : `${rows.length} records`}
        </span>
      </div>
      {kind === "volunteers" && (
        <Field name="outreach-filter" label="Filter by outreach">
          <select
            id="outreach-filter"
            className="filter-select"
            value={filter}
            onChange={(e) => setFilter(e.target.value)}
          >
            <option value="">All registrations</option>
            {(outreaches.data?.data || []).map((event) => (
              <option key={event.id} value={event.id}>
                {event.title} · {date(event.date)}
              </option>
            ))}
            <option value="Community">Volunteer community (ongoing)</option>
          </select>
        </Field>
      )}
      {kind === "volunteers" && outreaches.error && (
        <>
          <ErrorBox error={outreaches.error} />
          <button onClick={outreaches.reload}>Reload outreach options</button>
        </>
      )}
      {kind === "withdrawals" && (
        <>
          <p className="warning-box">
            <strong>Demo withdrawals.</strong> No money moves. Mark paid only
            changes the simulated status.
          </p>
          <Field name="withdrawal-filter" label="Filter by status">
            <select
              id="withdrawal-filter"
              className="filter-select"
              value={filter}
              onChange={(e) => setFilter(e.target.value)}
            >
              <option value="">All withdrawals</option>
              {["Requested", "Approved", "Paid", "Rejected"].map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
          </Field>
        </>
      )}
      <ErrorBox error={error} />
      {error && (
        <button onClick={() => setVersion((v) => v + 1)}>Try again</button>
      )}
      <p role="status" className="success-text">
        {notice}
      </p>
      {!loading && !error && !rows.length && (
        <Card>
          <p className="empty">
            No {kind === "offers" ? "practical help offers" : kind} to show.
          </p>
        </Card>
      )}
      {rows.length > 0 && kind === "messages" && (
        <div className="card message-list">
          {rows.map((row) => (
            <article key={row.reference}>
              <div className="spread">
                <div>{person(row)}</div>
                <span className="small mono muted">
                  {row.reference} · {date(row.createdAt)}
                </span>
              </div>
              <p className="success-text">About: {row.about}</p>
              <p className="message-body">{row.message}</p>
            </article>
          ))}
        </div>
      )}
      {rows.length > 0 && kind !== "messages" && (
        <div className="card table-card">
          <div
            className="table-scroll"
            role="region"
            aria-label={`${kind} table`}
            tabIndex={0}
          >
            <table>
              <thead>
                <tr>
                  {(kind === "offers"
                    ? [
                        "Reference",
                        "Supporter",
                        "Help type",
                        "Campaign",
                        "Available · Area",
                        "Status",
                      ]
                    : kind === "volunteers"
                      ? [
                          "Reference",
                          "Volunteer",
                          "Outreach",
                          "Role / interests",
                          "Availability",
                          "Area",
                          "Registered",
                        ]
                      : [
                          "Reference",
                          "Campaign / applicant",
                          "Amount",
                          "Payout (private)",
                          "Requested",
                          "Status",
                          "Actions",
                        ]
                  ).map((label) => (
                    <th scope="col" key={label}>
                      {label}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {rows.map((row) => (
                  <tr key={row.reference}>
                    <td>
                      {ref(row)}
                      {kind === "volunteers" && (
                        <span className="small muted block">{row.kind}</span>
                      )}
                    </td>
                    {kind === "offers" ? (
                      <>
                        <td>{person(row)}</td>
                        <td>
                          {text(row.types)}
                          {row.details && (
                            <p className="small muted">“{row.details}”</p>
                          )}
                        </td>
                        <td>{row.campaignTitle}</td>
                        <td>
                          {text(row.availability)} · {row.area}
                        </td>
                        <td>
                          <select
                            aria-label={`Status for ${row.reference}`}
                            aria-invalid={
                              offerErrorRef === row.reference &&
                              !!error?.fields?.status
                            }
                            aria-describedby={
                              offerErrorRef === row.reference &&
                              error?.fields?.status
                                ? `offer-error-${row.reference}`
                                : undefined
                            }
                            value={row.status}
                            disabled={busy === row.reference}
                            onChange={(e) => updateOffer(row, e.target.value)}
                          >
                            {["New", "Contacted", "Not needed"].map((s) => (
                              <option key={s}>{s}</option>
                            ))}
                          </select>
                          {offerErrorRef === row.reference &&
                            error?.fields?.status && (
                              <p
                                id={`offer-error-${row.reference}`}
                                className="field-error"
                              >
                                ⚠ {error.fields.status}
                              </p>
                            )}
                        </td>
                      </>
                    ) : kind === "volunteers" ? (
                      <>
                        <td>{person(row)}</td>
                        <td>{row.outreachLabel}</td>
                        <td>{text(row.roleOrInterests)}</td>
                        <td>{text(row.availability)}</td>
                        <td>{row.area}</td>
                        <td>{date(row.createdAt)}</td>
                      </>
                    ) : (
                      <>
                        <td>
                          <strong>{row.campaignTitle}</strong>
                          <span className="block muted small">
                            {row.applicantName}
                          </span>
                        </td>
                        <td className="money">{money(row.amount)}</td>
                        <td>
                          {row.payout.bankName}
                          <br />
                          <span className="mono">
                            {row.payout.accountNumber}
                          </span>
                          <br />
                          {row.payout.accountName}
                        </td>
                        <td>{date(row.requestedAt)}</td>
                        <td>
                          <Pill status={row.status} />
                        </td>
                        <td>
                          <div className="actions withdrawal-actions">
                            {row.status === "Requested" && (
                              <button
                                className="primary"
                                onClick={() =>
                                  setModal({ row, action: "approve" })
                                }
                              >
                                Approve
                              </button>
                            )}
                            {["Requested", "Approved"].includes(row.status) && (
                              <button
                                className="danger-outline"
                                onClick={() =>
                                  setModal({ row, action: "reject" })
                                }
                              >
                                Reject
                              </button>
                            )}
                            {row.status === "Approved" && (
                              <button
                                className="primary"
                                onClick={() =>
                                  setModal({ row, action: "mark_paid" })
                                }
                              >
                                Mark paid
                              </button>
                            )}
                            {["Paid", "Rejected"].includes(row.status) && (
                              <span className="small muted">Complete</span>
                            )}
                          </div>
                        </td>
                      </>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="table-note">
            {kind === "offers"
              ? "Status only: New / Contacted / Not needed. CrowdAid does not match or assign offers to patients."
              : kind === "volunteers"
                ? "Read-only registrations. Select a name to see full contact details."
                : "Demo only. Payout details are private and visible to admins only."}
            {kind === "offers" &&
              " Select a supporter to see full contact details."}
          </p>
        </div>
      )}
      {selected && (
        <Modal title={selected.name} onClose={() => setSelected(null)}>
          <p className="mono muted">{selected.reference}</p>
          <Details
            title="Private contact details"
            items={[
              ["Name", selected.name],
              ["Phone", selected.phone],
              ...(kind === "offers"
                ? [
                    ["Help types", selected.types],
                    ["Campaign", selected.campaignTitle],
                    ["Availability", selected.availability],
                    ["Area", selected.area],
                    ["Details", selected.details],
                  ]
                : kind === "volunteers"
                  ? [
                      ["Registration", selected.kind],
                      ["Outreach", selected.outreachLabel],
                      ["Role / interests", selected.roleOrInterests],
                      ["Availability", selected.availability],
                      ["Area", selected.area],
                    ]
                  : [
                      ["About", selected.about],
                      ["Message", selected.message],
                    ]),
            ]}
          />
        </Modal>
      )}
      {modal && (
        <ReasonModal
          action={`withdrawal_${modal.action}`}
          reference={modal.row.reference}
          onClose={() => setModal(null)}
          onConfirm={async (values) => {
            await adminApi.withdrawalAction(modal.row.reference, {
              action: modal.action,
              ...values,
            });
            setVersion((v) => v + 1);
            setNotice("Demo withdrawal updated. No money moved.");
          }}
        >
          <p>
            <strong>{money(modal.row.amount)}</strong> ·{" "}
            {modal.row.applicantName}
          </p>
        </ReasonModal>
      )}
    </div>
  );
}
