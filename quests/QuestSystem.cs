using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// =====================================================================
//  Ежедневные задания и достижения.
//
//  Чистый C#-синглтон (как DiggingManager/VisitorManager): живёт весь
//  процесс, не зависит от того, какая сцена загружена. Игровые системы
//  сообщают ему о событиях через On...() методы, экран QuestsUI читает
//  состояние и забирает награды через Claim...().
// =====================================================================

public class QuestReward
{
    public string ItemId;
    public int Amount;

    public QuestReward(string itemId, int amount)
    {
        ItemId = itemId;
        Amount = amount;
    }
}

public class DailyQuestDefinition
{
    public string Id;
    public string TitleKey;
    public string TitleFallback; // {0} — цель
}

public class AchievementDefinition
{
    public string Id;
    public string Title;
    public string DescriptionFormat; // {0} — цель
    public string StatId;
    public int Target;
    public List<QuestReward> Rewards = new();
}

public class QuestSaveData
{
    // --- Ежедневные ---
    public string DailyDate { get; set; } = "";
    public int DailyBaseReward { get; set; } = 0;        // "D": доход за 10 минут на момент первого входа за день
    public int DailyVisitorGoldTarget { get; set; } = 0; // доход за 15 минут на тот же момент
    public Dictionary<string, int> DailyProgress { get; set; } = new();
    public List<string> DailyClaimed { get; set; } = new();
    public bool DailyBonusClaimed { get; set; } = false;
    public bool DailySetCounted { get; set; } = false;   // сегодняшний полный комплект уже учтён в статистике

    // --- Достижения ---
    public Dictionary<string, long> Stats { get; set; } = new();
    public List<string> VisitedLocations { get; set; } = new();
    public List<string> ClaimedAchievements { get; set; } = new();
}

public class QuestSystem
{
    private static QuestSystem _instance;
    public static QuestSystem Instance
    {
        get
        {
            if (_instance == null) _instance = new QuestSystem();
            return _instance;
        }
    }

    // ===== ID ежедневных заданий =====
    public const string DailyVisitorGold = "visitor_gold";
    public const string DailyExtractBones = "extract_bones";
    public const string DailyBuyTicket = "buy_ticket";
    public const string DailyOpenConcretion = "open_concretion";
    public const string DailyUseDiggers = "use_diggers";

    // ===== ID счётчиков для достижений =====
    public const string StatBones = "bones_extracted";
    public const string StatTiles = "tiles_dug";
    public const string StatConcretions = "concretions_opened";
    public const string StatCollections = "collections_assembled";
    public const string StatSkeletonsDisplayed = "skeletons_displayed_max";
    public const string StatVisitors = "visitors_admitted";
    public const string StatLocations = "locations_visited";
    public const string StatDailySets = "daily_sets_completed";

    // ===== Баланс =====
    public const int MinDailyReward = 100;          // нижний порог награды D
    public const float RewardIncomeSeconds = 600f;  // награда = 10 минут дохода
    public const float GoldTargetSeconds = 900f;    // цель "золото с посетителей" = 15 минут дохода
    public const string DailyBonusItemId = "rare_concretion";
    public const int DailyBonusAmount = 1;

    private readonly List<DailyQuestDefinition> _dailyQuests = new();
    private readonly List<AchievementDefinition> _achievements = new();

    private QuestSaveData _data = new();

    private QuestSystem()
    {
        RegisterDailyQuests();
        RegisterAchievements();
    }

    // =================================================================
    //  Определения
    // =================================================================

