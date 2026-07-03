#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import statistics
import struct
import sys
import uuid
import zlib
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass, replace
from datetime import datetime, timezone
from pathlib import Path
from typing import Iterable

sys.path.insert(0, str(Path(__file__).resolve().parent))
from validate_solvability import GeneratedLevel, Layout, SolveResult, SolvabilityValidator  # noqa: E402


GENERATOR_VERSION = "solitaire-content-pipeline-1.0"
SOLVER_VERSION = "validate_solvability.py-1.0"
DEFAULT_LEVELS = 1000
DEFAULT_MAX_STATES = 120_000
DEFAULT_BUDGET_MAX_STATES = 5_000
DEFAULT_MAX_ATTEMPTS = 20
DEFAULT_REGEN_ATTEMPTS = 40
DEFAULT_PLUS10_TARGET_PERCENT = 20
DEFAULT_PLUS10_TARGET_START_LEVEL = 1
PROJECT_IMAGE_WIDTH_RANGE = (150, 280)
PROJECT_IMAGE_HEIGHT_RANGE = (190, 300)


PACK_BY_CATEGORY = {
    "pets": "AnimalsPack",
    "farm": "AnimalsPack",
    "forest": "AnimalsPack",
    "insects": "AnimalsPack",
    "food": "FoodPack",
    "breakfast": "FoodPack",
    "dessert": "FoodPack",
    "drinks": "FoodPack",
    "cup": "FoodPack",
    "kitchen": "FoodPack",
    "transport": "TransportPack",
    "travel": "TransportPack",
    "city": "TransportPack",
    "nature": "NaturePack",
    "weather": "NaturePack",
    "sea": "NaturePack",
    "garden": "NaturePack",
    "flowers": "NaturePack",
    "winter": "NaturePack",
    "summer": "NaturePack",
    "tools": "ToolsPack",
    "school": "ToolsPack",
    "books": "ToolsPack",
    "office": "ToolsPack",
    "phone": "ToolsPack",
    "photo": "ToolsPack",
    "art": "ToolsPack",
    "cinema": "ToolsPack",
    "space": "SpacePack",
    "shapes": "FantasyPack",
}

TRICKY_CATEGORIES = {
    "business",
    "internet",
    "photo",
    "health",
    "body",
    "work",
}

PACK_ORDER = [
    "BasePack",
    "AnimalsPack",
    "FoodPack",
    "TransportPack",
    "NaturePack",
    "ToolsPack",
    "FantasyPack",
    "SpacePack",
    "PremiumPack",
    "TrickyPack",
]

EARLY_CATEGORIES = {
    "cup",
    "food",
    "time",
    "home",
    "kitchen",
    "family",
    "pets",
    "garden",
    "colors",
    "emotions",
    "friendship",
    "breakfast",
    "dessert",
    "drinks",
    "farm",
    "toys",
}

MID_CATEGORIES = EARLY_CATEGORIES | {
    "travel",
    "music",
    "sport",
    "nature",
    "weather",
    "city",
    "school",
    "books",
    "art",
    "cinema",
    "transport",
    "sea",
    "clothes",
    "shoes",
    "winter",
    "summer",
    "forest",
    "flowers",
    "insects",
    "hobbies",
    "camping",
    "tools",
}

LATE_CATEGORIES = MID_CATEGORIES | {
    "space",
    "office",
    "technology",
    "business",
    "internet",
    "phone",
    "photo",
    "shapes",
    "health",
    "body",
    "work",
    "holidays",
}


@dataclass(frozen=True)
class PngInfo:
    path: str
    width: int
    height: int
    bit_depth: int
    color_type: int
    has_alpha_channel: bool
    transparent_pixels: int
    opaque_edge_pixels: int
    total_pixels: int

    @property
    def has_transparency(self) -> bool:
        return self.transparent_pixels > 0

    @property
    def opaque_edge_ratio(self) -> float:
        return self.opaque_edge_pixels / max(1, (self.width * 2) + (self.height * 2) - 4)


def read_cards(root: Path) -> dict:
    return json.loads((root / "Assets/Resources/Data/cards.json").read_text(encoding="utf-8"))


