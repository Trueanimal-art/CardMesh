#!/usr/bin/env python3
from __future__ import annotations

import argparse
import heapq
import json
import math
import random
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


IMAGE_CARD_LOWER_TOLERANCE = 0.8


@dataclass(frozen=True)
class Layout:
    id: str
    columns_count: int
    column_counts: tuple[int, ...]
    deck_count: int
    reference_moves: int
    groups: tuple[tuple[int, int], ...]


@dataclass(frozen=True)
class GeneratedLevel:
    layout: Layout
    columns: tuple[tuple[str, ...], ...]
    stock: tuple[str, ...]
    required_by_gold: dict[str, int]
    image_cards: frozenset[str]
    image_goals: frozenset[str]
    selected_gold: frozenset[str]
    reference_moves: int
    generation_attempts: int
    generation_mode: str
    construction_moves: int


@dataclass(frozen=True)
class SolveResult:
    solved: bool
    moves: int | None
    states: int
    reason: str


class SolvabilityValidator:
    def __init__(self, root: Path) -> None:
        self.root = root
        self.cards_by_gold, self.gold_titles = self._load_cards()
        self.gold_ids = tuple(self.cards_by_gold)
        self.recent_card_ids: frozenset[str] = frozenset()
        self.regular_to_gold = {
            card_id: gold_id
            for gold_id, cards in self.cards_by_gold.items()
            for card_id in cards
        }
        self.regular_ids = frozenset(self.regular_to_gold)
        self.image_ids = frozenset(
            path.stem
            for path in (root / "Assets/Resources/ImageCards/Premium").glob("*.png")
        )
        self.full_image_goals = frozenset(
            gold_id
            for gold_id, cards in self.cards_by_gold.items()
            if all(card_id in self.image_ids for card_id in cards)
        )
        self.default_layout_id, self.image_start_level, self.image_percentage, self.layouts = self._load_level()

    def _load_cards(self) -> tuple[dict[str, tuple[str, ...]], dict[str, str]]:
        data = json.loads((self.root / "Assets/Resources/Data/cards.json").read_text())
        cards_by_gold: dict[str, tuple[str, ...]] = {}
        gold_titles: dict[str, str] = {}
        for gold in data["goldCards"]:
            gold_id = gold["id"]
            gold_titles[gold_id] = gold["title"]
            cards_by_gold[gold_id] = tuple(card["id"] for card in gold["cards"])
        return cards_by_gold, gold_titles

    def _load_level(self) -> tuple[str, int, int, tuple[Layout, ...]]:
        lines = (self.root / "Assets/Resources/Levels/Level_1.asset").read_text().splitlines()
        default_layout_id = self._find_scalar(lines, "defaultLayoutId")
        image_start_level = int(self._find_scalar(lines, "imageCardsStartFromLevel"))
        image_percentage = int(self._find_scalar(lines, "imageCardPercentage"))

        layouts: list[Layout] = []
        index = 0
        while index < len(lines):
            if not re.match(r"  - id:", lines[index]):
                index += 1
                continue

            layout_id = lines[index].split(":", 1)[1].strip()
            columns_count = 0
            column_counts: list[int] = []
            deck_count = 0
            reference_moves = 0
            groups: list[tuple[int, int]] = []
            index += 1
            while index < len(lines) and not re.match(r"  - id:", lines[index]):
                text = lines[index].strip()
                if text.startswith("columnsCount:"):
                    columns_count = int(text.split(":", 1)[1])
                elif text.startswith("deckCount:"):
                    deck_count = int(text.split(":", 1)[1])
                elif text.startswith("referenceMoves:"):
                    reference_moves = int(text.split(":", 1)[1])
                elif text.startswith("- goldCardsCount:"):
                    gold_count = int(text.split(":", 1)[1])
                    ordinary_count = int(lines[index + 1].strip().split(":", 1)[1])
                    groups.append((gold_count, ordinary_count))
                    index += 1
                elif re.match(r"- \d+$", text):
                    column_counts.append(int(text[2:]))
                index += 1

            layouts.append(
                Layout(
                    layout_id,
                    columns_count,
                    tuple(column_counts),
                    deck_count,
                    reference_moves,
                    tuple(groups),
                )
            )

        return default_layout_id, image_start_level, image_percentage, tuple(layouts)

    @staticmethod
    def _find_scalar(lines: Iterable[str], key: str) -> str:
        prefix = f"{key}:"
        for line in lines:
            text = line.strip()
            if text.startswith(prefix):
                return text.split(":", 1)[1].strip()
        raise ValueError(f"Missing {key}")

    def generate_random(self, layout: Layout, seed: int, level_number: int) -> GeneratedLevel | None:
        rng = random.Random(seed)
        expected_total = layout.deck_count + sum(max(0, count) for count in layout.column_counts)
        available_gold_cards = list(self.gold_ids)
        rng.shuffle(available_gold_cards)

        selected: list[dict[str, object]] = []
        required_by_gold: dict[str, int] = {}
        pool: list[str] = []
        sorted_groups = sorted(layout.groups, key=lambda group: group[1], reverse=True)
        expected_gold = sum(count for count, _ in sorted_groups)
        expected_ordinary = max(0, expected_total - expected_gold)
        target_image_count = self._target_image_count(level_number, expected_ordinary)
        preferred_image_count = 0

        for gold_count, ordinary_per_gold in sorted_groups:
            for _ in range(gold_count):
                selected_gold = self._take_eligible_gold(
                    available_gold_cards,
                    ordinary_per_gold,
                    preferred_image_count < target_image_count,
                )
                if selected_gold is None:
                    return None

                ordinary_ids = list(self.cards_by_gold[selected_gold])
                rng.shuffle(ordinary_ids)
                preferred_ids = [card_id for card_id in ordinary_ids if card_id not in self.recent_card_ids]
                assignment_pool = preferred_ids if len(preferred_ids) >= ordinary_per_gold else ordinary_ids
                assigned = assignment_pool[:ordinary_per_gold]
                assigned_set = set(assigned)
                remaining = [card_id for card_id in ordinary_ids if card_id not in assigned_set]

                selected.append(
                    {
                        "gold": selected_gold,
                        "assigned": tuple(assigned),
                        "remaining": tuple(remaining),
                    }
                )
                required_by_gold[selected_gold] = ordinary_per_gold
                if selected_gold in self.full_image_goals:
                    preferred_image_count += len(assigned)

                pool.append(selected_gold)
                pool.extend(assigned)

        if len(pool) < expected_total:
            extra: list[str] = []
            for selected_gold in selected:
                extra.extend(selected_gold["remaining"])  # type: ignore[arg-type]
            rng.shuffle(extra)
            pool.extend(extra[: expected_total - len(pool)])
        elif len(pool) > expected_total:
            gold_ids = {selected_gold["gold"] for selected_gold in selected}
            ordinary_indices = [idx for idx, card_id in enumerate(pool) if card_id not in gold_ids]
            rng.shuffle(ordinary_indices)
            remove_indices = set(ordinary_indices[: len(pool) - expected_total])
            pool = [card_id for idx, card_id in enumerate(pool) if idx not in remove_indices]

        if len(pool) != expected_total:
            return None

        pool_ids = set(pool)
        image_cards, image_goals = self._select_image_cards(level_number, selected, pool_ids)
        rng.shuffle(pool)

        columns: list[tuple[str, ...]] = []
        cursor = 0
        for count in layout.column_counts:
            columns.append(tuple(pool[cursor : cursor + count]))
            cursor += count
        stock = tuple(pool[cursor : cursor + layout.deck_count])

        return GeneratedLevel(
            layout,
            tuple(columns),
            stock,
            required_by_gold,
            frozenset(image_cards),
            frozenset(image_goals),
            frozenset(required_by_gold),
            layout.reference_moves,
            1,
            "legacy_random",
            0,
        )

    def generate_guaranteed(self, layout: Layout, seed: int, level_number: int, max_attempts: int = 20) -> GeneratedLevel | None:
        for attempt in range(1, max_attempts + 1):
            attempt_seed = seed + (attempt * 7919)
            generated = self._generate_guaranteed_once(layout, attempt_seed, level_number, attempt)
            if generated is not None:
                return generated
        return None

    def _generate_guaranteed_once(self, layout: Layout, seed: int, level_number: int, attempt: int) -> GeneratedLevel | None:
        draft = self.generate_random(layout, seed, level_number)
        if draft is None:
            return None

        selected_order = list(draft.required_by_gold)
        random.Random(seed ^ 0x5A17).shuffle(selected_order)
        selected_order.sort(key=lambda gold_id: -draft.required_by_gold[gold_id])
        solution_order: list[str] = []
        solution_ids: set[str] = set()
        cards_by_selected_gold: dict[str, list[str]] = {}
        for gold_id in selected_order:
            required_count = draft.required_by_gold[gold_id]
            pool_cards = [
                card_id
                for card_id in self.cards_by_gold[gold_id]
                if any(card_id in column for column in draft.columns) or card_id in draft.stock
            ][:required_count]
            if len(pool_cards) < required_count:
                return None
            random.Random(seed ^ self._stable_hash(gold_id)).shuffle(pool_cards)
            cards_by_selected_gold[gold_id] = pool_cards

        active_goal_batch_size = max(1, min(layout.columns_count, 3 if layout.columns_count >= 4 else layout.columns_count))
        for batch in self._make_goal_batches(selected_order, active_goal_batch_size):
            for gold_id in batch:
                solution_order.append(gold_id)
                solution_ids.add(gold_id)

            max_cards = max((len(cards_by_selected_gold[gold_id]) for gold_id in batch), default=0)
            for card_index in range(max_cards):
                wave = list(batch)
                random.Random(seed + len(solution_order) + card_index).shuffle(wave)
                for gold_id in wave:
                    cards = cards_by_selected_gold[gold_id]
                    if card_index < len(cards):
                        card_id = cards[card_index]
                        solution_order.append(card_id)
                        solution_ids.add(card_id)

        all_cards = [card_id for column in draft.columns for card_id in column] + list(draft.stock)
        extras = [card_id for card_id in all_cards if card_id not in solution_ids]
        random.Random(seed).shuffle(extras)

        column_capacity = sum(layout.column_counts)
        column_solution_cards = min(column_capacity, len(solution_order))
        columns = self._deal_columns_round_robin(
            solution_order[:column_solution_cards],
            layout.column_counts,
        )

        stock = tuple(self._weave_stock(solution_order[column_solution_cards:], extras, seed))
        columns, stock = self._scatter_columns_and_stock(columns, stock, draft.required_by_gold, seed)
        max_gold_tops = 1 if layout.columns_count <= 3 else 2
        columns, stock = self._limit_initial_gold_tops(columns, stock, seed, max_gold_tops=max_gold_tops)
        columns, stock = self._break_initial_tableau_cascade(columns, stock, seed)
        columns, stock = self._force_stock_dependent_tableau_card(columns, stock, draft.required_by_gold, seed)
        columns, stock = self._bury_late_tableau_dependencies(columns, stock, draft.required_by_gold, seed)
        columns, stock = self._limit_initial_gold_tops(columns, stock, seed ^ 0xA11CE, max_gold_tops=max_gold_tops)
        columns, stock = self._break_initial_tableau_cascade(columns, stock, seed ^ 0xCA5CADE)
        if layout.columns_count >= 4:
            stock = self._force_second_stock_pass(columns, stock, draft.required_by_gold, seed)
        if sum(len(column) for column in columns) + len(stock) != len(all_cards):
            return None

        stock_solution_cards = max(0, len(solution_order) - column_solution_cards)
        construction_moves = column_solution_cards + (stock_solution_cards * 2)

        return GeneratedLevel(
            layout,
            tuple(columns),
            stock,
            draft.required_by_gold,
            draft.image_cards,
            draft.image_goals,
            draft.selected_gold,
            layout.reference_moves,
            attempt,
            "guaranteed_by_construction",
            construction_moves,
        )

    @staticmethod
    def _make_goal_batches(selected_order: list[str], batch_size: int) -> list[list[str]]:
        batches: list[list[str]] = []
        cursor = 0
        batch_size = max(1, batch_size)
        while cursor < len(selected_order):
            remaining = len(selected_order) - cursor
            if batch_size > 1 and remaining == batch_size + 1:
                current_size = 2
            else:
                current_size = min(batch_size, remaining)
            batches.append(selected_order[cursor: cursor + current_size])
            cursor += current_size
        return batches

    @staticmethod
    def _deal_columns_round_robin(solution_prefix: list[str], column_counts: tuple[int, ...]) -> list[tuple[str, ...]]:
        assigned: list[list[str]] = [[] for _ in column_counts]
        remaining = list(column_counts)
        column_index = 0
        for card_id in solution_prefix:
            checked = 0
            while checked < len(column_counts) and remaining[column_index] <= 0:
                column_index = (column_index + 1) % len(column_counts)
                checked += 1
            if checked >= len(column_counts):
                break

            assigned[column_index].append(card_id)
            remaining[column_index] -= 1
            column_index = (column_index + 1) % len(column_counts)

        return [tuple(reversed(cards)) for cards in assigned]

    def _weave_stock(self, solution_suffix: list[str], extras: list[str], seed: int) -> list[str]:
        rng = random.Random(seed ^ 0xC0FFEE)
        remaining_extras = list(extras)
        stock: list[str] = []
        solution_index = 0
        while solution_index < len(solution_suffix) or remaining_extras:
            should_place_extra = bool(remaining_extras) and (
                solution_index >= len(solution_suffix)
                or (len(stock) % 4 == 2)
                or rng.random() < 0.18
            )
            if should_place_extra:
                stock.append(remaining_extras.pop())
                continue

            stock.append(solution_suffix[solution_index])
            solution_index += 1

        return stock

    def _scatter_columns_and_stock(
        self,
        columns: list[tuple[str, ...]],
        stock: tuple[str, ...],
        required_by_gold: dict[str, int],
        seed: int,
    ) -> tuple[list[tuple[str, ...]], tuple[str, ...]]:
        rng = random.Random(seed ^ 0xD15EA5E)
        mutable_columns = [list(column) for column in columns]
        mutable_stock = list(stock)

        swaps_remaining = max(1, len(mutable_columns) - 1)
        sorted_gold_ids = sorted(required_by_gold, key=lambda gold_id: -required_by_gold[gold_id])
        for gold_id in sorted_gold_ids:
            if swaps_remaining <= 0:
                break
            if required_by_gold[gold_id] < 6:
                continue

            locations = self._category_locations(gold_id, mutable_columns, mutable_stock)
            if len(locations) >= 2:
                continue

            if "stock" in locations:
                source_stock_indices = [
                    index for index, card_id in enumerate(mutable_stock)
                    if self._card_association(card_id) == gold_id and not self._is_gold(card_id)
                ]
                target_positions = [
                    (column_index, card_index)
                    for column_index, column in enumerate(mutable_columns)
                    for card_index, card_id in enumerate(column[:-1])
                    if self._card_association(card_id) != gold_id
                ]
                if not source_stock_indices or not target_positions:
                    continue

                stock_index = rng.choice(source_stock_indices)
                column_index, card_index = rng.choice(target_positions)
                mutable_stock[stock_index], mutable_columns[column_index][card_index] = (
                    mutable_columns[column_index][card_index],
                    mutable_stock[stock_index],
                )
                swaps_remaining -= 1
                continue

            source_positions = [
                (column_index, card_index)
                for column_index, column in enumerate(mutable_columns)
                for card_index, card_id in enumerate(column[:-1])
                if self._card_association(card_id) == gold_id and not self._is_gold(card_id)
            ]
            target_stock_indices = [
                index for index, card_id in enumerate(mutable_stock)
                if self._card_association(card_id) != gold_id
            ]
            if not source_positions or not target_stock_indices:
                continue

            column_index, card_index = rng.choice(source_positions)
            stock_index = rng.choice(target_stock_indices)
            mutable_stock[stock_index], mutable_columns[column_index][card_index] = (
                mutable_columns[column_index][card_index],
                mutable_stock[stock_index],
            )
            swaps_remaining -= 1

        mutable_stock = self._rebalance_stock_runs(mutable_stock)
        return [tuple(column) for column in mutable_columns], tuple(mutable_stock)

    def _break_initial_tableau_cascade(
        self,
        columns: list[tuple[str, ...]],
        stock: tuple[str, ...],
        seed: int,
    ) -> tuple[list[tuple[str, ...]], tuple[str, ...]]:
        mutable_columns = [list(column) for column in columns]
        mutable_stock = list(stock)
        if not mutable_stock:
            return columns, stock

        top_gold_ids = {
            column[-1]
            for column in mutable_columns
            if column and self._is_gold(column[-1])
        }
        if not top_gold_ids:
            return columns, stock

        rng = random.Random(seed ^ 0xCA5CADE)
        stock_indices = list(range(len(mutable_stock)))
        rng.shuffle(stock_indices)

        for column in mutable_columns:
            if not column:
                continue
            top_card = column[-1]
            if self._is_gold(top_card) or self._card_association(top_card) not in top_gold_ids:
                continue

            replacement_index = next(
                (
                    index
                    for index in stock_indices
                    if not self._is_gold(mutable_stock[index])
                    and self._card_association(mutable_stock[index]) not in top_gold_ids
                ),
                None,
            )
            if replacement_index is None:
                continue

            column[-1], mutable_stock[replacement_index] = mutable_stock[replacement_index], column[-1]
            stock_indices.remove(replacement_index)

        return [tuple(column) for column in mutable_columns], tuple(mutable_stock)

    def find_initial_tableau_cascade(self, level: GeneratedLevel) -> dict | None:
        top_gold_ids = {
            column[-1]
            for column in level.columns
            if column and self._is_gold(column[-1])
        }
        if not top_gold_ids:
            return None

        matching_top_cards = []
        for column_index, column in enumerate(level.columns):
            if not column:
                continue

            top_card = column[-1]
            if self._is_gold(top_card):
                continue

            association = self._card_association(top_card)
            if association in top_gold_ids:
                matching_top_cards.append(
                    {
                        "columnIndex": column_index,
                        "cardId": top_card,
                        "association": association,
                    }
                )

        if not matching_top_cards:
            return None

        return {
            "topGoldIds": sorted(top_gold_ids),
            "matchingTopCards": matching_top_cards,
        }

    def _bury_late_tableau_dependencies(
        self,
        columns: list[tuple[str, ...]],
        stock: tuple[str, ...],
        required_by_gold: dict[str, int],
        seed: int,
    ) -> tuple[list[tuple[str, ...]], tuple[str, ...]]:
        mutable_columns = [list(column) for column in columns]
        mutable_stock = list(stock)
        if not mutable_stock:
            return columns, stock

        hidden_positions = [
            (column_index, card_index)
            for column_index, column in enumerate(mutable_columns)
            for card_index in range(0, max(0, len(column) - 1))
        ]
        if not hidden_positions:
            return columns, stock

        hidden_positions.sort(key=lambda item: (item[1], item[0]))
        late_start = max(0, len(mutable_stock) // 2)
        late_stock_indices = [
            index
            for index in range(late_start, len(mutable_stock))
            if not self._is_gold(mutable_stock[index])
            and self._card_association(mutable_stock[index]) in required_by_gold
        ]
        if not late_stock_indices:
            return columns, stock

        rng = random.Random(seed ^ 0x7A81EA)
        rng.shuffle(late_stock_indices)
        swaps_remaining = min(len(hidden_positions), len(late_stock_indices), max(1, len(mutable_columns)))
        used_column_indices: set[int] = set()
        for stock_index in late_stock_indices:
            target = next(
                (
                    position
                    for position in hidden_positions
                    if position[0] not in used_column_indices
                    and self._card_association(mutable_columns[position[0]][position[1]])
                    != self._card_association(mutable_stock[stock_index])
                ),
                None,
            )
            if target is None:
                continue

            column_index, card_index = target
            mutable_columns[column_index][card_index], mutable_stock[stock_index] = (
                mutable_stock[stock_index],
                mutable_columns[column_index][card_index],
            )
            used_column_indices.add(column_index)
            hidden_positions.remove(target)
            swaps_remaining -= 1
            if swaps_remaining <= 0:
                break

        return [tuple(column) for column in mutable_columns], tuple(mutable_stock)

    def _force_stock_dependent_tableau_card(
        self,
        columns: list[tuple[str, ...]],
        stock: tuple[str, ...],
        required_by_gold: dict[str, int],
        seed: int,
    ) -> tuple[list[tuple[str, ...]], tuple[str, ...]]:
        mutable_columns = [list(column) for column in columns]
        mutable_stock = list(stock)
        stock_gold_ids = {
            card_id
            for card_id in mutable_stock
            if self._is_gold(card_id) and card_id in required_by_gold
        }
        if not stock_gold_ids:
            return columns, stock

        for column in mutable_columns:
            for card_id in column:
                if not self._is_gold(card_id) and self._card_association(card_id) in stock_gold_ids:
                    return columns, stock

        rng = random.Random(seed ^ 0x57C0C1)
        candidate_gold_ids = list(stock_gold_ids)
        rng.shuffle(candidate_gold_ids)
        for gold_id in candidate_gold_ids:
            source_stock_indices = [
                index
                for index, card_id in enumerate(mutable_stock)
                if not self._is_gold(card_id) and self._card_association(card_id) == gold_id
            ]
            if not source_stock_indices:
                continue

            target_positions = [
                (column_index, card_index)
                for column_index, column in enumerate(mutable_columns)
                for card_index, card_id in enumerate(column)
                if not self._is_gold(card_id) and self._card_association(card_id) != gold_id
            ]
            if not target_positions:
                continue

            source_stock_index = rng.choice(source_stock_indices)
            column_index, card_index = rng.choice(target_positions)
            mutable_stock[source_stock_index], mutable_columns[column_index][card_index] = (
                mutable_columns[column_index][card_index],
                mutable_stock[source_stock_index],
            )
            return [tuple(column) for column in mutable_columns], tuple(mutable_stock)

        return columns, stock

    def _force_second_stock_pass(
        self,
        columns: list[tuple[str, ...]],
        stock: tuple[str, ...],
        required_by_gold: dict[str, int],
        seed: int,
    ) -> tuple[str, ...]:
        if len(stock) < 4:
            return stock

        mutable_stock = list(stock)
        top_gold_ids = {
            column[-1]
            for column in columns
            if column and self._is_gold(column[-1])
        }
        candidate_gold_ids = [
            gold_id
            for gold_id in required_by_gold
            if gold_id not in top_gold_ids and gold_id in mutable_stock
        ]
        rng = random.Random(seed ^ 0x51A7E2)
        rng.shuffle(candidate_gold_ids)

        for gold_id in candidate_gold_ids:
            regular_indices = [
                index
                for index, card_id in enumerate(mutable_stock)
                if not self._is_gold(card_id) and self._card_association(card_id) == gold_id
            ]
            if not regular_indices:
                continue

            gold_index = mutable_stock.index(gold_id)
            regular_index = regular_indices[0]
            regular_card = mutable_stock.pop(regular_index)
            if regular_index < gold_index:
                gold_index -= 1
            mutable_stock.insert(0, regular_card)

            gold_index = mutable_stock.index(gold_id)
            target_gold_index = min(len(mutable_stock) - 1, max(3, len(mutable_stock) // 3))
            if gold_index < target_gold_index:
                gold_card = mutable_stock.pop(gold_index)
                mutable_stock.insert(target_gold_index, gold_card)
            return tuple(mutable_stock)

        return stock

    def _limit_initial_gold_tops(
        self,
        columns: list[tuple[str, ...]],
        stock: tuple[str, ...],
        seed: int,
        max_gold_tops: int = 1,
    ) -> tuple[list[tuple[str, ...]], tuple[str, ...]]:
        rng = random.Random(seed ^ 0x601D)
        mutable_columns = [list(column) for column in columns]
        mutable_stock = list(stock)
        gold_top_columns = [
            column_index
            for column_index, column in enumerate(mutable_columns)
            if column and self._is_gold(column[-1])
        ]
        if len(gold_top_columns) <= max_gold_tops:
            return columns, stock

        rng.shuffle(gold_top_columns)
        for column_index in gold_top_columns[max_gold_tops:]:
            column = mutable_columns[column_index]
            swap_index = self._find_hidden_non_gold_index(column)
            if swap_index is not None:
                column[-1], column[swap_index] = column[swap_index], column[-1]
                continue

            stock_index = self._find_stock_non_gold_index(mutable_stock, rng)
            if stock_index is None:
                continue

            column[-1], mutable_stock[stock_index] = mutable_stock[stock_index], column[-1]

        return [tuple(column) for column in mutable_columns], tuple(mutable_stock)

    def _find_hidden_non_gold_index(self, column: list[str]) -> int | None:
        for index in range(len(column) - 2, -1, -1):
            if not self._is_gold(column[index]):
                return index
        return None

    def _find_stock_non_gold_index(self, stock: list[str], rng: random.Random) -> int | None:
        candidates = [index for index, card_id in enumerate(stock) if not self._is_gold(card_id)]
        return rng.choice(candidates) if candidates else None

    def _category_locations(self, gold_id: str, columns: list[list[str]], stock: list[str]) -> set[str]:
        locations: set[str] = set()
        for column_index, column in enumerate(columns):
            if any(self._card_association(card_id) == gold_id for card_id in column):
                locations.add(f"c{column_index}")
        if any(self._card_association(card_id) == gold_id for card_id in stock):
            locations.add("stock")
        return locations

    def _rebalance_stock_runs(self, stock: list[str], max_run: int = 3) -> list[str]:
        result = list(stock)
        for index in range(len(result)):
            if self._run_length_ending_at(result, index) <= max_run:
                continue

            current_association = self._card_association(result[index])
            for swap_index in range(index + 1, len(result)):
                if self._card_association(result[swap_index]) == current_association:
                    continue
                result[index], result[swap_index] = result[swap_index], result[index]
                if (
                    self._run_length_ending_at(result, index) <= max_run
                    and self._run_length_around(result, swap_index) <= max_run
                ):
                    break
                result[index], result[swap_index] = result[swap_index], result[index]
        return result

    def _run_length_ending_at(self, cards: list[str], index: int) -> int:
        association = self._card_association(cards[index])
        length = 1
        cursor = index - 1
        while cursor >= 0 and self._card_association(cards[cursor]) == association:
            length += 1
            cursor -= 1
        return length

    def _run_length_around(self, cards: list[str], index: int) -> int:
        association = self._card_association(cards[index])
        length = 1
        cursor = index - 1
        while cursor >= 0 and self._card_association(cards[cursor]) == association:
            length += 1
            cursor -= 1
        cursor = index + 1
        while cursor < len(cards) and self._card_association(cards[cursor]) == association:
            length += 1
            cursor += 1
        return length

    def _card_association(self, card_id: str) -> str:
        return self._gold_id(card_id) if self._is_gold(card_id) else self._regular_association(card_id)

    @staticmethod
    def _stable_hash(value: str) -> int:
        result = 2166136261
        for char in value:
            result ^= ord(char)
            result = (result * 16777619) & 0xFFFFFFFF
        return result

    def _take_eligible_gold(
        self,
        available: list[str],
        required_ordinary: int,
        prefer_image_goal: bool,
    ) -> str | None:
        if prefer_image_goal:
            for index, gold_id in enumerate(available):
                if len(self.cards_by_gold[gold_id]) >= required_ordinary and gold_id in self.full_image_goals:
                    return available.pop(index)

        for index, gold_id in enumerate(available):
            if len(self.cards_by_gold[gold_id]) >= required_ordinary:
                return available.pop(index)

        return None

    def _select_image_cards(
        self,
        level_number: int,
        selected: list[dict[str, object]],
        pool_ids: set[str],
    ) -> tuple[set[str], set[str]]:
        if not self._uses_image_cards(level_number):
            return set(), set()

        selected_gold_ids = {entry["gold"] for entry in selected}
        total_ordinary = sum(1 for card_id in pool_ids if card_id not in selected_gold_ids)
        target_count = self._target_image_count(level_number, total_ordinary)
        image_candidates = [
            entry
            for entry in selected
            if entry["gold"] in self.full_image_goals
            and any(card_id in pool_ids for card_id in self.cards_by_gold[entry["gold"]])  # type: ignore[index]
        ]
        image_candidates.sort(
            key=lambda entry: self._count_pool_cards_for_gold(entry["gold"], pool_ids),  # type: ignore[arg-type]
            reverse=True,
        )

        image_cards: set[str] = set()
        image_goals: set[str] = set()
        for entry in image_candidates:
            gold_id = entry["gold"]  # type: ignore[assignment]
            before = len(image_cards)
            for card_id in self.cards_by_gold[gold_id]:
                if card_id in pool_ids:
                    image_cards.add(card_id)
            if len(image_cards) > before:
                image_goals.add(gold_id)
            if len(image_cards) >= target_count:
                break

        return image_cards, image_goals

    def _count_pool_cards_for_gold(self, gold_id: str, pool_ids: set[str]) -> int:
        return sum(1 for card_id in self.cards_by_gold[gold_id] if card_id in pool_ids)

    def _uses_image_cards(self, level_number: int) -> bool:
        return max(1, level_number) >= self.image_start_level and self.image_percentage > 0

    def _target_image_count(self, level_number: int, ordinary_count: int) -> int:
        if ordinary_count <= 0 or not self._uses_image_cards(level_number):
            return 0
        target_count = round(ordinary_count * (self.image_percentage / 100.0))
        tolerated_count = math.ceil(target_count * IMAGE_CARD_LOWER_TOLERANCE)
        return max(1, tolerated_count)

    def solve(self, level: GeneratedLevel, max_states: int, ignore_move_limit: bool = False) -> SolveResult:
        initial_columns = tuple((tuple(self._symbolize(card_id) for card_id in column), 1 if column else 0) for column in level.columns)
        initial_stock = tuple(self._symbolize(card_id) for card_id in level.stock)
        initial_state, initial_forced_moves = self._settle_forced_moves(
            self._canonical_state(
                initial_columns,
                initial_stock,
                tuple(),
                tuple(None for _ in range(level.layout.columns_count)),
                frozenset(),
            ),
            level,
        )

        if not ignore_move_limit and initial_forced_moves > level.reference_moves:
            return SolveResult(False, None, 0, "move_limit")

        queue: list[tuple[int, int, int, object]] = []
        counter = 0
        heapq.heappush(queue, (self._priority(initial_state, level, initial_forced_moves), initial_forced_moves, counter, initial_state))
        best_moves_by_state = {initial_state: initial_forced_moves}
        states = 0

        while queue and states < max_states:
            _, moves_used, _, state = heapq.heappop(queue)
            if best_moves_by_state.get(state, 10**9) != moves_used:
                continue

            states += 1
            _, _, _, _, completed = state
            if len(completed) >= len(level.selected_gold):
                return SolveResult(True, moves_used, states, "solved")
            if not ignore_move_limit and moves_used >= level.reference_moves:
                continue

            for raw_next_state in self._next_decision_states(state, level):
                settled_next_state, forced_moves = self._settle_forced_moves(raw_next_state, level)
                next_moves = moves_used + 1 + forced_moves
                if not ignore_move_limit and next_moves > level.reference_moves:
                    continue
                previous = best_moves_by_state.get(settled_next_state)
                if previous is not None and previous <= next_moves:
                    continue
                best_moves_by_state[settled_next_state] = next_moves
                counter += 1
                heapq.heappush(
                    queue,
                    (self._priority(settled_next_state, level, next_moves), next_moves, counter, settled_next_state),
                )

        reason = "state_limit" if states >= max_states else "exhausted"
        return SolveResult(False, None, states, reason)

    def solve_exhaustive(self, level: GeneratedLevel, max_states: int, ignore_move_limit: bool = False) -> SolveResult:
        initial_columns = tuple((tuple(self._symbolize(card_id) for card_id in column), 1 if column else 0) for column in level.columns)
        initial_stock = tuple(self._symbolize(card_id) for card_id in level.stock)
        initial_state = self._canonical_state(
            initial_columns,
            initial_stock,
            tuple(),
            tuple(None for _ in range(level.layout.columns_count)),
            frozenset(),
        )

        queue: list[tuple[int, int, int, object]] = []
        counter = 0
        heapq.heappush(queue, (self._priority(initial_state, level, 0), 0, counter, initial_state))
        best_moves_by_state = {initial_state: 0}
        states = 0

        while queue and states < max_states:
            _, moves_used, _, state = heapq.heappop(queue)
            if best_moves_by_state.get(state, 10**9) != moves_used:
                continue

            states += 1
            columns, queued, revealed, goals, completed = state
            if len(completed) >= len(level.selected_gold):
                return SolveResult(True, moves_used, states, "solved")
            if not ignore_move_limit and moves_used >= level.reference_moves:
                continue

            for next_state in self._next_states(state, level):
                next_moves = moves_used + 1
                previous = best_moves_by_state.get(next_state)
                if previous is not None and previous <= next_moves:
                    continue
                best_moves_by_state[next_state] = next_moves
                counter += 1
                heapq.heappush(
                    queue,
                    (self._priority(next_state, level, next_moves), next_moves, counter, next_state),
                )

        reason = "state_limit" if states >= max_states else "exhausted"
        return SolveResult(False, None, states, reason)

    def _settle_forced_moves(self, state: object, level: GeneratedLevel) -> tuple[object, int]:
        moves = 0
        changed = True
        while changed:
            changed = False
            columns, queued, revealed, goals, completed = state

            if revealed:
                top_revealed = revealed[-1]
                if self._is_gold(top_revealed):
                    empty_index = next((index for index, goal in enumerate(goals) if goal is None), None)
                    if empty_index is not None:
                        state = self._move_to_goal(state, level, "draw", None, (top_revealed,), 1, empty_index)
                        moves += 1
                        changed = True
                        continue
                else:
                    stack = (top_revealed,)
                    for goal_index, goal in enumerate(goals):
                        if goal is not None and self._can_goal_accept(goal, stack, level):
                            state = self._move_to_goal(state, level, "draw", None, stack, 1, goal_index)
                            moves += 1
                            changed = True
                            break
                    if changed:
                        continue

            for column_index, (cards, faceup_count) in enumerate(columns):
                if not cards or faceup_count <= 0:
                    continue

                stack = self._top_faceup_run(cards, faceup_count)
                if not stack:
                    continue

                for goal_index, goal in enumerate(goals):
                    if self._can_goal_slot_accept(goal, stack, level):
                        state = self._move_to_goal(state, level, "column", column_index, stack, len(stack), goal_index)
                        moves += 1
                        changed = True
                        break
                if changed:
                    break

        return state, moves

    def _next_decision_states(self, state: object, level: GeneratedLevel) -> Iterable[object]:
        columns, queued, revealed, goals, completed = state

        for column_index, (cards, faceup_count) in enumerate(columns):
            if not cards or faceup_count <= 0:
                continue

            stack = self._top_faceup_run(cards, faceup_count)
            if not stack:
                continue

            moving_card = stack[-1] if self._is_gold(stack[-1]) else stack[0]
            for target_index, (target_cards, _) in enumerate(columns):
                if target_cards and self._is_gold_headed_stack(stack) and len(stack) > 1:
                    continue
                if column_index == target_index:
                    continue
                if self._can_column_accept(target_cards, moving_card):
                    if not target_cards and len(stack) == len(cards):
                        continue
                    yield self._move_to_column(
                        state,
                        "column",
                        column_index,
                        stack,
                        len(stack),
                        target_index,
                    )

        draw_state = self._draw_next(state)
        if draw_state is not None:
            yield draw_state

    def _top_faceup_run(self, cards: tuple[str, ...], faceup_count: int) -> tuple[str, ...]:
        faceup_start = len(cards) - faceup_count
        top_card = cards[-1]
        top_association = self._card_association(top_card)
        run_start = len(cards) - 1
        while run_start - 1 >= faceup_start:
            candidate = cards[run_start - 1]
            if self._is_gold(candidate) or self._card_association(candidate) != top_association:
                break
            run_start -= 1
        return cards[run_start:]

    def solve_tableau_without_draw(self, level: GeneratedLevel, max_states: int, ignore_move_limit: bool = True) -> SolveResult:
        initial_columns = tuple((tuple(self._symbolize(card_id) for card_id in column), 1 if column else 0) for column in level.columns)
        initial_state = self._canonical_state(
            initial_columns,
            tuple(),
            tuple(),
            tuple(None for _ in range(level.layout.columns_count)),
            frozenset(),
        )

        queue: list[tuple[int, int, int, object]] = []
        counter = 0
        heapq.heappush(queue, (0, 0, counter, initial_state))
        best_moves_by_state = {initial_state: 0}
        states = 0

        while queue and states < max_states:
            _, moves_used, _, state = heapq.heappop(queue)
            if best_moves_by_state.get(state, 10**9) != moves_used:
                continue

            states += 1
            columns, _, _, _, _ = state
            if all(len(cards) == 0 for cards, _ in columns):
                return SolveResult(True, moves_used, states, "tableau_cleared_without_draw")
            if not ignore_move_limit and moves_used >= level.reference_moves:
                continue

            for next_state in self._next_states(state, level, allow_draw=False):
                next_moves = moves_used + 1
                previous = best_moves_by_state.get(next_state)
                if previous is not None and previous <= next_moves:
                    continue
                best_moves_by_state[next_state] = next_moves
                counter += 1
                heapq.heappush(queue, (next_moves, next_moves, counter, next_state))

        reason = "state_limit" if states >= max_states else "requires_stock"
        return SolveResult(False, None, states, reason)

    def solve_tableau_with_stock_recycles(
        self,
        level: GeneratedLevel,
        max_states: int,
        max_recycles: int,
        ignore_move_limit: bool = True,
    ) -> SolveResult:
        initial_columns = tuple((tuple(self._symbolize(card_id) for card_id in column), 1 if column else 0) for column in level.columns)
        initial_state = self._canonical_state(
            initial_columns,
            tuple(self._symbolize(card_id) for card_id in level.stock),
            tuple(),
            tuple(None for _ in range(level.layout.columns_count)),
            frozenset(),
        )

        queue: list[tuple[int, int, int, int, object]] = []
        counter = 0
        heapq.heappush(queue, (0, 0, 0, counter, initial_state))
        best_moves_by_key = {(initial_state, 0): 0}
        states = 0

        while queue and states < max_states:
            _, moves_used, recycles_used, _, state = heapq.heappop(queue)
            if best_moves_by_key.get((state, recycles_used), 10**9) != moves_used:
                continue

            states += 1
            columns, _, _, _, _ = state
            if all(len(cards) == 0 for cards, _ in columns):
                pass_count = recycles_used + 1
                return SolveResult(True, moves_used, states, f"tableau_cleared_with_{pass_count}_stock_passes")
            if not ignore_move_limit and moves_used >= level.reference_moves:
                continue

            for next_state in self._next_states(state, level, allow_draw=False):
                next_moves = moves_used + 1
                key = (next_state, recycles_used)
                previous = best_moves_by_key.get(key)
                if previous is not None and previous <= next_moves:
                    continue
                best_moves_by_key[key] = next_moves
                counter += 1
                heapq.heappush(queue, (next_moves, next_moves, recycles_used, counter, next_state))

            draw_state, did_recycle = self._draw_next_with_recycle_info(state)
            if draw_state is not None:
                next_recycles = recycles_used + (1 if did_recycle else 0)
                if next_recycles <= max_recycles:
                    next_moves = moves_used + 1
                    key = (draw_state, next_recycles)
                    previous = best_moves_by_key.get(key)
                    if previous is None or previous > next_moves:
                        best_moves_by_key[key] = next_moves
                        counter += 1
                        heapq.heappush(queue, (next_moves, next_moves, next_recycles, counter, draw_state))

        reason = "state_limit" if states >= max_states else "requires_more_stock_passes"
        return SolveResult(False, None, states, reason)

    def find_stock_pressure_violation(self, level: GeneratedLevel, max_states: int) -> dict | None:
        initial_columns = tuple((tuple(self._symbolize(card_id) for card_id in column), 1 if column else 0) for column in level.columns)
        initial_stock = tuple(self._symbolize(card_id) for card_id in level.stock)
        initial_state, initial_forced_moves = self._settle_forced_moves(
            self._canonical_state(
                initial_columns,
                initial_stock,
                tuple(),
                tuple(None for _ in range(level.layout.columns_count)),
                frozenset(),
            ),
            level,
        )

        tail_allowance = self._stock_pressure_tail_allowance(level.layout)
        minimum_columns = self._minimum_tableau_cards_during_stock_pressure(level.layout)
        min_remaining_columns = self._remaining_tableau_cards(initial_state)

        queue: list[tuple[int, int, int, int, object]] = []
        counter = 0
        heapq.heappush(
            queue,
            (
                min_remaining_columns,
                0,
                initial_forced_moves,
                counter,
                initial_state,
            ),
        )
        best_moves_by_state = {initial_state: initial_forced_moves}
        states = 0

        while queue and states < max_states:
            _, _, moves_used, _, state = heapq.heappop(queue)
            if best_moves_by_state.get(state, 10**9) != moves_used:
                continue

            states += 1
            columns, queued, _, _, completed = state
            queued_count = len(queued)
            remaining_columns = self._remaining_tableau_cards(state)
            min_remaining_columns = min(min_remaining_columns, remaining_columns)
            draws = max(0, len(level.stock) - queued_count)

            if queued_count > tail_allowance and remaining_columns < minimum_columns:
                return {
                    "draws": draws,
                    "states": states,
                    "remainingColumns": remaining_columns,
                    "remainingQueued": queued_count,
                    "minimumColumns": minimum_columns,
                    "stockPressureTailAllowance": tail_allowance,
                    "minRemainingColumns": min_remaining_columns,
                    "completedGoals": len(completed),
                }

            if queued_count <= tail_allowance:
                continue

            for raw_next_state in self._next_decision_states(state, level):
                settled_next_state, forced_moves = self._settle_forced_moves(raw_next_state, level)
                next_moves = moves_used + 1 + forced_moves
                previous = best_moves_by_state.get(settled_next_state)
                if previous is not None and previous <= next_moves:
                    continue

                best_moves_by_state[settled_next_state] = next_moves
                counter += 1
                heapq.heappush(
                    queue,
                    (
                        self._remaining_tableau_cards(settled_next_state),
                        max(0, len(level.stock) - len(settled_next_state[1])),
                        next_moves,
                        counter,
                        settled_next_state,
                    ),
                )

        return None

    def simulate_greedy_stock_passes(self, level: GeneratedLevel, max_passes: int) -> dict:
        columns = [list(column) for column in level.columns]
        queued = list(level.stock)
        revealed: list[str] = []
        goals: list[tuple[str, int] | None] = [None for _ in range(level.layout.columns_count)]
        completed: set[str] = set()
        passes_completed = 0
        draws = 0
        solved_before_first_pass_end = False
        stock_pressure_tail_allowance = self._stock_pressure_tail_allowance(level.layout)
        minimum_columns_during_stock_pressure = self._minimum_tableau_cards_during_stock_pressure(level.layout)
        min_remaining_columns_during_stock_pressure = sum(len(column) for column in columns)
        early_tableau_pressure_violation: dict | None = None

        def columns_empty() -> bool:
            return all(not column for column in columns)

        def remaining_columns() -> int:
            return sum(len(column) for column in columns)

        def record_stock_pressure(stage: str) -> None:
            nonlocal min_remaining_columns_during_stock_pressure
            nonlocal early_tableau_pressure_violation
            if passes_completed != 0 or len(queued) <= stock_pressure_tail_allowance:
                return

            remaining = remaining_columns()
            min_remaining_columns_during_stock_pressure = min(
                min_remaining_columns_during_stock_pressure,
                remaining,
            )
            if remaining < minimum_columns_during_stock_pressure and early_tableau_pressure_violation is None:
                early_tableau_pressure_violation = {
                    "stage": stage,
                    "draws": draws,
                    "remainingColumns": remaining,
                    "remainingQueued": len(queued),
                    "minimumColumns": minimum_columns_during_stock_pressure,
                    "stockPressureTailAllowance": stock_pressure_tail_allowance,
                }

        def solved() -> bool:
            return len(completed) >= len(level.selected_gold)

        def faceup_run(column: list[str]) -> tuple[str, ...]:
            if not column:
                return tuple()
            association = self._card_association(column[-1])
            run_start = len(column) - 1
            while run_start - 1 >= 0:
                candidate = column[run_start - 1]
                if self._is_gold(candidate) or self._card_association(candidate) != association:
                    break
                run_start -= 1
            return tuple(column[run_start:])

        def can_goal_accept(goal: tuple[str, int] | None, stack: tuple[str, ...]) -> bool:
            return self._can_goal_slot_accept(goal, stack, level)

        def add_to_goal(goal_index: int, stack: tuple[str, ...]) -> None:
            goal = goals[goal_index]
            if self._is_gold_headed_stack(stack):
                assert goal is None
                goal_id = self._gold_id(stack[-1])
                count = len(stack) - 1
            else:
                assert goal is not None
                goal_id, count = goal
                count += len(stack)
            if count >= level.required_by_gold[goal_id]:
                goals[goal_index] = None
                completed.add(goal_id)
            else:
                goals[goal_index] = (goal_id, count)

        def settle() -> None:
            changed = True
            while changed:
                changed = False

                if revealed:
                    stack = (revealed[-1],)
                    for goal_index, goal in enumerate(goals):
                        if can_goal_accept(goal, stack):
                            revealed.pop()
                            add_to_goal(goal_index, stack)
                            changed = True
                            break
                if changed:
                    continue

                for column in columns:
                    stack = faceup_run(column)
                    if not stack:
                        continue
                    for goal_index, goal in enumerate(goals):
                        if can_goal_accept(goal, stack):
                            del column[-len(stack):]
                            add_to_goal(goal_index, stack)
                            changed = True
                            break
                    if changed:
                        break
                if changed:
                    continue

                for source_index, source_column in enumerate(columns):
                    stack = faceup_run(source_column)
                    if not stack:
                        continue
                    moving_card = stack[-1] if self._is_gold_headed_stack(stack) else stack[0]
                    for target_index, target_column in enumerate(columns):
                        if target_column and self._is_gold_headed_stack(stack) and len(stack) > 1:
                            continue
                        if source_index == target_index:
                            continue
                        if not self._can_column_accept(tuple(target_column), moving_card):
                            continue
                        if not target_column and len(source_column) == len(stack):
                            continue

                        del source_column[-len(stack):]
                        target_column.extend(stack)
                        changed = True
                        break
                    if changed:
                        break

        settle()
        record_stock_pressure("initial_settle")
        while passes_completed < max_passes:
            if solved() or columns_empty():
                if queued:
                    solved_before_first_pass_end = True
                return {
                    "solved": solved(),
                    "columnsEmpty": columns_empty(),
                    "solvedBeforeFirstPassEnd": solved_before_first_pass_end,
                    "passesCompleted": passes_completed,
                    "draws": draws,
                    "remainingColumns": remaining_columns(),
                    "remainingQueued": len(queued),
                    "remainingRevealed": len(revealed),
                    "minimumColumnsDuringStockPressure": minimum_columns_during_stock_pressure,
                    "stockPressureTailAllowance": stock_pressure_tail_allowance,
                    "minRemainingColumnsDuringStockPressure": min_remaining_columns_during_stock_pressure,
                    "earlyTableauPressureViolation": early_tableau_pressure_violation,
                }

            if queued:
                revealed.append(queued.pop(0))
                draws += 1
                settle()
                record_stock_pressure("after_draw_settle")
                if passes_completed == 0 and queued and (solved() or columns_empty()):
                    solved_before_first_pass_end = True
                continue

            passes_completed += 1
            if solved() or columns_empty() or passes_completed >= max_passes:
                break
            if not revealed:
                break
            queued = revealed
            revealed = []
            settle()

        return {
            "solved": solved(),
            "columnsEmpty": columns_empty(),
            "solvedBeforeFirstPassEnd": solved_before_first_pass_end,
            "passesCompleted": passes_completed,
            "draws": draws,
            "remainingColumns": remaining_columns(),
            "remainingQueued": len(queued),
            "remainingRevealed": len(revealed),
            "minimumColumnsDuringStockPressure": minimum_columns_during_stock_pressure,
            "stockPressureTailAllowance": stock_pressure_tail_allowance,
            "minRemainingColumnsDuringStockPressure": min_remaining_columns_during_stock_pressure,
            "earlyTableauPressureViolation": early_tableau_pressure_violation,
        }

    @staticmethod
    def _minimum_tableau_cards_during_stock_pressure(layout: Layout) -> int:
        return 2 if layout.columns_count <= 4 else 3

    @staticmethod
    def _stock_pressure_tail_allowance(layout: Layout) -> int:
        return max(4, math.ceil(layout.deck_count * 0.25))

    @staticmethod
    def _remaining_tableau_cards(state: object) -> int:
        columns, _, _, _, _ = state
        return sum(len(cards) for cards, _ in columns)

    def _next_states(self, state: object, level: GeneratedLevel, allow_draw: bool = True) -> Iterable[object]:
        columns, queued, revealed, goals, completed = state

        source_moves: list[tuple[str, int | None, tuple[str, ...], int, bool]] = []
        for column_index, (cards, faceup_count) in enumerate(columns):
            if not cards or faceup_count <= 0:
                continue
            faceup_start = len(cards) - faceup_count
            top_card = cards[-1]
            if self._is_gold(top_card):
                stack = self._top_faceup_run(cards, faceup_count)
                source_moves.append(("column", column_index, stack, len(stack), True))
                continue

            top_association = self._regular_association(top_card)
            run_start = len(cards) - 1
            while run_start - 1 >= faceup_start:
                candidate = cards[run_start - 1]
                if self._is_gold(candidate) or self._regular_association(candidate) != top_association:
                    break
                run_start -= 1

            max_run_length = len(cards) - run_start
            for stack_count in range(1, max_run_length + 1):
                stack = cards[len(cards) - stack_count :]
                source_moves.append(("column", column_index, stack, stack_count, stack_count == max_run_length))

        if revealed:
            source_moves.append(("draw", None, (revealed[-1],), 1, True))

        for source_kind, source_index, stack, stack_count, allow_column_move in source_moves:
            for goal_index, goal in enumerate(goals):
                if self._can_goal_slot_accept(goal, stack, level):
                    yield self._move_to_goal(
                        state,
                        level,
                        source_kind,
                        source_index,
                        stack,
                        stack_count,
                        goal_index,
                    )

            for target_index, (target_cards, _) in enumerate(columns):
                if not allow_column_move:
                    continue
                if target_cards and self._is_gold_headed_stack(stack) and len(stack) > 1:
                    continue
                if source_kind == "column" and source_index == target_index:
                    continue
                moving_card = stack[-1] if self._is_gold_headed_stack(stack) else stack[0]
                if self._can_column_accept(target_cards, moving_card):
                    if source_kind == "column" and not target_cards and stack_count == len(columns[source_index][0]):
                        continue
                    yield self._move_to_column(
                        state,
                        source_kind,
                        source_index,
                        stack,
                        stack_count,
                        target_index,
                    )

        if allow_draw:
            draw_state = self._draw_next(state)
            if draw_state is not None:
                yield draw_state

    def _move_to_goal(
        self,
        state: object,
        level: GeneratedLevel,
        source_kind: str,
        source_index: int | None,
        stack: tuple[str, ...],
        stack_count: int,
        goal_index: int,
    ) -> object:
        columns, queued, revealed, goals, completed = state
        columns = list(columns)
        queued = tuple(queued)
        revealed = tuple(revealed)
        goals = list(goals)
        completed = set(completed)

        columns, revealed = self._remove_from_source(columns, revealed, source_kind, source_index, stack_count)
        if self._is_gold_headed_stack(stack):
            goal_id = self._gold_id(stack[-1])
            count = len(stack) - 1
            if count >= level.required_by_gold[goal_id]:
                goals[goal_index] = None
                completed.add(goal_id)
            else:
                goals[goal_index] = (goal_id, count)
        else:
            goal_id, count = goals[goal_index]
            count += len(stack)
            if count >= level.required_by_gold[goal_id]:
                goals[goal_index] = None
                completed.add(goal_id)
            else:
                goals[goal_index] = (goal_id, count)

        return self._canonical_state(tuple(columns), queued, revealed, tuple(goals), frozenset(completed))

    def _move_to_column(
        self,
        state: object,
        source_kind: str,
        source_index: int | None,
        stack: tuple[str, ...],
        stack_count: int,
        target_index: int,
    ) -> object:
        columns, queued, revealed, goals, completed = state
        columns = list(columns)
        queued = tuple(queued)
        revealed = tuple(revealed)

        columns, revealed = self._remove_from_source(columns, revealed, source_kind, source_index, stack_count)
        target_cards, target_faceup = columns[target_index]
        columns[target_index] = (target_cards + stack, target_faceup + len(stack))
        return self._canonical_state(tuple(columns), queued, revealed, tuple(goals), frozenset(completed))

    @staticmethod
    def _remove_from_source(
        columns: list[tuple[tuple[str, ...], int]],
        revealed: tuple[str, ...],
        source_kind: str,
        source_index: int | None,
        stack_count: int,
    ) -> tuple[list[tuple[tuple[str, ...], int]], tuple[str, ...]]:
        if source_kind == "draw":
            return columns, revealed[:-1]

        assert source_index is not None
        cards, faceup = columns[source_index]
        remaining = cards[:-stack_count]
        if remaining:
            faceup = max(1, faceup - stack_count)
        else:
            faceup = 0
        columns[source_index] = (remaining, faceup)
        return columns, revealed

    def _draw_next(self, state: object) -> object | None:
        draw_state, _ = self._draw_next_with_recycle_info(state)
        return draw_state

    def _draw_next_with_recycle_info(self, state: object) -> tuple[object | None, bool]:
        columns, queued, revealed, goals, completed = state
        if queued:
            next_card = queued[0]
            return self._canonical_state(columns, queued[1:], revealed + (next_card,), goals, completed), False
        if revealed:
            queued = revealed
            next_card = queued[0]
            return self._canonical_state(columns, queued[1:], (next_card,), goals, completed), True
        return None, False

    def _can_goal_slot_accept(
        self,
        goal: tuple[str, int] | None,
        stack: tuple[str, ...],
        level: GeneratedLevel,
    ) -> bool:
        if not stack:
            return False
        if goal is None:
            return self._can_empty_goal_accept(stack, level)
        return self._can_goal_accept(goal, stack, level)

    def _can_empty_goal_accept(self, stack: tuple[str, ...], level: GeneratedLevel) -> bool:
        if not self._is_gold_headed_stack(stack):
            return False
        goal_id = self._gold_id(stack[-1])
        regular_cards = stack[:-1]
        if any(self._is_gold(card_id) or self._regular_association(card_id) != goal_id for card_id in regular_cards):
            return False
        uses_image = goal_id in level.image_goals
        if any((self._regular_association(card_id) in level.image_goals) != uses_image for card_id in regular_cards):
            return False
        return len(regular_cards) <= level.required_by_gold[goal_id]

    def _can_goal_accept(self, goal: tuple[str, int], stack: tuple[str, ...], level: GeneratedLevel) -> bool:
        goal_id, count = goal
        if any(self._is_gold(card_id) for card_id in stack):
            return False
        if any(self._regular_association(card_id) != goal_id for card_id in stack):
            return False
        uses_image = goal_id in level.image_goals
        if any((self._regular_association(card_id) in level.image_goals) != uses_image for card_id in stack):
            return False
        return count + len(stack) <= level.required_by_gold[goal_id]

    def _can_column_accept(self, target_cards: tuple[str, ...], moving_card: str) -> bool:
        if not target_cards:
            return True
        top_card = target_cards[-1]
        if self._is_gold(top_card):
            return False
        return self._regular_association(top_card) == self._card_association(moving_card)

    def _is_gold_headed_stack(self, stack: tuple[str, ...]) -> bool:
        return bool(stack) and self._is_gold(stack[-1])

    def _same_association(self, cards: tuple[str, ...]) -> bool:
        if not cards:
            return False
        association = self._regular_association(cards[0])
        return all(self._regular_association(card_id) == association for card_id in cards)

    def _is_gold(self, card_id: str) -> bool:
        if card_id.startswith("G:"):
            return True
        if card_id.startswith("R:"):
            return False
        return card_id in self.cards_by_gold

    def _symbolize(self, card_id: str) -> str:
        if card_id in self.cards_by_gold:
            return f"G:{card_id}"
        return f"R:{self.regular_to_gold[card_id]}"

    @staticmethod
    def _gold_id(card_id: str) -> str:
        return card_id[2:] if card_id.startswith("G:") else card_id

    def _regular_association(self, card_id: str) -> str:
        if card_id.startswith("R:"):
            return card_id[2:]
        return self.regular_to_gold[card_id]

    def _canonical_state(
        self,
        columns: tuple[tuple[tuple[str, ...], int], ...],
        queued: tuple[str, ...],
        revealed: tuple[str, ...],
        goals: tuple[tuple[str, int] | None, ...],
        completed: frozenset[str],
    ) -> object:
        normalized_columns = tuple(sorted(columns, key=lambda item: (item[0], item[1])))
        normalized_goals = tuple(sorted(goals, key=lambda item: ("", -1) if item is None else item))
        return normalized_columns, queued, revealed, normalized_goals, completed

    def _priority(self, state: object, level: GeneratedLevel, moves_used: int) -> int:
        columns, queued, revealed, goals, completed = state
        hidden = sum(max(0, len(cards) - faceup) for cards, faceup in columns)
        active_goal_cards = sum(count for goal in goals if goal is not None for _, count in [goal])
        active_goals = sum(1 for goal in goals if goal is not None)
        unresolved = len(level.selected_gold) - len(completed)
        stock_weight = max(0, len(queued) - 1)
        return (
            moves_used * 8
            + hidden * 30
            + unresolved * 45
            + stock_weight
            - len(completed) * 200
            - active_goal_cards * 12
            - active_goals * 8
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate generated Solitaire association deals with a search solver.")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--samples", type=int, default=100)
    parser.add_argument("--seed-start", type=int, default=1)
    parser.add_argument("--level-number", type=int, default=1)
    parser.add_argument("--max-states", type=int, default=120_000)
    parser.add_argument("--ignore-move-limit", action="store_true")
    parser.add_argument("--mode", choices=("guaranteed", "random"), default="guaranteed")
    parser.add_argument("--max-attempts", type=int, default=20)
    parser.add_argument("--layout", action="append", help="Limit to layout id. Can be passed multiple times.")
    args = parser.parse_args()

    validator = SolvabilityValidator(args.root)
    layouts = [layout for layout in validator.layouts if not args.layout or layout.id in set(args.layout)]
    if not layouts:
        raise SystemExit("No layouts selected.")

    print("=== DATA ===")
    print(f"gold_categories={len(validator.cards_by_gold)}")
    print(f"regular_cards={len(validator.regular_ids)}")
    print(f"premium_png={len(validator.image_ids)}")
    print(
        "full_image_categories="
        + ",".join(f"{gold_id}:{validator.gold_titles[gold_id]}" for gold_id in sorted(validator.full_image_goals))
    )
    print(f"image_start_level={validator.image_start_level}")
    print(f"image_percentage={validator.image_percentage}")
    print("=== SOLVABILITY ===")

    overall_solved = 0
    overall_generated = 0
    for layout in layouts:
        solved = 0
        generated = 0
        failed_generation = 0
        unsolved: list[tuple[int, SolveResult]] = []
        move_counts: list[int] = []
        state_counts: list[int] = []
        attempt_counts: list[int] = []

        for offset in range(args.samples):
            seed = args.seed_start + offset
            level = validator.generate_guaranteed(layout, seed, args.level_number, args.max_attempts) if args.mode == "guaranteed" else validator.generate_random(layout, seed, args.level_number)
            if level is None:
                failed_generation += 1
                continue

            generated += 1
            attempt_counts.append(level.generation_attempts)
            result = validator.solve(level, args.max_states, args.ignore_move_limit)
            state_counts.append(result.states)
            if result.solved:
                solved += 1
                assert result.moves is not None
                move_counts.append(result.moves)
            else:
                unsolved.append((seed, result))

        overall_solved += solved
        overall_generated += generated
        rate = (solved / generated * 100.0) if generated else 0.0
        min_moves = min(move_counts) if move_counts else None
        max_moves = max(move_counts) if move_counts else None
        avg_moves = (sum(move_counts) / len(move_counts)) if move_counts else 0.0
        avg_states = (sum(state_counts) / len(state_counts)) if state_counts else 0.0
        avg_attempts = (sum(attempt_counts) / len(attempt_counts)) if attempt_counts else 0.0
        examples = ",".join(f"{seed}:{result.reason}/{result.states}" for seed, result in unsolved[:8])
        print(
            f"{layout.id}: generated={generated} generation_failed={failed_generation} "
            f"solved={solved}/{generated} rate={rate:.1f}% moves_min_avg_max={min_moves}/{avg_moves:.1f}/{max_moves} "
            f"avg_states={avg_states:.0f} avg_attempts={avg_attempts:.1f} mode={args.mode} unsolved_examples={examples or '-'}"
        )

    overall_rate = (overall_solved / overall_generated * 100.0) if overall_generated else 0.0
    print(f"OVERALL: solved={overall_solved}/{overall_generated} rate={overall_rate:.1f}%")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
