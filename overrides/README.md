# Overrides manuales de `unreleased`

`unreleased-overrides.json` corrige a mano los casos donde la heurística automática
(peso de drop en `DT_VariantWeights[_<Season>]`, ver `docs/arquitectura.md`) no puede
saber la verdad porque no está en ningún dato del pak — solo se confirma jugando.

**No es una dependencia externa en runtime.** El ingest no consulta ninguna web: este
archivo vive en el repo y lo editás vos cuando confirmás algo en el juego. Si nunca lo
tocás, el ingest sigue funcionando igual con la heurística automática sola.

## Formato

```json
{
  "<id del sprite>": {
    "unreleased": false,
    "note": "por qué se corrigió, y cuándo se confirmó"
  }
}
```

## Ejemplo real (parche 42.10)

Los 15 archivos `ESD_*_Variant_LootHacker` de la temporada Override ya existen en el juego,
pero por ahora solo el de Crown está realmente activo (confirmado jugando, no por archivos).
El resto la heurística los deja bien como `unreleased: true`. El día que actives el de otro
personaje, agregás una entrada así:

```json
{
  "crown_hacker": {
    "unreleased": false,
    "note": "Confirmado en el juego el 2026-09-10: el Cheat Code de Loot Hacker ya lo activa para Crown."
  }
}
```

## Cuándo usarlo

- Cuando jugás y ves que algo listado como `unreleased: true` en realidad **sí** se puede conseguir.
- Cuando algo que el ingest marca como liberado en realidad todavía **no** sale (poco común, pero
  puede pasar si Epic sube el archivo antes de activarlo del todo).

No hace falta tocarlo por cada parche — solo cuando encontrás un caso puntual que la heurística
automática no puede resolver sola.
