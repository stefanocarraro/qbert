using Qbert.Core.Board;

namespace Qbert.Core.Tests;

public class BoardTests
{
    [Theory]
    [InlineData(Direction.UpRight, 2, 1)]
    [InlineData(Direction.UpLeft, 2, 0)]
    [InlineData(Direction.DownRight, 4, 2)]
    [InlineData(Direction.DownLeft, 4, 1)]
    [InlineData(Direction.Left, 3, 0)]
    [InlineData(Direction.Right, 3, 2)]
    public void Step_MovesToNeighbour(Direction direction, int row, int col)
    {
        var next = new Cell(3, 1).Step(direction);

        Assert.Equal(new Cell(row, col), next);
    }

    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(6, 6, true)]
    [InlineData(6, 0, true)]
    [InlineData(-1, 0, false)]
    [InlineData(7, 0, false)]
    [InlineData(2, 3, false)]
    [InlineData(2, -1, false)]
    public void IsOnPyramid_ChecksBounds(int row, int col, bool expected) =>
        Assert.Equal(expected, new Cell(row, col).IsOnPyramid);

    [Fact]
    public void Single_ReachesTargetInOneStepAndStays()
    {
        var pyramid = new Pyramid(ColorRule.Single);
        var cell = new Cell(2, 1);

        int first = pyramid.Step(cell);
        int second = pyramid.Step(cell);

        Assert.Equal(Pyramid.TargetPoints, first);
        Assert.Equal(0, second);
        Assert.Equal(1, pyramid.StageAt(cell));
    }

    [Fact]
    public void Double_NeedsTwoSteps()
    {
        var pyramid = new Pyramid(ColorRule.Double);
        var cell = new Cell(0, 0);

        Assert.Equal(Pyramid.IntermediatePoints, pyramid.Step(cell));
        Assert.Equal(Pyramid.TargetPoints, pyramid.Step(cell));
        Assert.Equal(0, pyramid.Step(cell));
        Assert.Equal(2, pyramid.StageAt(cell));
    }

    [Fact]
    public void Toggle_RevertsOnSecondStep()
    {
        var pyramid = new Pyramid(ColorRule.Toggle);
        var cell = new Cell(1, 1);

        pyramid.Step(cell);
        int points = pyramid.Step(cell);

        Assert.Equal(0, points);
        Assert.Equal(0, pyramid.StageAt(cell));
    }

    [Fact]
    public void DoubleRevert_ThirdStepGoesBackToIntermediate()
    {
        var pyramid = new Pyramid(ColorRule.DoubleRevert);
        var cell = new Cell(1, 0);

        pyramid.Step(cell);
        pyramid.Step(cell);
        pyramid.Step(cell);

        Assert.Equal(1, pyramid.StageAt(cell));
    }

    [Fact]
    public void Revert_StopsAtStartColor()
    {
        var pyramid = new Pyramid(ColorRule.Double);
        var cell = new Cell(4, 2);
        pyramid.Step(cell);

        pyramid.Revert(cell);
        pyramid.Revert(cell);

        Assert.Equal(0, pyramid.StageAt(cell));
    }

    [Fact]
    public void IsComplete_OnlyWhenEveryCubeIsTarget()
    {
        var pyramid = new Pyramid(ColorRule.Single);
        var cells = AllCells().ToList();
        foreach (var cell in cells.Skip(1))
        {
            pyramid.Step(cell);
        }

        Assert.False(pyramid.IsComplete);
        pyramid.Step(cells[0]);
        Assert.True(pyramid.IsComplete);
    }

    [Fact]
    public void StageAt_OffPyramid_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Pyramid(ColorRule.Single).StageAt(new Cell(7, 0)));

    [Fact]
    public void Screen_RowsTileWithoutGaps()
    {
        var (apexX, apexY) = Screen.Top(new Cell(0, 0));
        var (leftX, leftY) = Screen.Top(new Cell(1, 0));

        Assert.Equal(apexX - Screen.HalfCube, leftX);
        Assert.Equal(apexY + Screen.RowHeight, leftY);
    }

    internal static IEnumerable<Cell> AllCells() =>
        Enumerable.Range(0, Pyramid.Rows).SelectMany(row => Enumerable.Range(0, row + 1).Select(col => new Cell(row, col)));
}
