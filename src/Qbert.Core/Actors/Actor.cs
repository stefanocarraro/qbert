using Qbert.Core.Board;

namespace Qbert.Core.Actors;

/// <summary>
/// What an actor is doing right now.
/// </summary>
public enum Motion
{
    Idle,
    Jumping,
    Dropping,
    Falling,
    Riding
}

/// <summary>
/// Anything that hops on the pyramid, Q*bert included: holds the lattice position and animates moves between cells.
/// </summary>
public class Actor
{
    /// <summary>Frames needed to drop from the hover point onto the apex after a disk ride.</summary>
    public const int ApexDropFrames = 24;

    private const float _jumpArc = 10f;
    private const float _dropHeight = 48f;
    private const float _gravity = 0.3f;

    private int _timer;
    private int _duration;
    private float _fallOffset;
    private float _fallSpeed;

    /// <summary>Gets the cell the actor stands on, or is heading to while moving.</summary>
    public Cell Cell { get; private set; }

    /// <summary>Gets the cell the current move started from.</summary>
    public Cell From { get; private set; }

    /// <summary>Gets the current motion.</summary>
    public Motion Motion { get; private set; }

    /// <summary>Gets the direction of the last jump, used to pick the sprite orientation.</summary>
    public Direction Facing { get; private set; } = Direction.DownRight;

    /// <summary>Gets a value indicating whether a fall started from an upward jump, so it happens behind the pyramid.</summary>
    public bool FallsBehind { get; private set; }

    /// <summary>Gets a value indicating whether a falling actor has left the screen.</summary>
    public bool IsOffScreen => Motion == Motion.Falling && Screen.Top(Cell).Y + _fallOffset > Screen.Height + 32;

    /// <summary>Gets the cell used for collisions, or <c>null</c> while the actor cannot touch anything.</summary>
    public Cell? CollisionCell => Motion switch
    {
        Motion.Idle => Cell,
        Motion.Jumping => Progress < 0.5f ? From : Cell,
        Motion.Dropping => Progress > 0.75f ? Cell : null,
        _ => null
    };

    /// <summary>Gets the pixel offset applied when drawing, relative to the cube top of <see cref="Cell"/>.</summary>
    protected virtual (float X, float Y) DisplayOffset => (0, 0);

    /// <summary>Gets the height of the jump parabola in pixels.</summary>
    protected virtual float JumpArc => _jumpArc;

    private float Progress => _duration == 0 ? 1f : _timer / (float)_duration;

    /// <summary>
    /// Puts the actor at rest on a cell.
    /// </summary>
    /// <param name="cell">Destination cell.</param>
    public void Place(Cell cell)
    {
        Cell = From = cell;
        Motion = Motion.Idle;
    }

    /// <summary>
    /// Starts a jump to the neighbouring cell.
    /// </summary>
    /// <param name="direction">Jump direction.</param>
    /// <param name="frames">Jump duration in frames.</param>
    public void JumpTo(Direction direction, int frames)
    {
        From = Cell;
        Cell = Cell.Step(direction);
        Facing = direction;
        FallsBehind = direction is Direction.UpLeft or Direction.UpRight;
        Begin(Motion.Jumping, frames);
    }

    /// <summary>
    /// Starts a vertical drop from above onto a cell.
    /// </summary>
    /// <param name="cell">Landing cell.</param>
    /// <param name="frames">Drop duration in frames.</param>
    public void DropOnto(Cell cell, int frames)
    {
        Cell = From = cell;
        Begin(Motion.Dropping, frames);
    }

    /// <summary>
    /// Starts a ride from the current cell to the hover point above the apex.
    /// </summary>
    /// <param name="frames">Ride duration in frames.</param>
    public void Ride(int frames) => Begin(Motion.Riding, frames);

    /// <summary>
    /// Starts falling off the pyramid from the current position.
    /// </summary>
    public void StartFalling()
    {
        Motion = Motion.Falling;
        _fallOffset = 0;
        _fallSpeed = 0;
    }

    /// <summary>
    /// Advances the current motion by one frame.
    /// </summary>
    /// <returns><c>true</c> when a jump or drop completed on this frame.</returns>
    public bool Advance()
    {
        switch (Motion)
        {
            case Motion.Falling:
                _fallSpeed += _gravity;
                _fallOffset += _fallSpeed;
                return false;
            case Motion.Idle:
                return false;
        }

        if (++_timer < _duration)
        {
            return false;
        }

        if (Motion == Motion.Riding)
        {
            DropOnto(new Cell(0, 0), ApexDropFrames);
            return false;
        }

        Motion = Motion.Idle;
        return true;
    }

    /// <summary>
    /// Computes where the actor is drawn: feet position in screen pixels.
    /// </summary>
    /// <returns>Screen coordinates of the actor's feet.</returns>
    public (float X, float Y) ScreenPosition()
    {
        var (x, y) = RawPosition();
        var (dx, dy) = DisplayOffset;
        return (x + dx, y + dy);
    }

    private (float X, float Y) RawPosition()
    {
        var target = Screen.Top(Cell);
        float t = Progress;
        switch (Motion)
        {
            case Motion.Jumping:
                var start = Screen.Top(From);
                float arc = JumpArc * MathF.Sin(MathF.PI * t);
                return (Lerp(start.X, target.X, t), Lerp(start.Y, target.Y, t) - arc);
            case Motion.Dropping:
                return (target.X, target.Y - (1 - t) * _dropHeight);
            case Motion.Falling:
                return (target.X, target.Y + _fallOffset);
            case Motion.Riding:
                var apex = Screen.Top(new Cell(0, 0));
                return (Lerp(target.X, apex.X, t), Lerp(target.Y, apex.Y - _dropHeight, t));
            default:
                return target;
        }
    }

    private void Begin(Motion motion, int frames)
    {
        Motion = motion;
        _timer = 0;
        _duration = frames;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
