using UnityEngine;

public static class RuntimeSpriteFactory
{
    private static Sprite whiteSprite;
    private static Sprite circleSprite;
    private static Sprite smileSprite;
    private static Sprite snakeHeadSprite;
    private static Sprite snakeHeadEatingSprite;
    private static Sprite snakeBodySprite;
    private static Sprite berrySprite;
    private static Sprite wallSprite;
    private static Sprite mineBodySprite;
    private static Sprite tableSprite;
    private static Sprite spikeSprite;

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

    public static Sprite SnakeHeadSprite
    {
        get
        {
            if (snakeHeadSprite == null)
            {
                snakeHeadSprite = CreateSnakeHeadSprite(128, false);
            }

            return snakeHeadSprite;
        }
    }

    public static Sprite SnakeHeadEatingSprite
    {
        get
        {
            if (snakeHeadEatingSprite == null)
            {
                snakeHeadEatingSprite = CreateSnakeHeadSprite(128, true);
            }

            return snakeHeadEatingSprite;
        }
    }

    public static Sprite SnakeBodySprite
    {
        get
        {
            if (snakeBodySprite == null)
            {
                snakeBodySprite = CreateSnakeBodySprite(128);
            }

            return snakeBodySprite;
        }
    }

    public static Sprite BerrySprite
    {
        get
        {
            if (berrySprite == null)
            {
                berrySprite = CreateBerrySprite(128);
            }

            return berrySprite;
        }
    }

    public static Sprite WallSprite
    {
        get
        {
            if (wallSprite == null)
            {
                wallSprite = CreateWallSprite(256, 40);
            }

            return wallSprite;
        }
    }

    public static Sprite MineBodySprite
    {
        get
        {
            if (mineBodySprite == null)
            {
                mineBodySprite = CreateMineBodySprite(128);
            }

            return mineBodySprite;
        }
    }

    public static Sprite TableSprite
    {
        get
        {
            if (tableSprite == null)
            {
                tableSprite = CreateTableSprite(128);
            }

            return tableSprite;
        }
    }

