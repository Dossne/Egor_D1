using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    private class LevelConfig
    {
        public List<Vector2> berryPositions = new();
        public List<ObstacleConfig> obstacles = new();
        public List<SpikePlatformConfig> spikePlatforms = new();
    }

    [System.Serializable]
    private class ObstacleConfig
    {
        public ObstacleType type;
        public Vector2 position;
        public float radius;
        public float length;
        public bool horizontal;
    }

    [System.Serializable]
    private class SpikePlatformConfig
    {
        public Vector2 position;
        public Vector2 size = new(1.22f, 1.22f);
    }

    private enum ObstacleType
    {
        Pillar = 0,
        Line = 1
    }

    [Header("Game Rules")]
    [SerializeField] private int startSnakeLength = 5;
    [SerializeField] private int growthPerBerry = 3;
    [FormerlySerializedAs("snakeSpeed")]
    [SerializeField] private float snakeSpeed = 2.625f;
    [SerializeField] private float snakeSpeedFactor = 1f;
    [SerializeField] private float snakeSpeedBoostFactor = 1.25f;
    [SerializeField] private float levelTimerDurationSeconds = 60f;
    [SerializeField] private float deathMenuDelaySeconds = 1f;
    [SerializeField] private List<LevelConfig> levels = new();

    [Header("Arena")]
    [SerializeField] private Vector2 baseArenaSize = new(8f, 12f);
    [SerializeField] private Vector2 nextLevelSizeMultiplierRange = new(1.5f, 2f);
    [SerializeField] private float wallThickness = 0.4f;
    [SerializeField] private float fixedCameraSize = 6.4f;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0f, -10f);

    [Header("UI Style (GUI Pro - Fantasy RPG)")]
    [SerializeField] private Font uiFont;
    [SerializeField] private Sprite uiPopupSprite;
    [SerializeField] private Sprite uiButtonSprite;
    [SerializeField] private Sprite uiJoystickSprite;
    [SerializeField] private Sprite uiTimerIconSprite;
    [SerializeField] private Sprite uiWinIconSprite;
    [SerializeField] private Sprite uiRetryIconSprite;
    [SerializeField] private Sprite uiResultRibbonSprite;
    [SerializeField] private Sprite uiStarSprite;

    private readonly List<Berry> activeBerries = new();
    private readonly List<GameObject> activeJuiceDroplets = new();
    private readonly List<Vector2> currentMinePositions = new();
    private readonly List<float> currentMineRadii = new();

    private SnakeController snakeController;
    private JoystickInput joystickInput;
    private Text levelText;
    private Text timerText;
    private GameObject levelCompleteMenu;
    private Text levelCompleteText;
    private Text resultRibbonText;
    private readonly List<Image> levelStarImages = new();
    private GameObject nextLevelButton;
    private Camera mainCamera;
    private Transform arenaRoot;
    private readonly List<float> levelArenaScales = new();
    private Vector2 currentArenaSize;
    private int currentLevelIndex;
    private bool levelFinished;
    private float remainingLevelTime;
    private bool isResultMenuPending;
    private const int TotalLevelCount = 21;
    private const float BerryRadius = 0.372f;
    private const float BerryWallClearance = 0.2f;
    private const float BerryMineClearance = 0.15f;
    private readonly Color timerDefaultColor = new(1f, 0.95f, 0.2f);
    private readonly Color titleTextColor = new(0.97f, 0.91f, 0.68f);
    private readonly Color bodyTextColor = new(0.96f, 0.93f, 0.82f);
    private readonly Color panelTintColor = new(0.12f, 0.1f, 0.22f, 0.96f);
    private readonly Color buttonFallbackColor = new(0.62f, 0.14f, 0.2f, 0.98f);
    private readonly Color iconTintColor = new(1f, 0.95f, 0.78f, 1f);

    private void Awake()
    {
        Time.timeScale = 1f;
        TryAutoAssignGuiProAssets();
        SetupPortraitOrientation();
        SetupCamera();
        SetupUi();
        EnsureLevelsConfigured();
        BuildArena();
        CreateSnake();
        SpawnCurrentLevelBerries();
        remainingLevelTime = Mathf.Max(0f, levelTimerDurationSeconds);
        UpdateLevelText();
        UpdateTimerVisual();
    }

    private void LateUpdate()
    {
        if (mainCamera == null || snakeController == null || levelFinished)
        {
            return;
        }

        var snakePosition = snakeController.transform.position;
        mainCamera.transform.position = new Vector3(snakePosition.x, snakePosition.y, 0f) + cameraOffset;
    }

    private void Update()
    {
        if (levelFinished)
        {
            return;
        }

        remainingLevelTime = Mathf.Max(0f, remainingLevelTime - Time.deltaTime);
        UpdateTimerVisual();
        if (remainingLevelTime <= 0f)
        {
            HandleTimeExpired();
        }
    }

    private void SetupPortraitOrientation()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = fixedCameraSize;
        mainCamera.transform.position = cameraOffset;
        mainCamera.backgroundColor = new Color(0.08f, 0.08f, 0.11f);
    }

    private void SetupUi()
    {
        var canvasObject = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        var joystickArea = new GameObject("JoystickTouchArea", typeof(RectTransform), typeof(Image), typeof(JoystickInput));
        joystickArea.transform.SetParent(canvasObject.transform, false);
        var joystickAreaRect = joystickArea.GetComponent<RectTransform>();
        joystickAreaRect.anchorMin = Vector2.zero;
        joystickAreaRect.anchorMax = Vector2.one;
        joystickAreaRect.offsetMin = Vector2.zero;
        joystickAreaRect.offsetMax = Vector2.zero;

        var touchAreaImage = joystickArea.GetComponent<Image>();
        touchAreaImage.color = new Color(0f, 0f, 0f, 0f);
        touchAreaImage.raycastTarget = true;

        var joystickRoot = new GameObject("Joystick", typeof(RectTransform), typeof(Image));
        joystickRoot.transform.SetParent(joystickArea.transform, false);
        var joystickRect = joystickRoot.GetComponent<RectTransform>();
        joystickRect.anchorMin = new Vector2(0.5f, 0.5f);
        joystickRect.anchorMax = new Vector2(0.5f, 0.5f);
        joystickRect.pivot = new Vector2(0.5f, 0.5f);
        joystickRect.sizeDelta = new Vector2(236f, 236f);

        var joystickBg = joystickRoot.GetComponent<Image>();
        joystickBg.color = new Color(1f, 1f, 1f, 0.78f);
        joystickBg.sprite = uiJoystickSprite != null ? uiJoystickSprite : RuntimeSpriteFactory.CircleSprite;
        joystickBg.type = uiJoystickSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        joystickBg.preserveAspect = uiJoystickSprite == null;
        joystickBg.raycastTarget = false;

        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(joystickRoot.transform, false);
        var handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(102f, 102f);
        var handleImage = handleObject.GetComponent<Image>();
        handleImage.sprite = uiButtonSprite != null ? uiButtonSprite : RuntimeSpriteFactory.CircleSprite;
        handleImage.color = new Color(1f, 1f, 1f, 0.93f);
        handleImage.type = uiButtonSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        handleImage.preserveAspect = uiButtonSprite == null;
        handleImage.raycastTarget = false;

        joystickInput = joystickArea.GetComponent<JoystickInput>();
        joystickInput.SetReferences(joystickRect, handleRect);

        levelText = CreateText(canvasObject.transform, "LevelText", new Vector2(20f, -20f), new Vector2(420f, 96f), TextAnchor.UpperLeft, 48);
        levelText.color = titleTextColor;
        levelText.text = "level 1";
        timerText = CreateText(canvasObject.transform, "TimerText", new Vector2(0f, -24f), new Vector2(420f, 132f), TextAnchor.UpperCenter, 72);
        var timerRect = timerText.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1f);
        timerRect.anchorMax = new Vector2(0.5f, 1f);
        timerRect.pivot = new Vector2(0.5f, 1f);
        timerText.color = timerDefaultColor;
        CreateDecorativeIcon(canvasObject.transform, "TimerIcon", uiTimerIconSprite, new Vector2(-190f, -54f), new Vector2(80f, 80f), iconTintColor);

        levelCompleteMenu = new GameObject("LevelCompleteMenu", typeof(RectTransform), typeof(Image));
        levelCompleteMenu.transform.SetParent(canvasObject.transform, false);
        var menuRect = levelCompleteMenu.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(700f, 760f);
        var menuBackground = levelCompleteMenu.GetComponent<Image>();
        menuBackground.sprite = uiPopupSprite;
        menuBackground.type = uiPopupSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        menuBackground.color = uiPopupSprite != null
            ? Color.white
            : panelTintColor;

        CreateDecorativeIcon(levelCompleteMenu.transform, "ResultIcon", uiWinIconSprite, new Vector2(0f, 300f), new Vector2(92f, 92f), iconTintColor);

        levelCompleteText = CreateText(levelCompleteMenu.transform, "LevelCompleteText", new Vector2(0f, 100f), new Vector2(640f, 120f), TextAnchor.MiddleCenter, 72);
        var levelCompleteRect = levelCompleteText.GetComponent<RectTransform>();
        levelCompleteRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelCompleteRect.anchorMax = new Vector2(0.5f, 0.5f);
        levelCompleteRect.pivot = new Vector2(0.5f, 0.5f);
        levelCompleteRect.anchoredPosition = new Vector2(0f, 100f);
        levelCompleteText.color = bodyTextColor;
        levelCompleteText.text = "great run!";

        var ribbonObject = new GameObject("ResultRibbon", typeof(RectTransform), typeof(Image));
        ribbonObject.transform.SetParent(levelCompleteMenu.transform, false);
        var ribbonRect = ribbonObject.GetComponent<RectTransform>();
        ribbonRect.anchorMin = new Vector2(0.5f, 0.5f);
        ribbonRect.anchorMax = new Vector2(0.5f, 0.5f);
        ribbonRect.pivot = new Vector2(0.5f, 0.5f);
        ribbonRect.anchoredPosition = new Vector2(0f, 235f);
        ribbonRect.sizeDelta = new Vector2(620f, 146f);
        var ribbonImage = ribbonObject.GetComponent<Image>();
        ribbonImage.sprite = uiResultRibbonSprite != null ? uiResultRibbonSprite : RuntimeSpriteFactory.RibbonBannerSprite;
        ribbonImage.type = Image.Type.Sliced;
        ribbonImage.color = Color.white;

        resultRibbonText = CreateText(ribbonObject.transform, "RibbonText", Vector2.zero, new Vector2(560f, 110f), TextAnchor.MiddleCenter, 48);
        var ribbonTextRect = resultRibbonText.GetComponent<RectTransform>();
        ribbonTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        ribbonTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        ribbonTextRect.pivot = new Vector2(0.5f, 0.5f);
        ribbonTextRect.anchoredPosition = new Vector2(0f, 0f);
        resultRibbonText.color = Color.white;
        resultRibbonText.resizeTextForBestFit = true;
        resultRibbonText.resizeTextMinSize = 26;
        resultRibbonText.resizeTextMaxSize = 48;

        CreateStarRow(levelCompleteMenu.transform);

        nextLevelButton = CreateMenuButton(levelCompleteMenu.transform, "NextLevelButton", "next level", new Vector2(0f, -90f), uiWinIconSprite, HandleNextLevelPressed);
        CreateMenuButton(levelCompleteMenu.transform, "RetryButton", "retry", new Vector2(0f, -220f), uiRetryIconSprite, HandleRetryPressed);
        levelCompleteMenu.SetActive(false);
    }

    private void CreateStarRow(Transform parent)
    {
        var starsRoot = new GameObject("LevelStars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        starsRoot.transform.SetParent(parent, false);
        var starsRootRect = starsRoot.GetComponent<RectTransform>();
        starsRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        starsRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        starsRootRect.pivot = new Vector2(0.5f, 0.5f);
        starsRootRect.anchoredPosition = new Vector2(0f, 165f);
        starsRootRect.sizeDelta = new Vector2(480f, 130f);

        var layout = starsRoot.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 22f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (var i = 0; i < 3; i++)
        {
            var starObject = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            starObject.transform.SetParent(starsRoot.transform, false);
            var starRect = starObject.GetComponent<RectTransform>();
            starRect.sizeDelta = new Vector2(112f, 112f);
            var layoutElement = starObject.GetComponent<LayoutElement>();
            layoutElement.preferredWidth = 112f;
            layoutElement.preferredHeight = 112f;

            var starImage = starObject.GetComponent<Image>();
            starImage.sprite = uiStarSprite != null ? uiStarSprite : RuntimeSpriteFactory.StarSprite;
            starImage.type = Image.Type.Simple;
            starImage.preserveAspect = true;
            levelStarImages.Add(starImage);
        }
    }

    private void BuildArena()
    {
        if (arenaRoot != null)
        {
            Destroy(arenaRoot.gameObject);
        }

        arenaRoot = new GameObject("ArenaRoot").transform;
        currentArenaSize = GetArenaSizeForLevel(currentLevelIndex);

        CreateArenaBackground();
        var topPosition = new Vector2(0f, currentArenaSize.y * 0.5f + wallThickness * 0.5f);
        var bottomPosition = new Vector2(0f, -currentArenaSize.y * 0.5f - wallThickness * 0.5f);
        var leftPosition = new Vector2(-currentArenaSize.x * 0.5f - wallThickness * 0.5f, 0f);
        var rightPosition = new Vector2(currentArenaSize.x * 0.5f + wallThickness * 0.5f, 0f);

        CreateWall("TopWall", topPosition, new Vector2(currentArenaSize.x + wallThickness * 2f, wallThickness));
        CreateWall("BottomWall", bottomPosition, new Vector2(currentArenaSize.x + wallThickness * 2f, wallThickness));
        CreateWall("LeftWall", leftPosition, new Vector2(wallThickness, currentArenaSize.y));
        CreateWall("RightWall", rightPosition, new Vector2(wallThickness, currentArenaSize.y));

        CreateLaserCorner("TopLeftCorner", new Vector2(leftPosition.x, topPosition.y));
        CreateLaserCorner("TopRightCorner", new Vector2(rightPosition.x, topPosition.y));
        CreateLaserCorner("BottomLeftCorner", new Vector2(leftPosition.x, bottomPosition.y));
        CreateLaserCorner("BottomRightCorner", new Vector2(rightPosition.x, bottomPosition.y));
        CreateLevelObstacles();
        ApplyCameraSizeForCurrentArena();
    }

    private void CreateSnake()
    {
        var snakeObject = new GameObject("SnakeHead", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SnakeBody), typeof(SnakeController));
        snakeObject.transform.position = Vector3.zero;

        snakeController = snakeObject.GetComponent<SnakeController>();
        snakeController.Setup(this, joystickInput, startSnakeLength, snakeSpeed * snakeSpeedFactor * snakeSpeedBoostFactor);
    }

    public void HandleBerryCollected(Berry berry)
    {
        if (levelFinished)
        {
            return;
        }

        if (activeBerries.Remove(berry))
        {
            Destroy(berry.gameObject);
        }

        if (activeBerries.Count == 0)
        {
            levelFinished = true;
            ShowResultMenu($"level {currentLevelIndex + 1} complete", true);
            UpdateLevelStarsVisual(GetStarsForRemainingTime());
            Time.timeScale = 0f;
        }
    }

    public void SpawnBerryJuiceSplash(Vector3 center)
    {
        const int dropletCount = 9;
        for (var i = 0; i < dropletCount; i++)
        {
            var droplet = new GameObject($"JuiceDrop_{i + 1}", typeof(SpriteRenderer));
            droplet.transform.position = center;
            droplet.transform.localScale = Vector3.one * Random.Range(0.1f, 0.18f);
            activeJuiceDroplets.Add(droplet);

            var dropletRenderer = droplet.GetComponent<SpriteRenderer>();
            dropletRenderer.sprite = RuntimeSpriteFactory.CircleSprite;
            dropletRenderer.color = new Color(0.9f, 0.16f, 0.18f, Random.Range(0.68f, 0.86f));
            dropletRenderer.sortingOrder = 0;

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var distance = Random.Range(0.28f, 1.05f);
            var duration = Random.Range(0.12f, 0.24f);
            StartCoroutine(AnimateJuiceDroplet(droplet, center, direction * distance, duration));
        }
    }

    public void HandleLose(Vector3 deathPosition, bool spawnExplosion)
    {
        if (levelFinished || isResultMenuPending)
        {
            return;
        }

        levelFinished = true;
        isResultMenuPending = true;
        if (snakeController != null)
        {
            snakeController.gameObject.SetActive(false);
        }

        SpawnSnakeDeathJuice(deathPosition);
        if (spawnExplosion)
        {
            SpawnMineExplosion(deathPosition);
        }

        StartCoroutine(ShowResultMenuAfterDelay("you died", false));
    }

    private void RestartCurrentLevel()
    {
        StopAllCoroutines();
        Time.timeScale = 1f;
        levelFinished = false;
        isResultMenuPending = false;
        remainingLevelTime = Mathf.Max(0f, levelTimerDurationSeconds);
        if (levelCompleteMenu != null)
        {
            levelCompleteMenu.SetActive(false);
        }

        if (snakeController != null)
        {
            Destroy(snakeController.gameObject);
        }

        for (var i = 0; i < activeBerries.Count; i++)
        {
            if (activeBerries[i] != null)
            {
                Destroy(activeBerries[i].gameObject);
            }
        }

        activeBerries.Clear();
        ClearJuiceDroplets();

        CreateSnake();
        BuildArena();
        SpawnCurrentLevelBerries();
        UpdateLevelText();
        UpdateTimerVisual();
    }

    private void SpawnCurrentLevelBerries()
    {
        if (levels.Count == 0)
        {
            return;
        }

        var level = levels[Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1)];
        var berryPositions = BuildBerrySpawnPositions(level);
        for (var i = 0; i < berryPositions.Count; i++)
        {
            var berryObject = new GameObject($"Berry_{i + 1}", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Berry));
            berryObject.transform.position = berryPositions[i];

            var berry = berryObject.GetComponent<Berry>();
            berry.Setup(this, growthPerBerry);
            activeBerries.Add(berry);
        }
    }

    private void EnsureLevelsConfigured()
    {
        if (levels.Count == 0)
        {
            levels = new List<LevelConfig>
            {
                new() { berryPositions = new List<Vector2> { new(-2.6f, -2.2f), new(0.3f, 1.6f), new(2.3f, 3.1f) } }
            };
        }

        if (levels.Count < TotalLevelCount)
        {
            for (var i = levels.Count; i < TotalLevelCount; i++)
            {
                levels.Add(new LevelConfig());
            }
        }

        EnsureProceduralLevelContent();

        EnsureLevelArenaScales();
    }

    private void EnsureProceduralLevelContent()
    {
        for (var i = 1; i < levels.Count; i++)
        {
            if (levels[i].obstacles == null || levels[i].obstacles.Count == 0)
            {
                levels[i].obstacles = GenerateObstaclesForLevel(i);
            }

            if (levels[i].berryPositions.Count < 4 || levels[i].berryPositions.Count > 6)
            {
                levels[i].berryPositions = GenerateBerryPositionsForLevel(i, levels[i].obstacles);
            }

            if (i >= 2 && (levels[i].spikePlatforms == null || levels[i].spikePlatforms.Count == 0))
            {
                levels[i].spikePlatforms = GenerateSpikePlatformsForLevel(i, levels[i].obstacles);
            }
        }
    }

    private void HandleNextLevelPressed()
    {
        if (levels.Count == 0)
        {
            return;
        }

        currentLevelIndex = (currentLevelIndex + 1) % levels.Count;
        RestartCurrentLevel();
    }

    private void HandleRetryPressed()
    {
        RestartCurrentLevel();
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = $"level {currentLevelIndex + 1}";
        }
    }

    private void CreateWall(string name, Vector2 position, Vector2 size)
    {
        var isVerticalWall = size.y > size.x;
        var wall = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(WallCollision));
        wall.transform.SetParent(arenaRoot, true);
        wall.transform.position = position;
        wall.transform.localScale = isVerticalWall
            ? new Vector3(size.y, size.x, 1f)
            : new Vector3(size.x, size.y, 1f);

        if (isVerticalWall)
        {
            wall.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        var renderer = wall.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.WallSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 3;

        var glow = new GameObject("Glow", typeof(SpriteRenderer));
        glow.transform.SetParent(wall.transform, false);
        glow.transform.localScale = new Vector3(1.15f, 3.1f, 1f);

        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = RuntimeSpriteFactory.WallSprite;
        glowRenderer.color = new Color(1f, 0.12f, 0.12f, 0.42f);
        glowRenderer.sortingOrder = 2;

        var collider = wall.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        var wallCollision = wall.GetComponent<WallCollision>();
        wallCollision.SetType(WallCollision.HazardType.Laser);
    }

    private IEnumerator AnimateJuiceDroplet(GameObject droplet, Vector3 startPosition, Vector2 offset, float duration)
    {
        if (droplet == null)
        {
            yield break;
        }

        var dropletTransform = droplet.transform;
        var elapsed = 0f;
        var targetPosition = startPosition + (Vector3)offset;
        while (elapsed < duration)
        {
            if (dropletTransform == null)
            {
                yield break;
            }

            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var easeOut = 1f - (1f - t) * (1f - t);
            dropletTransform.position = Vector3.Lerp(startPosition, targetPosition, easeOut);
            yield return null;
        }

        if (dropletTransform != null)
        {
            dropletTransform.position = targetPosition;
        }
    }

    private void CreateLevelObstacles()
    {
        currentMinePositions.Clear();
        currentMineRadii.Clear();

        if (currentLevelIndex <= 0 || levels.Count == 0)
        {
            return;
        }

        var level = levels[Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1)];
        var levelScale = GetArenaScaleForLevel(currentLevelIndex);
        if (level.obstacles != null)
        {
            for (var i = 0; i < level.obstacles.Count; i++)
            {
                var obstacle = level.obstacles[i];
                if (obstacle.type == ObstacleType.Pillar)
                {
                    CreateMineObstacle($"Mine_{i + 1}", obstacle.position * levelScale, obstacle.radius);
                    currentMinePositions.Add(obstacle.position);
                    currentMineRadii.Add(obstacle.radius);
                }
            }
        }

        if (currentLevelIndex >= 2 && level.spikePlatforms != null)
        {
            for (var i = 0; i < level.spikePlatforms.Count; i++)
            {
                var platform = level.spikePlatforms[i];
                CreateSpikePlatform($"SpikePlatform_{i + 1}", platform.position * levelScale, platform.size);
            }
        }
    }

    private void CreateMineObstacle(string name, Vector2 position, float radius)
    {
        var mine = new GameObject(name, typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(WallCollision));
        mine.transform.SetParent(arenaRoot, true);
        mine.transform.position = position;
        var diameter = radius * 2f;
        mine.transform.localScale = new Vector3(diameter, diameter, 1f);

        var renderer = mine.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.MineBodySprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 3;

        var collider = mine.GetComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.5f;

        var wallCollision = mine.GetComponent<WallCollision>();
        wallCollision.SetType(WallCollision.HazardType.Mine);

        var center = new GameObject("Center", typeof(SpriteRenderer));
        center.transform.SetParent(mine.transform, false);
        center.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
        var centerRenderer = center.GetComponent<SpriteRenderer>();
        centerRenderer.sprite = RuntimeSpriteFactory.CircleSprite;
        centerRenderer.color = new Color(1f, 0.25f, 0.2f, 1f);
        centerRenderer.sortingOrder = 5;

        for (var i = 0; i < 8; i++)
        {
            var spike = new GameObject($"Spike_{i + 1}", typeof(SpriteRenderer));
            spike.transform.SetParent(mine.transform, false);
            var angle = i * 45f;
            spike.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            spike.transform.localPosition = Quaternion.Euler(0f, 0f, angle) * (Vector3.right * 0.72f);
            spike.transform.localScale = new Vector3(0.44f, 0.12f, 1f);

            var spikeRenderer = spike.GetComponent<SpriteRenderer>();
            spikeRenderer.sprite = RuntimeSpriteFactory.WallSprite;
            spikeRenderer.color = new Color(0.95f, 0.18f, 0.14f, 1f);
            spikeRenderer.sortingOrder = 4;
        }

        var glow = new GameObject("Glow", typeof(SpriteRenderer));
        glow.transform.SetParent(mine.transform, false);
        glow.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = RuntimeSpriteFactory.CircleSprite;
        glowRenderer.color = new Color(1f, 0.12f, 0.12f, 0.42f);
        glowRenderer.sortingOrder = 2;
    }

    private void CreateSpikePlatform(string name, Vector2 position, Vector2 size)
    {
        var platform = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(SpikePlatform));
        platform.transform.SetParent(arenaRoot, true);
        platform.transform.position = position;
        platform.transform.localScale = new Vector3(size.x, size.y, 1f);

        var baseRenderer = platform.GetComponent<SpriteRenderer>();
        baseRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        baseRenderer.color = new Color(0.44f, 0.44f, 0.44f, 1f);
        baseRenderer.sortingOrder = 2;

        var border = new GameObject("Border", typeof(SpriteRenderer));
        border.transform.SetParent(platform.transform, false);
        border.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
        var borderRenderer = border.GetComponent<SpriteRenderer>();
        borderRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        borderRenderer.color = new Color(0.26f, 0.26f, 0.26f, 1f);
        borderRenderer.sortingOrder = 1;

        var spikeRoots = new List<Transform>();
        const int spikeColumns = 4;
        const int spikeRows = 4;
        const float spikePaddingX = 0.15f;
        const float spikePaddingY = 0.15f;
        const float loweredSpikeOffset = -0.16f;
        var minX = -0.5f + spikePaddingX;
        var maxX = 0.5f - spikePaddingX;
        var minY = -0.5f + spikePaddingY + loweredSpikeOffset;
        var maxY = 0.5f - spikePaddingY + loweredSpikeOffset;
        for (var y = 0; y < spikeRows; y++)
        {
            for (var x = 0; x < spikeColumns; x++)
            {
                var spike = new GameObject($"Spike_{x}_{y}", typeof(SpriteRenderer));
                spike.transform.SetParent(platform.transform, false);
                var xPosition = spikeColumns == 1 ? 0f : Mathf.Lerp(minX, maxX, (float)x / (spikeColumns - 1));
                var yPosition = spikeRows == 1 ? loweredSpikeOffset : Mathf.Lerp(minY, maxY, (float)y / (spikeRows - 1));
                spike.transform.localPosition = new Vector3(xPosition, yPosition, 0f);
                spike.transform.localScale = new Vector3(0.3f, 0.34f, 1f);
                var spikeRenderer = spike.GetComponent<SpriteRenderer>();
                spikeRenderer.sprite = RuntimeSpriteFactory.SpikeSprite;
                spikeRenderer.color = Color.white;
                spikeRenderer.sortingOrder = 3;
                spikeRoots.Add(spike.transform);
            }
        }

        var collider = platform.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = Vector2.one;

        var spikePlatform = platform.GetComponent<SpikePlatform>();
        spikePlatform.Setup(spikeRoots, baseRenderer);
    }

    private void CreateLineObstacle(string name, Vector2 position, float length, bool horizontal)
    {
        var size = horizontal ? new Vector2(length, wallThickness) : new Vector2(wallThickness, length);
        CreateWall(name, position, size);
    }

    private List<Vector2> BuildBerrySpawnPositions(LevelConfig level)
    {
        var desiredCount = Mathf.Max(3, level.berryPositions != null ? level.berryPositions.Count : 0);
        var levelScale = GetArenaScaleForLevel(currentLevelIndex);
        var unscaledMines = new List<Vector2>();
        var unscaledMineRadii = new List<float>();
        for (var i = 0; i < currentMinePositions.Count; i++)
        {
            unscaledMines.Add(currentMinePositions[i]);
            unscaledMineRadii.Add(currentMineRadii[i]);
        }

        var layout = GenerateBerryLayout(currentLevelIndex, desiredCount, unscaledMines, unscaledMineRadii);
        level.berryPositions = layout;
        return layout.ConvertAll(position => position * levelScale);
    }

    private List<Vector2> GenerateBerryLayout(int levelIndex, int berryCount, List<Vector2> minePositions, List<float> mineRadii)
    {
        var random = new System.Random(8117 + levelIndex * 61);
        var positions = new List<Vector2>(berryCount);
        var nearTarget = Mathf.Min(2, berryCount);
        var nearGenerated = 0;
        var attempts = 0;
        var maxAttempts = 800;

        while (positions.Count < berryCount && attempts < maxAttempts)
        {
            attempts++;
            var nearHazard = nearGenerated < nearTarget;
            var candidate = nearHazard ? SampleNearHazardPoint(random, minePositions, mineRadii) : SampleSafePoint(random);
            if (!IsBerryPositionValid(candidate, positions, minePositions, mineRadii, nearHazard))
            {
                continue;
            }

            positions.Add(candidate);
            if (nearHazard)
            {
                nearGenerated++;
            }
        }

        while (positions.Count < berryCount)
        {
            var fallback = SampleSafePoint(random);
            if (IsBerryPositionValid(fallback, positions, minePositions, mineRadii, false))
            {
                positions.Add(fallback);
            }
            else
            {
                positions.Add(new Vector2(positions.Count * 0.65f - 1.3f, -2.2f + positions.Count * 0.45f));
            }
        }

        return positions;
    }

    private Vector2 SampleNearHazardPoint(System.Random random, List<Vector2> minePositions, List<float> mineRadii)
    {
        var preferWalls = minePositions.Count == 0 || random.NextDouble() < 0.55;
        if (preferWalls)
        {
            var edge = random.Next(0, 4);
            var halfWidth = baseArenaSize.x * 0.5f - (BerryRadius + BerryWallClearance + 0.15f);
            var halfHeight = baseArenaSize.y * 0.5f - (BerryRadius + BerryWallClearance + 0.15f);
            var depth = Mathf.Lerp(0.55f, 1.35f, (float)random.NextDouble());
            switch (edge)
            {
                case 0:
                    return new Vector2(Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()), halfHeight - depth);
                case 1:
                    return new Vector2(Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()), -halfHeight + depth);
                case 2:
                    return new Vector2(-halfWidth + depth, Mathf.Lerp(-halfHeight, halfHeight, (float)random.NextDouble()));
                default:
                    return new Vector2(halfWidth - depth, Mathf.Lerp(-halfHeight, halfHeight, (float)random.NextDouble()));
            }
        }

        var mineIndex = random.Next(0, minePositions.Count);
        var mine = minePositions[mineIndex];
        var radius = mineRadii[mineIndex] + BerryRadius + BerryMineClearance + Mathf.Lerp(0.15f, 0.5f, (float)random.NextDouble());
        var angle = Mathf.Lerp(0f, Mathf.PI * 2f, (float)random.NextDouble());
        return mine + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
    }

    private Vector2 SampleSafePoint(System.Random random)
    {
        var halfWidth = baseArenaSize.x * 0.5f - (BerryRadius + BerryWallClearance + 0.4f);
        var halfHeight = baseArenaSize.y * 0.5f - (BerryRadius + BerryWallClearance + 0.4f);
        return new Vector2(
            Mathf.Lerp(-halfWidth, halfWidth, (float)random.NextDouble()),
            Mathf.Lerp(-halfHeight, halfHeight, (float)random.NextDouble()));
    }

    private bool IsBerryPositionValid(
        Vector2 candidate,
        List<Vector2> existingPositions,
        List<Vector2> minePositions,
        List<float> mineRadii,
        bool mustBeNearHazard)
    {
        var halfWidth = baseArenaSize.x * 0.5f;
        var halfHeight = baseArenaSize.y * 0.5f;
        var distanceToWall = Mathf.Min(halfWidth - Mathf.Abs(candidate.x), halfHeight - Mathf.Abs(candidate.y));
        if (distanceToWall < BerryRadius + BerryWallClearance)
        {
            return false;
        }

        var isNearHazard = distanceToWall <= BerryRadius + BerryWallClearance + 1.35f;
        for (var i = 0; i < minePositions.Count; i++)
        {
            var distanceToMineCenter = Vector2.Distance(candidate, minePositions[i]);
            var minAllowed = mineRadii[i] + BerryRadius + BerryMineClearance;
            if (distanceToMineCenter < minAllowed)
            {
                return false;
            }

            if (distanceToMineCenter <= minAllowed + 0.55f)
            {
                isNearHazard = true;
            }
        }

        if (mustBeNearHazard && !isNearHazard)
        {
            return false;
        }

        for (var i = 0; i < existingPositions.Count; i++)
        {
            if (Vector2.Distance(candidate, existingPositions[i]) < 1.35f)
            {
                return false;
            }
        }

        if (candidate.magnitude < 1.2f)
        {
            return false;
        }

        return true;
    }

    private List<Vector2> GenerateBerryPositionsForLevel(int levelIndex, List<ObstacleConfig> obstacles)
    {
        var random = new System.Random(7021 + levelIndex * 37);
        var berryCount = random.Next(4, 7);
        var obstaclePositions = new List<Vector2>();
        var obstacleRadii = new List<float>();
        if (obstacles != null)
        {
            for (var i = 0; i < obstacles.Count; i++)
            {
                if (obstacles[i].type != ObstacleType.Pillar)
                {
                    continue;
                }

                obstaclePositions.Add(obstacles[i].position);
                obstacleRadii.Add(obstacles[i].radius);
            }
        }

        return GenerateBerryLayout(levelIndex, berryCount, obstaclePositions, obstacleRadii);
    }

    private List<ObstacleConfig> GenerateObstaclesForLevel(int levelIndex)
    {
        var random = new System.Random(9901 + levelIndex * 53);
        var obstacles = new List<ObstacleConfig>();
        var pillarCount = random.Next(3, 6);

        for (var i = 0; i < pillarCount; i++)
        {
            obstacles.Add(new ObstacleConfig
            {
                type = ObstacleType.Pillar,
                position = CreateObstaclePosition(random, obstacles),
                radius = Mathf.Lerp(0.14f, 0.22f, (float)random.NextDouble())
            });
        }

        return obstacles;
    }

    private List<SpikePlatformConfig> GenerateSpikePlatformsForLevel(int levelIndex, List<ObstacleConfig> obstacles)
    {
        var random = new System.Random(12811 + levelIndex * 71);
        var platforms = new List<SpikePlatformConfig>();
        var targetCount = random.Next(1, 4);
        while (platforms.Count < targetCount)
        {
            var candidate = CreateSpikePlatformPosition(random, platforms, obstacles);
            platforms.Add(new SpikePlatformConfig
            {
                position = candidate,
                size = new Vector2(Mathf.Lerp(1.12f, 1.34f, (float)random.NextDouble()), Mathf.Lerp(1.12f, 1.34f, (float)random.NextDouble()))
            });
        }

        return platforms;
    }

    private Vector2 CreateSpikePlatformPosition(System.Random random, List<SpikePlatformConfig> existingPlatforms, List<ObstacleConfig> obstacles)
    {
        const float minX = 1.2f;
        const float maxX = 3.7f;
        const float minY = 1.8f;
        const float maxY = 5.5f;
        var attempts = 0;
        while (attempts < 80)
        {
            attempts++;
            var xSign = random.Next(0, 2) == 0 ? -1f : 1f;
            var ySign = random.Next(0, 2) == 0 ? -1f : 1f;
            var candidate = new Vector2(
                xSign * Mathf.Lerp(minX, maxX, (float)random.NextDouble()),
                ySign * Mathf.Lerp(minY, maxY, (float)random.NextDouble()));

            var tooClose = false;
            for (var i = 0; i < existingPlatforms.Count; i++)
            {
                if (Vector2.Distance(candidate, existingPlatforms[i].position) < 2f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose && obstacles != null)
            {
                for (var i = 0; i < obstacles.Count; i++)
                {
                    if (obstacles[i].type != ObstacleType.Pillar)
                    {
                        continue;
                    }

                    if (Vector2.Distance(candidate, obstacles[i].position) < 1.8f)
                    {
                        tooClose = true;
                        break;
                    }
                }
            }

            if (!tooClose)
            {
                return candidate;
            }
        }

        return new Vector2(Mathf.Lerp(-2.6f, 2.6f, (float)random.NextDouble()), Mathf.Lerp(-4.8f, 4.8f, (float)random.NextDouble()));
    }

    private Vector2 CreateObstaclePosition(System.Random random, List<ObstacleConfig> existingObstacles)
    {
        const float minRange = 1.3f;
        const float maxRange = 3.8f;
        const float minDistanceFromOtherObstacles = 1.3f;
        Vector2 candidate = Vector2.zero;
        var attempts = 0;
        while (attempts < 50)
        {
            attempts++;
            candidate = new Vector2(
                Mathf.Lerp(-maxRange, maxRange, (float)random.NextDouble()),
                Mathf.Lerp(-maxRange, maxRange, (float)random.NextDouble()));

            if (candidate.magnitude < minRange)
            {
                continue;
            }

            var farEnough = true;
            for (var i = 0; i < existingObstacles.Count; i++)
            {
                if (Vector2.Distance(candidate, existingObstacles[i].position) < minDistanceFromOtherObstacles)
                {
                    farEnough = false;
                    break;
                }
            }

            if (farEnough)
            {
                break;
            }
        }

        return candidate;
    }

    private void ClearJuiceDroplets()
    {
        for (var i = activeJuiceDroplets.Count - 1; i >= 0; i--)
        {
            var droplet = activeJuiceDroplets[i];
            if (droplet != null)
            {
                Destroy(droplet);
            }
        }

        activeJuiceDroplets.Clear();
    }

    private void CreateLaserCorner(string name, Vector2 position)
    {
        var corner = new GameObject(name, typeof(SpriteRenderer));
        corner.transform.SetParent(arenaRoot, true);
        corner.transform.position = position;
        corner.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        var renderer = corner.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.CircleSprite;
        renderer.color = new Color(1f, 0.45f, 0.4f, 1f);
        renderer.sortingOrder = 4;

        var glow = new GameObject("CornerGlow", typeof(SpriteRenderer));
        glow.transform.SetParent(corner.transform, false);
        glow.transform.localScale = new Vector3(2f, 2f, 1f);

        var glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = RuntimeSpriteFactory.CircleSprite;
        glowRenderer.color = new Color(1f, 0.1f, 0.1f, 0.32f);
        glowRenderer.sortingOrder = 1;
    }

    private void CreateArenaBackground()
    {
        var background = new GameObject("ArenaBackground", typeof(SpriteRenderer));
        background.transform.SetParent(arenaRoot, true);
        background.transform.position = Vector3.zero;
        background.transform.localScale = Vector3.one;

        var renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.TableSprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = currentArenaSize;
        renderer.color = Color.white;
        renderer.sortingOrder = -1;
    }

    private float GetArenaScaleForLevel(int levelIndex)
    {
        EnsureLevelArenaScales();
        return levelArenaScales[Mathf.Clamp(levelIndex, 0, levelArenaScales.Count - 1)];
    }

    private Vector2 GetArenaSizeForLevel(int levelIndex)
    {
        return baseArenaSize * GetArenaScaleForLevel(levelIndex);
    }

    private void ApplyCameraSizeForCurrentArena()
    {
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.orthographicSize = fixedCameraSize;
    }

    private void EnsureLevelArenaScales()
    {
        if (levels.Count == 0)
        {
            return;
        }

        if (levelArenaScales.Count == levels.Count)
        {
            return;
        }

        levelArenaScales.Clear();
        var minScale = Mathf.Min(nextLevelSizeMultiplierRange.x, nextLevelSizeMultiplierRange.y);
        var maxScale = Mathf.Max(nextLevelSizeMultiplierRange.x, nextLevelSizeMultiplierRange.y);
        for (var i = 0; i < levels.Count; i++)
        {
            levelArenaScales.Add(Random.Range(minScale, maxScale));
        }
    }

    private Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, int fontSize)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Shadow));
        textObject.transform.SetParent(parent, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        var text = textObject.GetComponent<Text>();
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.supportRichText = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.fontStyle = FontStyle.Normal;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = string.Empty;
        var shadow = textObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(2f, -2f);
        shadow.useGraphicAlpha = true;
        return text;
    }

    private void ShowResultMenu(string resultText, bool showNextLevelButton)
    {
        if (levelCompleteMenu == null || levelCompleteText == null)
        {
            return;
        }

        if (resultRibbonText != null)
        {
            resultRibbonText.text = resultText.ToUpperInvariant();
        }

        levelCompleteText.text = showNextLevelButton ? "great run!" : "tap retry to continue";
        if (!showNextLevelButton)
        {
            UpdateLevelStarsVisual(0);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.SetActive(showNextLevelButton);
        }

        levelCompleteMenu.SetActive(true);
    }

    private IEnumerator ShowResultMenuAfterDelay(string resultText, bool showNextLevelButton)
    {
        yield return new WaitForSeconds(deathMenuDelaySeconds);
        ShowResultMenu(resultText, showNextLevelButton);
        Time.timeScale = 0f;
        isResultMenuPending = false;
    }

    private void HandleTimeExpired()
    {
        if (levelFinished || isResultMenuPending)
        {
            return;
        }

        levelFinished = true;
        isResultMenuPending = true;
        if (snakeController != null)
        {
            snakeController.gameObject.SetActive(false);
        }

        StartCoroutine(ShowResultMenuAfterDelay("time left", false));
    }

    private void UpdateTimerVisual()
    {
        if (timerText == null)
        {
            return;
        }

        var seconds = Mathf.CeilToInt(remainingLevelTime);
        var minutesPart = seconds / 60;
        var secondsPart = seconds % 60;
        timerText.text = $"{minutesPart:00}:{secondsPart:00}";

        if (remainingLevelTime <= 10f)
        {
            var pulse = 1f + 0.12f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f));
            timerText.transform.localScale = new Vector3(pulse, pulse, 1f);
            timerText.color = Color.red;
        }
        else
        {
            timerText.transform.localScale = Vector3.one;
            timerText.color = timerDefaultColor;
        }
    }

    private int GetStarsForRemainingTime()
    {
        var seconds = Mathf.FloorToInt(remainingLevelTime);
        if (seconds >= 30)
        {
            return 3;
        }

        if (seconds >= 20)
        {
            return 2;
        }

        return 1;
    }

    private void UpdateLevelStarsVisual(int starsCount)
    {
        if (levelStarImages.Count == 0)
        {
            return;
        }

        starsCount = Mathf.Clamp(starsCount, 0, 3);
        for (var i = 0; i < levelStarImages.Count; i++)
        {
            if (levelStarImages[i] == null)
            {
                continue;
            }

            var isFilled = i < starsCount;
            levelStarImages[i].color = isFilled
                ? Color.white
                : new Color(0.6f, 0.6f, 0.6f, 0.38f);
        }
    }

    private void SpawnSnakeDeathJuice(Vector3 deathPosition)
    {
        SpawnBerryJuiceSplash(deathPosition);
        if (snakeController == null)
        {
            return;
        }

        var body = snakeController.GetComponent<SnakeBody>();
        if (body == null)
        {
            return;
        }

        var segments = body.Segments;
        for (var i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                SpawnBerryJuiceSplash(segments[i].position);
            }
        }

        body.ClearSegments();
    }

    private void SpawnMineExplosion(Vector3 center)
    {
        const int flashCount = 10;
        for (var i = 0; i < flashCount; i++)
        {
            var flash = new GameObject($"ExplosionFlash_{i + 1}", typeof(SpriteRenderer));
            flash.transform.position = center;
            flash.transform.localScale = Vector3.one * Random.Range(0.2f, 0.4f);

            var flashRenderer = flash.GetComponent<SpriteRenderer>();
            flashRenderer.sprite = RuntimeSpriteFactory.CircleSprite;
            flashRenderer.color = new Color(1f, 0.55f, 0.15f, 0.8f);
            flashRenderer.sortingOrder = 6;
            activeJuiceDroplets.Add(flash);

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var distance = Random.Range(0.6f, 1.4f);
            var duration = Random.Range(0.08f, 0.16f);
            StartCoroutine(AnimateJuiceDroplet(flash, center, direction * distance, duration));
        }
    }

    private GameObject CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
    {
        return CreateMenuButton(parent, name, label, anchoredPosition, null, callback);
    }

    private GameObject CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPosition, Sprite iconSprite, UnityEngine.Events.UnityAction callback)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(420f, 92f);

        var buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.sprite = uiButtonSprite != null ? uiButtonSprite : RuntimeSpriteFactory.RoundedButtonSprite;
        buttonImage.type = Image.Type.Sliced;
        buttonImage.color = uiButtonSprite != null
            ? Color.white
            : buttonFallbackColor;

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);
        button.transition = Selectable.Transition.ColorTint;
        button.targetGraphic = buttonImage;
        button.colors = new ColorBlock
        {
            normalColor = Color.white,
            highlightedColor = new Color(1f, 0.95f, 0.84f, 1f),
            pressedColor = new Color(0.88f, 0.82f, 0.72f, 1f),
            selectedColor = new Color(1f, 0.95f, 0.84f, 1f),
            disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.65f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        var text = CreateText(buttonObject.transform, "Label", new Vector2(0f, 0f), new Vector2(320f, 92f), TextAnchor.MiddleCenter, 44);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(22f, 0f);
        text.color = bodyTextColor;
        text.text = label;

        if (iconSprite != null)
        {
            CreateDecorativeIcon(buttonObject.transform, "Icon", iconSprite, new Vector2(-140f, 0f), new Vector2(56f, 56f), iconTintColor);
        }

        return buttonObject;
    }

    private GameObject CreateDecorativeIcon(Transform parent, string name, Sprite iconSprite, Vector2 anchoredPosition, Vector2 size, Color tint)
    {
        if (iconSprite == null)
        {
            return null;
        }

        var iconObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(parent, false);
        var iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = anchoredPosition;
        iconRect.sizeDelta = size;

        var iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = iconSprite;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.color = tint;
        iconImage.raycastTarget = false;
        return iconObject;
    }

    private void TryAutoAssignGuiProAssets()
    {
#if UNITY_EDITOR
        if (uiFont != null && uiPopupSprite != null && uiButtonSprite != null && uiJoystickSprite != null && uiResultRibbonSprite != null && uiStarSprite != null)
        {
            return;
        }

        const string packageMarker = "GUI Pro - Fantasy RPG";
        var packageRoots = AssetDatabase.GetSubFolders("Assets")
            .Where(path => path.Contains(packageMarker))
            .ToArray();
        if (packageRoots.Length == 0)
        {
            return;
        }

        uiFont = uiFont != null ? uiFont : FindAssetInFolders<Font>(packageRoots, "t:Font", "font");
        uiPopupSprite = uiPopupSprite != null ? uiPopupSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "panel", "window", "popup", "frame");
        uiButtonSprite = uiButtonSprite != null ? uiButtonSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "button");
        uiJoystickSprite = uiJoystickSprite != null ? uiJoystickSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "circle", "ring", "pad", "joystick");
        uiTimerIconSprite = uiTimerIconSprite != null ? uiTimerIconSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "clock", "time", "hourglass");
        uiWinIconSprite = uiWinIconSprite != null ? uiWinIconSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "star", "crown", "gem");
        uiRetryIconSprite = uiRetryIconSprite != null ? uiRetryIconSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "refresh", "retry", "arrow");
        uiResultRibbonSprite = uiResultRibbonSprite != null ? uiResultRibbonSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "ribbon", "banner");
        uiStarSprite = uiStarSprite != null ? uiStarSprite : FindAssetInFolders<Sprite>(packageRoots, "t:Sprite", "star");
#endif
    }

#if UNITY_EDITOR
    private static T FindAssetInFolders<T>(string[] folders, string typeQuery, params string[] preferredNameMarkers) where T : Object
    {
        var guids = AssetDatabase.FindAssets(typeQuery, folders);
        var preferred = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => preferredNameMarkers.Any(marker => path.ToLowerInvariant().Contains(marker)))
            .FirstOrDefault();
        if (!string.IsNullOrEmpty(preferred))
        {
            return AssetDatabase.LoadAssetAtPath<T>(preferred);
        }

        var firstPath = guids.Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        return string.IsNullOrEmpty(firstPath) ? null : AssetDatabase.LoadAssetAtPath<T>(firstPath);
    }
#endif

}
