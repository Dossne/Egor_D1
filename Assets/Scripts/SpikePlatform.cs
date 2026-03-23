using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SpikePlatform : MonoBehaviour
{
    [SerializeField] private float hiddenDurationSeconds = 5f;
    [SerializeField] private float extendedDurationSeconds = 1f;
    [SerializeField] private float loweredSpikeOffset = -0.16f;
    [SerializeField] private float raisedSpikeOffset = 0.22f;

    private readonly List<Transform> spikeTransforms = new();
    private readonly List<Vector3> spikeLoweredPositions = new();
    private readonly HashSet<SnakeController> snakesInside = new();
    private SpriteRenderer baseRenderer;
    private bool spikesExtended;

    public void Setup(List<Transform> spikes, SpriteRenderer platformRenderer)
    {
        spikeTransforms.Clear();
        spikeTransforms.AddRange(spikes);
        CacheLoweredSpikePositions();
        baseRenderer = platformRenderer;
        ApplySpikeOffset(0f);
        SetSpikesVisible(false);
        StartCoroutine(SpikeCycleLoop());
    }

    private IEnumerator SpikeCycleLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(hiddenDurationSeconds);
            SetSpikesExtended(true);
            yield return new WaitForSeconds(extendedDurationSeconds);
            SetSpikesExtended(false);
        }
    }

    private void SetSpikesExtended(bool value)
    {
        spikesExtended = value;
        SetSpikesVisible(value);
        ApplySpikeOffset(value ? raisedSpikeOffset - loweredSpikeOffset : 0f);

        if (baseRenderer != null)
        {
            baseRenderer.color = value
                ? new Color(0.56f, 0.56f, 0.56f, 1f)
                : new Color(0.44f, 0.44f, 0.44f, 1f);
        }

        if (!value)
        {
            return;
        }

        foreach (var snake in snakesInside)
        {
            if (snake != null)
            {
                snake.Kill(false);
            }
        }
    }

    private void ApplySpikeOffset(float yOffset)
    {
        for (var i = 0; i < spikeTransforms.Count; i++)
        {
            var spike = spikeTransforms[i];
            if (spike == null)
            {
                continue;
            }

            var basePosition = i < spikeLoweredPositions.Count ? spikeLoweredPositions[i] : spike.localPosition;
            spike.localPosition = new Vector3(basePosition.x, basePosition.y + yOffset, basePosition.z);
        }
    }

    private void CacheLoweredSpikePositions()
    {
        spikeLoweredPositions.Clear();
        for (var i = 0; i < spikeTransforms.Count; i++)
        {
            var spike = spikeTransforms[i];
            spikeLoweredPositions.Add(spike == null ? Vector3.zero : spike.localPosition);
        }
    }

    private void SetSpikesVisible(bool isVisible)
    {
        for (var i = 0; i < spikeTransforms.Count; i++)
        {
            if (spikeTransforms[i] != null)
            {
                spikeTransforms[i].gameObject.SetActive(isVisible);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var snake = other.GetComponent<SnakeController>();
        if (snake == null)
        {
            return;
        }

        snakesInside.Add(snake);
        if (spikesExtended)
        {
            snake.Kill(false);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var snake = other.GetComponent<SnakeController>();
        if (snake != null)
        {
            snakesInside.Remove(snake);
        }
    }
}
