// Isolated fictional campaigner demo. Public pages never import this module.
const user = {
  id: "00000000-0000-4000-8000-000000000022",
  name: "Aminat Yusuf",
  email: "aminat@crowdaid.demo",
  role: "campaigner",
};
const campaign = {
  id: "demo-usman",
  title: "Demo: Help Usman get kidney surgery",
  published: true,
  raised: 25000,
  goal: 5000000,
  supporters: 2,
  withdrawn: 5000,
  pendingWithdrawals: 3000,
  available: 17000,
  needs: ["Transport to hospital", "Meals for family"],
  updates: [
    {
      date: "2026-11-25",
      title: "Demo community support",
      text: "Fictional donations and offers illustrate the dashboard. No real money moves.",
    },
  ],
};
const donations = [
  {
    reference: "CA-DON-0001",
    displayName: "Anonymous supporter",
    amount: 15000,
    createdAt: "2026-11-25T09:10:00Z",
  },
  {
    reference: "CA-DON-0002",
    displayName: "Tunde B.",
    amount: 10000,
    createdAt: "2026-11-25T10:10:00Z",
  },
];
const offers = [
  {
    reference: "CA-OFF-0001",
    types: ["Transportation"],
    details: "Demo: available for morning appointments.",
    availability: ["Weekdays"],
    area: "Kuto",
    status: "New",
    createdAt: "2026-11-25T09:00:00Z",
  },
];
let payout = {
  bankName: "Demo Bank",
  accountNumber: "0001234567",
  accountName: "Aminat Yusuf",
};
const withdrawals = [
  {
    reference: "CA-WDR-0001",
    amount: 5000,
    status: "Paid",
    requestedAt: "2026-11-24T10:00:00Z",
    decidedAt: "2026-11-24T12:00:00Z",
    note: null,
  },
  {
    reference: "CA-WDR-0002",
    amount: 3000,
    status: "Requested",
    requestedAt: "2026-11-25T10:00:00Z",
    decidedAt: null,
    note: null,
  },
];
const usedTokens = new Set();
let unpublished = false;
const clone = (value) => structuredClone(value);
const fail = (status, code, message, fields = {}) => {
  throw { response: { status, data: { error: { code, message, fields } } } };
};
function activationCheck(token) {
  if (
    !token ||
    (!token.startsWith("demo-") &&
      token !== "TEST-ONLY" &&
      !["expired", "used"].includes(token))
  )
    fail(
      404,
      "NOT_FOUND",
      "This link is not valid. Ask CrowdAid for a new link.",
    );
  if (token === "expired")
    fail(
      410,
      "TOKEN_EXPIRED",
      "This link has expired. Ask CrowdAid to send a new one.",
    );
  if (token === "used" || usedTokens.has(token))
    fail(410, "TOKEN_USED", "This link was already used. Sign in instead.");
  return {
    valid: true,
    name: user.name,
    emailMasked: "a•••@crowdaid.demo",
    applicationRef: "CA-REQ-0007",
    patientName: "Usman",
  };
}
function login(email) {
  user.email = email;
  return {
    token: `demo-campaigner.${Date.now() + 28800000}`,
    expiresIn: 28800,
    user: clone(user),
  };
}
function balance() {
  return (
    campaign.raised -
    withdrawals
      .filter((w) => ["Requested", "Approved", "Paid"].includes(w.status))
      .reduce((sum, w) => sum + w.amount, 0)
  );
}
export async function campaignerMock(
  method,
  path,
  data = {},
  params = {},
  token,
) {
  await new Promise((resolve) => setTimeout(resolve, 150));
  if (path === "/auth/activation/check") return activationCheck(data.token);
  if (path === "/auth/activate") {
    activationCheck(data.token);
    if (!data.password || data.password.length < 8)
      fail(400, "VALIDATION_ERROR", "Please fix the highlighted fields.", {
        password: "Use at least 8 characters.",
      });
    usedTokens.add(data.token);
    unpublished = true;
    return login(user.email);
  }
  if (path === "/auth/login") {
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(data.email || "") || !data.password)
      fail(401, "INVALID_CREDENTIALS", "Email or password is incorrect");
    return login(data.email.trim().toLowerCase());
  }
  if (
    !token?.startsWith("demo-campaigner.") ||
    !Number.isFinite(Number(token.split(".")[1])) ||
    Number(token.split(".")[1]) <= Date.now()
  )
    fail(401, "TOKEN_EXPIRED", "Your session expired. Sign in again.");
  if (path === "/auth/me") return clone(user);
  if (path === "/me/campaign")
    return {
      application: {
        reference: "CA-REQ-0007",
        status: unpublished ? "Approved" : "Published",
        patientName: "Usman",
      },
      campaign: unpublished
        ? null
        : {
            ...clone(campaign),
            available: balance(),
            pendingWithdrawals: withdrawals
              .filter((w) => ["Requested", "Approved"].includes(w.status))
              .reduce((sum, w) => sum + w.amount, 0),
          },
      payoutSet: !!payout,
    };
  if (path === "/me/campaign/donations")
    return {
      data: unpublished
        ? []
        : clone(
            donations.slice(
              ((params.page || 1) - 1) * (params.pageSize || 20),
              (params.page || 1) * (params.pageSize || 20),
            ),
          ),
      total: unpublished ? 0 : donations.length,
    };
  if (path === "/me/campaign/offers")
    return {
      data: unpublished
        ? []
        : clone(
            offers.filter((o) => !params.status || o.status === params.status),
          ),
    };
  if (path === "/me/payout") {
    if (method === "put") {
      const fields = {};
      for (const key of ["bankName", "accountName"])
        if (!data[key]?.trim()) fields[key] = "This field is required.";
      if (!/^\d{10}$/.test(data.accountNumber || ""))
        fields.accountNumber = "Enter exactly 10 digits.";
      if (Object.keys(fields).length)
        fail(
          400,
          "VALIDATION_ERROR",
          "Please fix the highlighted fields.",
          fields,
        );
      payout = clone(data);
    }
    return payout
      ? { ...payout, accountNumber: `••••••${payout.accountNumber.slice(-4)}` }
      : null;
  }
  if (path === "/me/withdrawals") {
    if (method === "get")
      return { data: clone(withdrawals), available: balance() };
    if (unpublished)
      fail(
        409,
        "CAMPAIGN_NOT_PUBLISHED",
        "Your campaign must be published before requesting a withdrawal.",
      );
    if (!payout)
      fail(409, "PAYOUT_DETAILS_REQUIRED", "Add payout details first.");
    if (!Number.isSafeInteger(data.amount) || data.amount < 1000)
      fail(400, "VALIDATION_ERROR", "Please fix the highlighted fields.", {
        amount: "Enter a whole naira amount of at least ₦1,000.",
      });
    if (data.amount > balance())
      fail(
        409,
        "INSUFFICIENT_FUNDS",
        "This amount is more than your available balance.",
        { amount: "Enter no more than the available balance." },
      );
    const row = {
      reference: `CA-WDR-${String(withdrawals.length + 1).padStart(4, "0")}`,
      amount: data.amount,
      status: "Requested",
      requestedAt: new Date().toISOString(),
      decidedAt: null,
      note: null,
    };
    withdrawals.unshift(row);
    return {
      reference: row.reference,
      amount: row.amount,
      status: row.status,
      available: balance(),
      demo: true,
    };
  }
  fail(404, "NOT_FOUND", "Demo endpoint not found.");
}
