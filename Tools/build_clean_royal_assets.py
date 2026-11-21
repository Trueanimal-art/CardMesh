from pathlib import Path
import subprocess
import tempfile

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Tools" / "CleanRoyalGenerated"
OUT = ROOT / "Assets" / "Resources" / "UI" / "CleanRoyal"


SPECS = {
    "daily_atlas_source.png": [
        ("daily_reward_card", (105, 8, 505, 555), (370, 500)),
        ("daily_reward_card_wide", (800, 8, 1435, 555), (410, 500)),
        ("daily_reward_hero", (8, 575, 1210, 945), (1580, 540)),
        ("daily_reward_card_available", (1210, 540, 1525, 990), (370, 500)),
    ],
    "royal_atlas_source.png": [
        ("royal_stage_idle", (205, 8, 685, 440), (460, 500)),
        ("royal_stage_active", (845, 8, 1325, 440), (460, 500)),
        ("royal_goal_panel", (5, 445, 1530, 670), (1440, 164)),
        ("royal_progress_track", (150, 675, 1390, 845), (672, 152)),
        ("royal_progress_fill", (205, 860, 1335, 990), (600, 88)),
    ],
    "collection_atlas_source.png": [
        ("collection_reward_card", (20, 15, 430, 395), (456, 444)),
        ("collection_progress_panel", (415, 105, 1515, 295), (1520, 300)),
        ("collection_info_panel", (20, 390, 1515, 670), (1520, 420)),
    ],
}


def fit_alpha(image: Image.Image, target: tuple[int, int]) -> Image.Image:
    with tempfile.TemporaryDirectory() as temp_dir:
        source_path = Path(temp_dir) / "source.png"
        alpha_path = Path(temp_dir) / "alpha.png"
        image.save(source_path)
        subprocess.run([
            "python3",
            str(Path.home() / ".codex/skills/.system/imagegen/scripts/remove_chroma_key.py"),
            "--input", str(source_path), "--out", str(alpha_path),
            "--auto-key", "border", "--soft-matte",
            "--transparent-threshold", "12", "--opaque-threshold", "220", "--despill",
        ], check=True, stdout=subprocess.DEVNULL)
        image = Image.open(alpha_path).convert("RGBA")
    alpha = image.getchannel("A")
    bbox = alpha.getbbox()
    if bbox:
        image = image.crop(bbox)
    tw, th = target
    scale = min((tw - 4) / image.width, (th - 4) / image.height)
    resized = image.resize((max(1, round(image.width * scale)), max(1, round(image.height * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", target, (0, 0, 0, 0))
    canvas.alpha_composite(resized, ((tw - resized.width) // 2, (th - resized.height) // 2))
    return canvas


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for source_name, assets in SPECS.items():
        source = Image.open(SRC / source_name).convert("RGB")
        for name, crop, target in assets:
            asset = fit_alpha(source.crop(crop), target)
            asset.save(OUT / f"{name}.png", optimize=True)
            print(f"wrote {name}.png {asset.size}")


if __name__ == "__main__":
    main()