    public static Sprite SpikeSprite
    {
        get
        {
            if (spikeSprite == null)
            {
                spikeSprite = CreateSpikeSprite(64, 88);
            }

            return spikeSprite;
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

    private static Sprite CreateSnakeBodySprite(int size)
    {
        var texture = CreateTransparentTexture(size, size);
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var radius = size * 0.44f;
        var border = size * 0.055f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                var distance = Vector2.Distance(point, center);
                if (distance > radius)
                {
                    continue;
                }

                var color = distance >= radius - border
                    ? new Color(0.55f, 0.07f, 0.1f, 1f)
                    : new Color(0.92f, 0.17f, 0.2f, 1f);

                if (x < center.x - size * 0.08f && y > center.y + size * 0.02f)
                {
                    color = Color.Lerp(color, Color.white, 0.18f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateSnakeHeadSprite(int size, bool mouthOpen)
    {
        var texture = CreateTransparentTexture(size, size);
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var radius = size * 0.45f;
        var border = size * 0.06f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                var distance = Vector2.Distance(point, center);
                if (distance > radius)
                {
                    continue;
                }

                var baseColor = distance >= radius - border
                    ? new Color(0.56f, 0.07f, 0.1f, 1f)
                    : new Color(0.94f, 0.19f, 0.22f, 1f);

                if (x < center.x - size * 0.1f && y > center.y)
                {
                    baseColor = Color.Lerp(baseColor, Color.white, 0.16f);
                }

                texture.SetPixel(x, y, baseColor);
            }
        }

        PaintCircle(texture, new Vector2(size * 0.62f, size * 0.6f), size * 0.05f, Color.black);
        PaintCircle(texture, new Vector2(size * 0.62f, size * 0.43f), size * 0.05f, Color.black);

        if (mouthOpen)
        {
            PaintCircle(texture, new Vector2(size * 0.8f, size * 0.52f), size * 0.12f, new Color(0.3f, 0f, 0f, 1f));
            PaintCircle(texture, new Vector2(size * 0.82f, size * 0.52f), size * 0.085f, new Color(0.85f, 0.2f, 0.25f, 1f));
        }
        else
        {
            PaintCircle(texture, new Vector2(size * 0.79f, size * 0.49f), size * 0.03f, new Color(0.3f, 0f, 0f, 1f));
            PaintCircle(texture, new Vector2(size * 0.81f, size * 0.49f), size * 0.03f, new Color(0.3f, 0f, 0f, 1f));
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateBerrySprite(int size)
    {
        var texture = CreateTransparentTexture(size, size);
        var berryCenter = new Vector2(size * 0.5f, size * 0.42f);
        var berryRadius = size * 0.33f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                var distance = Vector2.Distance(point, berryCenter);
                if (distance > berryRadius)
                {
                    continue;
                }

                var color = distance >= berryRadius - size * 0.05f
                    ? new Color(0.74f, 0.18f, 0.22f, 1f)
                    : new Color(0.92f, 0.38f, 0.42f, 1f);

                if (x < berryCenter.x - size * 0.07f && y > berryCenter.y + size * 0.05f)
                {
                    color = Color.Lerp(color, Color.white, 0.2f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        PaintStem(texture, size);
        PaintLeaf(texture, size);
        PaintCircle(texture, new Vector2(size * 0.4f, size * 0.44f), size * 0.055f, Color.black);
        PaintCircle(texture, new Vector2(size * 0.6f, size * 0.44f), size * 0.055f, Color.black);
        PaintCircle(texture, new Vector2(size * 0.385f, size * 0.46f), size * 0.015f, Color.white);
        PaintCircle(texture, new Vector2(size * 0.585f, size * 0.46f), size * 0.015f, Color.white);
        PaintCircle(texture, new Vector2(size * 0.36f, size * 0.36f), size * 0.05f, new Color(0.92f, 0.18f, 0.22f, 0.7f));
        PaintCircle(texture, new Vector2(size * 0.64f, size * 0.36f), size * 0.05f, new Color(0.92f, 0.18f, 0.22f, 0.7f));
        PaintSmile(texture, size);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.38f), size);
    }

    private static Sprite CreateWallSprite(int width, int height)
    {
        var texture = CreateTransparentTexture(width, height);
        var centerY = (height - 1) * 0.5f;
        var tubeRadius = height * 0.25f;

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var distY = Mathf.Abs(y - centerY);
                if (distY > height * 0.48f)
                {
                    continue;
                }

                var glow = Mathf.Clamp01(1f - distY / (height * 0.48f));
                var color = new Color(1f, 0.1f, 0.12f, glow * 0.35f);

                if (distY <= tubeRadius)
                {
                    var core = 1f - distY / tubeRadius;
                    color = new Color(1f, 0.2f + core * 0.25f, 0.2f + core * 0.2f, 0.85f);
                    if (distY < tubeRadius * 0.35f)
                    {
                        color = new Color(1f, 0.82f, 0.72f, 0.95f);
                    }
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Sprite CreateMineBodySprite(int size)
    {
        var texture = CreateTransparentTexture(size, size);
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        var radius = size * 0.44f;
        var border = size * 0.06f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var point = new Vector2(x, y);
                var distance = Vector2.Distance(point, center);
                if (distance > radius)
                {
                    continue;
                }

                var color = distance >= radius - border
                    ? new Color(0.2f, 0.2f, 0.24f, 1f)
                    : new Color(0.12f, 0.12f, 0.15f, 1f);

                if (x < center.x - size * 0.06f && y > center.y + size * 0.02f)
                {
                    color = Color.Lerp(color, new Color(0.45f, 0.45f, 0.5f, 1f), 0.32f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        PaintCircle(texture, new Vector2(size * 0.5f, size * 0.5f), size * 0.12f, new Color(0.7f, 0.08f, 0.08f, 1f));
        PaintCircle(texture, new Vector2(size * 0.47f, size * 0.54f), size * 0.03f, new Color(1f, 0.55f, 0.4f, 1f));
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateTableSprite(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;

        var baseColorA = new Color32(157, 225, 64, 255);
        var baseColorB = new Color32(140, 214, 59, 255);
        var darkGrass = new Color32(30, 129, 40, 255);
        var lightGrass = new Color32(98, 191, 65, 255);
        var flowerCore = new Color32(250, 235, 75, 255);
        var flowerPetal = new Color32(246, 246, 246, 255);

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var blend = (Mathf.Sin(x * 0.12f) + Mathf.Sin(y * 0.11f)) * 0.25f + 0.5f;
                texture.SetPixel(x, y, Color.Lerp(baseColorA, baseColorB, blend));
            }
        }

        var random = new System.Random(4183);
        for (var i = 0; i < 260; i++)
        {
            var x = random.Next(2, size - 2);
            var y = random.Next(2, size - 2);
            texture.SetPixel(x, y, lightGrass);
            texture.SetPixel(x + 1, y, darkGrass);
            texture.SetPixel(x, y + 1, darkGrass);
        }

        for (var i = 0; i < 90; i++)
        {
            var x = random.Next(3, size - 3);
            var y = random.Next(3, size - 3);
            texture.SetPixel(x, y, flowerCore);
            texture.SetPixel(x + 1, y, flowerPetal);
            texture.SetPixel(x - 1, y, flowerPetal);
            texture.SetPixel(x, y + 1, flowerPetal);
            texture.SetPixel(x, y - 1, flowerPetal);
        }

        texture.wrapMode = TextureWrapMode.Repeat;
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }

    private static Sprite CreateSpikeSprite(int width, int height)
    {
        var texture = CreateTransparentTexture(width, height);
        var apex = new Vector2((width - 1) * 0.5f, height - 1f);
        var baseLeft = new Vector2(width * 0.18f, 0f);
        var baseRight = new Vector2(width * 0.82f, 0f);

        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2(x, y);
                if (!PointInTriangle(point, baseLeft, baseRight, apex))
                {
                    continue;
                }

                var gradient = Mathf.Clamp01((float)y / (height - 1));
                var color = Color.Lerp(new Color(0.58f, 0.5f, 0.38f, 1f), new Color(0.86f, 0.1f, 0.1f, 1f), gradient);
                if (Mathf.Abs(x - width * 0.5f) < width * 0.07f)
                {
                    color = Color.Lerp(color, new Color(0.93f, 0.86f, 0.76f, 1f), 0.35f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), width);
    }

    private static Texture2D CreateTransparentTexture(int width, int height)
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

        return texture;
    }

    private static void PaintCircle(Texture2D texture, Vector2 center, float radius, Color color)
    {
        var minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius));
        var maxX = Mathf.Min(texture.width - 1, Mathf.CeilToInt(center.x + radius));
        var minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius));
        var maxY = Mathf.Min(texture.height - 1, Mathf.CeilToInt(center.y + radius));
        var maxDistance = radius * radius;

        for (var y = minY; y <= maxY; y++)
        {
            for (var x = minX; x <= maxX; x++)
            {
                var delta = new Vector2(x, y) - center;
                if (delta.sqrMagnitude <= maxDistance)
                {
                    texture.SetPixel(x, y, color);
                }
            }
        }
    }

    private static void PaintStem(Texture2D texture, int size)
    {
        for (var i = 0; i < size * 0.28f; i++)
        {
            var x = Mathf.RoundToInt(size * 0.5f + i * 0.22f);
            var y = Mathf.RoundToInt(size * 0.72f + i * 0.3f);
            PaintCircle(texture, new Vector2(x, y), size * 0.018f, new Color(0.62f, 0.43f, 0.3f, 1f));
        }
    }

    private static void PaintLeaf(Texture2D texture, int size)
    {
        var center = new Vector2(size * 0.62f, size * 0.86f);
        var radiusX = size * 0.14f;
        var radiusY = size * 0.1f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var nx = (x - center.x) / radiusX;
                var ny = (y - center.y) / radiusY;
                if (nx * nx + ny * ny > 1f)
                {
                    continue;
                }

                var mix = Mathf.Clamp01((y - (center.y - radiusY)) / (radiusY * 2f));
                var color = Color.Lerp(new Color(0.52f, 0.86f, 0.54f, 1f), new Color(0.7f, 0.93f, 0.7f, 1f), mix);
                texture.SetPixel(x, y, color);
            }
        }
    }

    private static void PaintSmile(Texture2D texture, int size)
    {
        var centerX = size * 0.5f;
        var centerY = size * 0.34f;
        var radius = size * 0.075f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = x - centerX;
                var dy = y - centerY;
                var dist = Mathf.Sqrt(dx * dx + dy * dy);
                var onArc = Mathf.Abs(dist - radius) <= 1.4f;
                if (onArc && y <= centerY)
                {
                    texture.SetPixel(x, y, Color.black);
                }
            }
        }
    }

    private static bool PointInTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c)
    {
        var denominator = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Mathf.Abs(denominator) < 0.0001f)
        {
            return false;
        }

        var alpha = ((b.y - c.y) * (point.x - c.x) + (c.x - b.x) * (point.y - c.y)) / denominator;
        var beta = ((c.y - a.y) * (point.x - c.x) + (a.x - c.x) * (point.y - c.y)) / denominator;
        var gamma = 1f - alpha - beta;
        return alpha >= 0f && beta >= 0f && gamma >= 0f;
    }
}
