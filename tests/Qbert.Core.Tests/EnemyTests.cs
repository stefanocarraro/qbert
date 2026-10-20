using Qbert.Core.Actors;
using Qbert.Core.Board;

namespace Qbert.Core.Tests;

public class EnemyTests
{
    [Theory]
    [InlineData(EnemyKind.Wrongway, 7, 0, true)]
    [InlineData(EnemyKind.Wrongway, 7, 7, false)]
    [InlineData(EnemyKind.Ugg, 7, 7, true)]
    [InlineData(EnemyKind.Ugg, 7, 0, false)]
    [InlineData(EnemyKind.RedBall, 7, 0, false)]
    [InlineData(EnemyKind.RedBall, 6, 0, true)]
    public void CanStandOn_DependsOnKind(EnemyKind kind, int row, int col, bool expected) =>
        Assert.Equal(expected, new Enemy(kind).CanStandOn(new Cell(row, col)));

    [Fact]
    public void Coily_ClimbsTowardTarget()
    {
        var coily = Coily(new Cell(6, 0));

        var move = EnemyBrain.ChooseMove(coily, new Cell(0, 0), new Random(1));

        Assert.Equal(Direction.UpRight, move);
    }

    [Fact]
    public void Coily_ReachesTargetOnSameRowWithoutLeavingPyramid()
    {
        var coily = Coily(new Cell(4, 0));
        var target = new Cell(4, 2);

        for (int hop = 0; hop < 4 && coily.Cell != target; hop++)
        {
            var move = EnemyBrain.ChooseMove(coily, target, new Random(1));
            Assert.NotNull(move);
            coily.Place(coily.Cell.Step(move.Value));
            Assert.True(coily.Cell.IsOnPyramid);
        }

        Assert.Equal(target, coily.Cell);
    }

    [Fact]
    public void Coily_StepsOffEdgeOnlyOntoTheLure()
    {
        var coily = Coily(new Cell(1, 0));

        var move = EnemyBrain.ChooseMove(coily, new Cell(0, -1), new Random(1));

        Assert.Equal(Direction.UpLeft, move);
    }

    [Fact]
    public void Coily_OnTarget_StaysPut() =>
        Assert.Null(EnemyBrain.ChooseMove(Coily(new Cell(3, 1)), new Cell(3, 1), new Random(1)));

    [Fact]
    public void Balls_OnlyBounceDown()
    {
        var ball = new Enemy(EnemyKind.RedBall);
        var random = new Random(7);

        for (int i = 0; i < 50; i++)
        {
            Assert.Contains(EnemyBrain.ChooseMove(ball, new Cell(0, 0), random), new Direction?[] { Direction.DownLeft, Direction.DownRight });
        }
    }

    [Fact]
    public void Hatch_TurnsPurpleBallIntoCoily()
    {
        var ball = new Enemy(EnemyKind.PurpleBall);

        ball.Hatch();

        Assert.Equal(EnemyKind.Coily, ball.Kind);
    }

    [Fact]
    public void Hatch_OtherKinds_Throws() =>
        Assert.Throws<InvalidOperationException>(() => new Enemy(EnemyKind.RedBall).Hatch());

    private static Enemy Coily(Cell cell)
    {
        var coily = new Enemy(EnemyKind.Coily);
        coily.Place(cell);
        return coily;
    }
}
