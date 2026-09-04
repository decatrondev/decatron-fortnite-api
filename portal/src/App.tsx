import { useEffect, useMemo, useState } from "react";

type Sprite = {
  id: string;
  name: string;
  theme: string;
  rarity: string;
  unreleased: boolean;
  season: string;
  character?: string;
};

const RARITY_COLOR: Record<string, string> = {
  Rare: "text-sky-400",
  Special: "text-emerald-400",
  Epic: "text-fuchsia-400",
  Legendary: "text-amber-400",
  Mythic: "text-yellow-300",
};

function useBaseUrl() {
  const [base, setBase] = useState(() => localStorage.getItem("fnapi.base") ?? "");
  useEffect(() => {
    localStorage.setItem("fnapi.base", base);
  }, [base]);
  return [base, setBase] as const;
}

function Endpoint({ method, path, desc }: { method: string; path: string; desc: string }) {
  return (
    <div className="flex flex-col gap-1 border border-neutral-800 rounded-lg p-3 bg-neutral-900/50">
      <div className="flex items-center gap-2 font-mono text-sm">
        <span className="text-emerald-400 font-semibold">{method}</span>
        <span className="text-neutral-100">{path}</span>
      </div>
      <p className="text-xs text-neutral-400">{desc}</p>
    </div>
  );
}

export default function App() {
  const [base, setBase] = useBaseUrl();
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

  return (
    <div className="min-h-screen max-w-6xl mx-auto px-5 py-8">
      <header className="mb-8">
        <h1 className="text-2xl font-bold text-neutral-100">Decatron Fortnite API</h1>
        <p className="text-neutral-400 mt-1">
          Fuente propia de sprites de coleccionable de Fortnite. Solo lectura. Datos extraídos de una
          instalación local del juego y servidos desde infraestructura propia.
        </p>
      </header>

      <section className="mb-8">
        <label className="text-xs uppercase tracking-wide text-neutral-500">Base URL de la API</label>
        <input
          value={base}
          onChange={(e) => setBase(e.target.value)}
          placeholder="(vacío = mismo dominio)  ej: https://fortnite-api.decatron.net"
          className="mt-1 w-full bg-neutral-900 border border-neutral-800 rounded-lg px-3 py-2 font-mono text-sm text-neutral-100 outline-none focus:border-neutral-600"
        />
      </section>

      <section className="grid sm:grid-cols-2 gap-3 mb-10">
        <Endpoint method="GET" path="/v1/sprites" desc="Catálogo completo. Filtros: ?season= ?theme= ?rarity= ?unreleased= ?character=" />
        <Endpoint method="GET" path="/v1/sprites/{id}" desc="Un sprite por id." />
        <Endpoint method="GET" path="/v1/sprites/{id}.png" desc="Redirige a la imagen con ?v=<hash> para caché." />
        <Endpoint method="GET" path="/v1/sprites-data.js" desc="Mismo catálogo como archivo JS (window.spritesData)." />
        <Endpoint method="GET" path="/sprites/{id}.png" desc="Imagen PNG RGBA. La sirve Nginx, caché inmutable." />
        <Endpoint method="GET" path="/swagger" desc="Documentación interactiva OpenAPI." />
      </section>

      <section>
        <div className="flex flex-wrap items-center gap-2 mb-4">
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

        {error && (
          <div className="text-sm text-red-400 border border-red-900 rounded-lg p-3 bg-red-950/40">
            No se pudo cargar {base || "(mismo dominio)"}/v1/sprites — {error}
          </div>
        )}

        <div className="grid grid-cols-3 sm:grid-cols-5 md:grid-cols-7 lg:grid-cols-9 gap-3">
          {filtered.map((s) => (
            <figure key={s.id} className="group" title={`${s.id}\n${s.rarity} · ${s.season}`}>
              <div className="aspect-square bg-neutral-900 border border-neutral-800 rounded-lg overflow-hidden grid place-items-center">
                <img
                  src={`${base}/sprites/${s.id}.png`}
                  alt={s.name}
                  loading="lazy"
                  className="w-full h-full object-contain [image-rendering:pixelated]"
                />
              </div>
              <figcaption className="mt-1 text-[11px] leading-tight text-neutral-400">
                <span className="block text-neutral-200 truncate">{s.name}</span>
                <span className={RARITY_COLOR[s.rarity] ?? "text-neutral-500"}>{s.rarity}</span>
                {s.unreleased && <span className="text-red-400"> · unreleased</span>}
              </figcaption>
            </figure>
          ))}
        </div>
      </section>

      <footer className="mt-12 text-xs text-neutral-600">
        Assets © Epic Games. Uso propio / verificación. Sin mirror público.
      </footer>
    </div>
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
