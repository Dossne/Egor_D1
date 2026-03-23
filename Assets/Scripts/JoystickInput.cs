using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class JoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform handle;
    [SerializeField] private float handleRange = 70f;

    private Canvas canvas;
    private Camera uiCamera;

    public Vector2 InputVector { get; private set; }

    public void SetReferences(RectTransform backgroundRect, RectTransform handleRect)
    {
        background = backgroundRect;
        handle = handleRect;
    }

    private void Awake()
    {
        CacheCanvas();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (background == null || handle == null)
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
        var normalized = localPoint / radius;
        InputVector = Vector2.ClampMagnitude(normalized, 1f);
        handle.anchoredPosition = InputVector * handleRange;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        InputVector = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
    }

    private void CacheCanvas()
    {
        canvas = GetComponentInParent<Canvas>();
        uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
    }
}
