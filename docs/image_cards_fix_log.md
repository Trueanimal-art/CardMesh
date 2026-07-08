# Image Cards Physical PNG Restore Log

Дата: 2026-07-07

## Вывод после повторной диагностики

Предыдущий импортный фикс (`FullRect`, `textureRect=0:0:240:240`) не решал основную проблему: часть файлов в `Assets/Resources/ImageCards/Premium/*.png` уже содержала физически или семантически обрезанный арт. Увеличение transparent padding вокруг такого PNG не восстанавливает утраченные части объекта.

Первопричина для текущей партии: битые PNG/исходные repair-срезы, а не UI layout, `cards.json`, anchors, scale или RectTransform. Для карт без чистого source сделан новый точечный арт; для остальных поставлены конкретные восстановленные PNG из существующих repair-источников.

## Что изменено

- Восстановлено target PNG: 140.
- Дополнительный второй проход по новым стрелкам: 26 PNG.
- Новый точечный арт: 26 карт.
- Восстановлено из `ai_atlas_repair/sliced`: 103 карт.
- Восстановлено из `final_cleanup_latest/sliced`: 11 карт.
- Не менялись: `Assets/Resources/Data/cards.json`, UI layout, RectTransform, anchors, scale.

## Таблица исправлений

| card id | file | проблема до | источник восстановления | действие | результат |
|---|---|---|---|---|---|
| `art_color` | `Assets/Resources/ImageCards/Premium/art_color.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `art_frame` | `Assets/Resources/ImageCards/Premium/art_frame.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `art_paint` | `Assets/Resources/ImageCards/Premium/art_paint.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `body_smile` | `Assets/Resources/ImageCards/Premium/body_smile.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `books_cover` | `Assets/Resources/ImageCards/Premium/books_cover.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `breakfast_butter` | `Assets/Resources/ImageCards/Premium/breakfast_butter.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `breakfast_cereal` | `Assets/Resources/ImageCards/Premium/breakfast_cereal.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `breakfast_porridge` | `Assets/Resources/ImageCards/Premium/breakfast_porridge.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `breakfast_toast` | `Assets/Resources/ImageCards/Premium/breakfast_toast.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `breakfast_yogurt` | `Assets/Resources/ImageCards/Premium/breakfast_yogurt.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `business_sale` | `Assets/Resources/ImageCards/Premium/business_sale.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `business_team` | `Assets/Resources/ImageCards/Premium/business_team.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `camping_fire` | `Assets/Resources/ImageCards/Premium/camping_fire.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `camping_lantern` | `Assets/Resources/ImageCards/Premium/camping_lantern.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `camping_map` | `Assets/Resources/ImageCards/Premium/camping_map.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `camping_tent` | `Assets/Resources/ImageCards/Premium/camping_tent.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `cinema_ticket` | `Assets/Resources/ImageCards/Premium/cinema_ticket.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `cinema_trailer` | `Assets/Resources/ImageCards/Premium/cinema_trailer.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `city_park` | `Assets/Resources/ImageCards/Premium/city_park.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `city_street` | `Assets/Resources/ImageCards/Premium/city_street.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `city_subway` | `Assets/Resources/ImageCards/Premium/city_subway.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `city_traffic` | `Assets/Resources/ImageCards/Premium/city_traffic.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `clothes_glove` | `Assets/Resources/ImageCards/Premium/clothes_glove.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `clothes_scarf` | `Assets/Resources/ImageCards/Premium/clothes_scarf.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_black` | `Assets/Resources/ImageCards/Premium/colors_black.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_blue` | `Assets/Resources/ImageCards/Premium/colors_blue.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_green` | `Assets/Resources/ImageCards/Premium/colors_green.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_orange` | `Assets/Resources/ImageCards/Premium/colors_orange.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_pink` | `Assets/Resources/ImageCards/Premium/colors_pink.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_red` | `Assets/Resources/ImageCards/Premium/colors_red.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_white` | `Assets/Resources/ImageCards/Premium/colors_white.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `colors_yellow` | `Assets/Resources/ImageCards/Premium/colors_yellow.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `dessert_cookie` | `Assets/Resources/ImageCards/Premium/dessert_cookie.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `dessert_cream` | `Assets/Resources/ImageCards/Premium/dessert_cream.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `dessert_icecream` | `Assets/Resources/ImageCards/Premium/dessert_icecream.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `dessert_pie` | `Assets/Resources/ImageCards/Premium/dessert_pie.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `dessert_waffle` | `Assets/Resources/ImageCards/Premium/dessert_waffle.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `drinks_coffee` | `Assets/Resources/ImageCards/Premium/drinks_coffee.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `drinks_lemonade` | `Assets/Resources/ImageCards/Premium/drinks_lemonade.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `drinks_smoothie` | `Assets/Resources/ImageCards/Premium/drinks_smoothie.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `emotions_calm` | `Assets/Resources/ImageCards/Premium/emotions_calm.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `emotions_interest` | `Assets/Resources/ImageCards/Premium/emotions_interest.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `emotions_joy` | `Assets/Resources/ImageCards/Premium/emotions_joy.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `emotions_warmth` | `Assets/Resources/ImageCards/Premium/emotions_warmth.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `family_album` | `Assets/Resources/ImageCards/Premium/family_album.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `family_dinner` | `Assets/Resources/ImageCards/Premium/family_dinner.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `family_home` | `Assets/Resources/ImageCards/Premium/family_home.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `family_mother` | `Assets/Resources/ImageCards/Premium/family_mother.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `family_sister` | `Assets/Resources/ImageCards/Premium/family_sister.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_barn` | `Assets/Resources/ImageCards/Premium/farm_barn.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_chicken` | `Assets/Resources/ImageCards/Premium/farm_chicken.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_cow` | `Assets/Resources/ImageCards/Premium/farm_cow.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_fence` | `Assets/Resources/ImageCards/Premium/farm_fence.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_field` | `Assets/Resources/ImageCards/Premium/farm_field.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_hay` | `Assets/Resources/ImageCards/Premium/farm_hay.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `farm_tractor` | `Assets/Resources/ImageCards/Premium/farm_tractor.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `flowers_bouquet` | `Assets/Resources/ImageCards/Premium/flowers_bouquet.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `flowers_peony` | `Assets/Resources/ImageCards/Premium/flowers_peony.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `forest_berry` | `Assets/Resources/ImageCards/Premium/forest_berry.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `forest_fern` | `Assets/Resources/ImageCards/Premium/forest_fern.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `forest_fox` | `Assets/Resources/ImageCards/Premium/forest_fox.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `forest_path` | `Assets/Resources/ImageCards/Premium/forest_path.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `forest_stump` | `Assets/Resources/ImageCards/Premium/forest_stump.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_chat` | `Assets/Resources/ImageCards/Premium/friendship_chat.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_friend` | `Assets/Resources/ImageCards/Premium/friendship_friend.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_memory` | `Assets/Resources/ImageCards/Premium/friendship_memory.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_secret` | `Assets/Resources/ImageCards/Premium/friendship_secret.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_smile` | `Assets/Resources/ImageCards/Premium/friendship_smile.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_team` | `Assets/Resources/ImageCards/Premium/friendship_team.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `friendship_walk` | `Assets/Resources/ImageCards/Premium/friendship_walk.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `garden_bench` | `Assets/Resources/ImageCards/Premium/garden_bench.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `garden_bush` | `Assets/Resources/ImageCards/Premium/garden_bush.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `garden_path` | `Assets/Resources/ImageCards/Premium/garden_path.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `garden_soil` | `Assets/Resources/ImageCards/Premium/garden_soil.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `health_care` | `Assets/Resources/ImageCards/Premium/health_care.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `health_clinic` | `Assets/Resources/ImageCards/Premium/health_clinic.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `health_sleep` | `Assets/Resources/ImageCards/Premium/health_sleep.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `hobbies_baking` | `Assets/Resources/ImageCards/Premium/hobbies_baking.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `hobbies_dance` | `Assets/Resources/ImageCards/Premium/hobbies_dance.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `hobbies_drawing` | `Assets/Resources/ImageCards/Premium/hobbies_drawing.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `hobbies_music` | `Assets/Resources/ImageCards/Premium/hobbies_music.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `hobbies_puzzle` | `Assets/Resources/ImageCards/Premium/hobbies_puzzle.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `holidays_candle` | `Assets/Resources/ImageCards/Premium/holidays_candle.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `home_key` | `Assets/Resources/ImageCards/Premium/home_key.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `internet_chat` | `Assets/Resources/ImageCards/Premium/internet_chat.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `internet_cloud` | `Assets/Resources/ImageCards/Premium/internet_cloud.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `internet_mail` | `Assets/Resources/ImageCards/Premium/internet_mail.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `kitchen_fork` | `Assets/Resources/ImageCards/Premium/kitchen_fork.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `kitchen_spoon` | `Assets/Resources/ImageCards/Premium/kitchen_spoon.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `music_concert` | `Assets/Resources/ImageCards/Premium/music_concert.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `music_guitar` | `Assets/Resources/ImageCards/Premium/music_guitar.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `music_piano` | `Assets/Resources/ImageCards/Premium/music_piano.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `music_rhythm` | `Assets/Resources/ImageCards/Premium/music_rhythm.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `nature_cloud` | `Assets/Resources/ImageCards/Premium/nature_cloud.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `nature_meadow` | `Assets/Resources/ImageCards/Premium/nature_meadow.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `nature_river` | `Assets/Resources/ImageCards/Premium/nature_river.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `nature_stone` | `Assets/Resources/ImageCards/Premium/nature_stone.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `office_badge` | `Assets/Resources/ImageCards/Premium/office_badge.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `office_paper` | `Assets/Resources/ImageCards/Premium/office_paper.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `office_printer` | `Assets/Resources/ImageCards/Premium/office_printer.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `office_screen` | `Assets/Resources/ImageCards/Premium/office_screen.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `phone_case` | `Assets/Resources/ImageCards/Premium/phone_case.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `phone_message` | `Assets/Resources/ImageCards/Premium/phone_message.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `photo_frame` | `Assets/Resources/ImageCards/Premium/photo_frame.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `photo_shot` | `Assets/Resources/ImageCards/Premium/photo_shot.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `photo_studio` | `Assets/Resources/ImageCards/Premium/photo_studio.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `school_bell` | `Assets/Resources/ImageCards/Premium/school_bell.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `school_board` | `Assets/Resources/ImageCards/Premium/school_board.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `school_desk` | `Assets/Resources/ImageCards/Premium/school_desk.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `shapes_circle` | `Assets/Resources/ImageCards/Premium/shapes_circle.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `shapes_cube` | `Assets/Resources/ImageCards/Premium/shapes_cube.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `shapes_ring` | `Assets/Resources/ImageCards/Premium/shapes_ring.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `shapes_triangle` | `Assets/Resources/ImageCards/Premium/shapes_triangle.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `shoes_boot` | `Assets/Resources/ImageCards/Premium/shoes_boot.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `shoes_heel` | `Assets/Resources/ImageCards/Premium/shoes_heel.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `shoes_pair` | `Assets/Resources/ImageCards/Premium/shoes_pair.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `shoes_sandal` | `Assets/Resources/ImageCards/Premium/shoes_sandal.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `sport_field` | `Assets/Resources/ImageCards/Premium/sport_field.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `sport_race` | `Assets/Resources/ImageCards/Premium/sport_race.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `sport_team` | `Assets/Resources/ImageCards/Premium/sport_team.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `summer_beach` | `Assets/Resources/ImageCards/Premium/summer_beach.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `summer_hat` | `Assets/Resources/ImageCards/Premium/summer_hat.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `summer_lake` | `Assets/Resources/ImageCards/Premium/summer_lake.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `summer_picnic` | `Assets/Resources/ImageCards/Premium/summer_picnic.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `summer_sandals` | `Assets/Resources/ImageCards/Premium/summer_sandals.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `technology_chip` | `Assets/Resources/ImageCards/Premium/technology_chip.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `technology_drone` | `Assets/Resources/ImageCards/Premium/technology_drone.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `time_night` | `Assets/Resources/ImageCards/Premium/time_night.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `time_time` | `Assets/Resources/ImageCards/Premium/time_time.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `tools_drill` | `Assets/Resources/ImageCards/Premium/tools_drill.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `tools_saw` | `Assets/Resources/ImageCards/Premium/tools_saw.png` | runtime PNG был старой/обрезанной версией; использован лучший доступный clean source | Tools/ImageCardAudit/final_cleanup_latest/sliced | уже совпадал с выбранным source | полный объект на preview, margin >= 20px |
| `toys_car` | `Assets/Resources/ImageCards/Premium/toys_car.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `toys_puzzle` | `Assets/Resources/ImageCards/Premium/toys_puzzle.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `toys_robot` | `Assets/Resources/ImageCards/Premium/toys_robot.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `travel_map` | `Assets/Resources/ImageCards/Premium/travel_map.png` | PNG физически/семантически обрезан; чистый полный source не найден или старый cleaner портил детали | новый точечный арт через image_gen + chroma-key cleanup | заменен PNG | полный объект на preview, margin >= 20px |
| `winter_cocoa` | `Assets/Resources/ImageCards/Premium/winter_cocoa.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `winter_sleigh` | `Assets/Resources/ImageCards/Premium/winter_sleigh.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `work_call` | `Assets/Resources/ImageCards/Premium/work_call.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `work_manager` | `Assets/Resources/ImageCards/Premium/work_manager.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |
| `work_salary` | `Assets/Resources/ImageCards/Premium/work_salary.png` | runtime PNG был старой/обрезанной версией; найден полный repair-source в том же стиле | Tools/ImageCardAudit/ai_atlas_repair/sliced | заменен PNG | полный объект на preview, margin >= 20px |

