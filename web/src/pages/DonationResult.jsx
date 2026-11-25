import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { publicApi } from "../api/public";
import { asError } from "../components/hooks";
import {
  ErrorSummary,
  Funding,
  Loading,
  ResultPanel,
  TestBadge,
  money,
} from "../components/UI";
import { clearDonationDraft } from "./Donate";
export default function DonationResult() {
  const [params] = useSearchParams();
  const reference = params.get("ref") || params.get("reference") || "";
  const [result, setResult] = useState(null);
  const [error, setError] = useState(null);
  const [busy, setBusy] = useState(false);
  const [round, setRound] = useState(0);
  useEffect(() => {
    if (!reference) return;
    const controller = new AbortController();
    let timer;
    let attempts = 0;
    setError(null);
    setBusy(true);
    setResult(null);
    async function check() {
      try {
        const response = await publicApi.donation(reference, controller.signal);
        if (controller.signal.aborted) return;
        setResult(response);
        attempts++;
        if (response.status === "success") clearDonationDraft();
        if (response.status === "pending" && attempts < 4)
          timer = setTimeout(check, 2500);
        else setBusy(false);
      } catch (e) {
        if (!controller.signal.aborted) {
          setError(asError(e));
          setBusy(false);
        }
      }
    }
    check();
    return () => {
      controller.abort();
      clearTimeout(timer);
    };
  }, [reference, round]);
  if (!reference)
    return (
      <div className="p-form-page">
        <ResultPanel tone="failed" title="Donation reference missing">
          <p>
            Open the return link from Paystack, or contact CrowdAid with your
            reference.
          </p>
          <Link className="button p-primary" to="/campaigns">
            See all campaigns
          </Link>
        </ResultPanel>
      </div>
    );
  return (
    <div className="p-form-page p-stack">
      {error && (
        <>
          <ErrorSummary error={error} />
          <button disabled={busy} onClick={() => setRound((v) => v + 1)}>
            Check status again
          </button>
        </>
      )}
      {!result && busy && (
        <Loading label="Checking your payment with the payment provider…" />
      )}
      {result && (
        <ResultPanel
          key={result.status}
          title={
            result.status === "success"
              ? "Thank you for your support"
              : result.status === "pending"
                ? "Waiting for confirmation"
                : "The payment didn't go through"
          }
          reference={result.reference}
          status={result.status}
          tone={
            result.status === "success"
              ? "success"
              : result.status === "pending"
                ? "pending"
                : "failed"
          }
        >
          <TestBadge testMode={result.testMode} />
          <p>
            {result.status === "success"
              ? `Your ${result.testMode ? "test " : ""}donation of ${money(result.amount)} to ${result.campaignTitle} was confirmed and added to the amount raised.`
              : result.status === "pending"
                ? "Your payment hasn't been confirmed yet, so the amount raised hasn't changed. Please don't pay again. Check the status in a moment."
                : "No confirmed donation was added to the campaign. You can try again with your previous details."}
          </p>
          <Funding {...result.campaign} />
          {result.status === "pending" && (
            <>
              <p className="p-hint" role="status">
                {busy
                  ? "Checking for confirmation…"
                  : "Automatic checks finished. You can check again below."}
              </p>
              <button disabled={busy} onClick={() => setRound((v) => v + 1)}>
                Check status again
              </button>
            </>
          )}
          <div className="p-actions">
            <Link
              className="button p-primary"
              to={`/campaigns/${encodeURIComponent(result.campaignId)}`}
            >
              Back to campaign
            </Link>
            {result.status === "failed" && (
              <Link
                className="button p-outline"
                to={`/donate?campaign=${encodeURIComponent(result.campaignId)}`}
              >
                Try again
              </Link>
            )}
          </div>
        </ResultPanel>
      )}
    </div>
  );
}
