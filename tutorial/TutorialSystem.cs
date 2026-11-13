using Godot;
using System;
using System.Collections.Generic;

// =====================================================================
//  Обучение.
//
//  Два сценария:
//   * Main  — большое интерактивное обучение при первом входе в игру:
//             все нажатия заблокированы, кроме подсвеченного элемента;
//   * Guide — справка из шестерёнки: только текст и подсветка, шаги
//             листаются стрелками, ничего в игре не меняет.
//
//  TutorialSystem — чистый C#-синглтон (как QuestSystem): живёт весь
//  процесс и переживает смену сцен. Рисует всё TutorialOverlay.
// =====================================================================

public enum TutorialMode { None, Main, Guide }

public class TutorialStep
{
    public string Id;
    public string TextKey;
    public string Emotion = "neutral";   // картинка res://assets/tutorial/professor/<emotion>.png

    /// <summary>Что подсветить (id из TutorialTargets). null — без подсветки.</summary>
    public string TargetId;

    /// <summary>true — подсвеченный элемент можно нажать; false — только показать.</summary>
    public bool AllowTargetInput = false;

    /// <summary>Только золотая линия по контуру — без заливки и свечения (для больших областей).</summary>
    public bool OutlineOnly = false;

    /// <summary>
    /// Свободный шаг: нажатия не блокируются, подсветки нет — игрок сам
    /// повторяет то, чему его уже научили. Завершается по IsDone.
    /// </summary>
    public bool FreePlay = false;

    /// <summary>Шаг листается тапом/стрелкой (иначе ждёт IsDone).</summary>
    public bool TapToContinue = false;

    /// <summary>Условие завершения шага; проверяется каждый кадр.</summary>
    public Func<bool> IsDone;

    /// <summary>
    /// Состояние игры, при котором шаг вообще выполним. Если false — основное
    /// обучение прекращается и вместо него запускается справка (Guide).
    /// </summary>
    public Func<bool> Precondition;

    public Action OnEnter;

    /// <summary>
    /// Шаг выполняется на раскопках. Если игрок при этом оказался в музее
    /// (перезапустил игру посреди обучения) — сначала ведём его обратно.
    /// </summary>
    public bool RequiresDig = false;
}

public class TutorialSaveData
{
    public bool MainStarted { get; set; } = false;
    public bool MainFinished { get; set; } = false;
    public string MainStepId { get; set; } = "";

    /// <summary>Плитки ("x,y"), которые игрок начал копать в обучении, по порядку.</summary>
    public List<string> DugTiles { get; set; } = new();

    /// <summary>Награда за пройденное обучение уже выдана.</summary>
    public bool RewardGiven { get; set; } = false;
}

/// <summary>
/// Реестр элементов, которые обучение умеет подсвечивать. Экраны сами
/// регистрируют свои кнопки под понятными id ("bottom.inventory").
/// </summary>
public static class TutorialTargets
{
    private static readonly Dictionary<string, Control> _controls = new();
    private static readonly Dictionary<string, Func<Rect2?>> _providers = new();

    public static void Register(string id, Control control)
    {
        _controls[id] = control;
    }

    /// <summary>Для целей в игровом мире (плитка, клетка пола): функция возвращает область на экране.</summary>
    public static void RegisterProvider(string id, Func<Rect2?> provider)
    {
        _providers[id] = provider;
    }

    public static bool TryGetRect(string id, out Rect2 rect)
    {
        rect = default;
        if (string.IsNullOrEmpty(id)) return false;

        if (_controls.TryGetValue(id, out var control))
        {
            if (GodotObject.IsInstanceValid(control) && control.IsInsideTree() && control.IsVisibleInTree())
            {
                rect = control.GetGlobalRect();
                return rect.Size.X > 0 && rect.Size.Y > 0;
            }
            return false;
        }

        if (_providers.TryGetValue(id, out var provider))
        {
            Rect2? result = null;
            try { result = provider(); } catch (ObjectDisposedException) { }
            if (result.HasValue)
            {
                rect = result.Value;
                return true;
            }
        }
        return false;
    }
}

public class TutorialSystem
{
    private static TutorialSystem _instance;
    public static TutorialSystem Instance
    {
        get
        {
            if (_instance == null) _instance = new TutorialSystem();
            return _instance;
        }
    }

    /// <summary>Награда за пройденное до конца основное обучение.</summary>
    public const int MainRewardCoins = 1000;

    public const string SceneMuseum = "museum";
    public const string SceneDig = "dig";