    private void RegisterDailyQuests()
    {
        _dailyQuests.Add(new DailyQuestDefinition { Id = DailyVisitorGold, TitleKey = "ui.quests.daily.visitor_gold", TitleFallback = "Заработать {0} золота на посетителях" });
        _dailyQuests.Add(new DailyQuestDefinition { Id = DailyExtractBones, TitleKey = "ui.quests.daily.extract_bones", TitleFallback = "Извлечь кости: {0}" });
        _dailyQuests.Add(new DailyQuestDefinition { Id = DailyBuyTicket, TitleKey = "ui.quests.daily.buy_ticket", TitleFallback = "Купить билет в магазине: {0}" });
        _dailyQuests.Add(new DailyQuestDefinition { Id = DailyOpenConcretion, TitleKey = "ui.quests.daily.open_concretion", TitleFallback = "Разбить конкрецию: {0}" });
        _dailyQuests.Add(new DailyQuestDefinition { Id = DailyUseDiggers, TitleKey = "ui.quests.daily.use_diggers", TitleFallback = "Назначить копателя на плитку: {0} раз" });
    }

    private void RegisterAchievements()
    {
        const string common = "common_concretion";
        const string rare = "rare_concretion";

        // --- Кости ---
        AddAchievement("bones_1", "Первая находка", "Извлечь кость", StatBones, 1, (common, 1));
        AddAchievement("bones_25", "Охотник за костями", "Извлечь костей: {0}", StatBones, 25, (rare, 1));
        AddAchievement("bones_100", "Палеонтолог", "Извлечь костей: {0}", StatBones, 100, ("ticket_morrison", 1));

        // --- Плитки ---
        AddAchievement("tiles_50", "Землекоп", "Полностью выкопать плиток: {0}", StatTiles, 50, (common, 2));
        AddAchievement("tiles_100", "Бригадир", "Полностью выкопать плиток: {0}", StatTiles, 100, (rare, 2));
        AddAchievement("tiles_200", "Прораб", "Полностью выкопать плиток: {0}", StatTiles, 200, ("ticket_morrison", 1));
        AddAchievement("tiles_500", "Карьер", "Полностью выкопать плиток: {0}", StatTiles, 500, ("ticket_gobi", 1));

        // --- Конкреции ---
        AddAchievement("concretions_10", "Камнелом", "Разбить конкреций: {0}", StatConcretions, 10, (rare, 1));
        AddAchievement("concretions_50", "Геолог", "Разбить конкреций: {0}", StatConcretions, 50, (rare, 3));
        AddAchievement("concretions_100", "Знаток недр", "Разбить конкреций: {0}", StatConcretions, 100, ("ticket_gobi", 1));

        // --- Коллекции и скелеты ---
        AddAchievement("collections_1", "Первый скелет", "Собрать коллекцию целиком", StatCollections, 1, (rare, 2));
        AddAchievement("skeletons_3", "Куратор", "Выставить собранных скелетов в музее: {0}", StatSkeletonsDisplayed, 3, ("ticket_morrison", 1));
        AddAchievement("skeletons_10", "Главный хранитель", "Выставить собранных скелетов в музее: {0}", StatSkeletonsDisplayed, 10, ("ticket_texas", 1));

        // --- Локации ---
        AddAchievement("locations_3", "Путешественник", "Побывать на разных локациях: {0}", StatLocations, 3, ("ticket_undoria", 1));
        AddAchievement("locations_5", "Вокруг света", "Побывать на всех локациях: {0}", StatLocations, 5, ("ticket_texas", 1));

        // --- Посетители ---
        AddAchievement("visitors_100", "Первые гости", "Принять посетителей: {0}", StatVisitors, 100, (common, 1));
        AddAchievement("visitors_1000", "Популярное место", "Принять посетителей: {0}", StatVisitors, 1000, (rare, 2));
        AddAchievement("visitors_5000", "Аншлаг", "Принять посетителей: {0}", StatVisitors, 5000, ("ticket_gobi", 1));

        // --- Ежедневные ---
        AddAchievement("daily_sets_7", "Завсегдатай", "Выполнить все ежедневные задания за день: {0} раз", StatDailySets, 7, ("ticket_texas", 1), (rare, 3));
    }

