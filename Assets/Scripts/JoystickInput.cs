using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 70f;

    private RectTransform touchArea;
    private Canvas canvas;
    private Camera uiCamera;
    private bool pointerActive;

    public Vector2 InputVector { get; private set; }

    public void SetReferences(RectTransform backgroundRect, RectTransform handleRect)
    {
        background = backgroundRect;
        handle = handleRect;
    }

    private void Awake()
    {
        touchArea = GetComponent<RectTransform>();
        CacheCanvas();
        SetBackgroundVisible(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (background == null || handle == null)
        {
            return;
        }

        if (canvas == null)
        {
            CacheCanvas();
        }

        if (touchArea != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(touchArea, eventData.position, uiCamera, out var localPoint))
        {
            background.anchoredPosition = localPoint;
            ClampBackgroundToTouchArea();
        }

        pointerActive = true;
        SetBackgroundVisible(true);
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null || !pointerActive)
        {
            return;
        }

        if (canvas == null)
        {
            CacheCanvas();
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, uiCamera, out var localPoint))
        {
            return;
        }

        var radius = background.sizeDelta.x * 0.5f;
        var localPointMagnitude = localPoint.magnitude;
        if (localPointMagnitude > radius && touchArea != null)
        {
            var overDistance = localPointMagnitude - radius;
            var moveOffset = localPoint.normalized * overDistance;
            background.anchoredPosition += moveOffset;
            ClampBackgroundToTouchArea();

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(background, eventData.position, uiCamera, out localPoint))
            {
                return;
            }
        }

        var normalized = radius > 0f ? localPoint / radius : Vector2.zero;
        InputVector = Vector2.ClampMagnitude(normalized, 1f);
        handle.anchoredPosition = InputVector * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pointerActive = false;
        InputVector = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }

        SetBackgroundVisible(false);
    }

    private void CacheCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }

    private void SetBackgroundVisible(bool isVisible)
    {
        if (background != null)
        {
            background.gameObject.SetActive(isVisible);
        }
    }

    private void ClampBackgroundToTouchArea()
    {
        if (background == null || touchArea == null)
        {
            return;
        }

        var areaRect = touchArea.rect;
        var radius = background.sizeDelta.x * 0.5f;
        var clampedX = Mathf.Clamp(background.anchoredPosition.x, areaRect.xMin + radius, areaRect.xMax - radius);
        var clampedY = Mathf.Clamp(background.anchoredPosition.y, areaRect.yMin + radius, areaRect.yMax - radius);
        background.anchoredPosition = new Vector2(clampedX, clampedY);
    }
}
