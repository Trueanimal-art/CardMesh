using System;
using System.Collections.Generic;
using UnityEngine;

namespace SolitaireAssociations
{
    public sealed class CardDatabase : MonoBehaviour
    {
        private const string DefaultCardsResourcePath = "Data/cards";
        private const string DefaultPremiumImageCardsResourcePath = "ImageCards/Premium";
        private const int DefaultGoalRequiredCount = 4;
        private const int DefaultImageAtlasColumns = 20;
        private const int DefaultImageAtlasRows = 20;

        [SerializeField] private string cardsResourcePath = DefaultCardsResourcePath;
        [SerializeField] private string premiumImageCardsResourcePath = DefaultPremiumImageCardsResourcePath;
        [SerializeField, Min(1)] private int goalRequiredCount = DefaultGoalRequiredCount;
        [SerializeField] private Texture2D imageCardsAtlas;
        [SerializeField, Min(1)] private int imageAtlasColumns = DefaultImageAtlasColumns;
        [SerializeField, Min(1)] private int imageAtlasRows = DefaultImageAtlasRows;

        private readonly Dictionary<string, GoldCardData> goldCardsById = new();
        private readonly Dictionary<string, RegularCardEntry> regularCardsById = new();
        private readonly Dictionary<string, Sprite> premiumImageSpritesByCardId = new();
        private readonly HashSet<string> missingPremiumImageSpriteIds = new();
        private readonly Dictionary<string, Sprite> imageSpritesByKey = new();
        private readonly Dictionary<int, Sprite> imageSpritesByIndex = new();
        private readonly List<GoldCardData> goldCards = new();
        private static readonly Dictionary<string, string> ImageKeysByCardId = new()
        {
            ["cup_cup"] = "r04-c01",
            ["cup_mug"] = "r04-c03",
            ["cup_coffee"] = "r04-c01",
            ["cup_drink"] = "r04-c04",
            ["cup_tea"] = "r04-c02",
            ["cup_saucer"] = "r05-c01",
            ["food_plate"] = "r05-c01",
            ["food_bread"] = "r03-c01",
            ["food_fruit"] = "r01-c01",
            ["time_clock"] = "r05-c14",
            ["time_alarm"] = "r05-c15",
            ["travel_train"] = "r14-c15",
            ["travel_plane"] = "r14-c13",
            ["sport_ball"] = "r16-c01",
            ["sport_medal"] = "r16-c17",
            ["nature_tree"] = "r12-c01",
            ["nature_mountain"] = "r12-c17",
            ["nature_stone"] = "r12-c16",
            ["nature_leaf"] = "r12-c10",
            ["nature_cloud"] = "r13-c05",
            ["weather_sun"] = "r13-c01",
            ["weather_rain"] = "r13-c06",
            ["weather_snow"] = "r13-c08",
            ["weather_storm"] = "r13-c09",
            ["weather_fog"] = "r13-c05",
            ["city_traffic"] = "r15-c01",
            ["home_sofa"] = "r06-c05",
            ["home_lamp"] = "r06-c10",
            ["home_pillow"] = "r06-c02",
            ["home_key"] = "r20-c17",
            ["kitchen_spoon"] = "r05-c03",
            ["kitchen_fork"] = "r05-c03",
            ["kitchen_knife"] = "r05-c03",
            ["kitchen_pan"] = "r05-c04",
            ["kitchen_kettle"] = "r05-c07",
            ["kitchen_fridge"] = "r05-c11",
            ["kitchen_oven"] = "r05-c12",
            ["school_notebook"] = "r17-c03",
            ["school_marker"] = "r17-c06",
            ["books_novel"] = "r17-c01",
            ["books_page"] = "r17-c02",
            ["books_shelf"] = "r06-c17",
            ["art_brush"] = "r18-c10",
            ["art_frame"] = "r06-c15",
            ["cinema_popcorn"] = "r04-c20",
            ["cinema_camera"] = "r19-c01",
            ["pets_dog"] = "r09-c01",
            ["pets_cat"] = "r09-c02",
            ["pets_paw"] = "r11-c20",
            ["garden_apple"] = "r01-c01",
            ["transport_bus"] = "r14-c04",
            ["transport_taxi"] = "r14-c05",
            ["transport_stop"] = "r15-c05",
            ["sea_shell"] = "r10-c18",
            ["sea_boat"] = "r14-c18",
            ["sea_sail"] = "r14-c19",
            ["sea_fish"] = "r10-c08",
            ["space_star"] = "r13-c03",
            ["space_planet"] = "r13-c13",
            ["space_rocket"] = "r13-c18",
            ["space_moon"] = "r13-c02",
            ["space_comet"] = "r13-c17",
            ["shapes_star"] = "r20-c03",
            ["clothes_shirt"] = "r08-c01",
            ["clothes_coat"] = "r08-c10",
            ["clothes_hat"] = "r08-c15",
            ["clothes_dress"] = "r08-c06",
            ["clothes_jeans"] = "r08-c08",
            ["clothes_glove"] = "r08-c16",
            ["shoes_boot"] = "r08-c14",
            ["shoes_sneaker"] = "r08-c12",
            ["shoes_heel"] = "r08-c13",
            ["health_water"] = "r04-c07",
            ["body_heart"] = "r20-c01",
            ["work_desk"] = "r06-c07",
            ["office_chair"] = "r06-c03",
            ["office_printer"] = "r17-c14",
            ["office_folder"] = "r17-c18",
            ["office_pen"] = "r17-c05",
            ["office_coffee"] = "r04-c01",
            ["office_screen"] = "r17-c13",
            ["internet_cloud"] = "r13-c05",
            ["phone_screen"] = "r19-c11",
            ["phone_camera"] = "r19-c01",
            ["photo_frame"] = "r06-c15",
            ["holidays_cake"] = "r03-c06",
            ["holidays_table"] = "r06-c06",
            ["winter_snow"] = "r13-c08",
            ["winter_pine"] = "r12-c03",
            ["summer_sun"] = "r13-c01",
            ["summer_berry"] = "r01-c05",
            ["breakfast_egg"] = "r03-c11",
            ["breakfast_yogurt"] = "r03-c10",
            ["breakfast_tea"] = "r04-c02",
            ["breakfast_butter"] = "r03-c08",
            ["dessert_cake"] = "r03-c06",
            ["dessert_icecream"] = "r04-c16",
            ["dessert_honey"] = "r04-c13",
            ["drinks_juice"] = "r04-c04",
            ["drinks_tea"] = "r04-c02",
            ["drinks_coffee"] = "r04-c01",
            ["drinks_water"] = "r04-c07",
            ["drinks_milk"] = "r03-c07",
            ["farm_tractor"] = "r14-c10",
            ["farm_cow"] = "r09-c16",
            ["farm_chicken"] = "r10-c01",
            ["forest_pine"] = "r12-c03",
            ["forest_mushroom"] = "r12-c15",
            ["forest_fox"] = "r09-c07",
            ["forest_owl"] = "r10-c04",
            ["forest_berry"] = "r01-c16",
            ["flowers_rose"] = "r12-c05",
            ["flowers_tulip"] = "r12-c07",
            ["flowers_lily"] = "r12-c08",
            ["insects_bee"] = "r11-c02",
            ["insects_ant"] = "r11-c04",
            ["insects_butterfly"] = "r11-c03",
            ["insects_beetle"] = "r11-c05",
            ["insects_ladybug"] = "r11-c01",
            ["camping_backpack"] = "r08-c20",
            ["camping_mug"] = "r04-c03",
            ["toys_train"] = "r14-c15",
            ["toys_ball"] = "r16-c01",
            ["toys_car"] = "r14-c01",
            ["tools_hammer"] = "r18-c01",
            ["tools_saw"] = "r18-c05",
            ["tools_drill"] = "r18-c07",
            ["tools_wrench"] = "r18-c02",
            ["tools_pliers"] = "r18-c04",
            ["tools_ruler"] = "r17-c09"
        };

