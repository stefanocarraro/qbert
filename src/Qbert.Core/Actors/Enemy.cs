using Qbert.Core.Board;

namespace Qbert.Core.Actors;

/// <summary>
/// Creatures that share the pyramid with Q*bert.
/// </summary>
public enum EnemyKind
{
    /// <summary>Bounces down; deadly on contact.</summary>
    RedBall,

    /// <summary>Bounces down and hatches into Coily on the bottom row.</summary>
    PurpleBall,

    /// <summary>Snake that chases Q*bert.</summary>
    Coily,

    /// <summary>Bounces down; catching it freezes every enemy.</summary>
    GreenBall,

    /// <summary>Bounces down reverting cubes; can be caught.</summary>
    Slick,

    /// <summary>Bounces down reverting cubes; can be caught.</summary>
    Sam,

    /// <summary>Walks the right-hand cube faces from the bottom-right corner; deadly.</summary>
    Ugg,

    /// <summary>Walks the left-hand cube faces from the bottom-left corner; deadly.</summary>
    Wrongway
}

/// <summary>
/// One enemy instance on the pyramid.
/// </summary>
/// <param name="kind">Initial kind; a purple ball later turns into Coily.</param>
public sealed class Enemy(EnemyKind kind) : Actor
{
    /// <summary>Gets the current kind.</summary>
    public EnemyKind Kind { get; private set; } = kind;

    /// <summary>Gets or sets the frames to wait before the next jump.</summary>
    public int Wait { get; set; }

    /// <summary>Gets a value indicating whether touching this enemy kills Q*bert.</summary>
    public bool IsDeadly => Kind is not (EnemyKind.GreenBall or EnemyKind.Slick or EnemyKind.Sam);

    /// <summary>Gets a value indicating whether the enemy moves along cube side faces instead of tops.</summary>
    public bool IsSideWalker => Kind is EnemyKind.Ugg or EnemyKind.Wrongway;

    /// <inheritdoc />
    /// <remarks>
    /// Side walkers keep the lattice cell Q*bert would collide with, but are drawn on the side face of the
    /// cube above it: the left face for Wrongway, the right face for Ugg.
    /// </remarks>
    protected override (float X, float Y) DisplayOffset => Kind switch
    {
        EnemyKind.Wrongway => (Screen.HalfCube / 2, -Screen.RowHeight / 2),
        EnemyKind.Ugg => (-Screen.HalfCube / 2, -Screen.RowHeight / 2),
        _ => (0, 0)
    };

    /// <inheritdoc />
    protected override float JumpArc => IsSideWalker ? 0 : base.JumpArc;

    /// <summary>
    /// Checks whether the enemy can stand on a cell.
    /// </summary>
    /// <param name="cell">Candidate cell.</param>
    /// <returns><c>true</c> when the cell is solid ground for this kind.</returns>
    public bool CanStandOn(Cell cell) => Kind switch
    {
        EnemyKind.Wrongway => new Cell(cell.Row - 1, cell.Col).IsOnPyramid,
        EnemyKind.Ugg => new Cell(cell.Row - 1, cell.Col - 1).IsOnPyramid,
        _ => cell.IsOnPyramid
    };

    /// <summary>
    /// Turns a purple ball into Coily.
    /// </summary>
    /// <exception cref="InvalidOperationException">The enemy is not a purple ball.</exception>
    public void Hatch()
    {
        if (Kind != EnemyKind.PurpleBall)
        {
            throw new InvalidOperationException($"Only a purple ball can hatch, not {Kind}");
        }

        Kind = EnemyKind.Coily;
    }
}