    public TutorialMode Mode { get; private set; } = TutorialMode.None;
    public bool IsActive => Mode != TutorialMode.None;

    /// <summary>Обучение сейчас запрещает двигать камеру и трогать мир вне подсветки.</summary>
    public bool BlocksWorld => IsActive && !(CurrentStep?.FreePlay ?? false);
    public string SceneName { get; private set; } = "";

    private readonly List<TutorialStep> _mainSteps = new();
    private readonly List<TutorialStep> _guideSteps = new();

    private int _index = 0;
    private bool _mainPending = false;   // новая игра: запустить при входе в музей
    private TutorialSaveData _data = new();

    private TutorialSystem()
    {
        BuildMainSteps();
        BuildGuideSteps();
        RegisterMuseumWorldTargets();
    }

    private List<TutorialStep> ActiveSteps => Mode == TutorialMode.Main ? _mainSteps : _guideSteps;

    /// <summary>Шаг сценария, на котором мы находимся (без учёта "объезда").</summary>
    private TutorialStep ScriptStep =>
        IsActive && _index >= 0 && _index < ActiveSteps.Count ? ActiveSteps[_index] : null;

    /// <summary>Шаг, который сейчас показывается игроку.</summary>
    public TutorialStep CurrentStep => _detourActive ? _backToDigStep : ScriptStep;

    // "Объезд": шаг требует раскопок, а игрок в музее — ведём к кнопке раскопок
    private bool _detourActive = false;
    private readonly TutorialStep _backToDigStep = new TutorialStep
    {
        Id = "back_to_dig", TextKey = "tutorial.main.back_to_dig",
        TargetId = "bottom.dig", AllowTargetInput = true
    };

    public int StepIndex => _index;
    public int StepCount => IsActive ? ActiveSteps.Count : 0;

    /// <summary>Меняется при каждой смене шага — оверлей по нему понимает, что пора обновить текст.</summary>
    public int StepVersion { get; private set; } = 0;

    // =================================================================
    //  Сценарий основного обучения
    // =================================================================

    // ---------- Проверки состояния игры ----------

    private static bool HasItem(string id)
    {
        var item = InventorySystem.Instance?.GetItem(id);
        return item != null && item.Amount > 0;
    }

    private bool InMuseum => SceneName == SceneMuseum;
    private bool InDig => SceneName == SceneDig;

    private static bool HasDigSession => LocationSystem.Instance.HasActiveDiggingSession();
    private bool CanReachDig => InDig || (InMuseum && HasDigSession);

    private int DugCount => _data.DugTiles.Count;

    private static bool InventoryOpen => InventoryUI.Instance != null && InventoryUI.Instance.Visible;

    private static bool PlacementActive =>
        PlacementModeUI.Current != null && GodotObject.IsInstanceValid(PlacementModeUI.Current) && PlacementModeUI.Current.IsPlacing;

    private static bool DisplayCaseOpen =>
        DisplayCaseUI.Current != null && GodotObject.IsInstanceValid(DisplayCaseUI.Current) && DisplayCaseUI.Current.Visible;

    /// <summary>Первая поставленная витрина и комната, где она стоит.</summary>
    private static bool TryFindDisplayCase(out Room room, out PlacedFurniture placed, Func<PlacedFurniture, bool> filter = null)
    {
        foreach (var r in MuseumLayout.Instance.Rooms)
        {
            foreach (var p in r.PlacedFurnitureList)
            {
                if (p.Furniture is DisplayCase && (filter == null || filter(p)))
                {
                    room = r;
                    placed = p;
                    return true;
                }
            }
        }
        room = null;
        placed = null;
        return false;
    }

    private static bool CaseExists => TryFindDisplayCase(out _, out _);

    private static bool AmmonitePlaced => TryFindDisplayCase(out _, out _,
        p => p.Items != null && p.Items.Exists(i => i.ResourceId == "ammonite"));

    private static bool HasConcretion => HasItem("common_concretion");
    private static bool HasAmmonite => HasItem("ammonite");

    /// <summary>Конкреция уже расколота (аммонит на руках или в витрине).</summary>
    private static bool AmmoniteObtained => HasAmmonite || AmmonitePlaced;

    // ---------- Зашитые находки ----------

    /// <summary>
    /// Что лежит под плитками в основном обучении — по порядку, в котором
    /// игрок начинает их копать (а не по координатам: неважно, куда он нажал).
    /// null — пустая плитка. Всё показывается уже на первой стадии раскопки.
    /// </summary>
    private static readonly string[] ScriptedLoot =
    {
        "common_concretion",    // 1-я плитка: первая поездка
        "triceratops_skull",    // 2-я
        null,                   // 3-я — пусто
        "triceratops_body",     // 4-я
        "triceratops_tail"      // 5-я
    };

