using System.Collections.Generic;
using UnityEngine;

public class SnakeBody : MonoBehaviour
{
    [SerializeField] private float segmentSpacing = 0.45f;

    private readonly List<Transform> segments = new();
    private readonly List<Vector3> movementHistory = new();
    private Transform segmentRoot;

    public IReadOnlyList<Transform> Segments => segments;

    public void Initialize(Transform head, int initialBodySegments, Transform root)
    {
        segmentRoot = root;
        movementHistory.Clear();
        movementHistory.Add(head.position);

        ClearSegments();

        for (var i = 0; i < initialBodySegments; i++)
        {
            var spawnPosition = head.position - (Vector3.right * segmentSpacing * (i + 1));
            CreateSegment(spawnPosition);
        }
    }

    public void TickFollow(Vector3 headPosition)
    {
        if (movementHistory.Count == 0)
        {
            movementHistory.Add(headPosition);
        }

        if (Vector3.Distance(movementHistory[^1], headPosition) > 0.05f)
        {
            movementHistory.Add(headPosition);
        }

        for (var i = 0; i < segments.Count; i++)
        {
            var distanceFromHead = segmentSpacing * (i + 1);
            segments[i].position = GetPointAtDistance(distanceFromHead);
        }

        var maxHistory = Mathf.CeilToInt((segments.Count + 2) * segmentSpacing / 0.05f);
        if (movementHistory.Count > maxHistory)
        {
            movementHistory.RemoveRange(0, movementHistory.Count - maxHistory);
        }
    }

    public void Grow(int additionalSegments)
    {
        if (additionalSegments <= 0)
        {
            return;
        }

        var spawnPosition = segments.Count > 0 ? segments[^1].position : transform.position;
        for (var i = 0; i < additionalSegments; i++)
        {
            CreateSegment(spawnPosition);
        }
    }

    public void ClearSegments()
    {
        for (var i = 0; i < segments.Count; i++)
        {
            if (segments[i] != null)
            {
                Destroy(segments[i].gameObject);
            }
        }

        segments.Clear();
    }

    private Transform CreateSegment(Vector3 position)
    {
        var segment = new GameObject($"BodySegment_{segments.Count + 1}");
        segment.transform.SetParent(segmentRoot, true);
        segment.transform.position = position;

        var renderer = segment.AddComponent<SpriteRenderer>();
        renderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        renderer.color = new Color(0.82f, 0.12f, 0.17f);
        renderer.sortingOrder = 3;
        segment.transform.localScale = new Vector3(0.5f, 0.5f, 1f);

        var collider = segment.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        segments.Add(segment.transform);
        return segment.transform;
    }

    private Vector3 GetPointAtDistance(float distance)
    {
        if (movementHistory.Count < 2)
        {
            return movementHistory[0];
        }

        var accumulated = 0f;

        for (var i = movementHistory.Count - 1; i > 0; i--)
        {
            var a = movementHistory[i];
            var b = movementHistory[i - 1];
            var segmentLength = Vector3.Distance(a, b);

            if (accumulated + segmentLength >= distance)
            {
                var remain = distance - accumulated;
                var t = segmentLength > 0f ? remain / segmentLength : 0f;
                return Vector3.Lerp(a, b, t);
            }

            accumulated += segmentLength;
        }

        return movementHistory[0];
    }
}
