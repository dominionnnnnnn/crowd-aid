import { Link } from "react-router-dom";
export default function NotFound({ campaign = false }) {
  return (
    <div className="p-container p-page p-not-found p-stack">
      <span className="p-icon-tile">
        <img src="/assets/logo/crowdaid-symbol.svg" width="36" alt="" />
      </span>
      <p className="p-eyebrow">
        {campaign ? "Campaign unavailable" : "404 · Page not found"}
      </p>
      <h1>
        {campaign
          ? "This campaign isn't available"
          : "We couldn't find this page"}
      </h1>
      <p className="p-muted">
        {campaign
          ? "It may not be published yet, or the link may be wrong."
          : "The link may have changed. You can still find a way to help."}
      </p>
      <div className="p-actions">
        <Link className="button p-primary" to="/campaigns">
          See all campaigns
        </Link>
        <Link className="button p-outline" to="/">
          Back to home
        </Link>
      </div>
    </div>
  );
}
