using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(SpriteRenderer))]
[RequireComponent(typeof(SnakeBody))]
public class SnakeController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float turnSpeedDegreesPerSecond = 180f;
    [SerializeField] private float eatAnimationDuration = 0.14f;

    private Rigidbody2D rb;
    private SpriteRenderer headRenderer;
    private SnakeBody snakeBody;
    private JoystickInput joystickInput;
    private GameManager gameManager;
    private Vector2 currentDirection = Vector2.right;
    private Vector2 previousPosition;
    private float movedDistance;
    private float eatAnimationTimer;
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

        headRenderer = GetComponent<SpriteRenderer>();
        headRenderer.sprite = RuntimeSpriteFactory.SnakeHeadSprite;
        headRenderer.color = Color.white;
        headRenderer.sortingOrder = 4;
        transform.localScale = new Vector3(0.68f, 0.68f, 1f);

        snakeBody = GetComponent<SnakeBody>();
        snakeBody.Initialize(transform, Mathf.Max(0, startLength - 1), transform.parent);

        currentDirection = Vector2.right;
        previousPosition = rb.position;
        movedDistance = 0f;
        eatAnimationTimer = 0f;
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
            var maxDegreesDelta = turnSpeedDegreesPerSecond * Time.fixedDeltaTime;
            var currentAngle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
            var targetAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
            var nextAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, maxDegreesDelta);
            var nextAngleRadians = nextAngle * Mathf.Deg2Rad;
            currentDirection = new Vector2(Mathf.Cos(nextAngleRadians), Mathf.Sin(nextAngleRadians)).normalized;
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

        if (eatAnimationTimer > 0f)
        {
            eatAnimationTimer -= Time.fixedDeltaTime;
            if (eatAnimationTimer <= 0f)
            {
                ApplyHeadSprite(false);
            }
        }
    }

    public void Grow(int amount)
    {
        snakeBody.Grow(amount);
    }

    public void PlayEatAnimation()
    {
        eatAnimationTimer = eatAnimationDuration;
        ApplyHeadSprite(true);
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

    private void ApplyHeadSprite(bool isEating)
    {
        if (headRenderer == null)
        {
            return;
        }

        headRenderer.sprite = isEating ? RuntimeSpriteFactory.SnakeHeadEatingSprite : RuntimeSpriteFactory.SnakeHeadSprite;
    }
}
