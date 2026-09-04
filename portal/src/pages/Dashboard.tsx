import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { fetchKeyInfo, issueApiKey } from "../lib/api";
import type { KeyInfo } from "../lib/api";

type DashboardProps = {
  base: string;
  apiKey: string | null;
  email: string | null;
  setApiKey: (key: string | null, forEmail?: string | null) => void;
};

export function Dashboard({ base, apiKey, email, setApiKey }: DashboardProps) {
  const navigate = useNavigate();
  const [info, setInfo] = useState<KeyInfo | null>(null);
  const [reveal, setReveal] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [issuing, setIssuing] = useState(false);
  const [newKey, setNewKey] = useState<string | null>(null);

  useEffect(() => {
    if (!apiKey) {
      navigate("/auth");
      return;
    }

    fetchKeyInfo(base, apiKey)
      .then(setInfo)
      .catch((e: Error) => {
        if (e.message === "unauthorized") {
          setApiKey(null);
          navigate("/auth");
        } else {
          setError(e.message);
        }
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [base, apiKey]);

  async function generateAnother() {
    if (!email || issuing) return;
    setIssuing(true);
    setError(null);
    try {
      const data = await issueApiKey(base, email);
      setApiKey(data.apiKey, email);
      setNewKey(data.apiKey);
      setReveal(true);
      setInfo(await fetchKeyInfo(base, data.apiKey));
    } catch (e) {
      setError(String((e as Error).message ?? e));
    } finally {
      setIssuing(false);
    }
  }

  if (!apiKey) {
    return null;
  }

  const displayKey = newKey ?? apiKey;
  const masked = displayKey.slice(0, 12) + "•".repeat(30);

  return (
    <div className="flex flex-col gap-6">
      {error && (
        <div className="text-sm text-red-400 border border-red-900 rounded-lg p-3 bg-red-950/40">{error}</div>
      )}

      <div className="grid md:grid-cols-[1.3fr_1fr] gap-5 items-start">
        <div className="flex flex-col gap-5">
          <div className="border border-neutral-800 rounded-xl p-5 bg-neutral-900/50">
            <div className="flex items-center justify-between mb-3.5">
              <span className="text-sm font-semibold text-neutral-100">Tu clave</span>
              <button onClick={() => setReveal((r) => !r)} className="font-mono text-xs text-emerald-400">
                {reveal ? "Ocultar" : "Mostrar"}
              </button>
            </div>
            <div className="font-mono text-sm bg-neutral-900 border border-neutral-800 rounded-lg p-3 text-neutral-100 break-all mb-3">
              {reveal ? displayKey : masked}
            </div>
            <div className="flex gap-6">
              <MetaField
                label="Creada"
                value={info ? new Date(info.createdAtUtc).toLocaleDateString() : "…"}
              />
              <MetaField
                label="Último uso"
                value={info?.lastUsedUtc ? new Date(info.lastUsedUtc).toLocaleString() : "todavía no"}
              />
              <MetaField label="Límite" value="120 req/min" />
            </div>
          </div>

          <div className="border border-neutral-800 rounded-xl p-5 bg-neutral-900/50">
            <div className="text-sm font-semibold text-neutral-100 mb-3">Endpoints</div>
            <div className="flex flex-col gap-2 font-mono text-xs text-neutral-400">
              <div>
                <span className="text-emerald-400 inline-block w-9">GET</span>/v1/sprites?season=Override&amp;theme=Gold
              </div>
              <div>
                <span className="text-emerald-400 inline-block w-9">GET</span>/v1/sprites/storm_scout_gold
              </div>
              <div>
                <span className="text-emerald-400 inline-block w-9">GET</span>/v1/sprites/storm_scout_gold.png
              </div>
            </div>
            <div className="flex gap-4 mt-3.5">
              <a
                href={`${base}/swagger`}
                target="_blank"
                rel="noreferrer"
                className="font-mono text-xs text-emerald-400 hover:text-emerald-300"
              >
                Ver documentación (Swagger) →
              </a>
              <a href="/" className="font-mono text-xs text-emerald-400 hover:text-emerald-300">
                Explorar sprites →
              </a>
            </div>
          </div>
        </div>

        <div className="flex flex-col gap-5">
          <div className="border border-neutral-800 rounded-xl p-5 bg-neutral-900/50">
            <div className="text-sm font-semibold text-neutral-100 mb-1">Uso</div>
            <p className="text-xs text-neutral-600 leading-relaxed mb-3.5">
              El conteo detallado de requests llega junto con los planes pagos. Por ahora sólo se registra
              el último uso.
            </p>
            <div className="text-2xl font-bold text-neutral-100">{info?.tier ?? "free"}</div>
            <div className="font-mono text-[11px] text-neutral-600">tier actual</div>
          </div>

          <div className="border border-neutral-800 rounded-xl p-5 bg-neutral-900/50 flex flex-col gap-2.5">
            <div className="text-sm font-semibold text-neutral-100">Acciones</div>
            <button
              onClick={generateAnother}
              disabled={issuing}
              className="py-2 rounded-lg border border-neutral-800 text-neutral-200 text-sm disabled:opacity-50"
            >
              {issuing ? "Generando…" : "+ Generar otra clave"}
            </button>
            <button disabled className="py-2 rounded-lg border border-neutral-800 text-neutral-600 text-sm cursor-not-allowed">
              Rotar clave (próximamente)
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}

function MetaField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <div className="font-mono text-[11px] text-neutral-600 uppercase tracking-wide">{label}</div>
      <div className="text-sm text-neutral-400">{value}</div>
    </div>
  );
}
