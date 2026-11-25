import { useEffect, useRef, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { publicApi } from "../api/public";
import { needToCategory } from "../components/constants";
import { useResource } from "../components/hooks";
import { usePublic } from "../components/PublicLayout";
import {
  BackLink,
  CampaignTile,
  Funding,
  Icon,
  LoadError,
  Loading,
  date,
  money,
} from "../components/UI";
import NotFound from "./NotFound";
export default function CampaignDetail() {
  const { id } = useParams();
  const resource = useResource(
    (signal) => publicApi.campaign(id, signal),
    [id],
  );
  if (resource.loading)
    return (
      <div className="p-container p-page">
        <Loading label="Loading campaign…" />
      </div>
    );
  if (resource.error)
    return resource.error.status === 404 ||
      resource.error.code === "NOT_FOUND" ||
      resource.error.code === "CAMPAIGN_NOT_FOUND" ? (
      <NotFound campaign />
    ) : (
      <div className="p-container p-page">
        <LoadError resource={resource} />
      </div>
    );
  return <DetailContent key={id} campaign={resource.data} />;
}
function DetailContent({ campaign: c }) {
  const { contact } = usePublic();
  const donateRef = useRef(null);
  const [sticky, setSticky] = useState(false);
  const [share, setShare] = useState(false);
  const [copied, setCopied] = useState(false);
  const [copyFailed, setCopyFailed] = useState(false);
  const url = window.location.href;
  const donate = `/donate?campaign=${encodeURIComponent(c.id)}`;
  const help = `/help?campaign=${encodeURIComponent(c.id)}`;
  useEffect(() => {
    const observer = new IntersectionObserver(([entry]) =>
      setSticky(!entry.isIntersecting && entry.boundingClientRect.top < 0),
    );
    if (donateRef.current) observer.observe(donateRef.current);
    return () => observer.disconnect();
  }, []);
  async function copy() {
    try {
      await navigator.clipboard.writeText(url);
      setCopied(true);
      setCopyFailed(false);
    } catch {
      setCopyFailed(true);
    }
  }
  return (
    <div className={sticky ? "p-detail-with-sticky" : ""}>
      <BackLink to="/campaigns">All campaigns</BackLink>
      <div className="p-container p-page p-detail-layout">
        <div className="p-detail-head p-stack">
          <div className="p-actions">
            {c.reviewed && (
              <span className="p-reviewed static">✓ Reviewed by CrowdAid</span>
            )}
            {c.raised >= c.goal && c.goal > 0 && (
              <span className="p-pill">Goal reached</span>
            )}
          </div>
          <h1>{c.title}</h1>
          <p className="p-muted">
            {c.location} · {c.condition}
          </p>
          <CampaignTile campaign={{ ...c, reviewed: false }} />
          <div className="p-trust">
            <span>
              <strong>What “Reviewed by CrowdAid” means</strong>
              <br />
              The CrowdAid team reviewed this request and the family agreed to
              publish it. It is not a medical verification or a hospital
              endorsement.
            </span>
          </div>
        </div>
        <aside className="p-card p-detail-funding p-stack">
          <h2 className="p-eyebrow">Funding progress</h2>
          <Funding {...c} large />
          <Link ref={donateRef} className="button p-primary" to={donate}>
            Donate
          </Link>
          <div className="p-two-buttons">
            <Link className="button p-outline" to={help}>
              Offer other help
            </Link>
            <button aria-expanded={share} onClick={() => setShare((v) => !v)}>
              ↗ Share
            </button>
          </div>
          {share && (
            <div className="p-stack">
              <a
                className="button"
                target="_blank"
                rel="noreferrer"
                href={`https://wa.me/?text=${encodeURIComponent(`${c.title} ${url}`)}`}
              >
                WhatsApp ↗
              </a>
              <button onClick={copy}>
                {copied ? "✓ Link copied" : "Copy link"}
              </button>
              {copyFailed && (
                <div className="p-warning">
                  <p>⚠ Select and copy this link:</p>
                  <input aria-label="Campaign link" readOnly value={url} />
                </div>
              )}
            </div>
          )}
          <p className="p-hint">
            CrowdAid charges no platform fee. Only confirmed payments count
            towards the amount raised.
          </p>
        </aside>
        <div className="p-detail-body p-stack">
          <section className="p-stack">
            <h2>About this campaign</h2>
            {c.story.map((paragraph, i) => (
              <p key={i}>{paragraph}</p>
            ))}
          </section>
          {c.needs.length > 0 && (
            <section className="p-stack">
              <h2>Practical help needed</h2>
              <p className="p-muted">
                Choose one to offer it. CrowdAid records your offer and contacts
                you if it's needed.
              </p>
              <ul className="p-needs-list">
                {c.needs.map((need) => (
                  <li key={need}>
                    <Link
                      to={`${help}&type=${encodeURIComponent(needToCategory(need))}`}
                    >
                      <Icon
                        name={
                          /transport/i.test(need)
                            ? "transport"
                            : /blood/i.test(need)
                              ? "blood"
                              : /meal/i.test(need)
                                ? "food"
                                : /equipment/i.test(need)
                                  ? "equipment"
                                  : "professional"
                        }
                      />
                      {need}
                      <span aria-hidden="true">›</span>
                    </Link>
                  </li>
                ))}
              </ul>
            </section>
          )}
          <section className="p-stack">
            <h2>How the funds will be used</h2>
            <table className="p-funds-table">
              <caption className="sr-only">Planned use of funds</caption>
              <tbody>
                {c.funds.map((item, i) => (
                  <tr key={i}>
                    <th scope="row">{item.label}</th>
                    <td>{money(item.amount)}</td>
                  </tr>
                ))}
                <tr>
                  <th scope="row">Funding goal</th>
                  <td>
                    <strong>{money(c.goal)}</strong>
                  </td>
                </tr>
              </tbody>
            </table>
            <p className="p-hint">
              Amounts are based on the estimate the family shared with CrowdAid.
              CrowdAid charges no platform fee.
            </p>
          </section>
          {c.updates.length > 0 && (
            <section className="p-stack">
              <h2>Campaign updates</h2>
              <ol className="p-updates">
                {c.updates.slice(0, 2).map((u, i) => (
                  <li key={i}>
                    <p className="p-hint">{date(u.date)} · from CrowdAid</p>
                    <h3>{u.title}</h3>
                    <p>{u.text}</p>
                  </li>
                ))}
              </ol>
            </section>
          )}
          <section className="p-stack">
            <h2>People behind this campaign</h2>
            <div className="p-organizer">
              <span aria-hidden="true">
                {c.organizer.name
                  .split(" ")
                  .map((x) => x[0])
                  .join("")
                  .slice(0, 2)}
              </span>
              <div>
                <strong>{c.organizer.name}</strong>
                <p className="p-hint">{c.organizer.role}</p>
              </div>
            </div>
            <p className="p-hint">
              To protect the family's privacy, their phone numbers and addresses
              are never shown. Questions about this campaign go to the CrowdAid
              team.
            </p>
            <button className="button p-outline" onClick={() => contact(c)}>
              <Icon name="chat" />
              Contact CrowdAid about this campaign
            </button>
          </section>
        </div>
      </div>
      {sticky && (
        <div className="p-sticky-donate">
          <div>
            <strong>{money(c.raised)}</strong>
            <small>of {money(c.goal)}</small>
          </div>
          <Link className="button p-outline" to={help}>
            Other help
          </Link>
          <Link className="button p-primary" to={donate}>
            Donate
          </Link>
        </div>
      )}
    </div>
  );
}
