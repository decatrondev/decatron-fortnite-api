import { useState } from "react";
import type { ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { issueApiKey } from "../lib/api";
import type { IssuedKey } from "../lib/api";

export function Auth({ base, setApiKey }: { base: string; setApiKey: (key: string | null, forEmail?: string | null) => void }) {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [name, setName] = useState("");
  const [issued, setIssued] = useState<IssuedKey | null>(null);
  const [reveal, setReveal] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

  async function submit() {
    if (!emailValid || loading) return;
    setLoading(true);
    setError(null);
    try {
      const data = await issueApiKey(base, email, name || undefined);
      setIssued(data);
      setApiKey(data.apiKey, email);
    } catch (e) {
      setError(String((e as Error).message ?? e));
    } finally {
      setLoading(false);
    }
  }

  if (issued) {
    const masked = issued.apiKey.slice(0, 12) + "•".repeat(30);
    const curlLine = `curl -H "X-Api-Key: ${reveal ? issued.apiKey : "fnapi_..."}" \\\n  ${base || location.origin}/v1/sprites`;

    return (
      <div className="max-w-md mx-auto flex flex-col gap-6">
        <div>
          <div className="flex items-center gap-2">
            <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 inline-block" />
            <span className="font-mono text-xs uppercase tracking-wide text-emerald-400">
              Cuenta creada — tier {issued.tier}
            </span>
          </div>
          <h1 className="text-2xl font-bold text-neutral-100 mt-1">Tu API key</h1>
        </div>

        <div className="flex flex-col gap-4 border border-neutral-800 rounded-xl p-6 bg-neutral-900/50">
          <div className="flex flex-col gap-2">
            <div className="flex items-center justify-between">
              <label className="font-mono text-xs uppercase tracking-wide text-neutral-500">X-Api-Key</label>
              <button onClick={() => setReveal((r) => !r)} className="font-mono text-xs text-emerald-400">
                {reveal ? "Ocultar" : "Mostrar"}
              </button>
            </div>
            <div className="font-mono text-sm bg-neutral-900 border border-neutral-800 rounded-lg p-3 text-neutral-100 break-all">
              {reveal ? issued.apiKey : masked}
            </div>
          </div>

          <div className="flex gap-2 p-3 rounded-lg bg-amber-950/20 border border-amber-800/30">
            <span className="font-mono text-xs text-amber-400 leading-relaxed">
              No se vuelve a mostrar. Guardala ahora — si la perdés, generás una nueva desde el dashboard.
            </span>
          </div>

          <div className="flex flex-col gap-2">
            <label className="font-mono text-xs uppercase tracking-wide text-neutral-500">Cómo usarla</label>
            <pre className="font-mono text-xs bg-neutral-900 border border-neutral-800 rounded-lg p-3 text-neutral-400 overflow-x-auto whitespace-pre">
              {curlLine}
            </pre>
          </div>
        </div>

        <button
          onClick={() => navigate("/dashboard")}
          className="py-2.5 rounded-lg bg-neutral-100 text-neutral-950 text-sm font-semibold hover:bg-white"
        >
          Ir al dashboard
        </button>
      </div>
    );
  }

  return (
    <div className="max-w-md mx-auto flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold text-neutral-100">Conseguí tu API key</h1>
        <p className="text-sm text-neutral-400 mt-1">
          Alta libre por email, sin contraseña. Te devolvemos la clave una sola vez — guardala en ese
          momento.
        </p>
      </div>

      <div className="flex flex-col gap-3 border border-neutral-800 rounded-xl p-6 bg-neutral-900/50">
        <Field label="Email">
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="vos@ejemplo.com"
            className="bg-neutral-900 border border-neutral-800 rounded-lg px-3 py-2.5 text-sm text-neutral-100 outline-none focus:border-neutral-600"
          />
        </Field>
        <Field label="Nombre (opcional)">
          <input
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="bot-twitch"
            className="bg-neutral-900 border border-neutral-800 rounded-lg px-3 py-2.5 text-sm text-neutral-100 outline-none focus:border-neutral-600"
          />
        </Field>

        {error && <div className="text-xs text-red-400">{error}</div>}

        <button
          onClick={submit}
          disabled={!emailValid || loading}
          className="mt-1 py-2.5 rounded-lg bg-neutral-100 text-neutral-950 text-sm font-semibold disabled:opacity-40 disabled:cursor-not-allowed enabled:hover:bg-white"
        >
          {loading ? "Generando…" : "Generar API key"}
        </button>

        <div className="flex items-center gap-1.5 mt-0.5">
          <span className="w-1 h-1 rounded-full bg-emerald-400 inline-block" />
          <span className="font-mono text-[11px] text-neutral-600">tier free · máx. 5 altas/hora por IP</span>
        </div>
      </div>
    </div>
  );
}

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label className="font-mono text-xs uppercase tracking-wide text-neutral-500">{label}</label>
      {children}
    </div>
  );
}