def parse_png_info(path: Path) -> PngInfo:
    data = path.read_bytes()
    if data[:8] != b"\x89PNG\r\n\x1a\n":
        raise ValueError(f"{path} is not a PNG")

    offset = 8
    ihdr = None
    trns = False
    idat_parts: list[bytes] = []
    while offset < len(data):
        length = struct.unpack(">I", data[offset : offset + 4])[0]
        chunk_type = data[offset + 4 : offset + 8]
        chunk_data = data[offset + 8 : offset + 8 + length]
        offset += length + 12
        if chunk_type == b"IHDR":
            ihdr = chunk_data
        elif chunk_type == b"tRNS":
            trns = True
        elif chunk_type == b"IDAT":
            idat_parts.append(chunk_data)
        elif chunk_type == b"IEND":
            break

    if ihdr is None:
        raise ValueError(f"{path} has no IHDR")

    width, height, bit_depth, color_type, _, _, _ = struct.unpack(">IIBBBBB", ihdr)
    has_alpha_channel = color_type in (4, 6) or trns
    transparent_pixels = 0
    opaque_edge_pixels = 0
    total_pixels = width * height

    if bit_depth == 8 and color_type == 6 and idat_parts:
        raw = zlib.decompress(b"".join(idat_parts))
        stride = width * 4
        previous = bytearray(stride)
        cursor = 0
        rows: list[bytearray] = []
        for _ in range(height):
            filter_type = raw[cursor]
            cursor += 1
            row = bytearray(raw[cursor : cursor + stride])
            cursor += stride
            unfilter_scanline(row, previous, 4, filter_type)
            rows.append(row)
            previous = row

        for y, row in enumerate(rows):
            for x in range(width):
                alpha = row[(x * 4) + 3]
                if alpha < 250:
                    transparent_pixels += 1
                elif x == 0 or y == 0 or x == width - 1 or y == height - 1:
                    opaque_edge_pixels += 1

    return PngInfo(
        path=path.as_posix(),
        width=width,
        height=height,
        bit_depth=bit_depth,
        color_type=color_type,
        has_alpha_channel=has_alpha_channel,
        transparent_pixels=transparent_pixels,
        opaque_edge_pixels=opaque_edge_pixels,
        total_pixels=total_pixels,
    )


