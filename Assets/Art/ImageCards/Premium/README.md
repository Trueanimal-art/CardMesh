# Premium ImageCards

This folder stores source sheets for the premium ImageCard set. Runtime cards use the
individual transparent sprites in `Assets/Resources/ImageCards/Premium`.

Style rules:
- soft 3D casual puzzle object icons;
- one centered object per card;
- transparent background;
- no text, emoji, UI badges, screenshots, or black backplates;
- consistent outline, lighting, saturation, and object scale.

Generation workflow:
1. Generate a 5x5 sprite sheet on a flat `#ff00ff` chroma-key background.
2. Remove chroma key with `remove_chroma_key.py` using soft matte and despill.
3. Split into 512x512 transparent PNGs.
4. Trim transparent padding around every sprite, leaving only a small safety margin.
5. Name each sprite exactly by its `cardId`.

Only cards with an approved PNG in `Resources/ImageCards/Premium` are eligible to
become ImageCards.

Image Goals are category-level. A gold card can become an Image Goal only when
every regular card in that category has an approved premium PNG. Partial category
mixing is not allowed.
