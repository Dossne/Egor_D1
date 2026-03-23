using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Game Rules")]
    [SerializeField] private int startSnakeLength = 5;
    [SerializeField] private int berryCount = 3;
    [SerializeField] private int growthPerBerry = 3;
    [SerializeField] private float snakeSpeed = 2.625f;

    [Header("Arena")]
    [SerializeField] private Vector2 arenaSize = new(8f, 12f);
    [SerializeField] private float wallThickness = 0.4f;
    [SerializeField] private float cameraHeightFactor = 0.4f;
    [SerializeField] private float minimumCameraSize = 4.5f;
    [SerializeField] private Vector3 cameraOffset = new(0f, 0f, -10f);

    private readonly List<Vector2> initialBerryPositions = new();
    private readonly List<Berry> activeBerries = new();

    private SnakeController snakeController;
    private JoystickInput joystickInput;
    private Text winText;
    private Camera mainCamera;
    private bool levelFinished;

    private void Awake()
    {
        Time.timeScale = 1f;
        SetupPortraitOrientation();
        SetupCamera();
        SetupUi();
        BuildArena();
        CreateSnake();
        CreateOrRestoreBerries(useStoredPositions: false);
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
        mainCamera.orthographicSize = Mathf.Max(arenaSize.y * cameraHeightFactor, minimumCameraSize);
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

        var winObject = new GameObject("WinText", typeof(RectTransform), typeof(Text));
        winObject.transform.SetParent(canvasObject.transform, false);
        var winRect = winObject.GetComponent<RectTransform>();
        winRect.anchorMin = new Vector2(0.5f, 0.5f);
        winRect.anchorMax = new Vector2(0.5f, 0.5f);
        winRect.anchoredPosition = new Vector2(0f, 280f);
        winRect.sizeDelta = new Vector2(600f, 180f);

        winText = winObject.GetComponent<Text>();
        winText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        winText.fontSize = 84;
        winText.alignment = TextAnchor.MiddleCenter;
        winText.color = new Color(1f, 0.95f, 0.2f);
        winText.text = string.Empty;
    }

    private void BuildArena()
    {
        CreateArenaBackground();
        var topPosition = new Vector2(0f, arenaSize.y * 0.5f + wallThickness * 0.5f);
        var bottomPosition = new Vector2(0f, -arenaSize.y * 0.5f - wallThickness * 0.5f);
        var leftPosition = new Vector2(-arenaSize.x * 0.5f - wallThickness * 0.5f, 0f);
        var rightPosition = new Vector2(arenaSize.x * 0.5f + wallThickness * 0.5f, 0f);

        CreateWall("TopWall", topPosition, new Vector2(arenaSize.x + wallThickness * 2f, wallThickness));
        CreateWall("BottomWall", bottomPosition, new Vector2(arenaSize.x + wallThickness * 2f, wallThickness));
        CreateWall("LeftWall", leftPosition, new Vector2(wallThickness, arenaSize.y));
        CreateWall("RightWall", rightPosition, new Vector2(wallThickness, arenaSize.y));

        CreateLaserCorner("TopLeftCorner", new Vector2(leftPosition.x, topPosition.y));
        CreateLaserCorner("TopRightCorner", new Vector2(rightPosition.x, topPosition.y));
        CreateLaserCorner("BottomLeftCorner", new Vector2(leftPosition.x, bottomPosition.y));
        CreateLaserCorner("BottomRightCorner", new Vector2(rightPosition.x, bottomPosition.y));
    }

    private void CreateSnake()
    {
        var snakeObject = new GameObject("SnakeHead", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Rigidbody2D), typeof(SnakeBody), typeof(SnakeController));
        snakeObject.transform.position = Vector3.zero;

        snakeController = snakeObject.GetComponent<SnakeController>();
        snakeController.Setup(this, joystickInput, startSnakeLength, snakeSpeed);
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
            winText.text = "you win";
            Time.timeScale = 0f;
        }
    }

    public void HandleLose()
    {
        if (levelFinished)
        {
            return;
        }

        RestartLevel();
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        levelFinished = false;
        winText.text = string.Empty;

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

        CreateSnake();
        CreateOrRestoreBerries(useStoredPositions: true);
    }

    private void CreateOrRestoreBerries(bool useStoredPositions)
    {
        if (!useStoredPositions)
        {
            initialBerryPositions.Clear();
        }

        for (var i = 0; i < berryCount; i++)
        {
            Vector2 spawnPosition;
            if (useStoredPositions && i < initialBerryPositions.Count)
            {
                spawnPosition = initialBerryPositions[i];
            }
            else
            {
                spawnPosition = GetBerrySpawnPosition();
                initialBerryPositions.Add(spawnPosition);
            }

            var berryObject = new GameObject($"Berry_{i + 1}", typeof(SpriteRenderer), typeof(CircleCollider2D), typeof(Berry));
            berryObject.transform.position = spawnPosition;

            var berry = berryObject.GetComponent<Berry>();
            berry.Setup(this, growthPerBerry);
            activeBerries.Add(berry);
        }
    }

    private Vector2 GetBerrySpawnPosition()
    {
        const int maxAttempts = 50;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var margin = 1f;
            var x = Random.Range(-arenaSize.x * 0.5f + margin, arenaSize.x * 0.5f - margin);
            var y = Random.Range(-arenaSize.y * 0.5f + margin, arenaSize.y * 0.5f - margin);
            var position = new Vector2(x, y);

            var overlapFound = false;
            for (var i = 0; i < initialBerryPositions.Count; i++)
            {
                if (Vector2.Distance(initialBerryPositions[i], position) < 1.2f)
                {
                    overlapFound = true;
                    break;
                }
            }

            if (!overlapFound)
            {
                return position;
            }
        }

        return Vector2.zero;
    }

    private static void CreateWall(string name, Vector2 position, Vector2 size)
    {
        var wall = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(WallCollision));
        wall.transform.position = position;
        wall.transform.localScale = new Vector3(size.x, size.y, 1f);

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

    private static void CreateLaserCorner(string name, Vector2 position)
    {
        var corner = new GameObject(name, typeof(SpriteRenderer));
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
        background.transform.position = Vector3.zero;
        background.transform.localScale = new Vector3(arenaSize.x, arenaSize.y, 1f);

        var renderer = background.GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        renderer.color = new Color(0.11f, 0.16f, 0.13f, 1f);
        renderer.sortingOrder = -1;
    }

}
