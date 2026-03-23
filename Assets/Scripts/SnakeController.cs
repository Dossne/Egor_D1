using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(SnakeBody))]
public class SnakeController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeedDegreesPerSecond = 180f;

    private Rigidbody2D rb;
    private SnakeBody snakeBody;
    private JoystickInput joystickInput;
    private GameManager gameManager;
    private Vector2 currentDirection = Vector2.right;
    private Vector2 previousPosition;
    private float movedDistance;
    private bool selfCollisionEnabled;
    private bool isAlive;

    public void Setup(GameManager manager, JoystickInput joystick, int startLength, float speed)
    {
        gameManager = manager;
        joystickInput = joystick;
        moveSpeed = speed;

        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = GetComponent<CircleCollider2D>();
        collider.isTrigger = true;

        var renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        renderer.color = new Color(0.92f, 0.1f, 0.15f);
        renderer.sortingOrder = 4;
        transform.localScale = new Vector3(0.62f, 0.62f, 1f);

        snakeBody = GetComponent<SnakeBody>();
        snakeBody.Initialize(transform, Mathf.Max(0, startLength - 1), transform.parent);

        currentDirection = Vector2.right;
        previousPosition = rb.position;
        movedDistance = 0f;
        selfCollisionEnabled = false;
        isAlive = true;
    }

    private void FixedUpdate()
    {
        if (!isAlive)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        var input = joystickInput != null ? joystickInput.InputVector : Vector2.zero;
        if (input.sqrMagnitude > 0.01f)
        {
            var targetDirection = input.normalized;
            var maxRadiansDelta = turnSpeedDegreesPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime;
            currentDirection = Vector2.RotateTowards(currentDirection, targetDirection, maxRadiansDelta, 0f).normalized;
        }

        rb.velocity = currentDirection * moveSpeed;
        movedDistance += Vector2.Distance(previousPosition, rb.position);
        previousPosition = rb.position;

        if (!selfCollisionEnabled && movedDistance >= snakeBody.SegmentSpacing * 2f)
        {
            selfCollisionEnabled = true;
        }

        if (currentDirection.sqrMagnitude > 0.001f)
        {
            var angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        snakeBody.TickFollow(transform.position);
    }

    public void Grow(int amount)
    {
        snakeBody.Grow(amount);
    }

    public void Kill()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        gameManager.HandleLose();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAlive)
        {
            return;
        }

        if (other.GetComponent<WallCollision>() != null)
        {
            Kill();
            return;
        }

        if (selfCollisionEnabled && snakeBody != null)
        {
            var segments = snakeBody.Segments;
            for (var i = 1; i < segments.Count; i++)
            {
                if (other.transform == segments[i])
                {
                    Kill();
                    return;
                }
            }
        }
    }
}
