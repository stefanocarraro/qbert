using Qbert.Core.Actors;
using Qbert.Core.Board;

namespace Qbert.Core.Tests;

public class GameTests
{
    private static readonly Cell _apex = new(0, 0);
    private static readonly Cell _belowLeft = new(1, 0);

    [Fact]
    public void PressStart_FromTitle_ShowsIntroThenPlays()
    {
        var game = new Game(new Random(1));

        game.PressStart();
        Assert.Equal(GameState.LevelIntro, game.State);
        game.PressStart();

        Assert.Equal(GameState.Playing, game.State);
        Assert.Equal(_apex, game.Player.Cell);
        Assert.Equal(Game.StartingSpareLives, game.SpareLives);
        Assert.Equal(Sound.Start, Assert.Single(game.DrainSounds()));
    }

    [Fact]
    public void LevelIntro_EndsByItself()
    {
        var game = new Game(new Random(1));
        game.PressStart();

        Run(game, Game.LevelIntroFrames);

        Assert.Equal(GameState.Playing, game.State);
    }

    [Fact]
    public void Jump_ColorsCubeAndScores()
    {
        var game = Playing();

        Jump(game, Direction.DownLeft);

        Assert.Equal(_belowLeft, game.Player.Cell);
        Assert.Equal(1, game.Pyramid.StageAt(_belowLeft));
        Assert.Equal(Pyramid.TargetPoints, game.Score);
    }

    [Fact]
    public void Input_SidewaysDirection_Throws() =>
        Assert.Throws<ArgumentException>(() => Playing().Input = Direction.Left);

    [Fact]
    public void JumpOffEdge_WithoutDisk_FallsAndRespawns()
    {
        var game = Playing();
        game.SetDisks();

        Jump(game, Direction.UpLeft);
        Assert.Equal(Motion.Falling, game.Player.Motion);
        RunUntil(game, () => game.Player.Motion == Motion.Idle);

        Assert.Equal(Game.StartingSpareLives - 1, game.SpareLives);
        Assert.Equal(_apex, game.Player.Cell);
        Assert.Contains(Sound.Fall, game.DrainSounds());
    }

    [Fact]
    public void LosingLastLife_EndsGameAndReturnsToTitle()
    {
        var game = Playing();
        game.SetDisks();

        for (int life = 0; life <= Game.StartingSpareLives; life++)
        {
            Jump(game, Direction.UpLeft);
            RunUntil(game, () => game.Player.Motion != Motion.Falling || game.State == GameState.GameOver);
        }

        Assert.Equal(GameState.GameOver, game.State);
        Run(game, Game.GameOverFrames);
        Assert.Equal(GameState.Title, game.State);
    }

    [Fact]
    public void Disk_CarriesPlayerBackToApex()
    {
        var game = Playing();
        game.SetDisks(new Cell(0, -1));
        Jump(game, Direction.DownLeft);

        Jump(game, Direction.UpLeft);
        Assert.Equal(Motion.Riding, game.Player.Motion);
        RunUntil(game, () => game.Player.Motion == Motion.Idle);

        Assert.Equal(_apex, game.Player.Cell);
        Assert.True(Assert.Single(game.Disks).Used);
    }

    [Fact]
    public void Disk_LuresChasingCoilyOffThePyramid()
    {
        var game = Playing();
        game.SetDisks(new Cell(0, -1));
        Jump(game, Direction.DownLeft);
        Jump(game, Direction.UpLeft);
        var coily = Place(game, EnemyKind.Coily, new Cell(2, 1), wait: 1);

        RunUntil(game, () => game.Player.Motion == Motion.Idle);

        Assert.Equal(Motion.Falling, coily.Motion);
        Assert.Equal(Pyramid.TargetPoints * 2 + Game.CoilyLurePoints, game.Score);
        Assert.Contains(Sound.CoilyFall, game.DrainSounds());
    }

    [Fact]
    public void RedBall_KillsPlayer_ThenEnemiesAreCleared()
    {
        var game = Playing();
        Place(game, EnemyKind.RedBall, _belowLeft, wait: 1000);

        Jump(game, Direction.DownLeft);
        Assert.Equal(GameState.Dying, game.State);
        Run(game, Game.DyingFrames);

        Assert.Equal(GameState.Playing, game.State);
        Assert.Equal(Game.StartingSpareLives - 1, game.SpareLives);
        Assert.Empty(game.Enemies);
        Assert.Equal(_belowLeft, game.Player.Cell);
    }

    [Fact]
    public void GreenBall_FreezesEnemiesAndScores()
    {
        var game = Playing();
        Place(game, EnemyKind.GreenBall, _belowLeft, wait: 1000);
        var frozen = Place(game, EnemyKind.RedBall, new Cell(5, 2), wait: 15);

        Jump(game, Direction.DownLeft);

        Assert.True(game.FreezeTimer > 0);
        Assert.Equal(Game.GreenBallPoints + Pyramid.TargetPoints, game.Score);
        Assert.Equal(Motion.Idle, frozen.Motion);
        Assert.Equal(new Cell(5, 2), frozen.Cell);
    }

    [Theory]
    [InlineData(EnemyKind.Slick)]
    [InlineData(EnemyKind.Sam)]
    public void SlickAndSam_CanBeCaught(EnemyKind kind)
    {
        var game = Playing();
        Place(game, kind, _belowLeft, wait: 1000);

        Jump(game, Direction.DownLeft);

        Assert.Empty(game.Enemies);
        Assert.Equal(Game.SlickSamPoints + Pyramid.TargetPoints, game.Score);
    }

