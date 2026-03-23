using UnityEngine;

public static class RuntimeSpriteFactory
{
    private static Sprite whiteSprite;
    private static Sprite circleSprite;
    private static Sprite smileSprite;

    public static Sprite WhiteSprite
    {
        get
        {
            if (whiteSprite == null)
            {
                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                whiteSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            return whiteSprite;
        }
    }

    public static Sprite CircleSprite
    {
        get
        {
            if (circleSprite == null)
            {
                circleSprite = CreateCircleSprite(64, 31.5f);
            }

            return circleSprite;
        }
    }

    public static Sprite SmileSprite
    {
        get
        {
            if (smileSprite == null)
            {
                smileSprite = CreateSmileSprite(64, 32);
            }

            return smileSprite;
        }
    }

    private static Sprite CreateCircleSprite(int size, float radius)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var maxDistance = radius * radius;
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var delta = new Vector2(x, y) - center;
                var inside = delta.sqrMagnitude <= maxDistance;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateSmileSprite(int width, int height)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, Color.clear);
            }
        }

        var centerX = (width - 1) * 0.5f;
        var centerY = height - 2f;
        var radius = Mathf.Min(width * 0.5f - 4f, height);
        var thickness = 2.2f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                var onArc = Mathf.Abs(dist - radius) <= thickness;
                if (!onArc || y > centerY)
                {
                    continue;
                }

                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
    }
}