    private void AddAchievement(string id, string title, string description, string statId, int target,
        params (string itemId, int amount)[] rewards)
    {
        var def = new AchievementDefinition
        {
            Id = id,
            Title = title,
            DescriptionFormat = description,
            StatId = statId,
            Target = target
        };
        foreach (var (itemId, amount) in rewards)
        {
            def.Rewards.Add(new QuestReward(itemId, amount));
        }
        _achievements.Add(def);
    }

    public IReadOnlyList<DailyQuestDefinition> GetDailyQuests() => _dailyQuests;
    public IReadOnlyList<AchievementDefinition> GetAchievements() => _achievements;

    // =================================================================
    //  Смена дня
    // =================================================================

    private static string TodayKey() => DateTime.Now.ToString("yyyy-MM-dd");

    /// <summary>
    /// Если наступил новый день (по местному времени устройства) — выдаёт
    /// новый набор заданий и фиксирует награду D по ТЕКУЩЕМУ доходу музея.
    /// Вызывается после загрузки сохранения и перед любым чтением/изменением
    /// ежедневного прогресса, так что смена дня посреди сессии тоже ловится.
    /// </summary>
    public void EnsureToday()
    {
        string today = TodayKey();
        if (_data.DailyDate == today) return;

        // Доход ещё нельзя посчитать (автозагрузки не готовы) — попробуем позже.
        if (MuseumSystem.Instance == null) return;

        float incomePerSecond = MuseumSystem.Instance.GetEstimatedIncomePerSecond();

        _data.DailyDate = today;
        _data.DailyBaseReward = Math.Max(MinDailyReward, Mathf.RoundToInt(incomePerSecond * RewardIncomeSeconds));
        _data.DailyVisitorGoldTarget = Math.Max(1, Mathf.RoundToInt(incomePerSecond * GoldTargetSeconds));
        _data.DailyProgress = new Dictionary<string, int>();
        _data.DailyClaimed = new List<string>();
        _data.DailyBonusClaimed = false;
        _data.DailySetCounted = false;

        GD.Print($"[Quests] 📅 Новый день {today}: награда за задание {_data.DailyBaseReward}, цель по золоту {_data.DailyVisitorGoldTarget}");
        SaveSystem.Instance?.MarkDirty();
    }

    /// <summary>Сколько осталось до обновления заданий (до местной полуночи).</summary>
    public TimeSpan GetTimeUntilReset()
    {
        var now = DateTime.Now;
        return now.Date.AddDays(1) - now;
    }

    // =================================================================
    //  Ежедневные задания: чтение
    // =================================================================

    public int GetDailyReward()
    {
        EnsureToday();
        return _data.DailyBaseReward;
    }

    public int GetDailyTarget(string questId)
    {
        EnsureToday();
        switch (questId)
        {
            case DailyVisitorGold: return Math.Max(1, _data.DailyVisitorGoldTarget);
            case DailyExtractBones: return 3;
            case DailyBuyTicket: return 1;
            case DailyOpenConcretion: return 1;
            case DailyUseDiggers: return 20;
            default: return 1;
        }
    }

    public int GetDailyProgress(string questId)
    {
        EnsureToday();
        int value = _data.DailyProgress.TryGetValue(questId, out int v) ? v : 0;
        return Math.Min(value, GetDailyTarget(questId));
    }

    public bool IsDailyCompleted(string questId) => GetDailyProgress(questId) >= GetDailyTarget(questId);
    public bool IsDailyClaimed(string questId) => _data.DailyClaimed.Contains(questId);

    public bool AreAllDailyCompleted()
    {
        foreach (var quest in _dailyQuests)
        {
            if (!IsDailyCompleted(quest.Id)) return false;
        }
        return true;
    }

    public bool IsDailyBonusClaimed()
    {
        EnsureToday();
        return _data.DailyBonusClaimed;
    }

    public string GetDailyTitle(DailyQuestDefinition quest)
    {
        string format = MobileUI.Tr(quest.TitleKey, quest.TitleFallback);
        return string.Format(format, GetDailyTarget(quest.Id));
    }

    // =================================================================
    //  Ежедневные задания: награды
    // =================================================================