        private bool isLoaded;

        public bool IsLoaded => isLoaded;

        private void Awake()
        {
            Load();
        }

        public bool Load()
        {
            if (isLoaded)
            {
                return true;
            }

            TextAsset jsonAsset = Resources.Load<TextAsset>(cardsResourcePath);
            if (jsonAsset == null)
            {
                Debug.LogError($"CardDatabase: cards json not found at Resources/{cardsResourcePath}.json");
                return false;
            }

            CardsPayload payload = JsonUtility.FromJson<CardsPayload>(jsonAsset.text);
            if (payload == null || payload.goldCards == null || payload.goldCards.Count == 0)
            {
                Debug.LogError("CardDatabase: cards json is empty or invalid.");
                return false;
            }

            goldCardsById.Clear();
            regularCardsById.Clear();
            goldCards.Clear();

            foreach (GoldCardData goldCard in payload.goldCards)
            {
                RegisterGoldCard(goldCard);
            }

            isLoaded = goldCardsById.Count > 0;
            if (!isLoaded)
            {
                Debug.LogError("CardDatabase: no valid gold cards were loaded.");
            }

            return isLoaded;
        }

        public bool TryCreateCardInstance(string cardId, out CardData cardData)
        {
            return TryCreateCardInstance(cardId, goalRequiredCount, out cardData);
        }

