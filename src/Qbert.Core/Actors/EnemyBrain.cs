using Qbert.Core.Board;

namespace Qbert.Core.Actors;

/// <summary>
/// Picks the next jump for each enemy kind.
/// </summary>
public static class EnemyBrain
{
    private static readonly Direction[] _bounce = [Direction.DownLeft, Direction.DownRight];
    private static readonly Direction[] _wrongway = [Direction.Right, Direction.UpRight];
    private static readonly Direction[] _ugg = [Direction.Left, Direction.UpLeft];
    private static readonly Direction[] _diagonals = [Direction.UpLeft, Direction.UpRight, Direction.DownLeft, Direction.DownRight];

    /// <summary>
    /// Chooses the next jump.
    /// </summary>
    /// <param name="enemy">Enemy about to jump.</param>
    /// <param name="target">Cell Coily chases: Q*bert, or the disk he escaped on.</param>
    /// <param name="random">Randomness source for bouncing kinds.</param>
    /// <returns>The jump direction, or <c>null</c> when Coily has no useful move.</returns>
    public static Direction? ChooseMove(Enemy enemy, Cell target, Random random) => enemy.Kind switch
    {
        EnemyKind.Coily => Chase(enemy, target),
        EnemyKind.Wrongway => Pick(_wrongway, random),
        EnemyKind.Ugg => Pick(_ugg, random),
        _ => Pick(_bounce, random)
    };

    /// <summary>
    /// Moves Coily one step closer to the target, by squared screen distance.
    /// </summary>
    /// <remarks>
    /// Coily only steps on cubes, except onto the target itself: that is how a disk lures him off the edge.
    /// </remarks>
    /// <param name="coily">The chasing snake.</param>
    /// <param name="target">Cell to approach.</param>
    /// <returns>The best direction, or <c>null</c> when no move reduces the distance.</returns>
    private static Direction? Chase(Enemy coily, Cell target)
    {
        Direction? best = null;
        float bestDistance = Distance(coily.Cell, target);
        foreach (var direction in _diagonals)
        {
            var next = coily.Cell.Step(direction);
            if (next != target && !next.IsOnPyramid)
            {
                continue;
            }

            float distance = Distance(next, target);
            if (distance < bestDistance)
            {
                best = direction;
                bestDistance = distance;
            }
        }

        return best;
    }

    private static float Distance(Cell a, Cell b)
    {
        var (ax, ay) = Screen.Top(a);
        var (bx, by) = Screen.Top(b);
        float dx = ax - bx;
        float dy = ay - by;
        return dx * dx + dy * dy;
    }

    private static Direction Pick(Direction[] options, Random random) => options[random.Next(options.Length)];
}
