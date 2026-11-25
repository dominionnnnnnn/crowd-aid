import { useEffect } from "react";
import { Link, useLocation } from "react-router-dom";
import { publicApi } from "../api/public";
import { useResource } from "../components/hooks";
import { usePublic } from "../components/PublicLayout";
import HeroCommunity from "../components/HeroCommunity";
import FAQ from "../components/FAQ";
import {
  CampaignCard,
  Empty,
  Icon,
  LoadError,
  Loading,
} from "../components/UI";
export default function Home() {
  const { giveOpen, setGiveOpen } = usePublic();
  const location = useLocation();
  const resource = useResource(
    (signal) => publicApi.campaigns({ featured: true }, signal),
    [],
  );
  useEffect(() => {
    if (location.pathname === "/give") setGiveOpen(true);
  }, [location.pathname, setGiveOpen]);
  return (
    <>
      <section className="p-hero">
        <div className="p-container p-hero-layout">
          <div className="p-hero-text p-stack">
            <p className="p-eyebrow">Community healthcare support · Abeokuta</p>
            <h1>Someone needs help. Someone else can help.</h1>
            <p className="p-hero-copy">
              CrowdAid helps people facing healthcare costs reach beyond their
              own circle, to neighbours across Abeokuta who can give money,
              practical help or their time.
            </p>
            <div className="p-hero-actions">
              <Link className="button p-primary" to="/apply">
                Get help
              </Link>
              <button
                className="button p-primary"
                aria-expanded={giveOpen}
                aria-controls="give-help-panel"
                onClick={() => setGiveOpen((v) => !v)}
              >
                Give help {giveOpen ? "−" : "+"}
              </button>
              <span>For yourself or someone you care for</span>
              <span>Donate, offer practical help or volunteer</span>
            </div>
          </div>
          <HeroCommunity />
          {giveOpen && (
            <div
              id="give-help-panel"
              role="region"
              aria-label="Ways to give help"
              className="p-give-grid"
            >
              {[
                [
                  "/campaigns",
                  "donate",
                  "Donate to a campaign",
                  "Give money towards someone's treatment",
                ],
                [
                  "/help",
                  "transport",
                  "Offer practical assistance",
                  "Transport, meals, blood donation, equipment or skills",
                ],
                [
                  "/volunteer",
                  "volunteer",
                  "Volunteer with CrowdAid",
                  "Join outreaches and community fundraising",
                ],
              ].map(([to, icon, title, copy]) => (
                <Link to={to} className="p-help-link" key={to}>
                  <span className="p-icon-tile">
                    <Icon name={icon} />
                  </span>
                  <span>
                    <strong>{title}</strong>
                    <small>{copy}</small>
                  </span>
                  <span aria-hidden="true">›</span>
                </Link>
              ))}
            </div>
          )}
        </div>
      </section>
      <section className="p-container p-section p-stack">
        <h2>How CrowdAid works</h2>
        <ol className="p-how-grid">
          {[
            [
              "request",
              "Tell us what you need",
              "Request support for yourself or someone you care for. It's free and takes a few minutes. No documents are needed to start.",
            ],
            [
              "reach",
              "Reach beyond your circle",
              "With your consent, CrowdAid shares your story with people across Abeokuta, not only the friends and family you already know.",
            ],
            [
              "community",
              "Get community support",
              "People donate online, offer practical help like transport or meals, and raise funds in person at CrowdAid outreaches.",
            ],
          ].map(([icon, title, copy], i) => (
            <li className="p-card p-stack" key={title}>
              <div className="p-step-label">
                <span className="p-icon-tile">
                  <Icon name={icon} />
                </span>
                Step {i + 1}
              </div>
              <h3>{title}</h3>
              <p className="p-muted">{copy}</p>
            </li>
          ))}
        </ol>
        <p className="p-trust">
          ✓{" "}
          <span>
            <strong>Every request is reviewed by the CrowdAid team</strong>{" "}
            before anything is published. Support comes from the community, so
            we can't promise how much any campaign will raise.
          </span>
        </p>
      </section>
      <section className="p-container p-section p-stack">
        <div className="p-spread">
          <h2>Featured campaign</h2>
          <Link to="/campaigns">See all campaigns</Link>
        </div>
        {resource.loading ? (
          <Loading label="Loading featured campaign…" cards />
        ) : resource.error ? (
          <LoadError resource={resource} />
        ) : resource.data.data.length ? (
          <CampaignCard campaign={resource.data.data[0]} featured />
        ) : (
          <Empty title="No published featured campaign yet">
            <p>Campaigns will appear here after review and publication.</p>
            <Link className="button p-outline" to="/campaigns">
              See all campaigns
            </Link>
          </Empty>
        )}
      </section>
      <FAQ />
    </>
  );
}
