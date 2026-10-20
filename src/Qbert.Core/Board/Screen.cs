namespace Qbert.Core.Board;

/// <summary>
/// Maps lattice cells to screen pixels on the 256x240 framebuffer.
/// </summary>
public static class Screen
{
    /// <summary>Framebuffer width in pixels.</summary>
    public const int Width = 256;

    /// <summary>Framebuffer height in pixels.</summary>
    public const int Height = 240;

    /// <summary>Half the width of a cube top face.</summary>
    public const int HalfCube = 16;

    /// <summary>Vertical distance between two cube rows.</summary>
    public const int RowHeight = 24;

    /// <summary>Horizontal center of the apex cube.</summary>
    public const int ApexX = 128;

    /// <summary>Vertical center of the apex cube top face.</summary>
    public const int ApexY = 56;

    /// <summary>
    /// Computes the center of a cube top face; works for cells outside the pyramid too.
    /// </summary>
    /// <param name="cell">Lattice position.</param>
    /// <returns>Screen coordinates of the top face center.</returns>
    public static (float X, float Y) Top(Cell cell) =>
        (ApexX + (2 * cell.Col - cell.Row) * HalfCube, ApexY + cell.Row * RowHeight);
}