        public bool TryCreateCardInstance(string cardId, CardType regularCardType, out CardData cardData)
        {
            return TryCreateCardInstance(cardId, goalRequiredCount, regularCardType, out cardData);
        }

        public bool TryCreateCardInstance(string cardId, int goldRequiredCountOverride, out CardData cardData)
        {
            return TryCreateCardInstance(cardId, goldRequiredCountOverride, CardType.Text, out cardData);
        }

        public bool TryCreateCardInstance(string cardId, int goldRequiredCountOverride, CardType regularCardType, out CardData cardData)
        {
            if (!Load() || string.IsNullOrWhiteSpace(cardId))
            {
                cardData = null;
                return false;
            }

            if (goldCardsById.TryGetValue(cardId, out GoldCardData goldCard))
            {
                cardData = CardData.CreateGoldCard(goldCard, goldRequiredCountOverride);
                return cardData != null;
            }

            if (regularCardsById.TryGetValue(cardId, out RegularCardEntry regularCard))
            {
                cardData = CardData.CreateRegularCard(regularCard.Card, regularCard.GoldCard, regularCardType);
                if (cardData != null && cardData.IsImageCard)
                {
                    cardData.SetIcon(ResolveImageCardSprite(cardData));
                }

                return cardData != null;
            }

            cardData = null;
            return false;
        }

        public void SetImageCardsAtlas(Texture2D atlasTexture, int columns = DefaultImageAtlasColumns, int rows = DefaultImageAtlasRows)
        {
            imageCardsAtlas = atlasTexture;
            imageAtlasColumns = Mathf.Max(1, columns);
            imageAtlasRows = Mathf.Max(1, rows);
            imageSpritesByKey.Clear();
            imageSpritesByIndex.Clear();
        }

        public IReadOnlyList<GoldCardData> GetGoldCards()
        {
            Load();
            return goldCards;
        }

        public List<CardData> CreateAllImageCardInstances()
        {
            List<CardData> imageCards = new();
            if (!Load())
            {
                return imageCards;
            }

            List<string> cardIds = new(regularCardsById.Keys);
            cardIds.Sort(System.StringComparer.Ordinal);
            foreach (string cardId in cardIds)
            {
                if (!TryCreateCardInstance(cardId, CardType.Image, out CardData cardData)
                    || cardData == null
                    || !cardData.IsImageCard
                    || cardData.Icon == null)
                {
                    continue;
                }

                imageCards.Add(cardData);
            }

            return imageCards;
        }

