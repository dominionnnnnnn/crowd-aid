import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { publicApi } from "../api/public";
import { needs } from "../components/constants";
import { useResource } from "../components/hooks";
import {
  CampaignCard,
  Empty,
  Field,
  HelpCategories,
  LoadError,
  Loading,
} from "../components/UI";
export default function Campaigns() {
  const [params, setParams] = useSearchParams();
  const [query, setQuery] = useState(params.get("q") || "");
  const q = params.get("q") || "";
  const location = params.get("location") || "";
  const need = params.get("need") || "";
  const resource = useResource(
    (signal) =>
      publicApi.campaigns(
        {
          q: q || undefined,
          location: location || undefined,
          need: need || undefined,
        },
        signal,
      ),
    [q, location, need],
  );
  const [knownLocations, setKnownLocations] = useState([]);
  const locations = resource.data?.locations?.length
    ? resource.data.locations
    : knownLocations;
  function update(key, value) {
    if (resource.data?.locations?.length)
      setKnownLocations(resource.data.locations);
    setParams((current) => {
      const next = new URLSearchParams(current);
      value ? next.set(key, value) : next.delete(key);
      return next;
    });
  }
  function clear() {
    setQuery("");
    setParams({});
  }
  return (
    <div className="p-container p-page p-stack">
      <div className="p-page-heading">
        <h1>Find a healthcare campaign to support</h1>
        <p>
          Every campaign here was reviewed by the CrowdAid team and published
          with the family's consent.
        </p>
      </div>
      <form
        className="p-search-bar"
        onSubmit={(e) => {
          e.preventDefault();
          update("q", query.trim());
        }}
      >
        <Field
          label="Search campaigns"
          name="q"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Name, condition or town"
          type="search"
        />
        <button className="button p-primary">Search</button>
        <Field
          label="Location"
          name="location"
          value={location}
          onChange={(e) => update("location", e.target.value)}
          options={[{ value: "", label: "All locations" }, ...locations]}
        />
        <Field
          label="Practical help needed"
          name="need"
          value={need}
          onChange={(e) => update("need", e.target.value)}
          options={[{ value: "", label: "All practical needs" }, ...needs]}
        />
      </form>
      <div className="p-spread">
        <p role="status" className="p-hint">
          {resource.loading
            ? "Loading campaigns…"
            : `${resource.data?.data?.length || 0} campaign${resource.data?.data?.length === 1 ? "" : "s"}`}
        </p>
        {(q || location || need) && (
          <button className="p-text-button" onClick={clear}>
            Clear filters
          </button>
        )}
      </div>
      {resource.loading ? (
        <Loading label="Loading campaigns…" cards />
      ) : resource.error ? (
        <LoadError resource={resource} />
      ) : resource.data.data.length ? (
        <div className="p-campaign-grid">
          {resource.data.data.map((c) => (
            <CampaignCard key={c.id} campaign={c} />
          ))}
        </div>
      ) : (
        <Empty
          title={
            q || location || need
              ? `No campaigns match${q ? ` “${q}”` : " these filters"}`
              : "No published campaigns yet"
          }
        >
          <p>
            Try a different name, condition or town, or clear your search and
            filters.
          </p>
          <div className="p-actions">
            <button onClick={clear} className="button p-outline">
              Clear search and filters
            </button>
            <Link to="/apply">Need help yourself? Request support</Link>
          </div>
        </Empty>
      )}
      <HelpCategories />
    </div>
  );
}