## Проверка

- Собран filesystem preview из реальных PNG после установки: `Tools/ImageCardAudit/physical_png_restore_preview/runtime_pages_by_id/page_01.png` ... `page_14.png`.
- Собран target contact sheet: `Tools/ImageCardAudit/physical_png_restore_preview/target_png_contact_sheet.png`.
- `python3 Tools/normalize_imagecard_assets.py --audit-only --root Assets/Resources/ImageCards/Premium --min-margin 20 --report Tools/ImageCardAudit/physical_png_restore_preview/post_restore_margin_audit.json`: `checked=402 normalized=0 failures=0`.
- Отдельная проверка 140 target-карт: `target_margin_failures=0`.
- Unity batch diagnostics не завершился из-за лицензирования Unity: `IPC channel to LicensingClient doesn't exist; aborting`, exit code 199. Это не ошибка PNG и не ошибка компиляции; поэтому контроль выполнен через filesystem preview из фактических runtime PNG.

## Почему предыдущие попытки не устранили проблему

- Alpha-margin audit проверял только расстояние непрозрачных пикселей до края, но не проверял, целый ли сам объект.
- `SpriteMeshType.FullRect` корректен для импорта, но не может показать пиксели, которых уже нет внутри PNG.
- Старый cleaner/chroma pipeline иногда портил реальные цветные детали, поэтому source-atlas нельзя было применять массово.
- Часть карт была восстановлена из старых срезов, которые визуально оставались тем же битым артом.

