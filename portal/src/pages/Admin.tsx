import { useEffect, useMemo, useState } from "react";
import {
  clearAdminOverride,
  fetchAdminSprites,
  fetchGgSync,
  getStoredAdminKey,
  setAdminOverride,
  setStoredAdminKey,
} from "../lib/api";
import type { AdminSprite, GgSyncResult } from "../lib/api";

export function Admin({ base }: { base: string }) {
  const [adminKey, setAdminKeyState] = useState(() => getStoredAdminKey());
  const [sprites, setSprites] = useState<AdminSprite[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [q, setQ] = useState("");
  const [season, setSeason] = useState("");
  const [onlyOverridden, setOnlyOverridden] = useState(false);

  async function load(key: string) {
    setLoading(true);
    setError(null);
    try {
      const data = await fetchAdminSprites(base, key);
      setSprites(data);
      setStoredAdminKey(key);
      setAdminKeyState(key);
    } catch (e) {
      if ((e as Error).message === "unauthorized") {
        setError("Clave incorrecta.");
        setStoredAdminKey(null);
        setAdminKeyState(null);
      } else {
        setError(String((e as Error).message ?? e));
      }
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (adminKey) load(adminKey);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [base]);

  const seasons = useMemo(() => [...new Set(sprites.map((s) => s.season))].sort(), [sprites]);

  const filtered = useMemo(
    () =>
      sprites.filter(
        (s) =>
          (!q || s.id.includes(q.toLowerCase()) || s.name.toLowerCase().includes(q.toLowerCase())) &&
          (!season || s.season === season) &&
          (!onlyOverridden || s.overridden),
      ),
    [sprites, q, season, onlyOverridden],
  );

  async function toggle(s: AdminSprite) {
    if (!adminKey) return;
    const next = !s.unreleased;
    setSprites((prev) => prev.map((x) => (x.id === s.id ? { ...x, unreleased: next, overridden: true } : x)));
    try {
      await setAdminOverride(base, adminKey, s.id, next);
    } catch {
      // revert on failure
      setSprites((prev) => prev.map((x) => (x.id === s.id ? s : x)));
    }
  }

  async function revert(s: AdminSprite) {
    if (!adminKey || s.computedUnreleased === undefined) return;
    setSprites((prev) =>
      prev.map((x) => (x.id === s.id ? { ...x, unreleased: s.computedUnreleased!, overridden: false, note: undefined } : x)),
    );
    try {
      await clearAdminOverride(base, adminKey, s.id);
    } catch {
      setSprites((prev) => prev.map((x) => (x.id === s.id ? s : x)));
    }
  }

  if (!adminKey) {
    return <LoginForm onSubmit={load} error={error} loading={loading} />;
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-2">
        <h1 className="text-lg font-semibold text-neutral-100 mr-auto">
          Admin — unreleased {loading ? "…" : `(${filtered.length}/${sprites.length})`}
        </h1>
        <button
          onClick={() => {
            setStoredAdminKey(null);
            setAdminKeyState(null);
          }}
          className="font-mono text-xs text-neutral-500 hover:text-neutral-300"
        >
          salir
        </button>
      </div>

      <GgSyncPanel base={base} adminKey={adminKey} onApplied={() => load(adminKey)} />

      <div className="flex flex-wrap items-center gap-2">
        <input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="buscar…"
          className="bg-neutral-900 border border-neutral-800 rounded-md px-2 py-1 text-sm outline-none focus:border-neutral-600"
        />
        <select
          value={season}
          onChange={(e) => setSeason(e.target.value)}
          className="bg-neutral-900 border border-neutral-800 rounded-md px-2 py-1 text-sm outline-none focus:border-neutral-600"
        >
          <option value="">season: todos</option>
          {seasons.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
        <label className="flex items-center gap-1.5 font-mono text-xs text-neutral-400">
          <input type="checkbox" checked={onlyOverridden} onChange={(e) => setOnlyOverridden(e.target.checked)} />
          solo con override
        </label>
      </div>

      {error && (
        <div className="text-sm text-red-400 border border-red-900 rounded-lg p-3 bg-red-950/40">{error}</div>
      )}

      <div className="border border-neutral-800 rounded-lg overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-neutral-900 text-neutral-500 font-mono text-xs uppercase">
            <tr>
              <th className="text-left px-3 py-2">Sprite</th>
              <th className="text-left px-3 py-2">Season</th>
              <th className="text-left px-3 py-2">Theme</th>
              <th className="text-left px-3 py-2">Rarity</th>
              <th className="text-left px-3 py-2">Estado</th>
              <th className="text-left px-3 py-2"></th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((s) => (
              <tr key={s.id} className="border-t border-neutral-800">
                <td className="px-3 py-2">
                  <div className="text-neutral-200">{s.name}</div>
                  <div className="font-mono text-[11px] text-neutral-600">{s.id}</div>
                </td>
                <td className="px-3 py-2 text-neutral-400">{s.season}</td>
                <td className="px-3 py-2 text-neutral-400">{s.theme}</td>
                <td className="px-3 py-2 text-neutral-400">{s.rarity}</td>
                <td className="px-3 py-2">
                  <button
                    onClick={() => toggle(s)}
                    className={`px-2 py-1 rounded font-mono text-xs border ${
                      s.unreleased
                        ? "border-red-800 text-red-400 bg-red-950/30"
                        : "border-emerald-800 text-emerald-400 bg-emerald-950/30"
                    }`}
                  >
                    {s.unreleased ? "no disponible" : "disponible"}
                  </button>
                  {s.overridden && (
                    <span className="ml-2 font-mono text-[10px] text-amber-400" title={s.note}>
                      override
                    </span>
                  )}
                </td>
                <td className="px-3 py-2">
                  {s.overridden && (
                    <button
                      onClick={() => revert(s)}
                      className="font-mono text-[11px] text-neutral-500 hover:text-neutral-300"
                    >
                      revertir
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function GgSyncPanel({
  base,
  adminKey,
  onApplied,
}: {
  base: string;
  adminKey: string;
  onApplied: () => void;
}) {
  const [result, setResult] = useState<GgSyncResult | null>(null);
  const [checked, setChecked] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(false);
  const [applying, setApplying] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function run() {
    setLoading(true);
    setError(null);
    try {
      const r = await fetchGgSync(base, adminKey);
      setResult(r);
      setChecked(new Set(r.toRelease.map((e) => e.id)));
    } catch (e) {
      setError(String((e as Error).message ?? e));
    } finally {
      setLoading(false);
    }
  }

  async function apply() {
    if (!result) return;
    setApplying(true);
    try {
      for (const entry of result.toRelease) {
        if (checked.has(entry.id)) {
          await setAdminOverride(base, adminKey, entry.id, false, "sincronizado con fortnite.gg");
        }
      }
      setResult(null);
      onApplied();
    } catch (e) {
      setError(String((e as Error).message ?? e));
    } finally {
      setApplying(false);
    }
  }

  return (
    <div className="border border-neutral-800 rounded-lg p-3 flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <span className="text-sm text-neutral-300 mr-auto">Sincronizar con fortnite.gg</span>
        <button
          onClick={run}
          disabled={loading}
          className="px-3 py-1 rounded font-mono text-xs border border-neutral-700 text-neutral-300 hover:border-neutral-500 disabled:opacity-40"
        >
          {loading ? "buscando…" : "buscar diferencias"}
        </button>
      </div>

      {error && <div className="text-xs text-red-400">{error}</div>}

      {result && (
        <div className="flex flex-col gap-3">
          {result.toRelease.length === 0 && result.suspicious.length === 0 && result.missing.length === 0 && (
            <div className="text-xs text-emerald-400 font-mono">todo sincronizado, sin diferencias</div>
          )}

          {result.toRelease.length > 0 && (
            <div className="flex flex-col gap-1.5">
              <div className="text-xs text-neutral-500 font-mono uppercase">
                A liberar ({result.toRelease.length}) — gg ya lo tiene, tu base dice no disponible
              </div>
              {result.toRelease.map((e) => (
                <label key={e.id} className="flex items-center gap-2 text-sm text-neutral-300">
                  <input
                    type="checkbox"
                    checked={checked.has(e.id)}
                    onChange={(ev) =>
                      setChecked((prev) => {
                        const next = new Set(prev);
                        if (ev.target.checked) next.add(e.id);
                        else next.delete(e.id);
                        return next;
                      })
                    }
                  />
                  <span className="font-mono text-[11px] text-neutral-600">{e.id}</span>
                  <span>
                    {e.character} · {e.theme} · {e.season}
                  </span>
                </label>
              ))}
              <button
                onClick={apply}
                disabled={applying || checked.size === 0}
                className="self-start mt-1 px-3 py-1 rounded font-mono text-xs border border-emerald-800 text-emerald-400 bg-emerald-950/30 disabled:opacity-40"
              >
                {applying ? "aplicando…" : `aplicar seleccionados (${checked.size})`}
              </button>
            </div>
          )}

          {result.suspicious.length > 0 && (
            <div className="flex flex-col gap-1">
              <div className="text-xs text-neutral-500 font-mono uppercase">
                Sospechosos ({result.suspicious.length}) — tu base dice disponible, gg no lo lista
              </div>
              <div className="text-xs text-neutral-400 max-h-40 overflow-y-auto flex flex-col gap-0.5">
                {result.suspicious.map((e) => (
                  <div key={e.id} className="font-mono">
                    {e.character} · {e.theme} · {e.season}
                  </div>
                ))}
              </div>
            </div>
          )}

          {result.missing.length > 0 && (
            <div className="flex flex-col gap-1">
              <div className="text-xs text-neutral-500 font-mono uppercase">
                Faltantes ({result.missing.length}) — gg lo tiene, no existe en tu catálogo (hace falta ingest)
              </div>
              <div className="text-xs text-amber-400 max-h-40 overflow-y-auto flex flex-col gap-0.5">
                {result.missing.map((c, i) => (
                  <div key={i} className="font-mono">
                    {c.character} · {c.theme} · {c.season}
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
}

function LoginForm({
  onSubmit,
  error,
  loading,
}: {
  onSubmit: (key: string) => void;
  error: string | null;
  loading: boolean;
}) {
  const [value, setValue] = useState("");

  return (
    <div className="max-w-sm mx-auto flex flex-col gap-4 mt-16">
      <h1 className="text-lg font-semibold text-neutral-100">Admin</h1>
      <input
        type="password"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        onKeyDown={(e) => e.key === "Enter" && value && onSubmit(value)}
        placeholder="clave de admin"
        className="bg-neutral-900 border border-neutral-800 rounded-lg px-3 py-2 text-sm text-neutral-100 outline-none focus:border-neutral-600"
      />
      {error && <div className="text-xs text-red-400">{error}</div>}
      <button
        onClick={() => value && onSubmit(value)}
        disabled={!value || loading}
        className="py-2 rounded-lg bg-neutral-100 text-neutral-950 text-sm font-semibold disabled:opacity-40"
      >
        {loading ? "Entrando…" : "Entrar"}
      </button>
    </div>
  );
}
