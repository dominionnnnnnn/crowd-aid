import { createContext, useContext, useEffect, useState } from "react";
import {
  Link,
  NavLink,
  Outlet,
  useLocation,
  useNavigate,
} from "react-router-dom";
import ContactForm from "./ContactForm";
import {
  getCampaignerToken,
  setCampaignerToken,
} from "../api/campaignerSession";

import { Modal } from "./UI";
const PublicContext = createContext(null);
export const usePublic = () => useContext(PublicContext);
export default function PublicLayout() {
  const [contact, setContact] = useState(undefined);
  const [menu, setMenu] = useState(false);
  const [signedIn, setSignedIn] = useState(!!getCampaignerToken());
  const [offline, setOffline] = useState(!navigator.onLine);
  const location = useLocation();
  const navigate = useNavigate();
  useEffect(() => {
    setMenu(false);
    setSignedIn(!!getCampaignerToken());
    window.scrollTo({ top: 0 });
  }, [location.pathname]);
  useEffect(() => {
    const changed = () => setSignedIn(!!getCampaignerToken());
    window.addEventListener("crowdaid:campaigner-session-changed", changed);
    window.addEventListener("crowdaid:campaigner-expired", changed);
    return () => {
      window.removeEventListener(
        "crowdaid:campaigner-session-changed",
        changed,
      );
      window.removeEventListener("crowdaid:campaigner-expired", changed);
    };
  }, []);
  const closeMenu = () => {
    setMenu(false);
  };
  const signOut = () => {
    setCampaignerToken(null);
    closeMenu();
    navigate("/login");
  };
  useEffect(() => {
    const on = () => setOffline(false);
    const off = () => setOffline(true);
    window.addEventListener("online", on);
    window.addEventListener("offline", off);
    return () => {
      window.removeEventListener("online", on);
      window.removeEventListener("offline", off);
    };
  }, []);
  return (
    <PublicContext.Provider
      value={{
        contact: (campaign = null) => setContact(campaign),
        offline,
      }}
    >
      <div className="public-app">
        <a className="p-skip" href="#public-main">
          Skip to main content
        </a>
        {offline && (
          <div className="p-offline" role="status">
            ⚠ You're offline. You can keep filling in forms; send them when
            you're back online.
          </div>
        )}
        <header className="p-header">
          <div className="p-header-inner">
            <Link to="/" aria-label="CrowdAid home">
              <img
                className="p-logo"
                src="/assets/logo/crowdaid-logo.svg"
                alt="CrowdAid"
              />
            </Link>
            <nav className="p-desktop-nav" aria-label="Main">
              <NavLink to="/" end>
                Home
              </NavLink>
              <NavLink to="/campaigns">Campaigns</NavLink>
              <NavLink to="/volunteer">Volunteer</NavLink>
              <NavLink to="/contact">Contact</NavLink>
              <Link className="button p-primary" to="/apply">
                Get help
              </Link>
              <NavLink to="/campaigns">Give help</NavLink>
              {signedIn ? (
                <>
                  <NavLink to="/dashboard">Dashboard</NavLink>
                  <button onClick={signOut}>Sign out</button>
                </>
              ) : (
                <NavLink to="/login">Sign in</NavLink>
              )}
            </nav>
            <div className="p-mobile-nav">
              <Link to="/campaigns">Campaigns</Link>
              <button
                aria-label="Menu"
                aria-expanded={menu}
                onClick={() => setMenu(true)}
              >
                ☰
              </button>
            </div>
          </div>
        </header>
        <main id="public-main" tabIndex={-1}>
          <Outlet />
        </main>
        <footer className="p-footer">
          <div className="p-container p-footer-grid">
            <div className="p-stack">
              <img
                src="/assets/logo/crowdaid-logo-tagline.svg"
                alt="CrowdAid — Healthcare, Together."
              />
              <p>
                Community healthcare support, starting in Abeokuta, Ogun State.
              </p>
            </div>
            <nav aria-label="Explore">
              <h2>Explore</h2>
              <Link to="/campaigns">Campaigns</Link>
              <Link to="/apply">Get help</Link>
              <Link to="/help">Offer practical help</Link>
              <Link to="/volunteer">Volunteer</Link>
              <Link to="/apply/status">Check application status</Link>
              <Link to="/login">Campaigner sign in</Link>
              <Link to="/contact">Contact</Link>
            </nav>
            <div className="p-stack">
              <h2>Contact CrowdAid</h2>
              <p>
                Questions about a campaign or your request? Send us a message
                and we'll reply by phone or WhatsApp.
              </p>
              <Link className="button p-outline" to="/contact">
                Contact CrowdAid
              </Link>
            </div>
          </div>
          <p className="p-container p-footer-note">
            CrowdAid charges no platform fee. Paystack test payments use no real
            money.
          </p>
        </footer>
        {menu && (
          <Modal title="Menu" onClose={closeMenu}>
            <nav className="p-menu" aria-label="Mobile main">
              <Link to="/" onClick={() => setMenu(false)}>
                Home
              </Link>
              <Link to="/campaigns" onClick={() => setMenu(false)}>
                Campaigns
              </Link>
              <Link
                className="button p-primary"
                to="/apply"
                onClick={closeMenu}
              >
                Get help
              </Link>
              <NavLink to="/campaigns" onClick={closeMenu}>
                Give help
              </NavLink>
              <Link to="/help" onClick={closeMenu}>
                Offer practical help
              </Link>
              <Link to="/volunteer" onClick={() => setMenu(false)}>
                Volunteer
              </Link>
              <Link to="/contact" onClick={() => setMenu(false)}>
                Contact
              </Link>
              {signedIn ? (
                <>
                  <Link to="/dashboard" onClick={closeMenu}>
                    Dashboard
                  </Link>
                  <button onClick={signOut}>Sign out</button>
                </>
              ) : (
                <Link to="/login" onClick={closeMenu}>
                  Sign in
                </Link>
              )}
            </nav>
          </Modal>
        )}
        {contact !== undefined && (
          <ContactDialog
            key={contact?.id || "general"}
            campaign={contact}
            onClose={() => setContact(undefined)}
          />
        )}
      </div>
    </PublicContext.Provider>
  );
}
function ContactDialog({ campaign, onClose }) {
  const [busy, setBusy] = useState(false);
  return (
    <Modal title="Contact CrowdAid" onClose={onClose} busy={busy}>
      <ContactForm
        campaign={campaign}
        onDone={onClose}
        onBusyChange={setBusy}
      />
    </Modal>
  );
}