    public bool ClaimDaily(string questId)
    {
        EnsureToday();
        if (!IsDailyCompleted(questId) || IsDailyClaimed(questId)) return false;

        _data.DailyClaimed.Add(questId);
        Wallet.Instance?.AddCoins(_data.DailyBaseReward);

        GD.Print($"[Quests] ✅ Награда за задание '{questId}': +{_data.DailyBaseReward} монет");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    public bool ClaimDailyBonus()
    {
        EnsureToday();
        if (_data.DailyBonusClaimed || !AreAllDailyCompleted()) return false;

        _data.DailyBonusClaimed = true;
        InventorySystem.Instance?.AddItem(DailyBonusItemId, DailyBonusAmount);

        GD.Print($"[Quests] 🎁 Бонус за все задания дня: {DailyBonusItemId} x{DailyBonusAmount}");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    private void AddDailyProgress(string questId, int amount)
    {
        if (amount <= 0) return;
        EnsureToday();
        if (_data.DailyDate != TodayKey()) return; // день ещё не инициализирован

        int target = GetDailyTarget(questId);
        int current = _data.DailyProgress.TryGetValue(questId, out int v) ? v : 0;
        if (current >= target) return;

        _data.DailyProgress[questId] = Math.Min(target, current + amount);

        // Полный комплект за день считаем в момент выполнения, а не получения бонуса
        if (!_data.DailySetCounted && AreAllDailyCompleted())
        {
            _data.DailySetCounted = true;
            AddStat(StatDailySets, 1);
        }

        SaveSystem.Instance?.MarkDirty();
    }

    // =================================================================
    //  Достижения
    // =================================================================

    private void AddStat(string statId, long amount)
    {
        if (amount <= 0) return;
        long current = _data.Stats.TryGetValue(statId, out long v) ? v : 0;
        _data.Stats[statId] = current + amount;
        SaveSystem.Instance?.MarkDirty();
    }

    private long GetStat(string statId)
    {
        if (statId == StatLocations) return _data.VisitedLocations.Count;
        if (statId == StatSkeletonsDisplayed) RefreshDisplayedSkeletons();
        return _data.Stats.TryGetValue(statId, out long v) ? v : 0;
    }

    /// <summary>
    /// "Выставлено скелетов" — это не счётчик событий, а состояние музея.
    /// Запоминаем максимум, чтобы уже выполненное достижение не "откатывалось",
    /// если игрок убрал скелет на склад до получения награды.
    /// </summary>
    public void RefreshDisplayedSkeletons()
    {
        var layout = MuseumLayout.Instance;
        if (layout?.Rooms == null) return;

        int count = 0;
        foreach (var room in layout.Rooms)
        {
            if (room?.PlacedFurnitureList == null) continue;
            foreach (var placed in room.PlacedFurnitureList)
            {
                if (placed?.Furniture is CollectionExhibit) count++;
            }
        }

        long best = _data.Stats.TryGetValue(StatSkeletonsDisplayed, out long v) ? v : 0;
        if (count > best)
        {
            _data.Stats[StatSkeletonsDisplayed] = count;
            SaveSystem.Instance?.MarkDirty();
        }
    }

    public int GetAchievementProgress(AchievementDefinition achievement)
    {
        return (int)Math.Min(GetStat(achievement.StatId), achievement.Target);
    }

    public bool IsAchievementCompleted(AchievementDefinition achievement)
        => GetStat(achievement.StatId) >= achievement.Target;

    public bool IsAchievementClaimed(AchievementDefinition achievement)
        => _data.ClaimedAchievements.Contains(achievement.Id);

    public bool ClaimAchievement(string achievementId)
    {
        var achievement = _achievements.Find(a => a.Id == achievementId);
        if (achievement == null) return false;
        if (IsAchievementClaimed(achievement) || !IsAchievementCompleted(achievement)) return false;

        _data.ClaimedAchievements.Add(achievement.Id);
        foreach (var reward in achievement.Rewards)
        {
            InventorySystem.Instance?.AddItem(reward.ItemId, reward.Amount);
        }

        GD.Print($"[Quests] 🏆 Получена награда за достижение '{achievement.Id}'");
        SaveSystem.Instance?.MarkDirty();
        return true;
    }

    /// <summary>Есть ли хоть одна неполученная награда — для красной точки на кнопке заданий.</summary>
    public bool HasClaimableRewards()
    {
        EnsureToday();

        foreach (var quest in _dailyQuests)
        {
            if (IsDailyCompleted(quest.Id) && !IsDailyClaimed(quest.Id)) return true;
        }
        if (!_data.DailyBonusClaimed && AreAllDailyCompleted()) return true;

        foreach (var achievement in _achievements)
        {
            if (!IsAchievementClaimed(achievement) && IsAchievementCompleted(achievement)) return true;
        }
        return false;
    }

    // =================================================================
    //  Тексты наград
    // =================================================================

    public static string GetItemName(string itemId)
    {
        switch (itemId)
        {
            case "common_concretion": return MobileUI.Tr("ui.quests.item.common_concretion", "Обычная конкреция");
            case "rare_concretion": return MobileUI.Tr("ui.quests.item.rare_concretion", "Редкая конкреция");
        }

        var ticket = GameData.GetTicket(itemId);
        if (ticket != null) return ticket.DisplayName;

        return GameData.GetResource(itemId)?.DisplayName ?? itemId;
    }

    public static string FormatRewards(List<QuestReward> rewards)
    {
        return string.Join(", ", rewards.Select(r =>
            r.Amount > 1 ? $"{GetItemName(r.ItemId)} x{r.Amount}" : GetItemName(r.ItemId)));
    }

    // =================================================================
    //  События от игровых систем
    // =================================================================

    /// <summary>Посетители принесли золото (вход в музей или догонка после раскопок).</summary>
    public void OnVisitorIncome(int gold, int visitors)
    {
        AddDailyProgress(DailyVisitorGold, gold);
        AddStat(StatVisitors, visitors);
    }

    /// <summary>Кость извлечена и попала на склад.</summary>
    public void OnBoneExtracted()
    {
        AddDailyProgress(DailyExtractBones, 1);
        AddStat(StatBones, 1);
    }

    /// <summary>Плитка выкопана полностью (дошла до FullyDug).</summary>
    public void OnTileFullyDug() => AddStat(StatTiles, 1);

    /// <summary>Копатель назначен на плитку.</summary>
    public void OnDiggerAssigned() => AddDailyProgress(DailyUseDiggers, 1);

    /// <summary>Билет куплен в магазине.</summary>
    public void OnTicketBought() => AddDailyProgress(DailyBuyTicket, 1);

    /// <summary>Конкреция разбита.</summary>
    public void OnConcretionOpened()
    {
        AddDailyProgress(DailyOpenConcretion, 1);
        AddStat(StatConcretions, 1);
    }

    /// <summary>Коллекция (скелет) собрана из частей.</summary>
    public void OnCollectionAssembled() => AddStat(StatCollections, 1);

    /// <summary>Игрок вошёл на сцену раскопок локации.</summary>
    public void OnLocationVisited(string locationId)
    {
        if (string.IsNullOrEmpty(locationId) || _data.VisitedLocations.Contains(locationId)) return;
        _data.VisitedLocations.Add(locationId);
        SaveSystem.Instance?.MarkDirty();
    }

    // =================================================================
    //  Сохранение
    // =================================================================

    public QuestSaveData GetSaveData() => _data;

    public void LoadFromSaveData(QuestSaveData data)
    {
        _data = data ?? new QuestSaveData();
        _data.DailyProgress ??= new Dictionary<string, int>();
        _data.DailyClaimed ??= new List<string>();
        _data.Stats ??= new Dictionary<string, long>();
        _data.VisitedLocations ??= new List<string>();
        _data.ClaimedAchievements ??= new List<string>();
    }
}
