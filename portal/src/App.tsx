import { useState } from "react";
import { Link, Route, Routes, useLocation } from "react-router-dom";
import { useBaseUrl, useStoredApiKey } from "./lib/api";
import { Landing } from "./pages/Landing";
import { Auth } from "./pages/Auth";
import { Dashboard } from "./pages/Dashboard";
import { Admin } from "./pages/Admin";

export default function App() {
  const [base, setBase] = useBaseUrl();
  const { apiKey, email, setApiKey } = useStoredApiKey();
  const location = useLocation();
  const [showBaseInput, setShowBaseInput] = useState(false);

  return (
    <div className="min-h-screen max-w-6xl mx-auto px-5 py-6">
      <nav className="flex items-center gap-5 mb-10">
        <Link to="/" className="font-bold text-neutral-100 mr-auto">
          Decatron Fortnite API
        </Link>
        <button
          onClick={() => setShowBaseInput((v) => !v)}
          className="font-mono text-xs text-neutral-500 hover:text-neutral-300"
        >
          base url
        </button>
        <a
          href={`${base}/swagger`}
          target="_blank"
          rel="noreferrer"
          className="font-mono text-xs text-neutral-400 hover:text-neutral-200"
        >
          Docs
        </a>
        {apiKey ? (
          <Link
            to="/dashboard"
            className={`font-mono text-xs hover:text-neutral-100 ${
              location.pathname === "/dashboard" ? "text-neutral-100" : "text-neutral-400"
            }`}
          >
            Dashboard
          </Link>
        ) : (
          <Link
            to="/auth"
            className="px-3 py-1.5 rounded-lg bg-neutral-100 text-neutral-950 text-xs font-semibold hover:bg-white"
          >
            Conseguir API key
          </Link>
        )}
      </nav>

      {showBaseInput && (
        <div className="mb-8">
          <label className="text-xs uppercase tracking-wide text-neutral-500">Base URL de la API</label>
          <input
            value={base}
            onChange={(e) => setBase(e.target.value)}
            placeholder="(vacío = mismo dominio)  ej: https://fortnite-api.decatron.net"
            className="mt-1 w-full bg-neutral-900 border border-neutral-800 rounded-lg px-3 py-2 font-mono text-sm text-neutral-100 outline-none focus:border-neutral-600"
          />
        </div>
      )}

      <Routes>
        <Route path="/" element={<Landing base={base} />} />
        <Route path="/auth" element={<Auth base={base} setApiKey={setApiKey} />} />
        <Route
          path="/dashboard"
          element={<Dashboard base={base} apiKey={apiKey} email={email} setApiKey={setApiKey} />}
        />
        <Route path="/admin" element={<Admin base={base} />} />
      </Routes>
    </div>
  );
}
