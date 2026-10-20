using Qbert.Core.Actors;
using Qbert.Core.Board;
using Qbert.Core.Rendering;

namespace Qbert.Core.Tests;

public class RendererTests
{
    private readonly FrameBuffer _frame = new(Screen.Width, Screen.Height);

    [Fact]
    public void Title_DrawsSomething()
    {
        new Renderer(_frame).Draw(new Game(new Random(1)));

        Assert.Contains(_frame.Pixels, pixel => pixel != Palette.Background);
    }

    [Fact]
    public void Playfield_PaintsApexWithStartColor()
    {
        var game = GameTests.Playing();
        game.Player.Place(new Cell(3, 1));

        new Renderer(_frame).Draw(game);

        var (x, y) = Screen.Top(new Cell(0, 0));
        Assert.Equal(Palette.Scheme(0).Start, _frame.Pixels[(int)y * Screen.Width + (int)x]);
    }

    [Fact]
    public void EveryEnemyKindAndOverlay_Renders()
    {
        var game = GameTests.Playing();
        int col = 0;
        foreach (var kind in Enum.GetValues<EnemyKind>())
        {
            GameTests.Place(game, kind, new Cell(6, col++ % 7), wait: 1000);
        }

        var renderer = new Renderer(_frame);
        renderer.Draw(game);
        game.TogglePause();
        renderer.Draw(game);
        game.TogglePause();
        GameTests.Place(game, EnemyKind.RedBall, new Cell(1, 0), wait: 1000);
        GameTests.Jump(game, Direction.DownLeft);

        Assert.Equal(GameState.Dying, game.State);
        renderer.Draw(game);
    }

    [Fact]
    public void LevelIntro_Renders()
    {
        var game = new Game(new Random(1));
        game.PressStart();

        new Renderer(_frame).Draw(game);

        Assert.Contains(_frame.Pixels, pixel => pixel != Palette.Background);
    }

    [Fact]
    public void FrameBuffer_ClipsOutOfBoundsWrites()
    {
        _frame.Set(-1, 5, Palette.White);
        _frame.Set(Screen.Width, 5, Palette.White);
        _frame.FillRect(-10, -10, 5, 5, Palette.White);

        Assert.All(_frame.Pixels, pixel => Assert.Equal(0u, pixel));
    }

    [Fact]
    public void Font_UnknownCharacter_Throws() =>
        Assert.Throws<ArgumentException>(() => Font.Draw(_frame, "~", 0, 0, Palette.White));
}
