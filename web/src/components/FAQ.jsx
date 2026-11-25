import { useId, useRef, useState } from "react";
import { Link } from "react-router-dom";
const items = [
  [
    "What is CrowdAid?",
    "CrowdAid is a community-powered healthcare assistance platform that connects people facing healthcare challenges with individuals willing to support them through donations, practical assistance and community outreach.",
  ],
  [
    "Who can request help on CrowdAid?",
    "Anyone seeking support for a healthcare need can submit a request for themselves or someone else, provided they have the appropriate consent and information.",
  ],
  [
    "How do I request assistance?",
    "Click “Get Help” and complete the request form with the necessary details about the person and their healthcare needs. The CrowdAid team will review your submission and contact you about the next steps.",
  ],
  [
    "Does submitting a request mean my campaign will be published?",
    "No. Every request goes through a review process. The CrowdAid team may contact you for additional information before deciding whether it can be approved and published.",
  ],
  [
    "What kind of support can I receive?",
    "Support may include financial donations, transportation, medical equipment, blood donor coordination, food and meals, professional assistance and other relevant forms of help.",
  ],
  [
    "Can I support someone without donating money?",
    "Absolutely! You can offer practical assistance, volunteer at a community outreach or help spread the word about a campaign.",
  ],
  [
    "How can I donate to a campaign?",
    "Browse published campaigns, select the person you would like to support, choose your donation amount and follow the payment instructions.",
  ],
  [
    "Can I donate anonymously?",
    "Yes, where the donation experience provides an anonymous option, you can choose not to display your name publicly.",
  ],
  [
    "How does CrowdAid review requests?",
    "Our team reviews submitted information, follows up where necessary and assesses requests before approving campaigns for publication. This process helps us manage requests responsibly. Review does not automatically mean independent medical verification or guarantee of treatment outcomes.",
  ],
  [
    "Is it free to request assistance?",
    "Submitting a request to CrowdAid is free. Any applicable charges associated with a donation or payment will be displayed separately before payment.",
  ],
  [
    "How can I volunteer with CrowdAid?",
    "You can join the CrowdAid volunteer community or register for a specific fundraising or community outreach activity through our Volunteer section.",
  ],
  [
    "How can I contact CrowdAid?",
    "You can reach the CrowdAid team through our Contact Us section for questions, assistance or further information.",
  ],
];
const links = {
  "Get Help": "/apply",
  "Volunteer section": "/volunteer",
  "Contact Us section": "/contact",
};
function Answer({ text }) {
  return text
    .split(/(Get Help|Volunteer section|Contact Us section)/g)
    .map((part, index) =>
      links[part] ? (
        <Link key={index} to={links[part]}>
          {part}
        </Link>
      ) : (
        part
      ),
    );
}
export default function FAQ() {
  const [open, setOpen] = useState(null);
  const [showAll, setShowAll] = useState(false);
  const headingRef = useRef(null);
  const id = useId();
  function toggleAll() {
    if (showAll) {
      if (open !== null && open >= 3) setOpen(null);
      setShowAll(false);
      requestAnimationFrame(() =>
        headingRef.current?.scrollIntoView({ block: "start" }),
      );
    } else {
      setShowAll(true);
    }
  }
  return (
    <section
      className="p-container p-section p-faq"
      aria-labelledby={`${id}-heading`}
    >
      <div className="p-faq-heading">
        <p className="p-eyebrow">A little clarity</p>
        <h2 ref={headingRef} id={`${id}-heading`}>
          Frequently asked questions
        </h2>
      </div>
      <div id={`${id}-list`} className="p-faq-list">
        {items
          .slice(0, showAll ? items.length : 3)
          .map(([question, answer], index) => {
            const expanded = open === index;
            return (
              <div
                className={`p-faq-item ${expanded ? "is-open" : ""}`}
                key={question}
              >
                <h3>
                  <button
                    id={`${id}-question-${index}`}
                    aria-expanded={expanded}
                    aria-controls={`${id}-answer-${index}`}
                    onClick={() => setOpen(expanded ? null : index)}
                  >
                    <span>{question}</span>
                    <span className="p-faq-toggle" aria-hidden="true">
                      {expanded ? "−" : "+"}
                    </span>
                  </button>
                </h3>
                <div
                  id={`${id}-answer-${index}`}
                  role="region"
                  aria-labelledby={`${id}-question-${index}`}
                  hidden={!expanded}
                >
                  <p>
                    <Answer text={answer} />
                  </p>
                </div>
              </div>
            );
          })}
      </div>
      <button
        type="button"
        className="button p-outline"
        aria-expanded={showAll}
        aria-controls={`${id}-list`}
        onClick={toggleAll}
      >
        {showAll ? "Show less" : "Read more"}
      </button>
    </section>
  );
}
