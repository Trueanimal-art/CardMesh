#!/usr/bin/env python3
from __future__ import annotations

import json
import shutil
from pathlib import Path

from PIL import Image


PROJECT_ROOT = Path(__file__).resolve().parents[1]
GEN_ROOT = Path("/Users/vnm/.codex/generated_images/019ea30d-50b5-76e2-ad38-bd4734f1b847")
RUNTIME_DIR = PROJECT_ROOT / "Assets/Resources/ImageCards/Premium"
OUT_ROOT = PROJECT_ROOT / "Tools/ImageCardAudit/ai_atlas_repair"
SLICED_DIR = OUT_ROOT / "sliced"
SOURCE_ATLAS = OUT_ROOT / "bad_cards_ai_repair_source_atlas.png"
PREVIEW_DIR = OUT_ROOT / "runtime_preview_pages"
CANVAS = 240
TARGET_MAX = 154


BAD_IDS = [
    item["id"]
    for item in json.loads((PROJECT_ROOT / "Tools/ImageCardAudit/bad_cards_manifest.json").read_text())["cards"]
]


SHEETS: list[dict[str, object]] = [
    {
        "prefix": "ig_072e4a46d32ca298016a377bda25b081",
        "cols": 4,
        "ids": [
            "body_hand", "body_foot", "body_eye", "body_ear",
            "body_heart", "body_back", "body_finger", "body_smile",
        ],
    },
    {
        "prefix": "ig_014fbee93953aecb016a377c585c248",
        "cols": 4,
        "ids": [
            "work_task", "work_salary", "work_shift", "work_manager",
            "work_report", "work_call", "work_desk", "work_break",
        ],
    },
    {
        "prefix": "ig_0020b8f35800d196016a377ce87bd",
        "cols": 4,
        "ids": [
            "technology_robot", "technology_sensor", "technology_chip", "technology_code",
            "technology_drone", "technology_laser", "technology_server", "technology_battery",
        ],
    },
    {
        "prefix": "ig_0a8d1496d379f4dd016a377f53baa",
        "cols": 4,
        "ids": [
            "dessert_cake", "dessert_cookie", "dessert_candy", "dessert_pie",
            "dessert_cream", "dessert_icecream", "dessert_waffle", "dessert_honey",
        ],
    },
    {
        "prefix": "ig_01c29043572c5863016a377fcfe3",
        "cols": 4,
        "ids": [
            "drinks_juice", "drinks_tea", "drinks_coffee", "drinks_water",
            "drinks_cocoa", "drinks_milk", "drinks_lemonade", "drinks_smoothie",
        ],
    },
    {
        "prefix": "ig_021b4c9a46dfd2a8016a37804763",
        "cols": 4,
        "ids": [
            "farm_barn", "farm_tractor", "farm_field", "farm_hay",
            "farm_cow", "farm_chicken", "farm_grain", "farm_fence",
        ],
    },
    {
        "prefix": "ig_0d9d5c349fd4958d016a265c65e34c",
        "cols": 5,
        "ids": [
            "pets_dog", "pets_cat", "pets_bowl", "pets_leash", "pets_paw",
            "sea_fish", "sea_shell", "sea_sail", "space_rocket", "space_planet",
            "space_moon", "space_star", "clothes_shirt", "clothes_coat", "clothes_hat",
            "clothes_dress", "clothes_jeans", "shoes_sneaker", "school_notebook", "office_pen",
            "office_folder", "office_printer", "phone_camera", "holidays_gift", "holidays_balloon",
        ],
    },
    {
        "prefix": "ig_0d9d5c349fd4958d016a26763dc4c0",
        "cols": 5,
        "ids": [
            "space_orbit", "space_comet", "space_astronaut", "space_galaxy", "weather_wind",
            "weather_storm", "weather_fog", "weather_frost", "weather_heat", "toys_ball",
            "pets_tail", "pets_vet", "transport_bus", "transport_taxi", "transport_wheel",
            "transport_driver", "transport_route", "transport_station", "transport_road", "transport_stop",
            "sea_wave", "sea_beach", "sea_island", "sea_lighthouse", "space_satellite",
        ],
    },
    {
        "prefix": "ig_0d9d5c349fd4958d016a265199c328",
        "cols": 5,
        "ids": [
            "food_fruit", "breakfast_banana", "food_bread", "drinks_tea", "office_coffee",
            "food_plate", "food_soup", "drinks_milk", "breakfast_egg", "dessert_cake",
            "time_clock", "time_alarm", "travel_train", "travel_plane", "sport_ball",
            "nature_tree", "nature_mountain", "weather_sun", "weather_rain", "weather_snow",
            "home_sofa", "home_lamp", "health_rest", "home_key", "kitchen_pan",
        ],
    },
    {
        "prefix": "ig_04e82a0f7efd356e016a378e4ee0",
        "cols": 4,
        "ids": [
            "phone_call", "phone_screen", "phone_case", "phone_app",
            "phone_charge", "phone_contact", "phone_message", "breakfast_butter",
        ],
    },
    {
        "prefix": "ig_01977f7cf5e4d24a016a378effb3",
        "cols": 4,
        "ids": [
            "photo_flash", "photo_album", "photo_portrait", "photo_frame",
            "photo_shot", "photo_focus", "photo_studio", "sea_beach",
        ],
    },
    {
        "prefix": "ig_04ff84dff1aba46f016a378bb898",
        "cols": 4,
        "ids": [
            "cinema_film", "cinema_actor", "cinema_scene", "cinema_screen",
            "cinema_ticket", "cinema_popcorn", "cinema_trailer", "breakfast_porridge",
        ],
    },
    {
        "prefix": "ig_0248d5b90fa739fe016a378da91f3",
        "cols": 4,
        "ids": [
            "shoes_boot", "shoes_sandal", "shoes_lace", "shoes_heel",
            "shoes_sole", "shoes_pair", "shoes_size", "breakfast_yogurt",
        ],
    },
    {
        "prefix": "ig_0c4ad301ae668049016a377ed8b13",
        "cols": 4,
        "ids": [
            "colors_red", "colors_blue", "colors_green", "colors_yellow",
            "colors_white", "colors_black", "colors_orange", "colors_pink",
        ],
    },
    {
        "prefix": "ig_0da0ec45e03591fc016a377ac0d5",
        "cols": 4,
        "ids": [
            "family_mother", "family_father", "family_child", "family_sister",
            "family_brother", "family_dinner", "family_home", "family_album",
        ],
    },
    {
        "prefix": "ig_0fd02388be44a6f0016a3781d8e",
        "cols": 4,
        "ids": [
            "insects_bee", "insects_ant", "insects_butterfly", "insects_beetle",
            "insects_wing", "insects_hive", "insects_cocoon", "insects_ladybug",
        ],
    },
    {
        "prefix": "ig_05b939b1a2ee18b8016a37842236",
        "cols": 4,
        "ids": [
            "camping_tent", "camping_fire", "camping_map", "camping_compass",
            "camping_backpack", "camping_lantern", "camping_trail", "camping_mug",
        ],
    },
    {
        "prefix": "ig_02b18c697a1b093a016a37838f09",
        "cols": 4,
        "ids": [
            "hobbies_drawing", "hobbies_knitting", "hobbies_chess", "hobbies_puzzle",
            "hobbies_dance", "hobbies_baking", "hobbies_reading", "hobbies_music",
        ],
    },
    {
        "prefix": "ig_05c4f533f068351a016a3781372c",
        "cols": 4,
        "ids": [
            "flowers_rose", "flowers_tulip", "flowers_lily", "flowers_peony",
            "flowers_bouquet", "flowers_petal", "flowers_stem", "flowers_vase",
        ],
    },
    {
        "prefix": "ig_01f7d0b44ad4663c016a3780c052",
        "cols": 4,
        "ids": [
            "forest_pine", "forest_mushroom", "forest_fox", "forest_owl",
            "forest_fern", "forest_stump", "forest_berry", "forest_path",
        ],
    },
    {
        "prefix": "ig_0ed8ec7b8441bf6d016a37827ff3",
        "cols": 4,
        "ids": [
            "emotions_joy", "emotions_calm", "emotions_surprise", "emotions_interest",
            "emotions_hope", "emotions_trust", "emotions_pride", "emotions_warmth",
        ],
    },
    {
        "prefix": "ig_0881c8f9a3cc093a016a37830886",
        "cols": 4,
        "ids": [
            "friendship_friend", "friendship_chat", "friendship_walk", "friendship_smile",
            "friendship_help", "friendship_team", "friendship_secret", "friendship_memory",
        ],
    },
    {
        "prefix": "ig_0787bd2f5f98b789016a3784b4ce",
        "cols": 4,
        "ids": [
            "toys_doll", "toys_blocks", "toys_train", "toys_puzzle",
            "toys_ball", "toys_robot", "toys_kite", "toys_car",
        ],
    },
    {
        "prefix": "ig_0fd8c2282c6d531f016a37855381",
        "cols": 4,
        "ids": [
            "tools_hammer", "tools_saw", "tools_drill", "tools_wrench",
            "tools_nail", "tools_pliers", "tools_ruler", "tools_level",
        ],
    },
    {
        "prefix": "ig_081ce5c992e56044016a37860fc1",
        "cols": 4,
        "ids": [
            "cup_cup", "cup_mug", "cup_handle", "cup_saucer",
            "cup_thermos", "clothes_scarf", "clothes_glove", "clothes_belt",
        ],
    },
    {
        "prefix": "ig_05d9e0f576275a1e016a3789da20",
        "cols": 4,
        "ids": [
            "school_class", "school_teacher", "school_desk", "school_board",
            "school_lesson", "school_bell", "winter_ski", "winter_ice",
        ],
    },
    {
        "prefix": "ig_0eeb5a5112955501016a37868415",
        "cols": 4,
        "ids": [
            "time_time", "time_minutes", "time_day", "time_night",
            "time_week", "time_hour", "office_chair", "office_paper",
        ],
    },
    {
        "prefix": "ig_07ebf0f685d9980e016a3787cdd1",
        "cols": 4,
        "ids": [
            "sport_coach", "sport_team", "sport_match", "sport_field",
            "sport_medal", "sport_race", "sport_player", "holidays_candle",
        ],
    },
    {
        "prefix": "ig_04ff7edc753bdf48016a3778b9d45",
        "cols": 4,
        "ids": [
            "business_product", "business_client", "business_brand", "business_sale",
            "business_team", "business_office", "business_contract", "business_market",
        ],
    },
    {
        "prefix": "ig_0a7cb962be7fd0cf016a377d87aab",
        "cols": 4,
        "ids": [
            "internet_site", "internet_link", "internet_search", "internet_mail",
            "internet_video", "internet_blog", "internet_cloud", "internet_chat",
        ],
    },
    {
        "prefix": "ig_0f7b5c23667d9fc7016a37892e28b4",
        "cols": 4,
        "ids": [
            "kitchen_stove", "kitchen_spoon", "kitchen_fork", "kitchen_knife",
            "kitchen_kettle", "kitchen_fridge", "kitchen_oven", "holidays_card",
        ],
    },
    {
        "prefix": "ig_0e76c37b37fa7074016a378c571d44",
        "cols": 4,
        "ids": [
            "garden_bench", "garden_shovel", "garden_seed", "garden_soil",
            "garden_bush", "garden_hose", "garden_path", "breakfast_jam",
        ],
    },
    {
        "prefix": "ig_030508ee5104b7ba016a378d036a8",
        "cols": 4,
        "ids": [
            "shapes_circle", "shapes_square", "shapes_triangle", "shapes_line",
            "shapes_oval", "shapes_cube", "shapes_ring", "breakfast_cereal",
        ],
    },
    {
        "prefix": "ig_029b802dfbba7c5a016a3779e9f8",
        "cols": 4,
        "ids": [
            "city_street", "city_park", "city_bridge", "city_square",
            "city_subway", "city_cafe", "city_tower", "city_traffic",
        ],
    },
    {
        "prefix": "ig_085a102e409deced016a377b47af",
        "cols": 4,
        "ids": [
            "health_doctor", "health_clinic", "health_pill", "health_rest",
            "health_water", "health_sleep", "health_pulse", "health_care",
        ],
    },
    {
        "prefix": "ig_0978d2ab219a1da9016a37795bf8",
        "cols": 4,
        "ids": [
            "music_song", "music_rhythm", "music_melody", "music_guitar",
            "music_piano", "music_note", "music_concert", "music_band",
        ],
    },
    {
        "prefix": "ig_019e03a0911ba53f016a378870570",
        "cols": 4,
        "ids": [
            "nature_river", "nature_stone", "nature_leaf", "nature_cloud",
            "nature_meadow", "nature_sky", "holidays_guest", "holidays_music",
        ],
    },
    {
        "prefix": "ig_096f2593f9733cab016a378a851ae",
        "cols": 4,
        "ids": [
            "books_cover", "books_reader", "books_shelf", "books_author",
            "books_chapter", "winter_pine", "winter_cocoa", "winter_sleigh",
        ],
    },
    {
        "prefix": "ig_076a8915e085ac26016a378fb5ff",
        "cols": 3,
        "ids": [
            "summer_hat", "summer_lake", "summer_berry",
            "summer_picnic", "summer_fan", "summer_sandals",
        ],
    },
    {
        "prefix": "ig_000de4fb8668a939016a378b218c",
        "cols": 4,
        "ids": [
            "art_paint", "art_canvas", "art_frame", "art_sketch",
            "art_gallery", "art_statue", "art_color", "breakfast_toast",
        ],
    },
    {
        "prefix": "ig_08f30a565bb18abb016a37872926",
        "cols": 4,
        "ids": [
            "travel_ticket", "travel_hotel", "travel_route", "travel_suitcase",
            "travel_map", "travel_passport", "office_badge", "office_screen",
        ],
    },
]


