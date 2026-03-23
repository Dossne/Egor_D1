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
        renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        renderer.color = new Color(0.9f, 0.15f, 0.2f);
        transform.localScale = new Vector3(0.32f, 0.32f, 1f);

        CreateEye("EyeLeft", new Vector3(-0.06f, 0.05f, 0f));
        CreateEye("EyeRight", new Vector3(0.06f, 0.05f, 0f));
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

    private void CreateEye(string eyeName, Vector3 localPosition)
    {
        if (transform.Find(eyeName) != null)
        {
            return;
        }

        var eye = new GameObject(eyeName);
        eye.transform.SetParent(transform, false);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = new Vector3(0.12f, 0.12f, 1f);

        var eyeRenderer = eye.AddComponent<SpriteRenderer>();
        eyeRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        eyeRenderer.color = Color.white;
        eyeRenderer.sortingOrder = 1;

        var pupil = new GameObject("Pupil");
        pupil.transform.SetParent(eye.transform, false);
        pupil.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        pupil.transform.localScale = new Vector3(0.4f, 0.4f, 1f);

        var pupilRenderer = pupil.AddComponent<SpriteRenderer>();
        pupilRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        pupilRenderer.color = Color.black;
        pupilRenderer.sortingOrder = 2;
    }
}