        private Sprite ResolveImageCardSprite(CardData cardData)
        {
            if (cardData == null)
            {
                return null;
            }

            Sprite premiumSprite = ResolvePremiumImageCardSprite(cardData.Id);
            if (premiumSprite != null)
            {
                return premiumSprite;
            }

            if (imageCardsAtlas == null)
            {
                return null;
            }

            if (!TryResolveAtlasIndex(cardData, out int atlasIndex, out string spriteKey))
            {
                return null;
            }

            if (imageSpritesByKey.TryGetValue(spriteKey, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            Sprite sprite = GetOrCreateAtlasSprite(atlasIndex, spriteKey);
            imageSpritesByKey[spriteKey] = sprite;
            return sprite;
        }

        public bool HasImageForCard(string cardId)
        {
            if (!Load() || string.IsNullOrWhiteSpace(cardId))
            {
                return false;
            }

            return regularCardsById.TryGetValue(cardId, out RegularCardEntry regularCard)
                && regularCard.Card != null
                && ResolvePremiumImageCardSprite(regularCard.Card.Id) != null;
        }

        private Sprite ResolvePremiumImageCardSprite(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId) || string.IsNullOrWhiteSpace(premiumImageCardsResourcePath))
            {
                return null;
            }

            if (premiumImageSpritesByCardId.TryGetValue(cardId, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            if (missingPremiumImageSpriteIds.Contains(cardId))
            {
                return null;
            }

            string resourcePath = $"{premiumImageCardsResourcePath.Trim().Trim('/')}/{cardId}";
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            if (sprite == null)
            {
                missingPremiumImageSpriteIds.Add(cardId);
                return null;
            }

            premiumImageSpritesByCardId[cardId] = sprite;
            return sprite;
        }

        private bool TryResolveAtlasIndex(CardData cardData, out int atlasIndex, out string spriteKey)
        {
            atlasIndex = 0;
            spriteKey = string.Empty;
            if (cardData == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(cardData.ImageKey) && TryGetExplicitAtlasIndex(cardData.ImageKey, out atlasIndex))
            {
                spriteKey = cardData.ImageKey;
                return true;
            }

            if (ImageKeysByCardId.TryGetValue(cardData.Id, out string mappedImageKey)
                && TryGetExplicitAtlasIndex(mappedImageKey, out atlasIndex))
            {
                spriteKey = mappedImageKey;
                return true;
            }

            return false;
        }

        private bool TryGetExplicitAtlasIndex(string imageKey, out int atlasIndex)
        {
            atlasIndex = 0;
            if (string.IsNullOrWhiteSpace(imageKey))
            {
                return false;
            }

            string[] parts = imageKey.Trim().ToLowerInvariant().Split('-');
            if (parts.Length != 2 || parts[0].Length < 2 || parts[1].Length < 2)
            {
                return false;
            }

            if (parts[0][0] != 'r' || parts[1][0] != 'c')
            {
                return false;
            }

            if (!int.TryParse(parts[0].Substring(1), out int row) || !int.TryParse(parts[1].Substring(1), out int column))
            {
                return false;
            }

            row = Mathf.Clamp(row - 1, 0, imageAtlasRows - 1);
            column = Mathf.Clamp(column - 1, 0, imageAtlasColumns - 1);
            atlasIndex = (row * imageAtlasColumns) + column;
            return true;
        }

        private Sprite GetOrCreateAtlasSprite(int atlasIndex, string spriteKey)
        {
            int atlasSize = Mathf.Max(1, imageAtlasColumns * imageAtlasRows);
            int safeIndex = Mathf.Abs(atlasIndex) % atlasSize;
            if (imageSpritesByIndex.TryGetValue(safeIndex, out Sprite cachedSprite))
            {
                return cachedSprite;
            }

            int row = safeIndex / imageAtlasColumns;
            int column = safeIndex % imageAtlasColumns;
            float cellWidth = imageCardsAtlas.width / (float)imageAtlasColumns;
            float cellHeight = imageCardsAtlas.height / (float)imageAtlasRows;
            int centerX = Mathf.RoundToInt((column + 0.5f) * cellWidth);
            int centerY = Mathf.RoundToInt(imageCardsAtlas.height - ((row + 0.5f) * cellHeight));
            int width = Mathf.Clamp(Mathf.RoundToInt(cellWidth * 1.7f), 1, imageCardsAtlas.width);
            int height = Mathf.Clamp(Mathf.RoundToInt(cellHeight * 1.9f), 1, imageCardsAtlas.height);
            int x = Mathf.Clamp(centerX - (width / 2), 0, imageCardsAtlas.width - width);
            int y = Mathf.Clamp(centerY - (height / 2), 0, imageCardsAtlas.height - height);

            Sprite sprite = CreateTrimmedTransparentSprite(
                x,
                y,
                width,
                height,
                new Vector2(centerX - x, centerY - y),
                new Vector2(cellWidth, cellHeight),
                row,
                column,
                spriteKey);
            sprite.name = $"ImageCard_{row + 1:D2}_{column + 1:D2}_{spriteKey}";
            imageSpritesByIndex[safeIndex] = sprite;
            return sprite;
        }

        private Sprite CreateTrimmedTransparentSprite(
            int x,
            int y,
            int width,
            int height,
            Vector2 cellCenter,
            Vector2 cellSize,
            int row,
            int column,
            string spriteKey)
        {
            try
            {
                Color32[] sourcePixels = imageCardsAtlas.GetPixels32();
                bool[] iconMask = BuildSelectedIconMask(sourcePixels, imageCardsAtlas.width, x, y, width, height, cellCenter, cellSize);
                RectInt contentRect = GetContentRect(iconMask, width, height);
                if (contentRect.width <= 0 || contentRect.height <= 0)
                {
                    return Sprite.Create(
                        imageCardsAtlas,
                        new Rect(x, y, width, height),
                        new Vector2(0.5f, 0.5f),
                        100f);
                }

                Texture2D iconTexture = new(contentRect.width, contentRect.height, TextureFormat.RGBA32, false)
                {
                    name = $"ImageCardTexture_{row + 1:D2}_{column + 1:D2}_{spriteKey}",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };

                Color32[] iconPixels = new Color32[contentRect.width * contentRect.height];
                for (int targetY = 0; targetY < contentRect.height; targetY++)
                {
                    for (int targetX = 0; targetX < contentRect.width; targetX++)
                    {
                        int cellX = contentRect.x + targetX;
                        int cellY = contentRect.y + targetY;
                        int cellIndex = (cellY * width) + cellX;
                        Color32 color = sourcePixels[((y + cellY) * imageCardsAtlas.width) + x + cellX];
                        if (!iconMask[cellIndex])
                        {
                            color.a = 0;
                        }

                        iconPixels[(targetY * contentRect.width) + targetX] = color;
                    }
                }

                iconTexture.SetPixels32(iconPixels);
                iconTexture.Apply(false, false);
                return Sprite.Create(
                    iconTexture,
                    new Rect(0f, 0f, iconTexture.width, iconTexture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
            catch (UnityException)
            {
                return Sprite.Create(
                    imageCardsAtlas,
                    new Rect(x, y, width, height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        private static bool[] BuildSelectedIconMask(
            Color32[] sourcePixels,
            int sourceWidth,
            int x,
            int y,
            int width,
            int height,
            Vector2 cellCenter,
            Vector2 cellSize)
        {
            bool[] visited = new bool[width * height];
            bool[] iconMask = new bool[width * height];
            Queue<int> queue = new();
            List<int> componentPixels = new();
            List<int> bestComponentPixels = new();
            float maxComponentDistance = Mathf.Max(cellSize.x * 0.42f, cellSize.y * 0.56f);
            int minUsefulPixels = Mathf.Max(5, Mathf.RoundToInt(width * height * 0.002f));
            float bestScore = float.MaxValue;

            for (int startY = 0; startY < height; startY++)
            {
                for (int startX = 0; startX < width; startX++)
                {
                    int startIndex = (startY * width) + startX;
                    if (visited[startIndex])
                    {
                        continue;
                    }

                    visited[startIndex] = true;
                    Color32 startColor = sourcePixels[((y + startY) * sourceWidth) + x + startX];
                    if (IsBackgroundPixel(startColor))
                    {
                        continue;
                    }

                    componentPixels.Clear();
                    componentPixels.Add(startIndex);
                    queue.Enqueue(startIndex);
                    int minX = startX;
                    int maxX = startX;
                    int minY = startY;
                    int maxY = startY;

                    while (queue.Count > 0)
                    {
                        int cellIndex = queue.Dequeue();
                        int cellX = cellIndex % width;
                        int cellY = cellIndex / width;

                        TryEnqueueIconPixel(cellX + 1, cellY);
                        TryEnqueueIconPixel(cellX - 1, cellY);
                        TryEnqueueIconPixel(cellX, cellY + 1);
                        TryEnqueueIconPixel(cellX, cellY - 1);
                        TryEnqueueIconPixel(cellX + 1, cellY + 1);
                        TryEnqueueIconPixel(cellX - 1, cellY + 1);
                        TryEnqueueIconPixel(cellX + 1, cellY - 1);
                        TryEnqueueIconPixel(cellX - 1, cellY - 1);
                    }

                    Vector2 componentCenter = new((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
                    float distance = Vector2.Distance(componentCenter, cellCenter);
                    bool isCloseToCellCenter = distance <= maxComponentDistance;
                    bool isLargeEnough = componentPixels.Count >= minUsefulPixels;
                    if (!isCloseToCellCenter || !isLargeEnough)
                    {
                        continue;
                    }

                    float areaBonus = Mathf.Min(18f, componentPixels.Count * 0.015f);
                    float score = distance - areaBonus;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestComponentPixels.Clear();
                        bestComponentPixels.AddRange(componentPixels);
                    }

                    void TryEnqueueIconPixel(int cellX, int cellY)
                    {
                        if (cellX < 0 || cellY < 0 || cellX >= width || cellY >= height)
                        {
                            return;
                        }

                        int cellIndex = (cellY * width) + cellX;
                        if (visited[cellIndex])
                        {
                            return;
                        }

                        visited[cellIndex] = true;
                        Color32 color = sourcePixels[((y + cellY) * sourceWidth) + x + cellX];
                        if (IsBackgroundPixel(color))
                        {
                            return;
                        }

                        componentPixels.Add(cellIndex);
                        queue.Enqueue(cellIndex);
                        minX = Mathf.Min(minX, cellX);
                        maxX = Mathf.Max(maxX, cellX);
                        minY = Mathf.Min(minY, cellY);
                        maxY = Mathf.Max(maxY, cellY);
                    }
                }
            }

            foreach (int componentIndex in bestComponentPixels)
            {
                iconMask[componentIndex] = true;
            }

            return iconMask;
        }

        private static RectInt GetContentRect(bool[] iconMask, int width, int height)
        {
            int minX = width;
            int minY = height;
            int maxX = -1;
            int maxY = -1;

            for (int cellY = 0; cellY < height; cellY++)
            {
                for (int cellX = 0; cellX < width; cellX++)
                {
                    if (!iconMask[(cellY * width) + cellX])
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, cellX);
                    minY = Mathf.Min(minY, cellY);
                    maxX = Mathf.Max(maxX, cellX);
                    maxY = Mathf.Max(maxY, cellY);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                return new RectInt(0, 0, 0, 0);
            }

            const int padding = 2;
            minX = Mathf.Max(0, minX - padding);
            minY = Mathf.Max(0, minY - padding);
            maxX = Mathf.Min(width - 1, maxX + padding);
            maxY = Mathf.Min(height - 1, maxY + padding);
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        private static bool IsBackgroundPixel(Color32 color)
        {
            return color.r <= 18 && color.g <= 18 && color.b <= 18;
        }

        private void RegisterGoldCard(GoldCardData goldCard)
        {
            if (goldCard == null || string.IsNullOrWhiteSpace(goldCard.Id) || string.IsNullOrWhiteSpace(goldCard.Title))
            {
                Debug.LogWarning("CardDatabase: skipped gold card with missing id or title.");
                return;
            }

            if (goldCardsById.ContainsKey(goldCard.Id) || regularCardsById.ContainsKey(goldCard.Id))
            {
                Debug.LogWarning($"CardDatabase: duplicate gold card id '{goldCard.Id}' was skipped.");
                return;
            }

            if (goldCard.Cards == null || goldCard.Cards.Count < 1)
            {
                Debug.LogWarning($"CardDatabase: gold card '{goldCard.Id}' does not contain playable cards.");
                return;
            }

            if (goldCard.Cards.Count < 8)
            {
                Debug.LogWarning($"CardDatabase: gold card '{goldCard.Id}' contains fewer than 8 cards.");
            }

            goldCardsById.Add(goldCard.Id, goldCard);
            goldCards.Add(goldCard);

            foreach (CardData regularCard in goldCard.Cards)
            {
                if (regularCard == null || string.IsNullOrWhiteSpace(regularCard.Id) || string.IsNullOrWhiteSpace(regularCard.Title))
                {
                    Debug.LogWarning($"CardDatabase: skipped invalid card inside gold card '{goldCard.Id}'.");
                    continue;
                }

                if (goldCardsById.ContainsKey(regularCard.Id) || regularCardsById.ContainsKey(regularCard.Id))
                {
                    Debug.LogWarning($"CardDatabase: duplicate regular card id '{regularCard.Id}' was skipped.");
                    continue;
                }

                regularCardsById.Add(regularCard.Id, new RegularCardEntry(goldCard, regularCard));
            }
        }

        [Serializable]
        private sealed class CardsPayload
        {
            public List<GoldCardData> goldCards = new();
        }

        private readonly struct RegularCardEntry
        {
            public RegularCardEntry(GoldCardData sourceGoldCard, CardData sourceCard)
            {
                GoldCard = sourceGoldCard;
                Card = sourceCard;
            }

            public GoldCardData GoldCard { get; }
            public CardData Card { get; }
        }
    }
}
