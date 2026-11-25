# CrowdAid prototype v2 — Figma handoff & audit

Source: `CrowdAid Prototype v2.dc.html` · Design System v1.1 · Wireframes v0.4.1
Assets: `assets/logo/*.svg`, `assets/icons/*.svg` (24px, 1.8 stroke, #0B6B3A)
PRD/master handbook: **not in project — not audited against.**

## 1. Screen inventory (routes)
| Priority | Screen | Route |
|---|---|---|
| P0 | Homepage (+ Give help panel) | `#/` · `#/give` |
| P0 | Campaign listing (search, filters, help categories) | `#/campaigns` |
| P0 | Campaign details | `#/campaigns/{id}` |
| P0 | Donation (form → processing → success / pending / failed) | `#/donate?campaign={id}` |
| P0 | Get help application (3 sections → received) | `#/apply` |
| P0 | Practical help offer | `#/help?campaign={id}&type={category}` |
| P1 | Volunteering (outreach / community) | `#/volunteer?path=outreach|community` |
| P1 | Admin (sign-in, applications, offers, volunteers, messages) | `#/admin` |
| P1 | Contact CrowdAid dialog (global, campaign-aware) | modal |
| P2 | Campaign not found, empty outreach | states |

Rebuild in Figma as native frames at 360 / 768 / 1280. HTML does not import as editable layers.

## 2. Tokens (unchanged from v1.1)
Primary #0B6B3A · hover #095A31 · pressed #074A28 · text #14271E · secondary text #4A5C54 · border #D5DFDA · input border #7A8A83 · surface tint #EAF6EE · error #B42318 / #FDECEA · warning #8A5300 / #FFF5E0 / #E0A43A · info #1D5FA8 / #EEF4FB.
Type: Plus Jakarta Sans 400–800. Radius 10 (controls) / 12–16 (cards) / 24 (dialogs). Targets ≥44px, primary buttons 48–56px. Focus: 3px ring.
Money: `₦` + en-NG grouping, whole naira, tabular numerals.

## 3. Components & states
- **Funding progress** — Raised | Goal (equal weight) → bar (role=progressbar, valuetext) → % funded · ₦ remaining / Goal reached → supporters.
- **Campaign card** — tint tile + Reviewed badge, title link, location · condition, 3-line preview, funding, View details / Donate.
- **Give help panel** — collapsed / expanded (aria-expanded); 3 rows: Donate, Practical assistance, Volunteer.
- **Search & filters** — query, Location, Practical help needed; result count (role=status); Clear filters; no-results empty state.
- **Help category card** — 6 categories; selected (✓), “Needed for this campaign” tag; needed categories sort first.
- **Amount picker** — presets (radio), custom (min ₦500), error.
- **Payment method** — Card / Bank transfer / USSD radio cards; required.
- **Demo outcome selector** — Success / Pending / Failed (demo only; hide in live).
- **Form field** — default, focus, error (3px #B42318 + ⚠ message), error summary with jump links.
- **Submit button** — idle, busy (spinner, disabled), failure banner (answers kept).
- **Confirmation panel** — success (green), pending (amber), failed (red, 3px).
- **Dialog** — Contact (form / sending / failed / sent), admin confirmations. Esc closes, focus returns to trigger.
- **Admin status pill** — New, In review, Info requested, Approved, Declined, Published.

## 4. Navigation map
Home → Get help → Apply → Received · Home → Give help → {Campaigns, Practical help, Volunteer}
Campaigns → Details → Donate → Result → Details · Details → Need item → Help form (category preselected)
Details → Contact (campaign attached) → Admin › Messages
Apply / Help / Volunteer / Contact submissions → Admin tabs (new rows tagged).
Admin: Approve and Publish are separate actions, each with a confirmation dialog.

## 5. Validation rules
- Phone: Nigerian mobile (0/234 + 10 digits). Email: optional except donation receipt.
- Donation: amount ≥ ₦500 integer; email required; method required.
- Help offer: ≥1 type, availability, area, name, phone, contact consent.
- Volunteer outreach: outreach, name, phone, area, stay answer, guidelines consent.
- Volunteer community: name, phone, area, ≥1 interest, ≥1 availability, contact consent.
- Apply: per-section required fields; private-review consent separate from public-publish consent (asked only after approval).

## 6. Backend dependencies
Payment provider (hosted checkout + webhook for confirmed/pending/failed) · campaign CMS with publish flag · application store with private access · admin auth & roles · messaging inbox · SMS/WhatsApp follow-up · audit log of status changes.

## 7. Open decisions (founder)
1. Payment provider and whether card fees are passed to donors (v2 shows ₦0 platform fee, no processing fee).
2. Keep “Demo payment” notice + footer demo line for the hackathon? (Recommended: yes.)
3. When to request documents privately, and retention period.
4. Real contact channels (phone/WhatsApp/email) to replace the contact form or sit beside it.
5. Outreach venues and the volunteer guidelines document.
6. Admin roles: who may approve vs publish.

## 8. Audit — verified vs specified
**Clicked through in v2:** all 8 routes render; listing filters/count; campaign details funding & share; donation validation → success (raised total updates); Contact dialog opens with focus; admin sign-in and tabs incl. Messages.
**Carried from v1, not re-tested in v2:** apply submit/fail, help submit, volunteer submits, admin approve/publish dialogs, pending/failed donation, keyboard order, 320px reflow.
**Not built:** real payment, file storage, auth, notifications.
