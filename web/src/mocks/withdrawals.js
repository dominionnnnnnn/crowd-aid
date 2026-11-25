const payout = {
  bankName: "Demo Bank",
  accountNumber: "0001234567",
  accountName: "Aminat Yusuf",
};
export const withdrawals = [
  {
    reference: "CA-WDR-0001",
    campaignTitle: "Help Usman get kidney surgery",
    applicantName: "Aminat Yusuf",
    amount: 500000,
    status: "Paid",
    requestedAt: "2026-11-20T09:00:00Z",
    isNew: false,
    payout: { ...payout },
  },
  {
    reference: "CA-WDR-0002",
    campaignTitle: "Help Usman get kidney surgery",
    applicantName: "Aminat Yusuf",
    amount: 300000,
    status: "Requested",
    requestedAt: "2026-11-25T09:00:00Z",
    isNew: true,
    payout: { ...payout },
  },
];
