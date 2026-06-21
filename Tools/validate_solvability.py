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
                assigned = ordinary_ids[:ordinary_per_gold]
                remaining = ordinary_ids[ordinary_per_gold:]

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
        solution_order: list[str] = []
        solution_ids: set[str] = set()
        for gold_id in selected_order:
            solution_order.append(gold_id)
            solution_ids.add(gold_id)
            required_count = draft.required_by_gold[gold_id]
            pool_cards = [
                card_id
                for card_id in self.cards_by_gold[gold_id]
                if any(card_id in column for column in draft.columns) or card_id in draft.stock
            ]
            pool_cards = pool_cards[:required_count]
            if len(pool_cards) < required_count:
                return None
            for card_id in pool_cards:
                solution_order.append(card_id)
                solution_ids.add(card_id)

        all_cards = [card_id for column in draft.columns for card_id in column] + list(draft.stock)
        extras = [card_id for card_id in all_cards if card_id not in solution_ids]
        random.Random(seed).shuffle(extras)
        deal_order = solution_order + extras

        columns: list[tuple[str, ...]] = []
        cursor = 0
        column_solution_cards = 0
        for count in layout.column_counts:
            chunk = deal_order[cursor: cursor + count]
            for index in range(cursor, min(cursor + count, len(solution_order))):
                column_solution_cards += 1
            cursor += count
            columns.append(tuple(reversed(chunk)))

        stock = tuple(deal_order[cursor: cursor + layout.deck_count])
        if sum(len(column) for column in columns) + len(stock) != len(all_cards):
            return None

        stock_solution_cards = max(0, len(solution_order) - column_solution_cards)
        construction_moves = column_solution_cards + (stock_solution_cards * 2)
        reference_moves = max(layout.reference_moves, construction_moves)

        return GeneratedLevel(
            layout,
            tuple(columns),
            stock,
            draft.required_by_gold,
            draft.image_cards,
            draft.image_goals,
            draft.selected_gold,
            reference_moves,
            attempt,
            "guaranteed_by_construction",
            construction_moves,
        )

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

    def _next_states(self, state: object, level: GeneratedLevel) -> Iterable[object]:
        columns, queued, revealed, goals, completed = state

        source_moves: list[tuple[str, int | None, tuple[str, ...], int, bool]] = []
        for column_index, (cards, faceup_count) in enumerate(columns):
            if not cards or faceup_count <= 0:
                continue
            faceup_start = len(cards) - faceup_count
            top_card = cards[-1]
            if self._is_gold(top_card):
                source_moves.append(("column", column_index, (top_card,), 1, False))
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
            first_card = stack[0]
            if self._is_gold(first_card):
                for goal_index, goal in enumerate(goals):
                    if goal is None:
                        yield self._move_to_goal(
                            state,
                            level,
                            source_kind,
                            source_index,
                            stack,
                            stack_count,
                            goal_index,
                        )
                        break
                continue

            for goal_index, goal in enumerate(goals):
                if goal is None:
                    continue
                if self._can_goal_accept(goal, stack, level):
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
                if source_kind == "column" and source_index == target_index:
                    continue
                if self._can_column_accept(target_cards, first_card):
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
        first_card = stack[0]
        if self._is_gold(first_card):
            goals[goal_index] = (self._gold_id(first_card), 0)
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
        columns, queued, revealed, goals, completed = state
        if queued:
            next_card = queued[0]
            return self._canonical_state(columns, queued[1:], revealed + (next_card,), goals, completed)
        if revealed:
            queued = revealed
            next_card = queued[0]
            return self._canonical_state(columns, queued[1:], (next_card,), goals, completed)
        return None

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
        if self._is_gold(moving_card):
            return False
        if not target_cards:
            return True
        top_card = target_cards[-1]
        if self._is_gold(top_card):
            return False
        return self._regular_association(top_card) == self._regular_association(moving_card)

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