    /// <summary>Пока идёт основное обучение, случайный лут на плитках отключён.</summary>
    public bool ControlsLoot => Mode == TutorialMode.Main || _mainPending;

    /// <summary>Вызывается плиткой, когда на неё впервые ставят копателя.</summary>
    public void OnTileDigStarted(Tile tile)
    {
        if (Mode != TutorialMode.Main || tile == null) return;

        string key = $"{tile.GridPosition.X},{tile.GridPosition.Y}";
        if (_data.DugTiles.Contains(key)) return;

        int order = _data.DugTiles.Count;
        _data.DugTiles.Add(key);

        string resourceId = order < ScriptedLoot.Length ? ScriptedLoot[order] : null;
        if (resourceId == null)
        {
            tile._loot = null;
        }
        else
        {
            var resource = GameData.GetResource(resourceId);
            tile._loot = new TileLoot
            {
                ResourceId = resourceId,
                Amount = 1,
                IsBone = resource is FossilDefinition fossil && fossil.Type == ResourceType.Bone,
                RevealedAtStage = TileStage.Dug1,
                Collected = false
            };
        }

        GD.Print($"[Tutorial] Плитка {key} — {order + 1}-я по счёту, под ней: {resourceId ?? "пусто"}");
        SaveSystem.Instance?.MarkDirty();
    }

    /// <summary>Содержимое конкреции в основном обучении (null — обычный случайный бросок).</summary>
    public string GetScriptedConcretionContent()
    {
        return Mode == TutorialMode.Main && !AmmoniteObtained ? "ammonite" : null;
    }

    // ---------- Шаги ----------

