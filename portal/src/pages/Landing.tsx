import { Link } from "react-router-dom";
import { Endpoint } from "../components/Endpoint";
import { SpritesExplorer } from "../components/SpritesExplorer";

export function Landing({ base }: { base: string }) {
  return (
    <div className="flex flex-col gap-10">
      <section className="flex flex-col gap-5 max-w-2xl">
        <div className="flex items-center gap-2">
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 inline-block" />
          <span className="font-mono text-xs uppercase tracking-wide text-neutral-500">
            Free tier disponible ahora
          </span>
        </div>
        <h1 className="text-4xl font-bold leading-tight tracking-tight text-neutral-100">
          Sprites de Fortnite,
          <br />
          directo de los archivos del juego.
        </h1>
        <p className="text-neutral-400 text-base leading-relaxed">
          Fuente propia, de solo lectura. Extraemos cada icono y su metadata de una instalación local de
          Fortnite y los servimos desde infraestructura propia — sin depender de terceros.
        </p>
        <div className="flex items-center gap-3">
          <Link
            to="/auth"
            className="px-5 py-2.5 rounded-lg bg-neutral-100 text-neutral-950 text-sm font-semibold hover:bg-white"
          >
            Conseguir API key gratis
          </Link>
          <span className="font-mono text-xs text-neutral-500">sin tarjeta · alta por email</span>
        </div>
      </section>

      <section className="flex gap-8 py-5 border-y border-neutral-800">
        <Stat value="186" label="sprites catalogados" />
        <Stat value="2" label="temporadas — Runners · Override" />
        <Stat value="120/min" label="límite del free tier" />
      </section>

      <section>
        <h2 className="text-xs font-semibold uppercase tracking-wide text-neutral-100 mb-2">Endpoints</h2>
        <div className="grid sm:grid-cols-2 gap-3">
          <Endpoint
            method="GET"
            path="/v1/sprites"
            desc="Catálogo completo. Filtros: ?season= ?theme= ?rarity= ?unreleased= ?character="
          />
          <Endpoint method="GET" path="/v1/sprites/{id}" desc="Un sprite por id." />
          <Endpoint
            method="GET"
            path="/v1/sprites/{id}.png"
            desc="Redirige a la imagen con ?v=<hash> para caché."
          />
          <Endpoint
            method="GET"
            path="/v1/sprites-data.js"
            desc="Mismo catálogo como archivo JS (window.spritesData)."
          />
          <Endpoint
            method="POST"
            path="/v1/keys"
            desc="Alta libre por email. Devuelve tu API key una sola vez."
          />
          <Endpoint method="GET" path="/v1/keys/me" desc="Estado de tu cuenta: tier, fechas, último uso." />
        </div>
      </section>

      <SpritesExplorer base={base} />

      <footer className="text-xs text-neutral-600 pt-4">
        Assets © Epic Games. Uso propio / verificación. Sin mirror público.
      </footer>
    </div>
  );
}

function Stat({ value, label }: { value: string; label: string }) {
  return (
    <div>
      <div className="text-2xl font-bold text-neutral-100">{value}</div>
      <div className="font-mono text-xs text-neutral-500">{label}</div>
    </div>
  );
}