    [Fact]
    public void Slick_RevertsTheCubeItLandsOn()
    {
        var game = Playing();
        game.Pyramid.Step(new Cell(3, 1));
        game.Pyramid.Step(new Cell(3, 2));
        Place(game, EnemyKind.Slick, new Cell(2, 1), wait: 1);

        Run(game, 20);

        Assert.Equal(1, game.Pyramid.StageAt(new Cell(3, 1)) + game.Pyramid.StageAt(new Cell(3, 2)));
    }

    [Fact]
    public void PurpleBall_OnBottomRow_HatchesIntoCoily()
    {
        var game = Playing();
        var ball = Place(game, EnemyKind.PurpleBall, new Cell(6, 3), wait: 1);

        game.Update();

        Assert.Equal(EnemyKind.Coily, ball.Kind);
    }

    [Fact]
    public void Ball_FallsOffBottomAndDisappears()
    {
        var game = Playing();
        Place(game, EnemyKind.RedBall, new Cell(6, 0), wait: 1);

        RunUntil(game, () => game.Enemies.Count == 0, maxFrames: 80);
    }

    [Fact]
    public void Spawner_AddsEnemiesOverTime()
    {
        var game = Playing();

        RunUntil(game, () => game.Enemies.Count > 0);
    }

    [Fact]
    public void ClearingPyramid_AwardsBonusAndStartsNextRound()
    {
        var game = Playing();
        game.SetDisks(new Cell(3, -1));

        ClearRound(game);
        Assert.Equal(GameState.RoundComplete, game.State);
        Assert.Equal(1000 + Game.UnusedDiskPoints, game.LastBonus);
        Run(game, Game.RoundCompleteFrames);

        Assert.Equal(GameState.Playing, game.State);
        Assert.Equal(2, game.Round);
        Assert.Equal(_apex, game.Player.Cell);
        Assert.Equal(0, game.Pyramid.StageAt(_belowLeft));
    }

    [Fact]
    public void FourthRound_AdvancesToNextLevelWithNewRule()
    {
        var game = Playing();

        for (int round = 0; round < Game.RoundsPerLevel; round++)
        {
            ClearRound(game);
            Run(game, Game.RoundCompleteFrames);
        }

        Assert.Equal(GameState.LevelIntro, game.State);
        Assert.Equal(2, game.Level);
        Assert.Equal(1, game.Round);
        game.PressStart();
        Assert.Equal(ColorRule.Double, game.Pyramid.Rule);
    }

    [Fact]
    public void Score_AwardsExtraLife()
    {
        var game = Playing();

        while (game.Score < Game.FirstExtraLife)
        {
            ClearRound(game);
            Run(game, Game.RoundCompleteFrames);
            game.PressStart();
        }

        Assert.Equal(Game.StartingSpareLives + 1, game.SpareLives);
        Assert.Contains(Sound.ExtraLife, game.DrainSounds());
    }

    [Fact]
    public void Pause_StopsTheSimulation()
    {
        var game = Playing();
        game.TogglePause();
        int frame = game.Frame;

        Run(game, 10);

        Assert.Equal(frame, game.Frame);
        game.TogglePause();
        game.Update();
        Assert.Equal(frame + 1, game.Frame);
    }

    [Theory]
    [InlineData(1, ColorRule.Single)]
    [InlineData(2, ColorRule.Double)]
    [InlineData(3, ColorRule.Toggle)]
    [InlineData(4, ColorRule.DoubleRevert)]
    [InlineData(5, ColorRule.Toggle)]
    [InlineData(6, ColorRule.DoubleRevert)]
    public void RuleFor_MapsLevels(int level, ColorRule rule) => Assert.Equal(rule, Game.RuleFor(level));

    internal static Game Playing()
    {
        var game = new Game(new Random(42));
        game.PressStart();
        game.PressStart();
        game.DrainSounds();
        return game;
    }

    internal static Enemy Place(Game game, EnemyKind kind, Cell cell, int wait)
    {
        var enemy = new Enemy(kind) { Wait = wait };
        enemy.Place(cell);
        game.AddEnemy(enemy);
        return enemy;
    }

    /// <summary>Starts a jump, releases the key, and runs until the jump lands.</summary>
    internal static void Jump(Game game, Direction direction)
    {
        game.Input = direction;
        game.Update();
        game.Input = null;
        Run(game, Game.PlayerJumpFrames);
    }

    internal static void Run(Game game, int frames)
    {
        for (int i = 0; i < frames; i++)
        {
            game.Update();
        }
    }

    /// <summary>Brings every cube but the one below-left of the apex to target, then lands on it.</summary>
    private static void ClearRound(Game game)
    {
        foreach (var cell in BoardTests.AllCells())
        {
            int goal = cell == _belowLeft ? game.Pyramid.TargetStage - 1 : game.Pyramid.TargetStage;
            while (game.Pyramid.StageAt(cell) < goal)
            {
                game.Pyramid.Step(cell);
            }
        }

        Jump(game, Direction.DownLeft);
    }

    private static void RunUntil(Game game, Func<bool> condition, int maxFrames = 1000)
    {
        for (int i = 0; i < maxFrames; i++)
        {
            if (condition())
            {
                return;
            }

            game.Update();
        }

        Assert.Fail($"Condition not met within {maxFrames} frames");
    }
}
