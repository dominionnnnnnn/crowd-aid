import { publicApi } from "../api/public";
import { requiredFields, validEmail } from "../components/constants";
import { useForm } from "../components/hooks";
import {
  BackLink,
  ErrorSummary,
  Field,
  ResultPanel,
  Submit,
} from "../components/UI";
export default function ApplicationStatus() {
  const f = useForm({ reference: "", email: "" }, "status-");
  return (
    <>
      <BackLink />
      <div className="p-form-page p-stack">
        <h1>Check application status</h1>
        <p className="p-muted">
          Enter your reference and the email you used to apply. Your private
          application details stay private.
        </p>
        <form
          noValidate
          onSubmit={f.submit(
            (v) =>
              publicApi.applicationStatus({
                reference: v.reference.trim(),
                email: v.email.trim(),
              }),
            (v) => ({
              ...requiredFields(v, ["reference", "email"]),
              ...(!validEmail(v.email)
                ? { email: "Enter a valid email address." }
                : {}),
            }),
          )}
        >
          <ErrorSummary error={f.error} prefix={f.prefix} />
          <Field
            label="Application reference *"
            {...f.bind("reference")}
            placeholder="CA-REQ-0001"
          />
          <Field
            label="Email used to apply *"
            {...f.bind("email")}
            type="email"
            autoComplete="email"
          />
          <Submit busy={f.busy} busyLabel="Checking…">
            Check status
          </Submit>
        </form>
        {f.result && (
          <ResultPanel
            key={f.result.reference + f.result.status}
            title="Your application"
            reference={f.result.reference}
            status={f.result.status}
          >
            <p>{f.result.message}</p>
          </ResultPanel>
        )}
      </div>
    </>
  );
}
