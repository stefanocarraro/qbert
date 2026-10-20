using Qbert.Core.Actors;
using Qbert.Core.Board;

namespace Qbert.Core.Rendering;

/// <summary>
/// Draws a <see cref="Game"/> snapshot onto a framebuffer.
/// </summary>
/// <param name="target">Surface of <see cref="Screen.Width"/> by <see cref="Screen.Height"/> pixels.</param>
public sealed class Renderer(FrameBuffer target)
{
    private const int _cubeSize = 32;
    private const string _curse = "@!#?@!";

    private static readonly byte[] _cubeMask = BuildCubeMask();

    /// <summary>
    /// Renders one frame.
    /// </summary>
    /// <param name="game">Game to draw.</param>
    public void Draw(Game game)
    {
        target.Clear(Palette.Background);
        switch (game.State)
        {
            case GameState.Title:
                DrawTitle(game);
                return;
            case GameState.LevelIntro:
                DrawLevelIntro(game);
                return;
        }

        DrawPlayfield(game);
        DrawHud(game);
        DrawOverlay(game);
    }

    private void DrawTitle(Game game)
    {
        DrawCentered("Q*BERT", 24, Palette.Orange, 4);
        Sprites.QbertFront.Draw(target, Screen.Width / 2 - 24, 64, scale: 3);
        if (game.Frame / 30 % 2 == 0)
        {
            DrawCentered("PRESS ENTER TO START", 128, Palette.Yellow);
        }

        DrawCentered("ARROWS OR NUMPAD 7 9 1 3", 152, Palette.White);
        DrawCentered("UP = UP-RIGHT   LEFT = UP-LEFT", 164, Palette.White);
        DrawCentered("P PAUSE   M MUTE", 176, Palette.White);
        DrawCentered($"HI SCORE {game.HighScore}", 204, Palette.Orange);
    }

    private void DrawLevelIntro(Game game)
    {
        var rule = Game.RuleFor(game.Level);
        var scheme = Palette.Scheme(game.RoundIndex);
        DrawCentered($"LEVEL {game.Level}", 40, Palette.Orange, 2);
        DrawCentered(RuleHint(rule), 80, Palette.White);
        uint[] steps = rule is ColorRule.Double or ColorRule.DoubleRevert
            ? [scheme.Start, scheme.Middle, scheme.Target]
            : [scheme.Start, scheme.Target];
        int left = Screen.Width / 2 - (steps.Length - 1) * 24;
        for (int i = 0; i < steps.Length; i++)
        {
            DrawCube(left + i * 48, 120, steps[i], scheme.Left, scheme.Right);
        }

        DrawCentered("PRESS ENTER", 200, Palette.Yellow);
    }

    private static string RuleHint(ColorRule rule) => rule switch
    {
        ColorRule.Single => "JUMP ON EVERY CUBE ONCE",
        ColorRule.Double => "JUMP ON EVERY CUBE TWICE",
        ColorRule.Toggle => "JUMP AGAIN AND THE CUBE FLIPS BACK",
        _ => "TWO JUMPS - A THIRD STEPS BACK"
    };

    private void DrawPlayfield(Game game)
    {
        var scheme = Palette.Scheme(game.RoundIndex);
        DrawActors(game, behindPyramid: true);
        DrawPyramid(game, scheme);
        DrawDisks(game);
        DrawActors(game, behindPyramid: false);
        if (game.State == GameState.Dying)
        {
            DrawCurse(game.Player);
        }
    }

