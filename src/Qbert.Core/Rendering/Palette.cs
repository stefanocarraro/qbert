namespace Qbert.Core.Rendering;

/// <summary>
/// Cube colors for one round.
/// </summary>
/// <param name="Start">Top color before any landing.</param>
/// <param name="Middle">Top color after the first of two required landings.</param>
/// <param name="Target">Top color the round asks for.</param>
/// <param name="Left">Left side face.</param>
/// <param name="Right">Right side face.</param>
public readonly record struct ColorScheme(uint Start, uint Middle, uint Target, uint Left, uint Right);

/// <summary>
/// Fixed colors of the game.
/// </summary>
public static class Palette
{
    /// <summary>Screen background.</summary>
    public static readonly uint Background = FrameBuffer.Rgb(0, 0, 0);

    /// <summary>Q*bert orange.</summary>
    public static readonly uint Orange = FrameBuffer.Rgb(255, 128, 0);

    /// <summary>Shaded orange.</summary>
    public static readonly uint DarkOrange = FrameBuffer.Rgb(190, 80, 0);

    /// <summary>Text and eyes.</summary>
    public static readonly uint White = FrameBuffer.Rgb(240, 240, 240);

    /// <summary>Pupils and outlines; slightly lifted so it stays distinct from the background.</summary>
    public static readonly uint Black = FrameBuffer.Rgb(16, 16, 16);

    /// <summary>Red ball and mouths.</summary>
    public static readonly uint Red = FrameBuffer.Rgb(230, 40, 40);

    /// <summary>Shaded red.</summary>
    public static readonly uint DarkRed = FrameBuffer.Rgb(140, 20, 20);

    /// <summary>Coily, purple ball, Ugg and Wrongway.</summary>
    public static readonly uint Purple = FrameBuffer.Rgb(170, 70, 220);

    /// <summary>Shaded purple.</summary>
    public static readonly uint DarkPurple = FrameBuffer.Rgb(100, 30, 140);

    /// <summary>Green ball, Slick and Sam.</summary>
    public static readonly uint Green = FrameBuffer.Rgb(60, 210, 60);

    /// <summary>Shaded green.</summary>
    public static readonly uint DarkGreen = FrameBuffer.Rgb(20, 120, 20);

    /// <summary>Highlights.</summary>
    public static readonly uint Yellow = FrameBuffer.Rgb(240, 220, 50);

    /// <summary>Spinning disk colors, cycled every few frames.</summary>
    public static readonly uint[] Rainbow =
    [
        FrameBuffer.Rgb(230, 40, 40),
        FrameBuffer.Rgb(240, 150, 30),
        FrameBuffer.Rgb(240, 230, 50),
        FrameBuffer.Rgb(60, 210, 60),
        FrameBuffer.Rgb(50, 120, 240),
        FrameBuffer.Rgb(170, 70, 220)
    ];

    private static readonly ColorScheme[] _schemes =
    [
        new(FrameBuffer.Rgb(40, 80, 200), FrameBuffer.Rgb(80, 200, 80), FrameBuffer.Rgb(230, 220, 60), FrameBuffer.Rgb(190, 160, 110), FrameBuffer.Rgb(110, 90, 60)),
        new(FrameBuffer.Rgb(200, 60, 60), FrameBuffer.Rgb(220, 140, 40), FrameBuffer.Rgb(60, 180, 230), FrameBuffer.Rgb(90, 120, 160), FrameBuffer.Rgb(50, 70, 100)),
        new(FrameBuffer.Rgb(230, 220, 60), FrameBuffer.Rgb(200, 80, 200), FrameBuffer.Rgb(60, 200, 90), FrameBuffer.Rgb(150, 150, 150), FrameBuffer.Rgb(90, 90, 90)),
        new(FrameBuffer.Rgb(60, 180, 180), FrameBuffer.Rgb(230, 120, 180), FrameBuffer.Rgb(230, 230, 230), FrameBuffer.Rgb(160, 90, 60), FrameBuffer.Rgb(100, 50, 30)),
        new(FrameBuffer.Rgb(120, 60, 200), FrameBuffer.Rgb(60, 200, 200), FrameBuffer.Rgb(230, 140, 40), FrameBuffer.Rgb(80, 140, 80), FrameBuffer.Rgb(40, 90, 40)),
        new(FrameBuffer.Rgb(230, 230, 230), FrameBuffer.Rgb(230, 80, 80), FrameBuffer.Rgb(40, 90, 230), FrameBuffer.Rgb(170, 140, 60), FrameBuffer.Rgb(110, 90, 30))
    ];

    /// <summary>
    /// Picks the cube colors for a round.
    /// </summary>
    /// <param name="roundIndex">0-based round index across the whole game.</param>
    /// <returns>The scheme, cycling through the available ones.</returns>
    public static ColorScheme Scheme(int roundIndex) => _schemes[roundIndex % _schemes.Length];
}
