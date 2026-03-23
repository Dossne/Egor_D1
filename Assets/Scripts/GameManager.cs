using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    private class LevelConfig
    {
        public List<Vector2> berryPositions = new();
    }

    [Header("Game Rules")]
    [SerializeField] private int startSnakeLength = 5;
    [SerializeField] private int growthPerBerry = 3;
    [FormerlySerializedAs("snakeSpeed")]
    [SerializeField] private float snakeSpeed = 2.625f;
    [SerializeField] private float snakeSpeedFactor = 0.75f;
    [SerializeField] private List<LevelConfig> levels = new();

    [Header("Arena")]
    [SerializeField] private Vector2 baseArenaSize = new(8f, 12f);
    [SerializeField] private Vector2 nextLevelSizeMultiplierRange = new(1.5f, 2f);
    [SerializeField] private float wallThickness = 0.4f;
    [SerializeField] private float cameraHeightFactor = 0.4f;
    [SerializeField] private float minimumCameraSize = 4.5f;
    [SerializeField] private float cameraZoomOutFactor = 1.2f;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0f, -10f);

    private readonly List<Berry> activeBerries = new();
    private readonly List<GameObject> activeJuiceDroplets = new();

    private SnakeController snakeController;
    private JoystickInput joystickInput;
    private Text levelText;
    private GameObject levelCompleteMenu;
    private Text levelCompleteText;
    private Camera mainCamera;
    private Transform arenaRoot;
    private readonly List<float> levelArenaScales = new();
    private Vector2 currentArenaSize;
    private int currentLevelIndex;
    private bool levelFinished;

    private void Awake()
    {
        Time.timeScale = 1f;
        SetupPortraitOrientation();
        SetupCamera();
        SetupUi();
        EnsureLevelsConfigured();
        BuildArena();
        CreateSnake();
        SpawnCurrentLevelBerries();
        UpdateLevelText();
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
        mainCamera.orthographicSize = Mathf.Max(baseArenaSize.y * cameraHeightFactor, minimumCameraSize) * cameraZoomOutFactor;
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
        joystickRect.sizeDelta = new Vector2(220f, 220f);

        var joystickBg = joystickRoot.GetComponent<Image>();
        joystickBg.color = new Color(1f, 1f, 1f, 0.18f);
        joystickBg.sprite = RuntimeSpriteFactory.CircleSprite;
        joystickBg.type = Image.Type.Simple;
        joystickBg.preserveAspect = true;
        joystickBg.raycastTarget = false;

        var handleObject = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleObject.transform.SetParent(joystickRoot.transform, false);
        var handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(90f, 90f);
        var handleImage = handleObject.GetComponent<Image>();
        handleImage.sprite = RuntimeSpriteFactory.CircleSprite;
        handleImage.color = new Color(1f, 1f, 1f, 0.55f);
        handleImage.type = Image.Type.Simple;
        handleImage.preserveAspect = true;
        handleImage.raycastTarget = false;

        joystickInput = joystickArea.GetComponent<JoystickInput>();
        joystickInput.SetReferences(joystickRect, handleRect);

        levelText = CreateText(canvasObject.transform, "LevelText", new Vector2(20f, -20f), new Vector2(420f, 96f), TextAnchor.UpperLeft, 48);
        levelText.color = new Color(1f, 0.95f, 0.2f);

        levelCompleteMenu = new GameObject("LevelCompleteMenu", typeof(RectTransform), typeof(Image));
        levelCompleteMenu.transform.SetParent(canvasObject.transform, false);
        var menuRect = levelCompleteMenu.GetComponent<RectTransform>();
        menuRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuRect.pivot = new Vector2(0.5f, 0.5f);
        menuRect.sizeDelta = new Vector2(700f, 420f);
        var menuBackground = levelCompleteMenu.GetComponent<Image>();
        menuBackground.color = new Color(0.05f, 0.08f, 0.11f, 0.92f);

        levelCompleteText = CreateText(levelCompleteMenu.transform, "LevelCompleteText", new Vector2(0f, 130f), new Vector2(640f, 120f), TextAnchor.MiddleCenter, 72);
        var levelCompleteRect = levelCompleteText.GetComponent<RectTransform>();
        levelCompleteRect.anchorMin = new Vector2(0.5f, 0.5f);
        levelCompleteRect.anchorMax = new Vector2(0.5f, 0.5f);
        levelCompleteRect.pivot = new Vector2(0.5f, 0.5f);
        levelCompleteRect.anchoredPosition = new Vector2(0f, 130f);
        levelCompleteText.color = new Color(1f, 0.95f, 0.2f);
        levelCompleteText.text = "level complete";

        CreateMenuButton(levelCompleteMenu.transform, "NextLevelButton", "next level", new Vector2(0f, 10f), HandleNextLevelPressed);
        CreateMenuButton(levelCompleteMenu.transform, "RetryButton", "retry", new Vector2(0f, -120f), HandleRetryPressed);
        levelCompleteMenu.SetActive(false);
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
        ApplyCameraSizeForCurrentArena();
    }

    private void CreateSnake()
    {
        var snakeObject = new GameObject("SnakeHead", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SnakeBody), typeof(SnakeController));
        snakeObject.transform.position = Vector3.zero;

        snakeController = snakeObject.GetComponent<SnakeController>();
        snakeController.Setup(this, joystickInput, startSnakeLength, snakeSpeed * snakeSpeedFactor);
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
            levelCompleteMenu.SetActive(true);
            levelCompleteText.text = $"level {currentLevelIndex + 1} complete";
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

    public void HandleLose()
    {
        if (levelFinished)
        {
            return;
        }

        RestartCurrentLevel(clearSplashes: true);
    }

    private void RestartCurrentLevel(bool clearSplashes)
    {
        Time.timeScale = 1f;
        levelFinished = false;
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
        if (clearSplashes)
        {
            ClearJuiceDroplets();
        }

        CreateSnake();
        BuildArena();
        SpawnCurrentLevelBerries();
        UpdateLevelText();
    }

    private void SpawnCurrentLevelBerries()
    {
        if (levels.Count == 0)
        {
            return;
        }

        var level = levels[Mathf.Clamp(currentLevelIndex, 0, levels.Count - 1)];
        var levelScale = GetArenaScaleForLevel(currentLevelIndex);
        for (var i = 0; i < level.berryPositions.Count; i++)
        {
            var berryObject = new GameObject($"Berry_{i + 1}", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Berry));
            berryObject.transform.position = level.berryPositions[i] * levelScale;

            var berry = berryObject.GetComponent<Berry>();
            berry.Setup(this, growthPerBerry);
            activeBerries.Add(berry);
        }
    }

    private void EnsureLevelsConfigured()
    {
        if (levels.Count > 0)
        {
            EnsureLevelArenaScales();
            return;
        }

        levels = new List<LevelConfig>
        {
            new() { berryPositions = new List<Vector2> { new(-2.6f, -2.2f), new(0.3f, 1.6f), new(2.3f, 3.1f) } },
            new() { berryPositions = new List<Vector2> { new(-2.2f, 2.8f), new(2.2f, 2.2f), new(0f, -1.9f), new(1.5f, -3.4f) } },
            new() { berryPositions = new List<Vector2> { new(-2.7f, 0.5f), new(-0.2f, 3f), new(2.5f, 0.8f), new(-1.4f, -2.9f), new(1.6f, -3.2f) } }
        };

        EnsureLevelArenaScales();
    }

    private void HandleNextLevelPressed()
    {
        if (levels.Count == 0)
        {
            return;
        }

        currentLevelIndex = (currentLevelIndex + 1) % levels.Count;
        RestartCurrentLevel(clearSplashes: true);
    }

    private void HandleRetryPressed()
    {
        RestartCurrentLevel(clearSplashes: false);
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
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var easeOut = 1f - (1f - t) * (1f - t);
            dropletTransform.position = Vector3.Lerp(startPosition, targetPosition, easeOut);
            yield return null;
        }

        if (droplet != null)
        {
            dropletTransform.position = targetPosition;
        }
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
        background.transform.localScale = new Vector3(currentArenaSize.x, currentArenaSize.y, 1f);

        var renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        renderer.color = new Color(0.11f, 0.16f, 0.13f, 1f);
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

        mainCamera.orthographicSize = Mathf.Max(currentArenaSize.y * cameraHeightFactor, minimumCameraSize) * cameraZoomOutFactor;
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
        levelArenaScales.Add(1f);

        var minScale = Mathf.Min(nextLevelSizeMultiplierRange.x, nextLevelSizeMultiplierRange.y);
        var maxScale = Mathf.Max(nextLevelSizeMultiplierRange.x, nextLevelSizeMultiplierRange.y);
        for (var i = 1; i < levels.Count; i++)
        {
            levelArenaScales.Add(Random.Range(minScale, maxScale));
        }
    }

    private static Text CreateText(Transform parent, string objectName, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, int fontSize)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.text = string.Empty;
        return text;
    }

    private static void CreateMenuButton(Transform parent, string name, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction callback)
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
        buttonImage.color = new Color(0.2f, 0.45f, 0.24f, 0.95f);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(callback);

        var text = CreateText(buttonObject.transform, "Label", new Vector2(0f, 0f), new Vector2(420f, 92f), TextAnchor.MiddleCenter, 44);
        var textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        text.color = Color.white;
        text.text = label;
    }

}
