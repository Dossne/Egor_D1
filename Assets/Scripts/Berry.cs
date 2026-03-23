using UnityEngine;

[RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
public class Berry : MonoBehaviour
{
    private GameManager gameManager;
    private int growthAmount;

    public void Setup(GameManager manager, int growBy)
    {
        gameManager = manager;
        growthAmount = growBy;

        var collider = GetComponent<CircleCollider2D>();
        collider.isTrigger = true;

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.CircleSprite;
        renderer.color = new Color(0.9f, 0.15f, 0.2f);
        renderer.sortingOrder = 5;
        transform.localScale = new Vector3(0.52f, 0.52f, 1f);

        CreateDot("EyeLeft", new Vector3(-0.08f, 0.07f, 0f), new Vector3(0.08f, 0.08f, 1f));
        CreateDot("EyeRight", new Vector3(0.08f, 0.07f, 0f), new Vector3(0.08f, 0.08f, 1f));
        CreateSmile();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var snake = other.GetComponent<SnakeController>();
        if (snake == null)
        {
            return;
        }

        snake.Grow(growthAmount);
        gameManager.HandleBerryCollected(this);
    }

    private void CreateDot(string name, Vector3 localPosition, Vector3 scale)
    {
        if (transform.Find(name) != null)
        {
            return;
        }

        var dot = new GameObject(name);
        dot.transform.SetParent(transform, false);
        dot.transform.localPosition = localPosition;
        dot.transform.localScale = scale;

        var dotRenderer = dot.AddComponent<SpriteRenderer>();
        dotRenderer.sprite = RuntimeSpriteFactory.CircleSprite;
        dotRenderer.color = Color.black;
        dotRenderer.sortingOrder = 6;
    }

    private void CreateSmile()
    {
        const string smileName = "Smile";
        if (transform.Find(smileName) != null)
        {
            return;
        }

        var smile = new GameObject(smileName);
        smile.transform.SetParent(transform, false);
        smile.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        smile.transform.localScale = new Vector3(0.45f, 0.25f, 1f);

        var smileRenderer = smile.AddComponent<SpriteRenderer>();
        smileRenderer.sprite = RuntimeSpriteFactory.SmileSprite;
        smileRenderer.color = Color.black;
        smileRenderer.sortingOrder = 6;
    }
}
