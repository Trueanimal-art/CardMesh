using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OfficeRunConfig", menuName = "WorkForUncle/Office Run Config")]
public class OfficeRunConfig : ScriptableObject
{
    [Serializable]
    public class ModifierData
    {
        public string title;
        [TextArea(2, 4)] public string description;
        [Min(1)] public int durationInQuestions = 2;
        [Range(-3, 3)] public int correctMadnessDelta;
        [Range(-3, 3)] public int wrongMadnessDelta;
        [Range(0, 3)] public int extraStreakOnCorrect;
        [Range(0, 3)] public int shieldCharges;

        public bool IsEmpty =>
            string.IsNullOrWhiteSpace(title) &&
            string.IsNullOrWhiteSpace(description) &&
            correctMadnessDelta == 0 &&
            wrongMadnessDelta == 0 &&
            extraStreakOnCorrect == 0 &&
            shieldCharges == 0;
    }

    [Serializable]
    public class EventChoice
    {
        public string label;
        [TextArea(2, 4)] public string outcomeText;
        [Range(-5, 5)] public int immediateMadnessDelta;
        public ModifierData modifier;

        public bool HasModifier => modifier != null && !modifier.IsEmpty;
    }

    [Serializable]
    public class RunEvent
    {
        public string eventId;
        public string title;
        [TextArea(2, 6)] public string description;
        public bool useOncePerRun = true;
        [Min(0)] public int minAnsweredQuestions;
        [Min(0)] public int minMadness;
        [Min(0)] public int maxMadness = 10;
        public EventChoice[] choices = Array.Empty<EventChoice>();

        public bool HasChoices => choices != null && choices.Length > 0;
    }

    [Header("Core")]
    [Min(0)] public int startingMadness = 2;
    [Min(1)] public int maxMadness = 10;
    [Min(1)] public int eventInterval = 2;
    [Range(-3, 0)] public int correctMadnessDelta = -1;
    [Range(0, 5)] public int wrongMadnessDelta = 2;
    [Min(0)] public int warningMadness = 6;
    [Min(0)] public int dangerMadness = 8;

    [Header("Briefing")]
    public string briefingTitle = "Выбери режим смены";
    [TextArea(2, 6)] public string briefingDescription = "Перед первым вопросом выбери, как переживать эту смену. Это влияет на темп, серию и рост безумия.";
    public EventChoice[] loadoutChoices = Array.Empty<EventChoice>();

    [Header("Feedback")]
    public string correctFeedback = "Сигнал распознан";
    public string wrongFeedback = "Система трещит";
    public string shieldFeedback = "Модификатор гасит удар";
    public string calmStateLabel = "Контроль";
    public string warningStateLabel = "Шторм";
    public string dangerStateLabel = "Перегрев";

    [Header("Random Events")]
    public List<RunEvent> randomEvents = new List<RunEvent>();

    public bool HasLoadoutChoices => loadoutChoices != null && loadoutChoices.Length > 0;

    public string GetStateLabel(int madness)
    {
        if (madness >= dangerMadness)
            return dangerStateLabel;

        if (madness >= warningMadness)
            return warningStateLabel;

        return calmStateLabel;
    }

    public RunEvent CreateLoadoutEvent()
    {
        return new RunEvent
        {
            eventId = "loadout",
            title = briefingTitle,
            description = briefingDescription,
            useOncePerRun = true,
            minAnsweredQuestions = 0,
            minMadness = 0,
            maxMadness = maxMadness,
            choices = loadoutChoices ?? Array.Empty<EventChoice>()
        };
    }

