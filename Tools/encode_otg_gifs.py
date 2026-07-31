from pathlib import Path
from PIL import Image

ROOT = Path("/Users/vnm/Documents/CardMesh/Artifacts/OneTapGarageSplash/PrototypeRound5")
FPS = 30

for variant in ("A", "B"):
    frame_dir = ROOT / f"Prototype_{variant}_frames"
    frames = [Image.open(p).convert("P", palette=Image.Palette.ADAPTIVE, colors=256) for p in sorted(frame_dir.glob("frame_*.png"))]
    output = ROOT / f"Prototype_{variant}.gif"
    frames[0].save(
        output,
        save_all=True,
        append_images=frames[1:],
        duration=round(1000 / FPS),
        loop=0,
        disposal=2,
        optimize=False,
    )
    print(f"Encoded {output.name}: {len(frames)} frames")
