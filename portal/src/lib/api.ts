import { useEffect, useState } from "react";

export type Sprite = {
  id: string;
  name: string;
  theme: string;
  rarity: string;
  unreleased: boolean;
  season: string;
  character?: string;
};

export type KeyInfo = {
  email: string;
  name?: string;
  tier: string;
  createdAtUtc: string;
  lastUsedUtc?: string;
};

export type IssuedKey = {
  apiKey: string;
  tier: string;
  note: string;
};

export function useBaseUrl() {
  const [base, setBase] = useState(() => localStorage.getItem("fnapi.base") ?? "");
  useEffect(() => {
    localStorage.setItem("fnapi.base", base);
  }, [base]);
  return [base, setBase] as const;
}

/** Clave y email persistidos en localStorage tras el alta en /v1/keys. */
export function useStoredApiKey() {
  const [apiKey, setApiKeyState] = useState(() => localStorage.getItem("fnapi.apiKey"));
  const [email, setEmailState] = useState(() => localStorage.getItem("fnapi.email"));

  function setApiKey(key: string | null, forEmail?: string | null) {
    setApiKeyState(key);
    if (key) localStorage.setItem("fnapi.apiKey", key);
    else localStorage.removeItem("fnapi.apiKey");

    if (forEmail !== undefined) {
      setEmailState(forEmail);
      if (forEmail) localStorage.setItem("fnapi.email", forEmail);
      else localStorage.removeItem("fnapi.email");
    }
  }

  return { apiKey, email, setApiKey };
}

export async function issueApiKey(base: string, email: string, name?: string): Promise<IssuedKey> {
  const res = await fetch(`${base}/v1/keys`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, name: name || undefined }),
  });
  const data = await res.json();
  if (!res.ok) {
    throw new Error(data?.error ?? `HTTP ${res.status}`);
  }
  return data as IssuedKey;
}

export async function fetchKeyInfo(base: string, apiKey: string): Promise<KeyInfo> {
  const res = await fetch(`${base}/v1/keys/me`, { headers: { "X-Api-Key": apiKey } });
  if (res.status === 401) {
    throw new Error("unauthorized");
  }
  if (!res.ok) {
    throw new Error(`HTTP ${res.status}`);
  }
  return (await res.json()) as KeyInfo;
}
