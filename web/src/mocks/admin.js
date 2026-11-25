import { applications, summary } from "./applications";
import { offers } from "./offers";
import { volunteers } from "./volunteers";
import { messages } from "./messages";
import { withdrawals } from "./withdrawals";
const clone = (value) => structuredClone(value);
const now = () => new Date().toISOString();
const withdrawalLog = [];
const fail = (status, code, message, fields = {}) => {
  throw { response: { status, data: { error: { code, message, fields } } } };
};
const conflict = () =>
  fail(
    409,
    "INVALID_STATUS",
    "This action is not allowed in the current status. Refresh and try again.",
  );
const required = (data, keys) => {
  const fields = Object.fromEntries(
    keys
      .filter((k) => !String(data?.[k] ?? "").trim())
      .map((k) => [k, "This field is required."]),
  );
  if (Object.keys(fields).length)
    fail(400, "VALIDATION_ERROR", "Please fix the highlighted fields.", fields);
};
const log = (app, action, note = null) =>
  app.log.push({ action, by: "CrowdAid Admin", at: now(), note });
function activation(app) {
  const expiresAt = new Date(Date.now() + 72 * 3600000).toISOString();
  // Return the link once, never put the raw token in the mock application store.
  const activationLink = `${window.location.origin}/activate?token=demo-${crypto.randomUUID()}`;
  app.account = {
    state: "Link sent",
    activationExpiresAt: expiresAt,
    activatedAt: app.account.activatedAt,
  };
  return {
    state: "Link sent",
    email: app.email,
    phone: app.phone,
    activationLink,
    expiresAt,
    emailSent: false,
  };
}
export async function mockRequest(method, path, data = {}, params = {}, token) {
  await new Promise((resolve) => setTimeout(resolve, 180));
  if (path === "/auth/login") {
    required(data, ["email", "password"]);
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email))
      fail(400, "VALIDATION_ERROR", "Please fix the highlighted fields.", {
        email: "Enter a valid email address.",
      });
    return {
      token: `demo-admin.${Date.now() + 28800000}`,
      expiresIn: 28800,
      admin: {
        id: "00000000-0000-4000-8000-000000000001",
        name: "CrowdAid Admin",
        role: "admin",
      },
    };
  }
  if (
    !token?.startsWith("demo-admin.") ||
    !Number.isFinite(Number(token.split(".")[1])) ||
    Number(token.split(".")[1]) <= Date.now()
  )
    fail(401, "TOKEN_EXPIRED", "Your session expired. Sign in again.");
  if (path === "/applications")
    return {
      data: applications
        .filter((a) => !params.status || a.detail.status === params.status)
        .map(summary),
    };
  if (path.startsWith("/applications/")) {
    const [, , ref, operation] = path.split("/");
    const record = applications.find((a) => a.reference === ref);
    if (!record) fail(404, "NOT_FOUND", "Application not found.");
    const app = record.detail;
    if (!operation) {
      if (app.status === "Received") {
        app.status = "In review";
        log(app, "Opened");
      }
      return clone(app);
    }
    if (operation === "verification" && method === "patch") {
      if (["Approved", "Published", "Rejected"].includes(app.status))
        conflict();
      if (
        ![
          "Not started",
          "In progress",
          "Verified",
          "Could not verify",
        ].includes(data.status)
      )
        fail(400, "VALIDATION_ERROR", "Choose a valid verification status.");
      app.verification = clone(data);
      if (data.status === "Verified") app.status = "Verified";
      else if (app.status === "Verified") app.status = "In review";
      log(app, "Verification updated", data.method || null);
      return clone(app);
    }
    if (operation === "actions") {
      const allowed = {
        request_info: ["In review", "Verified"],
        info_received: ["More info requested"],
        approve: ["Verified"],
        reject: ["In review", "Verified", "More info requested"],
      };
      if (!allowed[data.action]?.includes(app.status)) conflict();
      if (data.action === "approve" && app.verification.status !== "Verified")
        conflict();
      if (data.action === "request_info") required(data, ["needed", "message"]);
      if (data.action === "reject") required(data, ["reason", "message"]);
      app.status = {
        request_info: "More info requested",
        info_received: "In review",
        approve: "Approved",
        reject: "Rejected",
      }[data.action];
      log(
        app,
        app.status,
        [data.note, data.needed, data.reason, data.message]
          .filter(Boolean)
          .join("\n") || null,
      );
      if (data.action === "approve")
        return { reference: ref, status: app.status, account: activation(app) };
      return { reference: ref, status: app.status };
    }
    if (operation === "activation") {
      if (!["Approved", "Published"].includes(app.status)) conflict();
      log(app, "Activation link re-sent");
      return activation(app);
    }
    if (operation === "publish") {
      if (app.status !== "Approved") conflict();
      required(data, ["title", "location", "goal", "story", "consentRecorded"]);
      if (
        !Number.isInteger(data.goal) ||
        data.goal <= 0 ||
        !data.publicationConsent ||
        !data.contentReviewed
      )
        fail(
          400,
          "VALIDATION_ERROR",
          "Enter a positive whole naira goal and confirm both consent boxes.",
        );
      app.publication = clone(data);
      app.status = "Published";
      app.campaignId = `demo-${ref.toLowerCase()}`;
      log(app, "Published");
      return { campaignId: app.campaignId, status: app.status };
    }
  }
  if (path === "/offers")
    return {
      data: clone(
        offers.filter((o) => !params.status || o.status === params.status),
      ),
    };
  if (path.startsWith("/offers/") && method === "patch") {
    const offer = offers.find((o) => o.reference === path.split("/")[2]);
    if (!offer) fail(404, "NOT_FOUND", "Offer not found.");
    if (!["New", "Contacted", "Not needed"].includes(data.status))
      fail(400, "VALIDATION_ERROR", "Choose a valid offer status.");
    offer.status = data.status;
    return clone(offer);
  }
  if (path === "/volunteers")
    return {
      data: clone(
        volunteers.filter((v) =>
          params.kind
            ? v.kind === params.kind
            : params.outreachId
              ? v.kind === "Outreach" && params.outreachId === "o1"
              : true,
        ),
      ),
    };
  if (path === "/messages") return { data: clone(messages) };
  if (path === "/withdrawals")
    return {
      data: clone(
        withdrawals.filter((w) => !params.status || w.status === params.status),
      ),
    };
  if (path.startsWith("/withdrawals/")) {
    const row = withdrawals.find((w) => w.reference === path.split("/")[2]);
    if (!row) fail(404, "NOT_FOUND", "Withdrawal not found.");
    if (
      !{
        approve: ["Requested"],
        reject: ["Requested", "Approved"],
        mark_paid: ["Approved"],
      }[data.action]?.includes(row.status)
    )
      conflict();
    if (data.action === "reject") required(data, ["reason", "message"]);
    row.status = { approve: "Approved", reject: "Rejected", mark_paid: "Paid" }[
      data.action
    ];
    withdrawalLog.push({
      withdrawalRef: row.reference,
      action: data.action,
      by: "CrowdAid Admin",
      at: now(),
      note: data.note || data.transferReference || null,
      reason: data.reason || null,
      message: data.message || null,
    });
    return { reference: row.reference, status: row.status };
  }
  fail(404, "NOT_FOUND", "Demo endpoint not found.");
}
