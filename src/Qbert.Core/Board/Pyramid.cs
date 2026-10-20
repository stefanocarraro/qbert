namespace Qbert.Core.Board;

/// <summary>
/// How a cube top reacts when Q*bert lands on it.
/// </summary>
public enum ColorRule
{
    /// <summary>One landing turns the cube to the target color.</summary>
    Single,

    /// <summary>Two landings are needed: start, intermediate, target.</summary>
    Double,

    /// <summary>One landing reaches the target; landing again reverts to the start color.</summary>
    Toggle,

    /// <summary>Two landings reach the target; landing again reverts to the intermediate color.</summary>
    DoubleRevert
}

/// <summary>
/// The 28 cubes of the playfield and their color progress.
/// </summary>
public sealed class Pyramid
{
    /// <summary>Number of cube rows.</summary>
    public const int Rows = 7;

    /// <summary>Points awarded for reaching the intermediate color.</summary>
    public const int IntermediatePoints = 15;

    /// <summary>Points awarded for reaching the target color.</summary>
    public const int TargetPoints = 25;

    private readonly int[] _stages = new int[Rows * (Rows + 1) / 2];

    /// <summary>
    /// Initializes a pyramid with every cube at the start color.
    /// </summary>
    /// <param name="rule">Color rule for the current level.</param>
    public Pyramid(ColorRule rule) => Rule = rule;

    /// <summary>Gets the color rule in effect.</summary>
    public ColorRule Rule { get; }

    /// <summary>Gets the stage a cube must reach: 1 for single-step rules, 2 for double-step rules.</summary>
    public int TargetStage => Rule is ColorRule.Double or ColorRule.DoubleRevert ? 2 : 1;

    /// <summary>Gets a value indicating whether every cube shows the target color.</summary>
    public bool IsComplete => _stages.All(stage => stage == TargetStage);

    /// <summary>
    /// Reads the color stage of a cube.
    /// </summary>
    /// <param name="cell">Cube position; must be on the pyramid.</param>
    /// <returns>0 for the start color, up to <see cref="TargetStage"/>.</returns>
    public int StageAt(Cell cell) => _stages[Index(cell)];

    /// <summary>
    /// Applies a Q*bert landing to a cube.
    /// </summary>
    /// <param name="cell">Cube position; must be on the pyramid.</param>
    /// <returns>Points earned by the landing; 0 when the cube did not progress.</returns>
    public int Step(Cell cell)
    {
        int index = Index(cell);
        int before = _stages[index];
        int after = Next(before);
        _stages[index] = after;
        if (after <= before)
        {
            return 0;
        }

        return after == TargetStage ? TargetPoints : IntermediatePoints;
    }

    /// <summary>
    /// Moves a cube one stage back toward the start color, as Slick and Sam do.
    /// </summary>
    /// <param name="cell">Cube position; must be on the pyramid.</param>
    public void Revert(Cell cell)
    {
        int index = Index(cell);
        _stages[index] = Math.Max(0, _stages[index] - 1);
    }

    private int Next(int stage) => Rule switch
    {
        ColorRule.Single => 1,
        ColorRule.Double => Math.Min(2, stage + 1),
        ColorRule.Toggle => stage == 1 ? 0 : 1,
        ColorRule.DoubleRevert => stage == 2 ? 1 : stage + 1,
        _ => throw new InvalidOperationException($"Unknown color rule {Rule}")
    };

    private static int Index(Cell cell)
    {
        if (!cell.IsOnPyramid)
        {
            throw new ArgumentOutOfRangeException(nameof(cell), cell, "Cell is not on the pyramid");
        }

        return cell.Row * (cell.Row + 1) / 2 + cell.Col;
    }
}
