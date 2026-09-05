import { useEffect, useMemo, useState } from "react";
import type { Sprite } from "../lib/api";

const RARITY_COLOR: Record<string, string> = {
  Rare: "text-sky-400",
  Special: "text-emerald-400",
  Epic: "text-fuchsia-400",
  Legendary: "text-amber-400",
  Mythic: "text-yellow-300",
};

export function SpritesExplorer({ base }: { base: string }) {
  const [sprites, setSprites] = useState<Sprite[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const [q, setQ] = useState("");
  const [season, setSeason] = useState("");
  const [theme, setTheme] = useState("");
  const [rarity, setRarity] = useState("");

  useEffect(() => {
    setLoading(true);
    setError(null);
    fetch(`${base}/v1/sprites`)
      .then((r) => {
        if (!r.ok) throw new Error(`HTTP ${r.status}`);
        return r.json();
      })
      .then((data: Sprite[]) => setSprites(data))
      .catch((e) => setError(String(e)))
      .finally(() => setLoading(false));
  }, [base]);

  const seasons = useMemo(() => [...new Set(sprites.map((s) => s.season))].sort(), [sprites]);
  const themes = useMemo(() => [...new Set(sprites.map((s) => s.theme))].sort(), [sprites]);
  const rarities = useMemo(() => [...new Set(sprites.map((s) => s.rarity))].sort(), [sprites]);

  const filtered = useMemo(
    () =>
      sprites.filter(
        (s) =>
          (!q || s.id.includes(q.toLowerCase()) || s.name.toLowerCase().includes(q.toLowerCase())) &&
          (!season || s.season === season) &&
          (!theme || s.theme === theme) &&
          (!rarity || s.rarity === rarity),
      ),
    [sprites, q, season, theme, rarity],
  );

  // Progreso de la temporada elegida: cuántos de esa temporada ya se pueden conseguir ahora
  // (unreleased=false). No hay tracking de colección personal, el "conseguidos" arranca en 0.
  const seasonProgress = useMemo(() => {
    if (!season) return null;
    const inSeason = sprites.filter((s) => s.season === season);
    const available = inSeason.filter((s) => !s.unreleased).length;
    return { available, total: inSeason.length };
  }, [sprites, season]);

  return (
    <section>
      <div className="flex flex-wrap items-center gap-2 mb-1">
        <h2 className="text-lg font-semibold text-neutral-100 mr-auto">
          Explorador {loading ? "…" : `(${filtered.length}/${sprites.length})`}
        </h2>
        <input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="buscar…"
          className="bg-neutral-900 border border-neutral-800 rounded-md px-2 py-1 text-sm outline-none focus:border-neutral-600"
        />
        <Select value={season} onChange={setSeason} options={seasons} label="season" />
        <Select value={theme} onChange={setTheme} options={themes} label="theme" />
        <Select value={rarity} onChange={setRarity} options={rarities} label="rarity" />
      </div>

      <div className="mb-4 h-5">
        {seasonProgress && (
          <div className="flex items-center gap-2">
            <span className="font-mono text-sm text-neutral-200">0/{seasonProgress.available}</span>
            <span className="text-xs text-neutral-500">
              disponibles ahora en {season}
              {seasonProgress.total > seasonProgress.available &&
                ` · ${seasonProgress.total - seasonProgress.available} todavía sin salir`}
            </span>
          </div>
        )}
      </div>

      {error && (
        <div className="text-sm text-red-400 border border-red-900 rounded-lg p-3 bg-red-950/40 mb-4">
          No se pudo cargar {base || "(mismo dominio)"}/v1/sprites — {error}
        </div>
      )}

      <div className="grid grid-cols-3 sm:grid-cols-5 md:grid-cols-7 lg:grid-cols-9 gap-3">
        {filtered.map((s) => (
          <figure
            key={s.id}
            className="group"
            title={`${s.id}\n${s.rarity} · ${s.season}${s.unreleased ? " · todavía no disponible" : ""}`}
          >
            <div className="relative aspect-square bg-neutral-900 border border-neutral-800 rounded-lg overflow-hidden grid place-items-center">
              <img
                src={`${base}/sprites/${s.id}.png`}
                alt={s.name}
                loading="lazy"
                className={`w-full h-full object-contain [image-rendering:pixelated] ${
                  s.unreleased ? "opacity-30 grayscale" : ""
                }`}
              />
              {s.unreleased && (
                <div className="absolute inset-0 flex items-center justify-center bg-neutral-950/40">
                  <span className="px-1.5 py-0.5 rounded bg-neutral-950/90 border border-neutral-700 font-mono text-[9px] uppercase tracking-wide text-neutral-300">
                    No disponible
                  </span>
                </div>
              )}
            </div>
            <figcaption className="mt-1 text-[11px] leading-tight text-neutral-400">
              <span className="block text-neutral-200 truncate">{s.name}</span>
              <span className={RARITY_COLOR[s.rarity] ?? "text-neutral-500"}>{s.rarity}</span>
            </figcaption>
          </figure>
        ))}
      </div>
    </section>
  );
}

function Select({
  value,
  onChange,
  options,
  label,
}: {
  value: string;
  onChange: (v: string) => void;
  options: string[];
  label: string;
}) {
  return (
    <select
      value={value}
      onChange={(e) => onChange(e.target.value)}
      className="bg-neutral-900 border border-neutral-800 rounded-md px-2 py-1 text-sm outline-none focus:border-neutral-600"
    >
      <option value="">{label}: todos</option>
      {options.map((o) => (
        <option key={o} value={o}>
          {o}
        </option>
      ))}
    </select>
  );
}