    public static OfficeRunConfig CreateFallbackConfig()
    {
        var config = CreateInstance<OfficeRunConfig>();
        config.startingMadness = 2;
        config.maxMadness = 10;
        config.eventInterval = 2;
        config.correctMadnessDelta = -1;
        config.wrongMadnessDelta = 2;
        config.warningMadness = 6;
        config.dangerMadness = 8;
        config.briefingTitle = "Выбери режим смены";
        config.briefingDescription = "Сначала ты выбираешь стиль выживания, а уже потом читаешь офисные сигналы. Это не тест ради психотипа, а короткий survival-run по одной смене.";
        config.correctFeedback = "Сигнал распознан";
        config.wrongFeedback = "Офис давит сильнее";
        config.shieldFeedback = "Модификатор спасает серию";
        config.calmStateLabel = "Контроль";
        config.warningStateLabel = "Шторм";
        config.dangerStateLabel = "Перегрев";

        config.loadoutChoices = new[]
        {
            MakeChoice(
                "Тихий режим",
                "Надеваешь виртуальные наушники: первая ошибка не ломает темп и не поднимает безумие.",
                0,
                MakeModifier("Наушники", "Один промах за смену проходит мягко.", 10, 0, 0, 0, 1)),
            MakeChoice(
                "Кофеин-овердрайв",
                "Разгоняешься: правильные решения строят длинную серию, но промахи кусаются сильнее.",
                1,
                MakeModifier("Кофеин-овердрайв", "Каждое точное чтение ситуации даёт +1 к серии, но ошибки больнее.", 10, 0, 1, 1, 0)),
            MakeChoice(
                "Холодный дзен",
                "Идёшь через концентрацию: верные решения лучше снимают перегрев.",
                -1,
                MakeModifier("Холодный дзен", "Верные ответы дополнительно гасят безумие.", 10, -1, 0, 0, 0))
        };

        config.randomEvents = new List<RunEvent>
        {
            MakeEvent(
                "coffee_crash",
                "Кофемашина орёт как серверная",
                "Пока офис спорит, у тебя есть пара секунд решить, как пережить следующий отрезок.",
                2,
                0,
                10,
                MakeChoice(
                    "Влить эспрессо",
                    "Разгоняешься до дрожи в пальцах. Серия растёт быстрее, но цена ошибок выше.",
                    1,
                    MakeModifier("Двойной эспрессо", "Следующие 2 вопроса точные ответы дают +1 к серии, ошибки поднимают безумие сильнее.", 2, 0, 1, 1, 0)),
                MakeChoice(
                    "Пить воду и жить",
                    "Сбрасываешь обороты и чуть легче контролируешь перегрев.",
                    -1,
                    MakeModifier("Водный буфер", "Следующие 2 точных ответа сильнее успокаивают шкалу.", 2, -1, 0, 0, 0))),
            MakeEvent(
                "hr_ping",
                "HR зовёт на короткую синхронизацию",
                "Классический офисный квест: можно очаровать систему или уйти в невидимость.",
                2,
                0,
                10,
                MakeChoice(
                    "Включить обаяние",
                    "Ты держишь лицо. Один следующий промах переживётся без обвала серии.",
                    0,
                    MakeModifier("Социальная маска", "Один промах проходит без потери серии и без роста безумия.", 2, 0, 0, 0, 1)),
                MakeChoice(
                    "Ответить позже шаблоном",
                    "Уходишь в безопасную вежливость и выигрываешь немного воздуха.",
                    -1,
                    MakeModifier("Автоответчик", "Следующие 2 ошибки чуть мягче по безумию.", 2, 0, -1, 0, 0))),
            MakeEvent(
                "urgent_chat",
                "В чат падает сообщение «срочно»",
                "Самое время решить: тушить пожар сразу или сначала понять, что вообще горит.",
                2,
                0,
                10,
                MakeChoice(
                    "Прыгнуть в пожар",
                    "Темп растёт, вместе с ним и риск. Это выгодно только если читаешь ситуацию без ошибок.",
                    1,
                    MakeModifier("Пожарный режим", "Следующие 2 точных ответа дают +1 к серии, ошибки больнее.", 2, 0, 1, 1, 0)),
                MakeChoice(
                    "Проверить контекст",
                    "Делаешь паузу и успокаиваешь шкалу перед следующим заходом.",
                    -1,
                    MakeModifier("Пауза на контекст", "Следующие 2 точных ответа дополнительно гасят безумие.", 2, -1, 0, 0, 0))),
            MakeEvent(
                "meeting_room",
                "Созвон без повестки уже стартовал",
                "Либо заходишь и задаёшь темп, либо становишься призраком календаря.",
                4,
                0,
                10,
                MakeChoice(
                    "Зайти первым",
                    "Берёшь инициативу. Если дальше читаешь сигналы верно, серия растёт очень быстро.",
                    1,
                    MakeModifier("Первый в эфире", "Следующие 2 точных ответа дают +1 к серии.", 2, 0, 0, 1, 0)),
                MakeChoice(
                    "Спрятаться в статусе",
                    "Уходишь в тень. Один ближайший промах не должен тебя сломать.",
                    0,
                    MakeModifier("Невидимка", "Один промах можно пережить без штрафа к серии и безумию.", 2, 0, 0, 0, 1)))
        };

        return config;
    }

    private static EventChoice MakeChoice(string label, string outcome, int madnessDelta, ModifierData modifier)
    {
        return new EventChoice
        {
            label = label,
            outcomeText = outcome,
            immediateMadnessDelta = madnessDelta,
            modifier = modifier
        };
    }

    private static ModifierData MakeModifier(string title, string description, int duration, int correctMadness, int wrongMadness, int extraStreak, int shields)
    {
        return new ModifierData
        {
            title = title,
            description = description,
            durationInQuestions = Mathf.Max(1, duration),
            correctMadnessDelta = correctMadness,
            wrongMadnessDelta = wrongMadness,
            extraStreakOnCorrect = Mathf.Max(0, extraStreak),
            shieldCharges = Mathf.Max(0, shields)
        };
    }

    private static RunEvent MakeEvent(string id, string title, string description, int minAnswered, int minMadness, int maxMadness, params EventChoice[] choices)
    {
        return new RunEvent
        {
            eventId = id,
            title = title,
            description = description,
            useOncePerRun = true,
            minAnsweredQuestions = Mathf.Max(0, minAnswered),
            minMadness = Mathf.Max(0, minMadness),
            maxMadness = Mathf.Max(0, maxMadness),
            choices = choices ?? Array.Empty<EventChoice>()
        };
    }
}