    private void DrawPyramid(Game game, ColorScheme scheme)
    {
        for (int row = 0; row < Pyramid.Rows; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                var cell = new Cell(row, col);
                var (x, y) = Screen.Top(cell);
                DrawCube((int)x, (int)y, TopColor(game, scheme, cell), scheme.Left, scheme.Right);
            }
        }
    }

    private static uint TopColor(Game game, ColorScheme scheme, Cell cell)
    {
        if (game.State == GameState.RoundComplete)
        {
            return (game.Frame / 6 % 3) switch { 0 => scheme.Start, 1 => scheme.Middle, _ => scheme.Target };
        }

        int stage = game.Pyramid.StageAt(cell);
        if (stage == 0)
        {
            return scheme.Start;
        }

        return stage == game.Pyramid.TargetStage ? scheme.Target : scheme.Middle;
    }

    private void DrawCube(int centerX, int topY, uint top, uint left, uint right)
    {
        uint[] faces = [0, top, left, right];
        for (int y = 0; y < _cubeSize; y++)
        {
            for (int x = 0; x < _cubeSize; x++)
            {
                byte face = _cubeMask[y * _cubeSize + x];
                if (face != 0)
                {
                    target.Set(centerX - _cubeSize / 2 + x, topY - Screen.HalfCube / 2 + y, faces[face]);
                }
            }
        }
    }

    /// <summary>
    /// Precomputes which face each pixel of a cube belongs to: 0 none, 1 top, 2 left, 3 right.
    /// </summary>
    /// <returns>A 32x32 mask whose origin is 16 pixels left of and 8 pixels above the top-face center.</returns>
    private static byte[] BuildCubeMask()
    {
        var mask = new byte[_cubeSize * _cubeSize];
        for (int y = 0; y < _cubeSize; y++)
        {
            for (int x = 0; x < _cubeSize; x++)
            {
                float dx = x - _cubeSize / 2 + 0.5f;
                float dy = y - Screen.HalfCube / 2 + 0.5f;
                mask[y * _cubeSize + x] = FaceAt(dx, dy);
            }
        }

        return mask;
    }

    private static byte FaceAt(float dx, float dy)
    {
        if (MathF.Abs(dx) / Screen.HalfCube + MathF.Abs(dy) / (Screen.HalfCube / 2) <= 1)
        {
            return 1;
        }

        float edge = Screen.HalfCube / 2 - MathF.Abs(dx) / 2;
        if (dy < edge || dy >= edge + Screen.HalfCube)
        {
            return 0;
        }

        return dx < 0 ? (byte)2 : (byte)3;
    }

    private void DrawDisks(Game game)
    {
        foreach (var disk in game.Disks.Where(disk => !disk.Used))
        {
            var (x, y) = Screen.Top(disk.Cell);
            DrawDisk((int)x, (int)y + 2, game.Frame);
        }

        if (game.Player.Motion == Motion.Riding)
        {
            var (x, y) = game.Player.ScreenPosition();
            DrawDisk((int)x, (int)y + 2, game.Frame);
        }
    }

    private void DrawDisk(int centerX, int centerY, int frame)
    {
        const int radiusX = 8;
        const int radiusY = 3;
        for (int dy = -radiusY; dy <= radiusY; dy++)
        {
            for (int dx = -radiusX; dx <= radiusX; dx++)
            {
                if (dx * dx * radiusY * radiusY + dy * dy * radiusX * radiusX > radiusX * radiusX * radiusY * radiusY)
                {
                    continue;
                }

                float turn = (MathF.Atan2(dy * 2.5f, dx) + MathF.PI) / (2 * MathF.PI);
                int band = ((int)(turn * Palette.Rainbow.Length) + frame / 4) % Palette.Rainbow.Length;
                target.Set(centerX + dx, centerY + dy, Palette.Rainbow[band]);
            }
        }
    }

    private void DrawActors(Game game, bool behindPyramid)
    {
        var actors = game.Enemies.Cast<Actor>()
            .Append(game.Player)
            .Where(actor => (actor.Motion == Motion.Falling && actor.FallsBehind) == behindPyramid)
            .OrderBy(actor => actor.ScreenPosition().Y);
        foreach (var actor in actors)
        {
            DrawActor(actor);
        }
    }

    private void DrawActor(Actor actor)
    {
        var (x, y) = actor.ScreenPosition();
        if (actor is Enemy { IsSideWalker: true } walker)
        {
            var turn = walker.Kind == EnemyKind.Ugg ? SpriteTransform.RotateRight : SpriteTransform.RotateLeft;
            Sprites.Gremlin.Draw(target, (int)x - 8, (int)y - 8, turn);
            return;
        }

        var (sprite, transform) = actor is Enemy enemy ? Look(enemy) : Look(actor.Facing);
        sprite.Draw(target, (int)x - sprite.Width / 2, (int)y - sprite.Height + 2, transform);
    }

    private static (Sprite Sprite, SpriteTransform Transform) Look(Direction facing) => facing switch
    {
        Direction.DownLeft => (Sprites.QbertFront, SpriteTransform.None),
        Direction.UpLeft => (Sprites.QbertBack, SpriteTransform.None),
        Direction.UpRight => (Sprites.QbertBack, SpriteTransform.FlipX),
        _ => (Sprites.QbertFront, SpriteTransform.FlipX)
    };

    private static (Sprite Sprite, SpriteTransform Transform) Look(Enemy enemy) => enemy.Kind switch
    {
        EnemyKind.RedBall => (Sprites.RedBall, SpriteTransform.None),
        EnemyKind.PurpleBall => (Sprites.PurpleBall, SpriteTransform.None),
        EnemyKind.GreenBall => (Sprites.GreenBall, SpriteTransform.None),
        EnemyKind.Slick => (Sprites.Slick, SpriteTransform.None),
        EnemyKind.Sam => (Sprites.Sam, SpriteTransform.None),
        EnemyKind.Coily => (Sprites.Coily,
            enemy.Facing is Direction.UpRight or Direction.DownRight ? SpriteTransform.FlipX : SpriteTransform.None),
        _ => throw new InvalidOperationException($"No sprite for {enemy.Kind}")
    };

    private void DrawCurse(Actor player)
    {
        var (x, y) = player.ScreenPosition();
        int width = Font.Measure(_curse) + 4;
        int left = (int)x - width / 2;
        int top = (int)y - 32;
        target.FillRect(left, top, width, Font.Height + 4, Palette.White);
        target.FillRect((int)x - 1, top + Font.Height + 4, 3, 3, Palette.White);
        Font.Draw(target, _curse, left + 2, top + 2, Palette.Black);
    }

    private void DrawHud(Game game)
    {
        var scheme = Palette.Scheme(game.RoundIndex);
        Font.Draw(target, "PLAYER 1", 4, 4, Palette.Orange);
        Font.Draw(target, $"{game.Score}", 4, 14, Palette.White);
        Font.Draw(target, "CHANGE TO:", 4, 28, Palette.Purple);
        DrawCube(32, 48, scheme.Target, scheme.Left, scheme.Right);
        for (int i = 0; i < Math.Min(game.SpareLives, 8); i++)
        {
            Sprites.QbertMini.Draw(target, 4, 76 + i * 10);
        }

        DrawRight("HI SCORE", 4, Palette.Orange);
        DrawRight($"{game.HighScore}", 14, Palette.White);
        DrawRight($"LEVEL {game.Level}", 28, Palette.Purple);
        DrawRight($"ROUND {game.Round}", 38, Palette.Purple);
    }

    private void DrawOverlay(Game game)
    {
        if (game.IsPaused)
        {
            DrawBanner("PAUSED");
        }
        else if (game.State == GameState.GameOver)
        {
            DrawBanner("GAME OVER");
        }
        else if (game.State == GameState.RoundComplete)
        {
            DrawCentered($"BONUS {game.LastBonus}", 228, Palette.Yellow);
        }
    }

    private void DrawBanner(string text)
    {
        int width = Font.Measure(text, 2) + 16;
        target.FillRect((Screen.Width - width) / 2, 104, width, Font.Height * 2 + 12, Palette.Black);
        DrawCentered(text, 110, Palette.Orange, 2);
    }

    private void DrawCentered(string text, int top, uint color, int scale = 1) =>
        Font.Draw(target, text, (Screen.Width - Font.Measure(text, scale)) / 2, top, color, scale);

    private void DrawRight(string text, int top, uint color) =>
        Font.Draw(target, text, Screen.Width - 4 - Font.Measure(text), top, color);
}