def unfilter_scanline(row: bytearray, previous: bytearray, bpp: int, filter_type: int) -> None:
    for index in range(len(row)):
        left = row[index - bpp] if index >= bpp else 0
        up = previous[index]
        up_left = previous[index - bpp] if index >= bpp else 0
        if filter_type == 0:
            value = row[index]
        elif filter_type == 1:
            value = row[index] + left
        elif filter_type == 2:
            value = row[index] + up
        elif filter_type == 3:
            value = row[index] + ((left + up) // 2)
        elif filter_type == 4:
            value = row[index] + paeth(left, up, up_left)
        else:
            raise ValueError(f"Unsupported PNG filter {filter_type}")
        row[index] = value & 0xFF


def paeth(left: int, up: int, up_left: int) -> int:
    estimate = left + up - up_left
    left_distance = abs(estimate - left)
    up_distance = abs(estimate - up)
    up_left_distance = abs(estimate - up_left)
    if left_distance <= up_distance and left_distance <= up_left_distance:
        return left
    if up_distance <= up_left_distance:
        return up
    return up_left


def collect_png_infos(root: Path) -> dict[str, PngInfo]:
    image_root = root / "Assets/Resources/ImageCards/Premium"
    return {path.stem: parse_png_info(path) for path in sorted(image_root.glob("*.png"))}


def category_allowed_level(category_id: str, has_image: bool) -> int:
    if category_id in TRICKY_CATEGORIES:
        return 801
    pack = PACK_BY_CATEGORY.get(category_id, "BasePack")
    if pack in {"BasePack", "AnimalsPack", "FoodPack"}:
        return 1
    if pack in {"TransportPack", "NaturePack", "ToolsPack"}:
        return 51
    if pack in {"FantasyPack", "SpacePack"}:
        return 501
    return 201 if has_image else 1


def difficulty_for_allowed_level(level: int) -> str:
    if level <= 50:
        return "simple"
    if level <= 200:
        return "easy"
    if level <= 500:
        return "medium"
    if level <= 800:
        return "medium_hard"
    return "hard"


def pack_for_category(category_id: str) -> str:
    if category_id in TRICKY_CATEGORIES:
        return "TrickyPack"
    return PACK_BY_CATEGORY.get(category_id, "BasePack")


def build_content_metadata(root: Path, validator: SolvabilityValidator) -> tuple[dict, dict, dict]:
    cards_payload = read_cards(root)
    png_infos = collect_png_infos(root)
    card_ids = {
        card["id"]
        for gold in cards_payload["goldCards"]
        for card in gold["cards"]
    }

    image_cards: list[dict] = []
    text_cards: list[dict] = []
    matching: list[dict] = []
    pack_cards: dict[str, set[str]] = {pack: set() for pack in PACK_ORDER}
    validation_errors: list[str] = []

    for gold in cards_payload["goldCards"]:
        category_id = gold["id"]
        category_title = gold["title"]
        base_pack = pack_for_category(category_id)
        category_has_images = any(card["id"] in png_infos for card in gold["cards"])
        allowed = category_allowed_level(category_id, category_has_images)
        difficulty = difficulty_for_allowed_level(allowed)

        for card in gold["cards"]:
            card_id = card["id"]
            word = card["title"]
            text_cards.append(
                {
                    "id": card_id,
                    "word": word,
                    "category": category_id,
                    "categoryTitle": category_title,
                    "difficulty": difficulty,
                    "tags": [category_id, base_pack.replace("Pack", "").lower()],
                    "allowedFromLevel": allowed,
                    "type": "TextCard",
                }
            )
            pack_cards[base_pack].add(card_id)
            if category_id in EARLY_CATEGORIES:
                pack_cards["BasePack"].add(card_id)

            if card_id in png_infos:
                info = png_infos[card_id]
                errors = validate_png_info(card_id, info)
                validation_errors.extend(errors)
                image_cards.append(
                    {
                        "id": card_id,
                        "displayName": word,
                        "category": category_id,
                        "categoryTitle": category_title,
                        "rarity": "premium",
                        "difficulty": difficulty,
                        "tags": [category_id, "premium", base_pack.replace("Pack", "").lower()],
                        "imagePath": f"ImageCards/Premium/{card_id}",
                        "width": info.width,
                        "height": info.height,
                        "allowedFromLevel": allowed,
                        "isPremium": True,
                        "type": "ImageCard",
                        "validationStatus": "valid" if not errors else "needs_review",
                        "validationErrors": errors,
                    }
                )
                matching.append(
                    {
                        "imageCardId": card_id,
                        "textCardId": card_id,
                        "relationType": "exact",
                        "allowedFromLevel": allowed,
                        "difficulty": difficulty,
                    }
                )
                pack_cards["PremiumPack"].add(card_id)

    unused_png = sorted(set(png_infos) - card_ids)
    for card_id in unused_png:
        info = png_infos[card_id]
        category_id = card_id.split("_", 1)[0]
        image_cards.append(
            {
                "id": card_id,
                "displayName": card_id.replace("_", " ").title(),
                "category": category_id,
                "categoryTitle": category_id.title(),
                "rarity": "premium_unused",
                "difficulty": "medium",
                "tags": [category_id, "premium", "unused"],
                "imagePath": f"ImageCards/Premium/{card_id}",
                "width": info.width,
                "height": info.height,
                "allowedFromLevel": 501,
                "isPremium": True,
                "type": "ImageCard",
                "validationStatus": "unused",
                "validationErrors": validate_png_info(card_id, info),
            }
        )
        pack_cards["PremiumPack"].add(card_id)

    content_cards = {
        "schemaVersion": 1,
        "generatedAt": utc_now(),
        "projectImageStandard": {
            "source": "existing Assets/Resources/ImageCards/Premium PNGs",
            "widthRange": list(PROJECT_IMAGE_WIDTH_RANGE),
            "heightRange": list(PROJECT_IMAGE_HEIGHT_RANGE),
            "currentMinWidth": min(info.width for info in png_infos.values()) if png_infos else 0,
            "currentMaxWidth": max(info.width for info in png_infos.values()) if png_infos else 0,
            "currentMinHeight": min(info.height for info in png_infos.values()) if png_infos else 0,
            "currentMaxHeight": max(info.height for info in png_infos.values()) if png_infos else 0,
            "requiredAlpha": True,
            "objectCoverageTarget": "75-85% useful card image area",
        },
        "imageCards": image_cards,
        "textCards": text_cards,
        "matchingMetadata": matching,
        "validation": {
            "imageCardsTotal": len(image_cards),
            "textCardsTotal": len(text_cards),
            "matchingRows": len(matching),
            "unusedPremiumPng": unused_png,
            "errors": validation_errors,
            "status": "valid" if not validation_errors else "needs_review",
        },
    }

    packs = []
    for pack in PACK_ORDER:
        cards = sorted(pack_cards[pack])
        allowed_from = min((category_allowed_level(card.split("_", 1)[0], card in png_infos) for card in cards), default=1)
        max_allowed = max((category_allowed_level(card.split("_", 1)[0], card in png_infos) for card in cards), default=1)
        packs.append(
            {
                "id": pack,
                "allowedFromLevel": allowed_from,
                "difficultyRange": [difficulty_for_allowed_level(allowed_from), difficulty_for_allowed_level(max_allowed)],
                "category": pack.replace("Pack", "") or "Base",
                "cards": cards,
                "validationStatus": "valid",
                "version": 1,
            }
        )

    content_packs = {
        "schemaVersion": 1,
        "generatedAt": utc_now(),
        "packs": packs,
    }

    matching_metadata = {
        "schemaVersion": 1,
        "generatedAt": utc_now(),
        "relations": matching,
        "policy": {
            "earlyLevels": "exact",
            "lateLevels": "synonym/category/tricky allowed only when explicitly added",
            "imageGoalRule": "A concrete gold goal can contain only ImageCard or only TextCard, never both.",
        },
    }
    return content_cards, content_packs, matching_metadata


def validate_png_info(card_id: str, info: PngInfo) -> list[str]:
    errors: list[str] = []
    if not info.has_alpha_channel:
        errors.append("missing_alpha_channel")
    if info.transparent_pixels <= 0:
        errors.append("no_transparent_pixels")
    if not (PROJECT_IMAGE_WIDTH_RANGE[0] <= info.width <= PROJECT_IMAGE_WIDTH_RANGE[1]):
        errors.append(f"width_out_of_project_range:{info.width}")
    if not (PROJECT_IMAGE_HEIGHT_RANGE[0] <= info.height <= PROJECT_IMAGE_HEIGHT_RANGE[1]):
        errors.append(f"height_out_of_project_range:{info.height}")
    if info.opaque_edge_ratio > 0.12:
        errors.append(f"opaque_edges_possible_background:{info.opaque_edge_ratio:.3f}")
    if not card_id:
        errors.append("empty_id")
    return errors


def select_layout_id(level_number: int, layouts_by_id: dict[str, Layout]) -> str:
    if level_number <= 50:
        preferred = ["layout_3_cols"]
    elif level_number <= 200:
        preferred = ["layout_3_cols", "layout_4_cols_v1"]
    elif level_number <= 500:
        preferred = ["layout_4_cols_v1", "layout_4_cols_v2", "layout_4_cols_v3"]
    elif level_number <= 800:
        preferred = ["layout_4_cols_v2", "layout_4_cols_v3", "layout_5_cols_v2"]
    else:
        preferred = ["layout_5_cols_v1", "layout_5_cols_v2"]

    available = [layout_id for layout_id in preferred if layout_id in layouts_by_id]
    if not available:
        available = sorted(layouts_by_id)
    return available[(level_number - 1) % len(available)]


def allowed_gold_ids_for_level(level_number: int, validator: SolvabilityValidator) -> tuple[str, ...]:
    if level_number <= 50:
        allowed = EARLY_CATEGORIES
    elif level_number <= 500:
        allowed = MID_CATEGORIES
    else:
        allowed = LATE_CATEGORIES

    selected = [gold_id for gold_id in validator.gold_ids if gold_id in allowed]
    if len(selected) < 15:
        selected = list(validator.gold_ids)
    return tuple(selected)


def difficulty_for_level(level_number: int) -> str:
    if level_number <= 50:
        return "simple"
    if level_number <= 200:
        return "easy_medium"
    if level_number <= 500:
        return "medium"
    if level_number <= 800:
        return "medium_hard"
    return "hard"


def generate_levels_manifest(
    root: Path,
    total_levels: int,
    max_states: int,
    budget_max_states: int,
    max_attempts: int,
    regen_attempts: int,
    seed_start: int,
    plus10_target_percent: int,
    plus10_target_start_level: int,
) -> dict:
    validator = SolvabilityValidator(root)
    layouts_by_id = {layout.id: layout for layout in validator.layouts}
    levels: list[dict] = []
    failed_seeds: list[dict] = []
    usage_counter: Counter[str] = Counter()
    category_counter: Counter[str] = Counter()
    layout_counter: Counter[str] = Counter()
    image_counter: Counter[str] = Counter()
    text_counter: Counter[str] = Counter()
    solver_moves: list[int] = []
    solver_states: list[int] = []
    generation_attempt_counts: list[int] = []
    move_budget_tiers: Counter[str] = Counter()

    for level_number in range(1, total_levels + 1):
        layout_id = select_layout_id(level_number, layouts_by_id)
        layout = layouts_by_id[layout_id]
        generated = None
        result = None
        source_seed = 0

        for regen_attempt in range(1, regen_attempts + 1):
            source_seed = seed_start + (level_number * 100_003) + (regen_attempt * 1_000_003)
            original_gold_ids = validator.gold_ids
            validator.gold_ids = allowed_gold_ids_for_level(level_number, validator)
            try:
                generated = validator.generate_guaranteed(layout, source_seed, level_number, max_attempts)
            finally:
                validator.gold_ids = original_gold_ids
            if generated is None:
                failed_seeds.append(
                    {
                        "levelNumber": level_number,
                        "seed": source_seed,
                        "layoutId": layout_id,
                        "reason": "generation_failed",
                    }
                )
                continue

            difficulty_ok, difficulty_report = validate_stock_pass_difficulty(validator, generated, max_states)
            if not difficulty_ok:
                failed_seeds.append(
                    {
                        "levelNumber": level_number,
                        "seed": source_seed,
                        "layoutId": layout_id,
                        **difficulty_report,
                    }
                )
                generated = None
                result = None
                continue

            original_budget = assess_move_budget(
                validator,
                generated,
                min(max_states, budget_max_states),
                include_unlimited=False,
            )
            if original_budget["tier"] in {"base", "plus10"}:
                target_plus10_count = 0
                eligible_count = max(0, level_number - plus10_target_start_level + 1)
                if eligible_count > 0:
                    target_plus10_count = round(eligible_count * (plus10_target_percent / 100.0))
                needs_plus10 = (
                    level_number >= plus10_target_start_level
                    and move_budget_tiers["plus10"] < target_plus10_count
                )
                move_budget = assign_gameplay_move_budget(
                    validator,
                    generated,
                    min(max_states, budget_max_states),
                    force_plus10=needs_plus10,
                    original_budget=original_budget,
                )
                if needs_plus10 and move_budget["tier"] != "plus10" and regen_attempt <= max(1, int(regen_attempts * 0.9)):
                    failed_seeds.append(
                        {
                            "levelNumber": level_number,
                            "seed": source_seed,
                            "layoutId": layout_id,
                            "reason": "plus10_quota_needs_ad_level",
                            "currentPlus10": move_budget_tiers["plus10"],
                            "targetPlus10": target_plus10_count,
                        }
                    )
                    generated = None
                    result = None
                    continue

                result = SolveResult(
                    solved=True,
                    moves=move_budget["moves"] or 0,
                    states=move_budget["states"],
                    reason=move_budget["reason"],
                )
                if result.solved:
                    break

            failed_seeds.append(
                {
                    "levelNumber": level_number,
                    "seed": source_seed,
                    "layoutId": layout_id,
                    "reason": f"move_budget_{original_budget['tier']}",
                    "states": original_budget["states"],
                    "moves": original_budget["moves"],
                    "limit": original_budget["limit"],
                }
            )
            generated = None
            result = None

        if generated is None or result is None or not result.solved:
            raise RuntimeError(f"Failed to generate solved level {level_number} after {regen_attempts} attempts")

        entry = build_level_entry(level_number, source_seed, generated, result.moves or 0, result.states, validator, move_budget)
        levels.append(entry)
        layout_counter[entry["layoutId"]] += 1
        generation_attempt_counts.append(entry["generationAttempts"])
        solver_moves.append(entry["solverMoves"])
        solver_states.append(entry["solverStates"])
        move_budget_tiers[entry["moveBudgetTier"]] += 1

        for card_id in entry["cards"]:
            usage_counter[card_id] += 1
            category = card_id.split("_", 1)[0]
            category_counter[category] += 1
            if card_id in generated.image_cards:
                image_counter[card_id] += 1
            elif card_id not in generated.required_by_gold:
                text_counter[card_id] += 1

    solved = len(levels)
    all_known_cards = set(validator.regular_ids) | set(validator.gold_ids)
    unused_cards = sorted(all_known_cards - set(usage_counter))
    overused_cards = [
        {"cardId": card_id, "count": count}
        for card_id, count in usage_counter.most_common(30)
    ]

    manifest = {
        "schemaVersion": 1,
        "totalLevels": total_levels,
        "generationDate": utc_now(),
        "generatorVersion": GENERATOR_VERSION,
        "solverVersion": SOLVER_VERSION,
        "contentPackVersions": {pack: 1 for pack in PACK_ORDER},
        "levels": levels,
        "solverStats": {
            "solved": solved,
            "total": total_levels,
            "solvedRate": round((solved / total_levels) * 100.0, 2) if total_levels else 0.0,
            "averageSolverMoves": round(statistics.mean(solver_moves), 2) if solver_moves else 0.0,
            "averageSolverStates": round(statistics.mean(solver_states), 2) if solver_states else 0.0,
            "averageGenerationAttempts": round(statistics.mean(generation_attempt_counts), 2) if generation_attempt_counts else 0.0,
            "moveBudgetTiers": dict(sorted(move_budget_tiers.items())),
        },
        "layoutStats": dict(sorted(layout_counter.items())),
        "contentUsageStats": {
            "cardUsageTop": overused_cards,
            "unusedCards": unused_cards,
            "unusedCardsCount": len(unused_cards),
            "categoryUsage": dict(sorted(category_counter.items())),
            "imageCardUsageTop": [{"cardId": card_id, "count": count} for card_id, count in image_counter.most_common(30)],
            "textCardUsageTop": [{"cardId": card_id, "count": count} for card_id, count in text_counter.most_common(30)],
            "imageCardsUsed": len(image_counter),
            "textCardsUsed": len(text_counter),
        },
        "failedSeeds": failed_seeds,
        "failedSeedsCount": len(failed_seeds),
    }
    return manifest


def validate_stock_pass_difficulty(
    validator: SolvabilityValidator,
    generated: GeneratedLevel,
    max_states: int,
) -> tuple[bool, dict]:
    without_draw = validator.simulate_greedy_stock_passes(generated, max_passes=0)
    if without_draw["solved"]:
        return False, {
            "reason": "level_solved_without_stock",
            "passReport": without_draw,
        }

    first_pass = validator.simulate_greedy_stock_passes(generated, max_passes=1)
    if generated.layout.columns_count <= 3:
        if not first_pass["solvedBeforeFirstPassEnd"]:
            return True, {
                "reason": "three_col_requires_full_first_stock_pass",
                "passReport": first_pass,
            }
        return False, {
            "reason": "three_col_solves_before_full_first_stock_pass",
            "passReport": first_pass,
        }

    if first_pass["solved"]:
        return False, {
            "reason": "large_layout_solves_in_one_stock_pass",
            "passReport": first_pass,
        }

    return True, {
        "reason": "large_layout_requires_multiple_stock_passes",
        "passReport": first_pass,
    }


def assess_move_budget(
    validator: SolvabilityValidator,
    generated: GeneratedLevel,
    max_states: int,
    include_unlimited: bool = True,
    base_limit: int | None = None,
) -> dict:
    base_limit = generated.reference_moves if base_limit is None else max(1, base_limit)
    base_level = replace(generated, reference_moves=base_limit)
    base_result = validator.solve(base_level, max_states, ignore_move_limit=False)
    if base_result.solved:
        return {
            "tier": "base",
            "limit": base_limit,
            "moves": base_result.moves,
            "states": base_result.states,
            "reason": base_result.reason,
        }

    plus10_level = replace(generated, reference_moves=base_limit + 10)
    plus10_result = validator.solve(plus10_level, max_states, ignore_move_limit=False)
    if plus10_result.solved:
        return {
            "tier": "plus10",
            "limit": base_limit + 10,
            "moves": plus10_result.moves,
            "states": plus10_result.states,
            "baseReason": base_result.reason,
            "reason": plus10_result.reason,
        }

    if not include_unlimited:
        return {
            "tier": "over_plus10_or_unsolved",
            "limit": base_limit + 10,
            "moves": None,
            "states": plus10_result.states,
            "baseReason": base_result.reason,
            "plus10Reason": plus10_result.reason,
            "reason": plus10_result.reason,
        }

    plus20_level = replace(generated, reference_moves=base_limit + 20)
    plus20_result = validator.solve(plus20_level, max_states, ignore_move_limit=False)
    if plus20_result.solved:
        return {
            "tier": "plus20",
            "limit": base_limit + 20,
            "moves": plus20_result.moves,
            "states": plus20_result.states,
            "baseReason": base_result.reason,
            "plus10Reason": plus10_result.reason,
            "reason": plus20_result.reason,
        }

    if not include_unlimited:
        return {
            "tier": "unsolved_within_plus20",
            "limit": base_limit + 20,
            "moves": None,
            "states": plus20_result.states,
            "baseReason": base_result.reason,
            "plus10Reason": plus10_result.reason,
            "plus20Reason": plus20_result.reason,
            "reason": plus20_result.reason,
        }

    unlimited_result = validator.solve(base_level, max_states, ignore_move_limit=True)
    return {
        "tier": "over_plus20" if unlimited_result.solved else "unsolved",
        "limit": base_limit + 20,
        "moves": unlimited_result.moves,
        "states": unlimited_result.states,
        "baseReason": base_result.reason,
        "plus10Reason": plus10_result.reason,
        "plus20Reason": plus20_result.reason,
        "reason": unlimited_result.reason,
    }


def assign_gameplay_move_budget(
    validator: SolvabilityValidator,
    generated: GeneratedLevel,
    max_states: int,
    force_plus10: bool,
    original_budget: dict,
) -> dict:
    if not force_plus10:
        return {
            **original_budget,
            "startingMoves": generated.reference_moves,
            "referenceMoves": generated.reference_moves,
        }

    solution_moves = original_budget.get("moves")
    if not isinstance(solution_moves, int) or solution_moves <= 1:
        return {
            **original_budget,
            "tier": "base",
            "startingMoves": generated.reference_moves,
            "referenceMoves": generated.reference_moves,
            "reason": "plus10_assignment_no_solution_moves",
        }

    for starting_moves in range(solution_moves - 1, 0, -1):
        candidate = assess_move_budget(
            validator,
            generated,
            max_states,
            include_unlimited=False,
            base_limit=starting_moves,
        )
        if candidate["tier"] == "plus10":
            return {
                **candidate,
                "startingMoves": starting_moves,
                "referenceMoves": generated.reference_moves,
            }

    return {
        **original_budget,
        "tier": "base",
        "startingMoves": generated.reference_moves,
        "referenceMoves": generated.reference_moves,
        "reason": "plus10_assignment_failed",
    }


def build_level_entry(
    level_number: int,
    source_seed: int,
    generated: GeneratedLevel,
    solver_moves: int,
    solver_states: int,
    validator: SolvabilityValidator,
    move_budget: dict | None = None,
) -> dict:
    all_cards = [card_id for column in generated.columns for card_id in column] + list(generated.stock)
    gold_cards = sorted(generated.required_by_gold)
    image_cards = sorted(card_id for card_id in all_cards if card_id in generated.image_cards)
    text_cards = sorted(
        card_id
        for card_id in all_cards
        if card_id not in generated.image_cards and card_id not in generated.required_by_gold
    )
    actual_seed = source_seed + (generated.generation_attempts * 7919)
    gold_goals = [
        {
            "id": gold_id,
            "requiredCount": generated.required_by_gold[gold_id],
            "isImageGoal": gold_id in generated.image_goals,
        }
        for gold_id in gold_cards
    ]
    return {
        "levelId": f"level_{level_number:04d}",
        "levelNumber": level_number,
        "seed": actual_seed,
        "sourceSeed": source_seed,
        "layoutId": generated.layout.id,
        "difficulty": difficulty_for_level(level_number),
        "columns": generated.layout.columns_count,
        "goalSlots": generated.layout.columns_count,
        "cardCount": len(all_cards),
        "deckCount": len(generated.stock),
        "goldenCards": len(gold_cards),
        "imageCards": len(image_cards),
        "textCards": len(text_cards),
        "referenceMoves": generated.reference_moves,
        "startingMoves": (move_budget or {}).get("startingMoves", generated.reference_moves),
        "generationAttempts": generated.generation_attempts,
        "solverSolved": True,
        "solverMoves": solver_moves,
        "solverStates": solver_states,
        "solverResult": "solved",
        "moveBudgetTier": (move_budget or {}).get("tier", "base"),
        "moveBudgetLimit": (move_budget or {}).get("limit", generated.reference_moves),
        "moveBudgetMoves": (move_budget or {}).get("moves", solver_moves),
        "cards": all_cards,
        "columnsData": [
            {"index": index, "cards": list(column)}
            for index, column in enumerate(generated.columns)
        ],
        "stockCards": list(generated.stock),
        "goldCards": gold_cards,
        "goldGoals": gold_goals,
        "imageCardIds": image_cards,
        "imageGoalIds": sorted(generated.image_goals),
        "categoryTitles": {
            gold_id: validator.gold_titles.get(gold_id, gold_id)
            for gold_id in gold_cards
        },
    }


def validate_manifest(root: Path, max_states: int) -> dict:
    validator = SolvabilityValidator(root)
    manifest_path = root / "Assets/Resources/GeneratedLevels/level_manifest.json"
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    layouts_by_id = {layout.id: layout for layout in validator.layouts}
    solved = 0
    failed: list[dict] = []
    state_counts: list[int] = []
    move_counts: list[int] = []
    move_budget_tiers: Counter[str] = Counter()
    move_budget_examples: list[dict] = []

    for entry in manifest["levels"]:
        duplicate_card_ids = {
            card_id: count
            for card_id, count in Counter(entry["cards"]).items()
            if count > 1
        }
        if duplicate_card_ids:
            failed.append(
                {
                    "levelId": entry["levelId"],
                    "levelNumber": entry["levelNumber"],
                    "seed": entry["seed"],
                    "layoutId": entry["layoutId"],
                    "reason": "duplicate_card_ids",
                    "duplicates": duplicate_card_ids,
                }
            )
            state_counts.append(0)
            continue

        layout = layouts_by_id[entry["layoutId"]]
        generated = GeneratedLevel(
            layout=layout,
            columns=tuple(tuple(column["cards"]) for column in sorted(entry["columnsData"], key=lambda item: item["index"])),
            stock=tuple(entry["stockCards"]),
            required_by_gold={goal["id"]: goal["requiredCount"] for goal in entry["goldGoals"]},
            image_cards=frozenset(entry["imageCardIds"]),
            image_goals=frozenset(entry["imageGoalIds"]),
            selected_gold=frozenset(entry["goldCards"]),
            reference_moves=entry.get("startingMoves", entry["referenceMoves"]),
            generation_attempts=entry["generationAttempts"],
            generation_mode="manifest_exact",
            construction_moves=entry["solverMoves"],
        )
        difficulty_ok, difficulty_report = validate_stock_pass_difficulty(validator, generated, max_states)
        move_budget = assess_move_budget(
            validator,
            generated,
            min(max_states, DEFAULT_BUDGET_MAX_STATES),
            include_unlimited=False,
        )
        move_budget_tiers[move_budget["tier"]] += 1
        if move_budget["tier"] != "base" and len(move_budget_examples) < 20:
            move_budget_examples.append(
                {
                    "levelId": entry["levelId"],
                    "levelNumber": entry["levelNumber"],
                    "layoutId": entry["layoutId"],
                    "tier": move_budget["tier"],
                    "moves": move_budget["moves"],
                    "limit": move_budget["limit"],
                    "reason": move_budget["reason"],
                }
            )
        state_counts.append(move_budget["states"])
        if move_budget["tier"] in {"base", "plus10"} and difficulty_ok:
            solved += 1
            move_counts.append(move_budget["moves"] or 0)
        else:
            failure_report = dict(difficulty_report)
            failure_report["reason"] = difficulty_report["reason"] if difficulty_ok else difficulty_report["reason"]
            if move_budget["tier"] not in {"base", "plus10"}:
                failure_report["reason"] = f"move_budget_{move_budget['tier']}"
            failed.append(
                {
                    "levelId": entry["levelId"],
                    "levelNumber": entry["levelNumber"],
                    "seed": entry["seed"],
                    "layoutId": entry["layoutId"],
                    "states": move_budget["states"],
                    "moves": move_budget["moves"],
                    "limit": move_budget["limit"],
                    **failure_report,
                }
            )

    total = len(manifest["levels"])
    return {
        "total": total,
        "solved": solved,
        "rate": round((solved / total) * 100.0, 2) if total else 0.0,
        "averageStates": round(statistics.mean(state_counts), 2) if state_counts else 0.0,
        "averageMoves": round(statistics.mean(move_counts), 2) if move_counts else 0.0,
        "moveBudgetTiers": dict(sorted(move_budget_tiers.items())),
        "moveBudgetExamples": move_budget_examples,
        "failed": failed,
    }


def write_json_resource(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    ensure_unity_meta(path.parent, is_folder=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    ensure_unity_meta(path, is_folder=False)


def ensure_unity_meta(path: Path, is_folder: bool) -> None:
    meta_path = path.with_name(path.name + ".meta")
    if meta_path.exists():
        return

    guid = uuid.uuid4().hex
    if is_folder:
        text = (
            "fileFormatVersion: 2\n"
            f"guid: {guid}\n"
            "folderAsset: yes\n"
            "DefaultImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
    else:
        text = (
            "fileFormatVersion: 2\n"
            f"guid: {guid}\n"
            "TextScriptImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
    meta_path.write_text(text, encoding="utf-8")


def utc_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def command_generate(args: argparse.Namespace) -> int:
    root = args.root.resolve()
    validator = SolvabilityValidator(root)
    content_cards, content_packs, matching_metadata = build_content_metadata(root, validator)
    manifest = generate_levels_manifest(
        root,
        args.levels,
        args.max_states,
        args.budget_max_states,
        args.max_attempts,
        args.regen_attempts,
        args.seed_start,
        args.plus10_target_percent,
        args.plus10_target_start_level,
    )

    write_json_resource(root / "Assets/Resources/ContentPacks/content_cards.json", content_cards)
    write_json_resource(root / "Assets/Resources/ContentPacks/content_packs.json", content_packs)
    write_json_resource(root / "Assets/Resources/ContentPacks/matching_metadata.json", matching_metadata)
    write_json_resource(root / "Assets/Resources/GeneratedLevels/level_manifest.json", manifest)
    write_json_resource(root / "Assets/Resources/GeneratedLevels/content_usage_stats.json", manifest["contentUsageStats"])

    print_report(manifest, content_cards)
    return 0


def command_validate(args: argparse.Namespace) -> int:
    report = validate_manifest(args.root.resolve(), args.max_states)
    print(
        f"manifest_validation solved={report['solved']}/{report['total']} "
        f"rate={report['rate']:.1f}% avg_moves={report['averageMoves']:.1f} "
        f"avg_states={report['averageStates']:.0f} "
        f"move_budget={report['moveBudgetTiers']}"
    )
    if report["moveBudgetExamples"]:
        for item in report["moveBudgetExamples"][:20]:
            print(f"move_budget {item}")
    if report["failed"]:
        for item in report["failed"][:20]:
            print(f"failed {item}")
        return 1
    return 0


def command_stats(args: argparse.Namespace) -> int:
    manifest = json.loads((args.root / "Assets/Resources/GeneratedLevels/level_manifest.json").read_text(encoding="utf-8"))
    print_report(manifest, None)
    return 0


def command_preview(args: argparse.Namespace) -> int:
    manifest = json.loads((args.root / "Assets/Resources/GeneratedLevels/level_manifest.json").read_text(encoding="utf-8"))
    entry = next((level for level in manifest["levels"] if level["levelNumber"] == args.level), None)
    if entry is None:
        raise SystemExit(f"Level {args.level} not found.")
    print(json.dumps(entry, ensure_ascii=False, indent=2))
    return 0


def print_report(manifest: dict, content_cards: dict | None) -> None:
    stats = manifest["solverStats"]
    print(
        f"generated_levels={manifest['totalLevels']} solved={stats['solved']}/{stats['total']} "
        f"rate={stats['solvedRate']:.1f}% avg_moves={stats['averageSolverMoves']:.1f} "
        f"avg_states={stats['averageSolverStates']:.0f} avg_generation_attempts={stats['averageGenerationAttempts']:.2f} "
        f"move_budget={stats.get('moveBudgetTiers', {})}"
    )
    print(f"layouts={manifest['layoutStats']}")
    print(f"failed_seeds={manifest['failedSeedsCount']}")
    usage = manifest["contentUsageStats"]
    print(
        f"image_cards_used={usage['imageCardsUsed']} text_cards_used={usage['textCardsUsed']} "
        f"unused_cards={usage['unusedCardsCount']}"
    )
    if content_cards is not None:
        validation = content_cards["validation"]
        print(
            f"content imageCards={validation['imageCardsTotal']} textCards={validation['textCardsTotal']} "
            f"matchingRows={validation['matchingRows']} png_status={validation['status']} "
            f"unusedPremiumPng={len(validation['unusedPremiumPng'])}"
        )
        if validation["errors"]:
            print("png_validation_errors=" + ",".join(validation["errors"][:20]))


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Generate content metadata and verified Solitaire level manifests.")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    subparsers = parser.add_subparsers(dest="command", required=True)

    generate = subparsers.add_parser("generate", help="Generate content metadata and a verified level manifest.")
    generate.add_argument("--levels", type=int, default=DEFAULT_LEVELS)
    generate.add_argument("--max-states", type=int, default=DEFAULT_MAX_STATES)
    generate.add_argument("--budget-max-states", type=int, default=DEFAULT_BUDGET_MAX_STATES)
    generate.add_argument("--max-attempts", type=int, default=DEFAULT_MAX_ATTEMPTS)
    generate.add_argument("--regen-attempts", type=int, default=DEFAULT_REGEN_ATTEMPTS)
    generate.add_argument("--seed-start", type=int, default=73_000)
    generate.add_argument("--plus10-target-percent", type=int, default=DEFAULT_PLUS10_TARGET_PERCENT)
    generate.add_argument("--plus10-target-start-level", type=int, default=DEFAULT_PLUS10_TARGET_START_LEVEL)
    generate.set_defaults(func=command_generate)

    validate = subparsers.add_parser("validate", help="Validate the generated manifest with the solver.")
    validate.add_argument("--max-states", type=int, default=DEFAULT_MAX_STATES)
    validate.set_defaults(func=command_validate)

    stats = subparsers.add_parser("stats", help="Print generated manifest usage stats.")
    stats.set_defaults(func=command_stats)

    preview = subparsers.add_parser("preview", help="Print one generated level entry.")
    preview.add_argument("level", type=int)
    preview.set_defaults(func=command_preview)
    return parser


def main(argv: Iterable[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
