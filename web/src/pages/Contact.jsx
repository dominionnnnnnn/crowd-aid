import { Link, useSearchParams } from "react-router-dom";
import { publicApi } from "../api/public";
import ContactForm from "../components/ContactForm";
import { useResource } from "../components/hooks";
import { BackLink, LoadError, Loading } from "../components/UI";
export default function Contact() {
  const [params] = useSearchParams();
  const id = params.get("campaign");
  const resource = useResource(
    (signal) => (id ? publicApi.campaign(id, signal) : Promise.resolve(null)),
    [id],
  );
  return (
    <>
      <BackLink />
      <div className="p-form-page p-stack">
        <div className="p-page-heading">
          <p className="p-eyebrow">We're here to help</p>
          <h1>Contact CrowdAid</h1>
          <p>
            Questions about a campaign or your request? Send us a message and
            we'll reply by phone or WhatsApp.
          </p>
        </div>
        {resource.loading ? (
          <Loading label="Loading contact form…" />
        ) : resource.error ? (
          <>
            <LoadError resource={resource} />
            <Link to="/contact">Send a general enquiry instead</Link>
          </>
        ) : (
          <ContactForm key={id || "general"} campaign={resource.data} />
        )}
      </div>
    </>
  );
}
