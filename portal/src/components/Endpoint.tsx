export function Endpoint({ method, path, desc }: { method: string; path: string; desc: string }) {
  const methodColor = method === "POST" ? "text-violet-400" : "text-emerald-400";
  return (
    <div className="flex flex-col gap-1 border border-neutral-800 rounded-lg p-3 bg-neutral-900/50">
      <div className="flex items-center gap-2 font-mono text-sm">
        <span className={`${methodColor} font-semibold`}>{method}</span>
        <span className="text-neutral-100">{path}</span>
      </div>
      <p className="text-xs text-neutral-400">{desc}</p>
    </div>
  );
}
