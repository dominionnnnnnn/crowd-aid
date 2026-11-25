import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import ScaffoldCheck from "./ScaffoldCheck";
import Admin from "./admin/Admin";
import "./styles.css";
export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/admin" element={<Admin />} />
        <Route path="/__check" element={<ScaffoldCheck />} />
        <Route path="*" element={<Navigate to="/admin" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