def resolve(prefix: str) -> Path:
    matches = sorted(GEN_ROOT.glob(f"{prefix}*.png"))
    if len(matches) != 1:
        raise RuntimeError(f"{prefix}: expected one source, got {len(matches)}")
    return matches[0]


def key_mask(pixel: tuple[int, int, int, int]) -> bool:
    r, g, b, a = pixel
    if a == 0:
        return True
    if g >= 145 and r <= 130 and b <= 130 and g - max(r, b) >= 35:
        return True
    if r >= 145 and b >= 145 and g <= 130 and min(r, b) - g >= 35:
        return True
    return False


def remove_key(image: Image.Image) -> Image.Image:
    image = image.convert("RGBA")
    pix = image.load()
    w, h = image.size
    for y in range(h):
        for x in range(w):
            if key_mask(pix[x, y]):
                pix[x, y] = (0, 0, 0, 0)
    remove_edge_bleed(image)
    return image


def remove_edge_bleed(image: Image.Image) -> None:
    alpha = image.getchannel("A")
    pix = image.load()
    alpha_pix = alpha.load()
    w, h = image.size
    seen = bytearray(w * h)
    components: list[tuple[int, bool, list[int]]] = []

    for start_y in range(h):
        for start_x in range(w):
            start = start_y * w + start_x
            if seen[start] or alpha_pix[start_x, start_y] == 0:
                continue
            stack = [start]
            seen[start] = 1
            pixels: list[int] = []
            touches_edge = False
            while stack:
                pos = stack.pop()
                x = pos % w
                y = pos // w
                pixels.append(pos)
                if x == 0 or y == 0 or x == w - 1 or y == h - 1:
                    touches_edge = True
                if x > 0:
                    nxt = pos - 1
                    if not seen[nxt] and alpha_pix[x - 1, y] > 0:
                        seen[nxt] = 1
                        stack.append(nxt)
                if x + 1 < w:
                    nxt = pos + 1
                    if not seen[nxt] and alpha_pix[x + 1, y] > 0:
                        seen[nxt] = 1
                        stack.append(nxt)
                if y > 0:
                    nxt = pos - w
                    if not seen[nxt] and alpha_pix[x, y - 1] > 0:
                        seen[nxt] = 1
                        stack.append(nxt)
                if y + 1 < h:
                    nxt = pos + w
                    if not seen[nxt] and alpha_pix[x, y + 1] > 0:
                        seen[nxt] = 1
                        stack.append(nxt)
            components.append((len(pixels), touches_edge, pixels))

    if not components:
        return
    largest_area = max(area for area, _, _ in components)
    for area, touches_edge, pixels in components:
        if not touches_edge:
            continue
        if area >= largest_area * 0.4 and area >= 5000:
            continue
        for pos in pixels:
            pix[pos % w, pos // w] = (0, 0, 0, 0)


def trim_and_fit(image: Image.Image) -> Image.Image:
    image = remove_key(image)
    bbox = image.getchannel("A").getbbox()
    if bbox is None:
        raise RuntimeError("empty image after chroma key")
    cropped = image.crop(bbox)
    w, h = cropped.size
    scale = min(TARGET_MAX / max(w, h), 1.0)
    resized = cropped.resize((max(1, round(w * scale)), max(1, round(h * scale))), Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", (CANVAS, CANVAS), (0, 0, 0, 0))
    canvas.alpha_composite(resized, ((CANVAS - resized.width) // 2, (CANVAS - resized.height) // 2))
    return canvas


def extract_sheet(sheet: dict[str, object], out: dict[str, Image.Image], provenance: dict[str, str]) -> None:
    source = resolve(str(sheet["prefix"]))
    image = Image.open(source).convert("RGBA")
    cols = int(sheet["cols"])
    ids = list(sheet["ids"])
    rows = (len(ids) + cols - 1) // cols
    cell_w = image.width / cols
    cell_h = image.height / rows
    for index, card_id in enumerate(ids):
        if card_id not in BAD_IDS:
            continue
        col = index % cols
        row = index // cols
        box = (
            round(col * cell_w),
            round(row * cell_h),
            round((col + 1) * cell_w),
            round((row + 1) * cell_h),
        )
        out[card_id] = trim_and_fit(image.crop(box))
        provenance[card_id] = source.name


def copy_meta(card_id: str) -> None:
    meta = RUNTIME_DIR / f"{card_id}.png.meta"
    if meta.exists():
        shutil.copy2(meta, SLICED_DIR / meta.name)


def build_source_atlas(images: dict[str, Image.Image]) -> None:
    ordered = [card_id for card_id in BAD_IDS if card_id in images]
    cols = 10
    rows = (len(ordered) + cols - 1) // cols
    atlas = Image.new("RGBA", (cols * CANVAS, rows * CANVAS), (0, 0, 0, 0))
    for index, card_id in enumerate(ordered):
        atlas.alpha_composite(images[card_id], ((index % cols) * CANVAS, (index // cols) * CANVAS))
    SOURCE_ATLAS.parent.mkdir(parents=True, exist_ok=True)
    atlas.save(SOURCE_ATLAS)


def load_cards() -> list[dict[str, str]]:
    data = json.loads((PROJECT_ROOT / "Assets/Resources/Data/cards.json").read_text(encoding="utf-8"))
    return [card for category in data["goldCards"] for card in category["cards"]]


def build_preview(root: Path) -> None:
    cards = load_cards()
    PREVIEW_DIR.mkdir(parents=True, exist_ok=True)
    cell_w, cell_h = 132, 166
    cols = 6
    font = None
    for page in range(14):
        subset = cards[page * 30 : (page + 1) * 30]
        rows = 5 if page < 13 else 2
        sheet = Image.new("RGB", (cols * cell_w, rows * cell_h), (7, 28, 24))
        for i, card in enumerate(subset):
            card_id = card["id"]
            path = root / f"{card_id}.png"
            if not path.exists():
                continue
            x = (i % cols) * cell_w
            y = (i // cols) * cell_h
            tile = Image.new("RGB", (110, 146), (244, 241, 231))
            icon = Image.open(path).convert("RGBA")
            preview_icon = icon.resize((96, 96), Image.Resampling.LANCZOS)
            tile.paste(preview_icon, ((110 - preview_icon.width) // 2, 34), preview_icon)
            sheet.paste(tile, (x + 11, y + 8))
        sheet.save(PREVIEW_DIR / f"page_{page + 1:02d}.png")


def main() -> None:
    images: dict[str, Image.Image] = {}
    provenance: dict[str, str] = {}
    for sheet in SHEETS:
        extract_sheet(sheet, images, provenance)

    SLICED_DIR.mkdir(parents=True, exist_ok=True)
    for card_id, image in images.items():
        image.save(SLICED_DIR / f"{card_id}.png")
        copy_meta(card_id)

    build_source_atlas(images)
    build_preview(SLICED_DIR)
    missing = [card_id for card_id in BAD_IDS if card_id not in images]
    (OUT_ROOT / "repair_report.json").write_text(
        json.dumps({"processed": sorted(images), "missing": missing, "provenance": provenance}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(f"processed={len(images)} missing={len(missing)}")
    if missing:
        print("\n".join(missing))


if __name__ == "__main__":
    main()
