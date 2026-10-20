namespace Qbert.Core.Rendering;

/// <summary>
/// How a sprite is transformed when drawn.
/// </summary>
public enum SpriteTransform
{
    None,
    FlipX,

    /// <summary>Rotates 90 degrees clockwise: the top of the sprite points right.</summary>
    RotateRight,

    /// <summary>Rotates 90 degrees counter-clockwise: the top of the sprite points left.</summary>
    RotateLeft
}

/// <summary>
/// Pixel-art image built from text rows; transparent pixels are stored as 0.
/// </summary>
public sealed class Sprite
{
    private readonly uint[] _pixels;

    private Sprite(int width, int height, uint[] pixels)
    {
        Width = width;
        Height = height;
        _pixels = pixels;
    }

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; }

    /// <summary>
    /// Builds a sprite from equal-length text rows.
    /// </summary>
    /// <param name="rows">One string per pixel row; <c>.</c> is transparent.</param>
    /// <param name="palette">Color for every other character used in <paramref name="rows"/>.</param>
    /// <returns>The parsed sprite.</returns>
    /// <exception cref="ArgumentException">Rows differ in length or use a character missing from the palette.</exception>
    public static Sprite Parse(string[] rows, IReadOnlyDictionary<char, uint> palette)
    {
        int width = rows[0].Length;
        var pixels = new uint[width * rows.Length];
        for (int y = 0; y < rows.Length; y++)
        {
            if (rows[y].Length != width)
            {
                throw new ArgumentException($"Row {y} has length {rows[y].Length}, expected {width}", nameof(rows));
            }

            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = ColorOf(rows[y][x], palette);
            }
        }

        return new Sprite(width, rows.Length, pixels);
    }

    /// <summary>
    /// Draws the sprite with its top-left corner at the given position.
    /// </summary>
    /// <param name="target">Destination surface.</param>
    /// <param name="left">Destination column of the top-left corner.</param>
    /// <param name="top">Destination row of the top-left corner.</param>
    /// <param name="transform">Flip or rotation to apply.</param>
    /// <param name="scale">Integer magnification.</param>
    public void Draw(FrameBuffer target, int left, int top, SpriteTransform transform = SpriteTransform.None, int scale = 1)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                uint color = _pixels[y * Width + x];
                if (color == 0)
                {
                    continue;
                }

                var (dx, dy) = Map(x, y, transform);
                target.FillRect(left + dx * scale, top + dy * scale, scale, scale, color);
            }
        }
    }

    private (int X, int Y) Map(int x, int y, SpriteTransform transform) => transform switch
    {
        SpriteTransform.FlipX => (Width - 1 - x, y),
        SpriteTransform.RotateRight => (Height - 1 - y, x),
        SpriteTransform.RotateLeft => (y, Width - 1 - x),
        _ => (x, y)
    };

    private static uint ColorOf(char symbol, IReadOnlyDictionary<char, uint> palette)
    {
        if (symbol == '.')
        {
            return 0;
        }

        return palette.TryGetValue(symbol, out uint color)
            ? color
            : throw new ArgumentException($"Palette has no color for '{symbol}'", nameof(palette));
    }
}