## Измененные инструменты/артефакты проверки

- `Tools/ImageCardAudit/manual_regenerated/`: новые точечные source/final PNG и `install_report.json`.
- `Tools/ImageCardAudit/physical_png_restore_preview/`: runtime-style preview pages, target contact sheet, margin audit.
- `docs/image_cards_fix_log.md`: этот журнал.

## Второй проход по новым стрелкам 2026-07-07

После ручной проверки в Unity пользователь прислал новый набор страниц со значительно меньшим числом оставшихся проблем. Старые проверки по alpha-margin уже проходили, но визуально часть объектов всё ещё выглядела недовосстановленной или слишком близкой к краям внутри карточки. Для этого набора не менялись UI, layout, `cards.json`, RectTransform, anchors или scale.

Новый точечный image generation был проверен на пилотной карте, но результат не совпал с текущим 3D-icon стилем, поэтому он не использован для установки. Вместо этого добавлен детерминированный инструмент `Tools/fix_new_arrow_imagecards.py`: он берёт существующий детальный PNG, удаляет остаточные key/atlas fragments, пересаживает объект на чистый прозрачный `240x240` холст и ограничивает объект `maxSide=138`, что даёт безопасный визуальный запас.

| card id | file | проблема до | источник восстановления | действие | результат |
|---|---|---|---|---|---|
| `breakfast_cereal` | `Assets/Resources/ImageCards/Premium/breakfast_cereal.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,57,51,57]` |
| `cinema_actor` | `Assets/Resources/ImageCards/Premium/cinema_actor.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[52,51,53,51]` |
| `city_park` | `Assets/Resources/ImageCards/Premium/city_park.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[72,51,73,51]` |
| `clothes_glove` | `Assets/Resources/ImageCards/Premium/clothes_glove.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[76,51,76,51]` |
| `dessert_waffle` | `Assets/Resources/ImageCards/Premium/dessert_waffle.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[53,51,53,51]` |
| `emotions_calm` | `Assets/Resources/ImageCards/Premium/emotions_calm.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[56,51,56,51]` |
| `emotions_warmth` | `Assets/Resources/ImageCards/Premium/emotions_warmth.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,53,51,54]` |
| `garden_soil` | `Assets/Resources/ImageCards/Premium/garden_soil.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,64,51,65]` |
| `hobbies_drawing` | `Assets/Resources/ImageCards/Premium/hobbies_drawing.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[61,51,62,51]` |
| `home_key` | `Assets/Resources/ImageCards/Premium/home_key.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[69,51,69,51]` |
| `insects_bee` | `Assets/Resources/ImageCards/Premium/insects_bee.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[60,51,60,51]` |
| `insects_butterfly` | `Assets/Resources/ImageCards/Premium/insects_butterfly.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[58,51,58,51]` |
| `kitchen_knife` | `Assets/Resources/ImageCards/Premium/kitchen_knife.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[86,51,87,51]` |
| `music_piano` | `Assets/Resources/ImageCards/Premium/music_piano.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[55,51,56,51]` |
| `nature_cloud` | `Assets/Resources/ImageCards/Premium/nature_cloud.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,64,51,64]` |
| `school_desk` | `Assets/Resources/ImageCards/Premium/school_desk.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[64,51,64,51]` |
| `shoes_pair` | `Assets/Resources/ImageCards/Premium/shoes_pair.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,59,51,60]` |
| `sport_field` | `Assets/Resources/ImageCards/Premium/sport_field.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,54,51,54]` |
| `sport_race` | `Assets/Resources/ImageCards/Premium/sport_race.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,53,51,53]` |
| `sport_team` | `Assets/Resources/ImageCards/Premium/sport_team.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[54,51,55,51]` |
| `summer_lake` | `Assets/Resources/ImageCards/Premium/summer_lake.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,57,51,58]` |
| `tools_drill` | `Assets/Resources/ImageCards/Premium/tools_drill.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[64,51,64,51]` |
| `tools_saw` | `Assets/Resources/ImageCards/Premium/tools_saw.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[80,51,80,51]` |
| `tools_wrench` | `Assets/Resources/ImageCards/Premium/tools_wrench.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[74,51,75,51]` |
| `toys_car` | `Assets/Resources/ImageCards/Premium/toys_car.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[51,57,51,58]` |
| `toys_train` | `Assets/Resources/ImageCards/Premium/toys_train.png` | визуально оставалась среди отмеченных стрелкой после первого восстановления | существующий детальный PNG | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[57,51,57,51]` |

Проверка второго прохода:

- `Tools/ImageCardAudit/new_arrow_second_pass/before_after.png`: before/after по 26 новым стрелкам.
- `Tools/ImageCardAudit/new_arrow_second_pass/runtime_pages_by_id/page_01.png` ... `page_14.png`: filesystem preview из реальных PNG после применения, в алфавитном порядке id как в Unity-экране.
- `Tools/ImageCardAudit/new_arrow_second_pass/report.json`: все 26 карт имеют `result=ok`, минимальный запас по target-набору `>= 51px`.
- `Tools/ImageCardAudit/new_arrow_second_pass/post_second_pass_margin_audit.json`: `checked=402 normalized=0 failures=0`.
- `git diff -- Assets/Resources/Data/cards.json | wc -l`: `0`.

## Третий проход по финальному набору стрелок 2026-07-07

После повторного ручного прохода пользователь прислал последний малый набор оставшихся визуальных проблем. Был проверен вариант полной процедурной регенерации в `Tools/ImageCardAudit/final_arrow_regenerated/`, но он был отбракован: часть карт теряла текущий 3D-icon стиль и становилась слишком плоской/схематичной. Поэтому установлен не flat-source, а детальная версия текущего арта с более жёстким cleanup/refit.

Использован тот же воспроизводимый инструмент `Tools/fix_new_arrow_imagecards.py`, расширенный параметрами `--ids` и `--max-side`. Для этого прохода применено `--max-side 124`, чтобы дать ещё больший безопасный transparent padding.

| card id | file | проблема до | источник восстановления | действие | результат |
|---|---|---|---|---|---|
| `camping_fire` | `Assets/Resources/ImageCards/Premium/camping_fire.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[73,58,73,58]` |
| `camping_backpack` | `Assets/Resources/ImageCards/Premium/camping_backpack.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[71,58,72,58]` |
| `cinema_film` | `Assets/Resources/ImageCards/Premium/cinema_film.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[72,58,73,58]` |
| `clothes_glove` | `Assets/Resources/ImageCards/Premium/clothes_glove.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[80,58,81,58]` |
| `dessert_waffle` | `Assets/Resources/ImageCards/Premium/dessert_waffle.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[60,58,60,58]` |
| `emotions_warmth` | `Assets/Resources/ImageCards/Premium/emotions_warmth.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[58,60,58,60]` |
| `garden_soil` | `Assets/Resources/ImageCards/Premium/garden_soil.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[58,70,58,70]` |
| `insects_butterfly` | `Assets/Resources/ImageCards/Premium/insects_butterfly.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[64,58,65,58]` |
| `sport_field` | `Assets/Resources/ImageCards/Premium/sport_field.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[58,60,58,61]` |
| `summer_lake` | `Assets/Resources/ImageCards/Premium/summer_lake.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[58,65,58,65]` |
| `time_day` | `Assets/Resources/ImageCards/Premium/time_day.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[69,58,70,58]` |
| `tools_drill` | `Assets/Resources/ImageCards/Premium/tools_drill.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[69,58,70,58]` |
| `tools_saw` | `Assets/Resources/ImageCards/Premium/tools_saw.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[84,58,84,58]` |
| `toys_car` | `Assets/Resources/ImageCards/Premium/toys_car.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[58,64,58,64]` |
| `toys_train` | `Assets/Resources/ImageCards/Premium/toys_train.png` | отмечена стрелкой в финальном наборе | существующий детальный PNG; flat-регенерация отбракована | cleanup + refit на прозрачный 240x240 холст | объект полный, margins `[63,58,64,58]` |

Проверка третьего прохода:

- `Tools/ImageCardAudit/final_arrow_detail_cleanup/before_after.png`: before/after по 15 финальным стрелкам.
- `Tools/ImageCardAudit/final_arrow_detail_cleanup/runtime_pages_by_id/page_02.png`, `page_03.png`, `page_04.png`, `page_06.png`, `page_07.png`, `page_11.png`, `page_12.png`: filesystem preview из реальных PNG после применения.
- `Tools/ImageCardAudit/final_arrow_detail_cleanup/report.json`: все 15 карт имеют `result=ok`, минимальный запас по target-набору `>= 58px`.
- `Tools/ImageCardAudit/final_arrow_detail_cleanup/post_final_arrow_margin_audit.json`: `checked=402 normalized=0 failures=0`.
- `git diff -- Assets/Resources/Data/cards.json | wc -l`: `0`.

## Финальная замена последних 10 стрелок 2026-07-07

Пользователь открыл предыдущий `before_after.png` из Codex-чата и справедливо указал, что `after` выглядел как уменьшенный/плоский replacement, а не как текущий 3D-icon стиль. Поэтому предыдущая процедурная замена для этих карт отбракована. В этом проходе использован только новый пользовательский атлас `/Users/vnm/Downloads/ChatGPT Image 7 июл. 2026 г., 13_41_38.png`; остальные карты не обрабатывались и не откатывались.

Новый источник: пользовательский atlas с ровно 10 нужными иконками. Извлечение сделано инструментом `Tools/extract_user_atlas_last10.py`: сначала проверен локальный mask preview, он отбракован из-за остаточного gradient-фона; затем применён `rembg`/`u2netp`, результат проверен по фактическим PNG в `Assets/Resources/ImageCards/Premium`.

| card id | file | проблема до | источник восстановления | действие | результат |
|---|---|---|---|---|---|
| `camping_backpack` | `Assets/Resources/ImageCards/Premium/camping_backpack.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[38,36,39,36]` |
| `city_park` | `Assets/Resources/ImageCards/Premium/city_park.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,39,36,40]` |
| `clothes_glove` | `Assets/Resources/ImageCards/Premium/clothes_glove.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[61,36,62,36]` |
| `dessert_waffle` | `Assets/Resources/ImageCards/Premium/dessert_waffle.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,52,36,52]` |
| `emotions_warmth` | `Assets/Resources/ImageCards/Premium/emotions_warmth.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,76,36,76]` |
| `garden_bench` | `Assets/Resources/ImageCards/Premium/garden_bench.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт имел нижний цветной артефакт | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,65,36,66]` |
| `garden_soil` | `Assets/Resources/ImageCards/Premium/garden_soil.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,52,36,52]` |
| `sport_field` | `Assets/Resources/ImageCards/Premium/sport_field.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,70,36,71]` |
| `tools_saw` | `Assets/Resources/ImageCards/Premium/tools_saw.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,73,36,73]`; отдельно открыт PNG, detached artifacts не обнаружены |
| `toys_train` | `Assets/Resources/ImageCards/Premium/toys_train.png` | procedural replacement не соответствовал 3D-icon стилю; старый арт был спорным/обрезанным | пользовательский atlas + `rembg` | вырезан из атласа, фон удалён, помещён на transparent 240x240 | полный объект, margins `[36,68,36,68]` |

Проверка финальной замены:

- `Tools/ImageCardAudit/user_atlas_last10_rembg/contact_sheet.png`: preview вырезки из пользовательского atlas до применения.
- `Tools/ImageCardAudit/user_atlas_last10_rembg/applied_assets_contact.png`: contact sheet из фактических PNG в `Assets/Resources/ImageCards/Premium` после применения.
- `Tools/ImageCardAudit/user_atlas_last10_rembg/report.json`: все 10 PNG имеют прозрачный запас от 36px и больше.
- `git diff -- Assets/Resources/Data/cards.json | wc -l`: `0`.
- `git diff --name-only -- Assets/Resources/Data/cards.json <10 target PNG>`: в списке только 10 PNG, `cards.json` отсутствует.
- UI, layout, RectTransform, anchors, scale и `cards.json` не менялись.

## Remaining broken cards

Пусто для карт, отмеченных стрелками в последнем наборе скриншотов. По filesystem preview из реальных PNG и margin-аудиту target-карт оставшихся обрезанных PNG среди этих 10 стрелок не обнаружено.

## Финальная замена последних 2 стрелок 2026-07-07

После полного ручного прохода пользователь отметил ещё две проблемные карты на странице 8/14: `kitchen_kettle` и `music_piano`. В этом проходе заменены только эти два PNG; остальные карты не обрабатывались.

Пользовательские источники:

- `kitchen_kettle`: `/Users/vnm/Downloads/ChatGPT Image 7 июл. 2026 г., 14_42_20.png`
- `music_piano`: `/Users/vnm/Downloads/ChatGPT Image 7 июл. 2026 г., 14_44_50.png`

Оба source PNG визуально выглядели готовыми, но alpha bbox занимал почти весь `1536x1024` кадр, то есть gradient-фон всё ещё присутствовал. Поэтому применён отдельный extractor `Tools/extract_final_two_imagecards.py`: `rembg`/`u2netp`, фильтрация alpha-компонент, fit на transparent `240x240` холст.

| card id | file | проблема до | источник восстановления | действие | результат |
|---|---|---|---|---|---|
| `kitchen_kettle` | `Assets/Resources/ImageCards/Premium/kitchen_kettle.png` | отмечена стрелкой как последняя визуально кривая карта | пользовательский PNG + `rembg` | фон удалён, объект помещён на transparent 240x240 | полный объект, margins `[39,36,40,36]` |
| `music_piano` | `Assets/Resources/ImageCards/Premium/music_piano.png` | отмечена стрелкой как последняя визуально кривая карта | пользовательский PNG + `rembg` | фон удалён, объект помещён на transparent 240x240 | полный объект, margins `[36,46,36,47]` |

Проверка:

- `Tools/ImageCardAudit/final_two_replacements/before_source_after.png`: before/source/final Assets PNG по двум картам.
- `Tools/ImageCardAudit/final_two_replacements/report.json`: обе карты имеют transparent padding не меньше 36px.
- `git diff -- Assets/Resources/Data/cards.json | wc -l`: `0`.
- `git diff --name-only -- Assets/Resources/Data/cards.json Assets/Resources/ImageCards/Premium/kitchen_kettle.png Assets/Resources/ImageCards/Premium/music_piano.png`: только два PNG, `cards.json` отсутствует.

## Remaining broken cards

Пусто для двух карт, отмеченных стрелками в последнем скриншоте. По filesystem preview из реальных PNG в `Assets/Resources/ImageCards/Premium` оставшихся обрезанных PNG среди этих двух стрелок не обнаружено.
