import { BrowserRouter, Route, Routes } from "react-router-dom";
import ScaffoldCheck from "./ScaffoldCheck";
import Admin from "./admin/Admin";
import PublicLayout from "./components/PublicLayout";
import Home from "./pages/Home";
import Campaigns from "./pages/Campaigns";
import CampaignDetail from "./pages/CampaignDetail";
import Apply from "./pages/Apply";
import ApplicationStatus from "./pages/ApplicationStatus";
import PracticalHelp from "./pages/PracticalHelp";
import Volunteer from "./pages/Volunteer";
import Donate from "./pages/Donate";
import DonationResult from "./pages/DonationResult";
import { Activate, Login } from "./pages/CampaignerAuth";
import Dashboard from "./pages/Dashboard";
import NotFound from "./pages/NotFound";
import "./styles.css";
import "./public.css";
export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/admin" element={<Admin />} />
        <Route path="/__check" element={<ScaffoldCheck />} />
        <Route element={<PublicLayout />}>
          <Route index element={<Home />} />
          <Route path="/give" element={<Home />} />
          <Route path="/campaigns" element={<Campaigns />} />
          <Route path="/campaigns/:id" element={<CampaignDetail />} />
          <Route path="/apply" element={<Apply />} />
          <Route path="/apply/status" element={<ApplicationStatus />} />
          <Route path="/help" element={<PracticalHelp />} />
          <Route path="/volunteer" element={<Volunteer />} />
          <Route path="/donate" element={<Donate />} />
          <Route path="/donate/result" element={<DonationResult />} />
          <Route path="/activate" element={<Activate />} />
          <Route path="/login" element={<Login />} />
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/dashboard/offers" element={<Dashboard />} />
          <Route path="/dashboard/withdraw" element={<Dashboard />} />
          <Route path="*" element={<NotFound />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}
