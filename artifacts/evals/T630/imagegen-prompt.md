# T630 imagegen prompt

- Mode: generate
- Purpose: fill the two actor silhouettes absent from the supplied PSD; prototype-only.
- Output handling: remove chroma key locally, split into two PNGs, validate alpha, retain this prompt and output hashes.

## Prompt

Create one wide horizontal 2D game-character sprite sheet on an absolutely flat, perfectly uniform solid chroma-key green background (#00FF00). Exactly two separate full-body enemy characters, one centered in the left half and one centered in the right half, with a wide empty green gap between them. They must not overlap or touch. Both face three-quarters toward screen-left. Match a hand-inked Chinese folk-fantasy action-game style: expressive cartoon proportions, thick charcoal-black contour lines, textured brush coloring, muted teal/ochre/brown palette, aged yellow talisman paper with restrained red accents, readable silhouettes, polished production concept art.

Left character: “Soul Puppet”, a creepy but readable jiangshi-inspired straw-and-paper effigy, lanky dangling wooden limbs tied with red cord, cracked pale paper mask, a forehead sealing talisman, small contained teal ghost flame held close to its body. Full body including feet is visible.

Right character: “Tomb Armor King”, a much larger hulking ancient tomb guardian, broad terracotta-and-stone body inside layered Chinese lamellar armor, weathered bronze fittings, broken crown crest, short red cloth accents, huge armored fists, imposing boss silhouette. Full body including feet is visible.

Asset constraints: no text, no labels, no border, no frame, no floor, no cast shadows, no scenery, no gradients in the green background, no extra characters, no detached props, no semi-transparent aura extending away from the silhouettes. Keep all character pixels opaque with crisp anti-aliased edges suitable for chroma-key removal. Leave generous green margin on every side.