    private void BuildMainSteps()
    {
        // ========== Глава 1. Музей ==========
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_01", TextKey = "tutorial.main.01", Emotion = "happy",
            TapToContinue = true,
            Precondition = () => InMuseum
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_02", TextKey = "tutorial.main.02", Emotion = "explain",
            TargetId = "top.coins", TapToContinue = true,
            Precondition = () => InMuseum
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_03", TextKey = "tutorial.main.03", Emotion = "explain",
            TargetId = "bottom.bar", TapToContinue = true, OutlineOnly = true,
            Precondition = () => InMuseum
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_04", TextKey = "tutorial.main.04",
            TargetId = "bottom.inventory", AllowTargetInput = true,
            Precondition = () => InMuseum && HasItem("ticket_canada"),
            IsDone = () => InventoryOpen
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_05", TextKey = "tutorial.main.05",
            TargetId = "inventory.ticket_use.ticket_canada", AllowTargetInput = true,
            // На раскопки уже уехали — тоже считается: шаг выполнен
            Precondition = () => InDig || (InMuseum && HasItem("ticket_canada")),
            OnEnter = () => InventoryUI.Instance?.SwitchToTicketsTab(),
            IsDone = () => InDig
        });

        // ========== Глава 2. Первые раскопки ==========
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_06", TextKey = "tutorial.main.06", Emotion = "explain",
            TapToContinue = true, RequiresDig = true,
            Precondition = () => CanReachDig,
            IsDone = () => DugCount >= 1 || HasConcretion || AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_07", TextKey = "tutorial.main.07",
            TargetId = "dig.tool.digger", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => CanReachDig,
            IsDone = () => DiggingUI.Instance?.SelectedTool == WorkerType.Digger
                           || DugCount >= 1 || HasConcretion || AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_08", TextKey = "tutorial.main.08",
            TargetId = "dig.tile.intact", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => CanReachDig,
            IsDone = () => DugCount >= 1 || HasConcretion || AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            // Ждём, пока копатель закончит (около 6 секунд)
            Id = "main_09", TextKey = "tutorial.main.09", Emotion = "explain",
            TargetId = "dig.tool.digger", OutlineOnly = true, RequiresDig = true,
            Precondition = () => CanReachDig,
            IsDone = () => DigSite.AnyTile(t => t.HasCollectableFinding) || HasConcretion || AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_10", TextKey = "tutorial.main.10", Emotion = "happy",
            TargetId = "dig.tile.finding", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => CanReachDig,
            IsDone = () => HasConcretion || AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_11", TextKey = "tutorial.main.11", Emotion = "explain",
            TargetId = "dig.return", AllowTargetInput = true,
            Precondition = () => InDig || InMuseum,
            IsDone = () => InMuseum
        });

        // ========== Глава 3. Первый экспонат ==========
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_12", TextKey = "tutorial.main.12",
            TargetId = "bottom.inventory", AllowTargetInput = true,
            Precondition = () => InMuseum && (HasConcretion || AmmoniteObtained),
            IsDone = () => InventoryOpen || AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_13", TextKey = "tutorial.main.13",
            TargetId = "inventory.tab.concretions", AllowTargetInput = true,
            Precondition = () => InMuseum && (HasConcretion || AmmoniteObtained),
            IsDone = () => AmmoniteObtained || ConcretionOpeningUI.Current != null
                           || (InventoryUI.Instance?.IsTabOpen("Concretions") ?? false)
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_14", TextKey = "tutorial.main.14",
            TargetId = "inventory.concretion_open.common_concretion", AllowTargetInput = true,
            Precondition = () => InMuseum && (HasConcretion || AmmoniteObtained),
            IsDone = () => AmmoniteObtained || ConcretionOpeningUI.Current != null
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_15", TextKey = "tutorial.main.15",
            TargetId = "concretion.hit", AllowTargetInput = true,
            Precondition = () => InMuseum && (HasConcretion || AmmoniteObtained),
            IsDone = () => AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_16", TextKey = "tutorial.main.16", Emotion = "happy",
            TapToContinue = true,
            Precondition = () => InMuseum && AmmoniteObtained
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_17", TextKey = "tutorial.main.17",
            TargetId = "bottom.shop", AllowTargetInput = true,
            Precondition = () => InMuseum && AmmoniteObtained,
            OnEnter = CloseMuseumWindows,
            IsDone = () => ShopOpen || PlacementActive || CaseExists
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_18", TextKey = "tutorial.main.18",
            TargetId = "shop.buy.display_case_1x1", AllowTargetInput = true,
            Precondition = () => InMuseum && (PlacementActive || CaseExists || Wallet.Instance.GetCoins() >= SmallCasePrice),
            OnEnter = () => { if (ShopOpen) MuseumShopUI.Current.ShowFurnitureTab(); },
            IsDone = () => PlacementActive || CaseExists
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_19", TextKey = "tutorial.main.19",
            TargetId = "placement.cell", AllowTargetInput = true,
            Precondition = () => InMuseum && (PlacementActive || CaseExists),
            IsDone = () => CaseExists || (PlacementActive && PlacementModeUI.Current.HasValidSelection)
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_20", TextKey = "tutorial.main.20",
            TargetId = "placement.confirm", AllowTargetInput = true,
            Precondition = () => InMuseum && (PlacementActive || CaseExists),
            IsDone = () => CaseExists
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_21", TextKey = "tutorial.main.21",
            TargetId = "museum.display_case", AllowTargetInput = true,
            Precondition = () => InMuseum && CaseExists && AmmoniteObtained,
            IsDone = () => DisplayCaseOpen || AmmonitePlaced
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_22", TextKey = "tutorial.main.22",
            TargetId = "case.add.ammonite", AllowTargetInput = true,
            Precondition = () => InMuseum && CaseExists && AmmoniteObtained,
            IsDone = () => AmmonitePlaced
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_23", TextKey = "tutorial.main.23", Emotion = "happy",
            TapToContinue = true,
            Precondition = () => InMuseum && AmmonitePlaced,
            OnEnter = CloseMuseumWindows
        });

        // ========== Глава 4. Навыки ==========
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_24", TextKey = "tutorial.main.24", Emotion = "explain",
            TargetId = "bottom.progress", AllowTargetInput = true,
            Precondition = () => InMuseum && (SecondDiggerUnlocked || CanUnlockSecondDigger),
            IsDone = () => SecondDiggerUnlocked || ProgressOpen
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_25", TextKey = "tutorial.main.25",
            TargetId = "skill.node." + DiggerSkillId, AllowTargetInput = true,
            Precondition = () => InMuseum && (SecondDiggerUnlocked || CanUnlockSecondDigger),
            OnEnter = () => { if (ProgressOpen) ProgressUI.Instance.ShowSkillTree(); },
            IsDone = () => SecondDiggerUnlocked || SkillPanelShowsDigger
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_26", TextKey = "tutorial.main.26",
            TargetId = "skill.upgrade", AllowTargetInput = true,
            Precondition = () => InMuseum && (SecondDiggerUnlocked || CanUnlockSecondDigger),
            IsDone = () => SecondDiggerUnlocked
        });
        _mainSteps.Add(new TutorialStep
        {
            // Второй билет не нужен: раскопки в Канаде ещё открыты
            Id = "main_27", TextKey = "tutorial.main.27", Emotion = "happy",
            TargetId = "bottom.dig", AllowTargetInput = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            OnEnter = CloseMuseumWindows,
            IsDone = () => InDig || SkeletonProgressed
        });

        // ========== Глава 5. Скелет ==========
        // Плитки по порядку копания: 2-я — череп, 3-я — пусто, 4-я — туловище, 5-я — хвост
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_28a", TextKey = "tutorial.main.28",
            TargetId = "dig.tool.digger", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => DiggingUI.Instance?.SelectedTool == WorkerType.Digger || DugCount >= 3 || SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_28b", TextKey = "tutorial.main.28",
            TargetId = "dig.tile.intact", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => DugCount >= 2 || SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_28c", TextKey = "tutorial.main.28",
            TargetId = "dig.tile.intact", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => DugCount >= 3 || SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            // Ждём, пока копатели дойдут до кости
            Id = "main_28w", TextKey = "tutorial.main.wait", RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => SkullAtLeast(BoneStage.Revealed)
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_29", TextKey = "tutorial.main.29", Emotion = "happy",
            TargetId = "dig.tool.extractor", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => DiggingUI.Instance?.SelectedTool == WorkerType.Extractor || SkullAtLeast(BoneStage.Extracting)
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_30", TextKey = "tutorial.main.30",
            TargetId = "dig.tile.bone", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => SkullAtLeast(BoneStage.Extracting)
        });
        _mainSteps.Add(new TutorialStep
        {
            // Пока извлекатель работает, цели нет и экран закрыт; когда кость
            // ляжет на плитку, на ней появится "окно"
            Id = "main_31", TextKey = "tutorial.main.31", Emotion = "explain",
            TargetId = "dig.tile.finding", AllowTargetInput = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => SkullAtLeast(BoneStage.Collected)
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_32", TextKey = "tutorial.main.32",
            TapToContinue = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => HasAllParts || SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            // Две оставшиеся части игрок добывает сам, без блокировки
            Id = "main_33", TextKey = "tutorial.main.33", Emotion = "explain",
            FreePlay = true, RequiresDig = true,
            Precondition = () => SkeletonProgressed || CanReachDig,
            IsDone = () => HasAllParts || SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_34", TextKey = "tutorial.main.34", Emotion = "happy",
            TargetId = "dig.return", AllowTargetInput = true,
            Precondition = () => HasAllParts || SkeletonProgressed,
            IsDone = () => InMuseum
        });

        // ========== Глава 6. Сборка и финал ==========
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_35a", TextKey = "tutorial.main.35",
            TargetId = "bottom.inventory", AllowTargetInput = true,
            Precondition = () => InMuseum && (HasAllParts || SkeletonProgressed),
            IsDone = () => InventoryOpen || SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            // Кнопка "Кости" раскрывает второй ряд вкладок и сама открывает сборку
            Id = "main_35b", TextKey = "tutorial.main.35",
            TargetId = "inventory.tab.bones", AllowTargetInput = true,
            Precondition = () => InMuseum && (HasAllParts || SkeletonProgressed),
            IsDone = () => SkeletonProgressed || InventoryTabOpen("Assembling")
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_36", TextKey = "tutorial.main.36",
            TargetId = "inventory.assemble." + SkeletonId, AllowTargetInput = true,
            Precondition = () => InMuseum && (HasAllParts || SkeletonProgressed),
            IsDone = () => SkeletonProgressed
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_37a", TextKey = "tutorial.main.37", Emotion = "happy",
            TargetId = "inventory.tab.skeletons", AllowTargetInput = true,
            Precondition = () => InMuseum && SkeletonProgressed,
            OnEnter = () => { if (!SkeletonPlaced && !PlacementActive && InventoryUI.Instance != null) InventoryUI.Instance.Visible = true; },
            IsDone = () => SkeletonPlaced || PlacementActive || InventoryTabOpen("Skeletons")
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_37b", TextKey = "tutorial.main.37",
            TargetId = "inventory.place." + SkeletonId, AllowTargetInput = true,
            Precondition = () => InMuseum && SkeletonProgressed,
            IsDone = () => SkeletonPlaced || PlacementActive
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_38a", TextKey = "tutorial.main.38",
            TargetId = "placement.cell", AllowTargetInput = true,
            Precondition = () => InMuseum && (SkeletonPlaced || PlacementActive),
            IsDone = () => SkeletonPlaced || (PlacementActive && PlacementModeUI.Current.HasValidSelection)
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_38b", TextKey = "tutorial.main.38",
            TargetId = "placement.confirm", AllowTargetInput = true,
            Precondition = () => InMuseum && (SkeletonPlaced || PlacementActive),
            IsDone = () => SkeletonPlaced
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_39", TextKey = "tutorial.main.39", Emotion = "happy",
            TapToContinue = true,
            Precondition = () => InMuseum && SkeletonPlaced
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_40a", TextKey = "tutorial.main.40", Emotion = "explain",
            TargetId = "bottom.shop", AllowTargetInput = true,
            Precondition = () => InMuseum,
            IsDone = () => ShopOpen
        });
        _mainSteps.Add(new TutorialStep
        {
            // Только показываем вкладку билетов — ничего не покупаем
            Id = "main_40b", TextKey = "tutorial.main.40", Emotion = "explain",
            TargetId = "shop.tab.tickets", AllowTargetInput = true,
            Precondition = () => InMuseum,
            IsDone = () => ShopOpen && MuseumShopUI.Current.IsTicketsTabOpen
        });
        _mainSteps.Add(new TutorialStep
        {
            Id = "main_41", TextKey = "tutorial.main.41", Emotion = "happy",
            TargetId = "top.settings", TapToContinue = true,
            Precondition = () => InMuseum,
            OnEnter = () =>
            {
                if (ShopOpen) MuseumShopUI.Current.Visible = false;
                GiveMainReward();
            }
        });
    }

    // ---------- Состояние для глав 4-6 ----------

    private const string DiggerSkillId = "digging_diggers_count";
    private const string SkeletonId = "triceratops";
    private static readonly string[] SkeletonParts = { "triceratops_skull", "triceratops_body", "triceratops_tail" };

    private static bool SecondDiggerUnlocked => SkillSystem.Instance.GetSkillLevel(DiggerSkillId) >= 1;
    private static bool CanUnlockSecondDigger => SkillSystem.Instance.CanUpgrade(DiggerSkillId);

    private static bool ProgressOpen =>
        ProgressUI.Instance != null && GodotObject.IsInstanceValid(ProgressUI.Instance) && ProgressUI.Instance.Visible;

    private static bool SkillPanelShowsDigger =>
        SkillDetailPanel.Current != null && GodotObject.IsInstanceValid(SkillDetailPanel.Current)
        && SkillDetailPanel.Current.IsShowing(DiggerSkillId);

    private static bool InventoryTabOpen(string tab) => InventoryUI.Instance?.IsTabOpen(tab) ?? false;

    private static bool HasAllParts
    {
        get
        {
            foreach (var part in SkeletonParts)
            {
                if (!HasItem(part)) return false;
            }
            return true;
        }
    }

    private static bool SkeletonPlaced
    {
        get
        {
            foreach (var room in MuseumLayout.Instance.Rooms)
            {
                foreach (var placed in room.PlacedFurnitureList)
                {
                    if (placed.Furniture is CollectionExhibit && placed.FurnitureTypeId == SkeletonId) return true;
                }
            }
            return false;
        }
    }

    /// <summary>
    /// Скелет уже собран: лежит в инвентаре, ставится прямо сейчас или стоит
    /// в зале. С этого момента шаги про раскопки и сборку считаются пройденными.
    /// </summary>
    private static bool SkeletonProgressed =>
        HasItem(SkeletonId) || SkeletonPlaced || (PlacementActive && !HasAllParts && SkullEverCollected);

    private static bool SkullEverCollected => ProgressSystem.Instance.UnlockedResources.Contains(SkeletonParts[0]);

    private enum BoneStage { Revealed, Extracting, Lying, Collected }

    /// <summary>Как далеко продвинулась добыча черепа (первой кости).</summary>
    private static bool SkullAtLeast(BoneStage stage)
    {
        string skull = SkeletonParts[0];

        // Уже в инвентаре (или дальше — ушёл в скелет)
        if (HasItem(skull) || SkeletonProgressed) return true;
        if (stage == BoneStage.Collected) return false;

        bool IsSkull(Tile t) => t._loot != null && t._loot.ResourceId == skull && !t._loot.Collected;

        if (DigSite.AnyTile(t => IsSkull(t) && t.Stage == TileStage.FullyDug)) return true;      // лежит на плитке
        if (stage == BoneStage.Lying) return false;

        if (DigSite.AnyTile(t => IsSkull(t) && t.Action == TileAction.Extracting)) return true;
        if (stage == BoneStage.Extracting) return false;

        return DigSite.AnyTile(t => IsSkull(t) && t.HasExtractableFinding);
    }

    private void GiveMainReward()
    {
        if (_data.RewardGiven) return;

        _data.RewardGiven = true;
        Wallet.Instance?.AddCoins(MainRewardCoins);
        GD.Print($"[Tutorial] 🎁 Награда за обучение: +{MainRewardCoins} монет");
        SaveSystem.Instance?.MarkDirty();
    }


    private const int SmallCasePrice = 200;

    private static bool ShopOpen =>
        MuseumShopUI.Current != null && GodotObject.IsInstanceValid(MuseumShopUI.Current) && MuseumShopUI.Current.Visible;

    /// <summary>Закрывает окна музея, оставшиеся от прошлого шага, чтобы открыть путь к следующему.</summary>
    private static void CloseMuseumWindows()
    {
        if (ConcretionOpeningUI.Current != null && GodotObject.IsInstanceValid(ConcretionOpeningUI.Current))
            ConcretionOpeningUI.Current.ForceClose();

        if (InventoryUI.Instance != null) InventoryUI.Instance.Visible = false;

        if (DisplayCaseUI.Current != null && GodotObject.IsInstanceValid(DisplayCaseUI.Current))
            DisplayCaseUI.Current.ForceClose();

        if (SkillDetailPanel.Current != null && GodotObject.IsInstanceValid(SkillDetailPanel.Current))
            SkillDetailPanel.Current.Hide();

        if (ProgressUI.Instance != null && GodotObject.IsInstanceValid(ProgressUI.Instance))
            ProgressUI.Instance.Visible = false;
    }

    /// <summary>Цели в мире музея, которые не привязаны к конкретному окну.</summary>
    private static void RegisterMuseumWorldTargets()
    {
        TutorialTargets.RegisterProvider("museum.display_case", () =>
        {
            var overlay = TutorialOverlay.Instance;
            if (overlay == null || !GodotObject.IsInstanceValid(overlay)) return null;
            if (!TryFindDisplayCase(out var room, out var placed)) return null;

            float gx = room.GlobalOffset.X + placed.Position.X + (placed.Size.X - 1) / 2f;
            float gy = room.GlobalOffset.Y + placed.Position.Y + (placed.Size.Y - 1) / 2f;

            // Запас вверх: витрина — высокий спрайт, стоящий на клетке
            return PlacementModeUI.MuseumWorldToScreenRect(overlay, gx, gy, placed.Size.X, placed.Size.Y, 70f);
        });
    }

    // =================================================================
    //  Справка (обучение из шестерёнки)
    // =================================================================

    private void BuildGuideSteps()
    {
        AddGuide("guide_01", null, "happy");
        AddGuide("guide_02", "top.coins");             // музей и доход
        AddGuide("guide_03", "bottom.inventory");      // билеты
        AddGuide("guide_04", "bottom.inventory");      // находки и кости
        AddGuide("guide_05", "bottom.inventory");      // сборка и скелеты
        AddGuide("guide_06", "bottom.inventory");      // конкреции
        AddGuide("guide_07", "bottom.shop");           // магазин
        AddGuide("guide_08", "bottom.progress");       // прогресс
        AddGuide("guide_09", "bottom.quests");         // задания
        AddGuide("guide_10", "bottom.dig");            // рабочие
        AddGuide("guide_11", "bottom.dig");            // находки на раскопках
        AddGuide("guide_12", null, "happy");
    }

    private void AddGuide(string id, string targetId, string emotion = "explain")
    {
        _guideSteps.Add(new TutorialStep
        {
            Id = id,
            TextKey = "tutorial.guide." + id.Substring(id.Length - 2),
            Emotion = emotion,
            TargetId = targetId,
            TapToContinue = true
        });
    }

    // =================================================================
    //  Запуск / завершение
    // =================================================================

    /// <summary>Вызывается при создании новой игры (сохранения нет).</summary>
    public void MarkNewGame()
    {
        _data = new TutorialSaveData();
        _mainPending = true;

        // Музей мог загрузиться раньше, чем создалась новая игра — тогда
        // запускаем сразу, не дожидаясь следующего входа в сцену.
        if (SceneName == SceneMuseum && Mode == TutorialMode.None
            && TutorialOverlay.Instance != null && GodotObject.IsInstanceValid(TutorialOverlay.Instance))
        {
            StartMain();
        }
    }

    /// <summary>Сцена сообщает, что она загрузилась (Museum / DigSite).</summary>
    public void OnSceneEntered(Node scene, string sceneName)
    {
        SceneName = sceneName;
        if (scene == null) return;

        TutorialOverlay.Ensure(scene.GetTree());

        // Новая игра или обучение было прервано выходом из игры — продолжаем
        bool shouldRunMain = _mainPending || (_data.MainStarted && !_data.MainFinished);
        if (Mode == TutorialMode.None && shouldRunMain && sceneName == SceneMuseum)
        {
            StartMain();
        }
    }

    private void StartMain()
    {
        _mainPending = false;
        Mode = TutorialMode.Main;

        // Продолжаем с сохранённого шага, если он есть
        _index = 0;
        if (_data.MainStarted && !string.IsNullOrEmpty(_data.MainStepId))
        {
            int saved = _mainSteps.FindIndex(s => s.Id == _data.MainStepId);
            if (saved >= 0) _index = saved;
        }

        _data.MainStarted = true;
        EnterStep();
    }

    /// <summary>Справка из шестерёнки. Ничего не меняет в игре.</summary>
    public void StartGuide(Node context)
    {
        if (context != null) TutorialOverlay.Ensure(context.GetTree());

        Mode = TutorialMode.Guide;
        _index = 0;
        EnterStep();
    }

    private void EnterStep()
    {
        _detourActive = false;
        StepVersion++;
        var step = CurrentStep;
        if (step == null) return;

        if (Mode == TutorialMode.Main)
        {
            _data.MainStepId = step.Id;
            SaveSystem.Instance?.MarkDirty();
        }

        step.OnEnter?.Invoke();
    }

    public void Advance()
    {
        if (!IsActive) return;

        _index++;
        if (_index >= ActiveSteps.Count)
        {
            Finish(completed: true);
            return;
        }
        EnterStep();
    }

    /// <summary>Назад — только в справке.</summary>
    public void Back()
    {
        if (Mode != TutorialMode.Guide || _index <= 0) return;
        _index--;
        EnterStep();
    }

    /// <summary>Крестик: пропустить обучение целиком. Награда за основное не выдаётся.</summary>
    public void Skip()
    {
        if (!IsActive) return;
        Finish(completed: false);
    }

    private void Finish(bool completed)
    {
        if (Mode == TutorialMode.Main)
        {
            _data.MainFinished = true;
            _data.MainStepId = "";
            SaveSystem.Instance?.MarkDirty();
            GD.Print(completed ? "[Tutorial] ✅ Основное обучение пройдено" : "[Tutorial] ⏭️ Основное обучение пропущено");
        }

        Mode = TutorialMode.None;
        _index = 0;
        _detourActive = false;
        StepVersion++;
    }

    /// <summary>
    /// Вызывается оверлеем каждый кадр: проверяет, выполним ли шаг и не
    /// завершён ли он.
    /// </summary>
    public void Tick()
    {
        if (Mode != TutorialMode.Main) return;

        var step = ScriptStep;
        if (step == null) return;

        // Шаг на раскопках, а игрок в музее (перезапустил игру): пока раскопки
        // ещё открыты — ведём его туда, сценарий при этом стоит на месте.
        bool needDetour = step.RequiresDig && InMuseum && HasDigSession
                          && !(step.IsDone != null && step.IsDone());
        if (needDetour != _detourActive)
        {
            _detourActive = needDetour;
            StepVersion++;
        }
        if (_detourActive) return;

        // Состояние игры не сходится со сценарием (нет билета, игрок в другом
        // месте и т.п.) — действий не требуем, показываем справку.
        if (step.Precondition != null && !step.Precondition())
        {
            GD.Print($"[Tutorial] ⚠️ Шаг '{step.Id}' невыполним в текущем состоянии — переключаюсь на справку");
            _data.MainFinished = true;
            _data.MainStepId = "";
            SaveSystem.Instance?.MarkDirty();

            Mode = TutorialMode.Guide;
            _index = 0;
            EnterStep();
            return;
        }

        if (step.IsDone != null && step.IsDone())
        {
            Advance();
        }
    }

    // =================================================================
    //  Сохранение
    // =================================================================

    public TutorialSaveData GetSaveData() => _data;

    public void LoadFromSaveData(TutorialSaveData data)
    {
        // В старых сохранениях блока нет — считаем обучение пройденным,
        // чтобы не запускать его игроку с уже обжитым музеем.
        _data = data ?? new TutorialSaveData { MainFinished = true };
        _data.DugTiles ??= new List<string>();
        _mainPending = false;
    }
}
