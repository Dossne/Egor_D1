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
        renderer.sprite = RuntimeSpriteFactory.BerrySprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 5;
        transform.localScale = new Vector3(0.62f, 0.62f, 1f);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var snake = other.GetComponent<SnakeController>();
        if (snake == null)
        {
            return;
        }

        snake.Grow(growthAmount);
        snake.PlayEatAnimation();
        gameManager.SpawnBerryJuiceSplash(transform.position);
        gameManager.HandleBerryCollected(this);
    }

}
