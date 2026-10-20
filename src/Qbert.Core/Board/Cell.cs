namespace Qbert.Core.Board;

/// <summary>
/// Movement directions on the isometric pyramid.
/// </summary>
/// <remarks>
/// The four diagonals are the moves available to Q*bert and to the enemies that bounce on the cube tops.
/// <see cref="Left"/> and <see cref="Right"/> are used only by the side walkers (Ugg and Wrongway).
/// </remarks>
public enum Direction
{
    UpRight,
    UpLeft,
    DownRight,
    DownLeft,
    Left,
    Right
}

/// <summary>
/// Position on the pyramid lattice: row 0 is the apex, row <c>n</c> holds <c>n + 1</c> cubes.
/// </summary>
/// <remarks>Cells outside the pyramid are valid values: they represent disks, falls and side-walker lanes.</remarks>
/// <param name="Row">Row index, 0 at the apex.</param>
/// <param name="Col">Column index inside the row, 0 at the left edge.</param>
public readonly record struct Cell(int Row, int Col)
{
    /// <summary>Gets a value indicating whether a cube exists at this position.</summary>
    public bool IsOnPyramid => Row >= 0 && Row < Pyramid.Rows && Col >= 0 && Col <= Row;

    /// <summary>
    /// Computes the neighbour reached by moving one step.
    /// </summary>
    /// <param name="direction">Move to apply.</param>
    /// <returns>The adjacent cell, which may lie outside the pyramid.</returns>
    public Cell Step(Direction direction) => direction switch
    {
        Direction.UpRight => new(Row - 1, Col),
        Direction.UpLeft => new(Row - 1, Col - 1),
        Direction.DownRight => new(Row + 1, Col + 1),
        Direction.DownLeft => new(Row + 1, Col),
        Direction.Left => new(Row, Col - 1),
        Direction.Right => new(Row, Col + 1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null)
    };
}
