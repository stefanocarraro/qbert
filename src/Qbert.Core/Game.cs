using Qbert.Core.Actors;
using Qbert.Core.Board;

namespace Qbert.Core;

/// <summary>
/// Top-level phases of a session.
/// </summary>
public enum GameState
{
    Title,
    LevelIntro,
    Playing,
    Dying,
    RoundComplete,
    GameOver
}

/// <summary>
/// Sound effects the host should play; drained once per frame.
/// </summary>
public enum Sound
{
    Start,
    Jump,
    Fall,
    Curse,
    Disk,
    Catch,
    CoilyFall,
    Hatch,
    RoundComplete,
    ExtraLife,
    GameOver
}

/// <summary>
/// Q*bert rules engine: advances the whole simulation one fixed 60 Hz frame at a time.
/// </summary>
public sealed class Game
{
    /// <summary>Rounds in each level; a level shares one color rule.</summary>
    public const int RoundsPerLevel = 4;

    /// <summary>Spare lives at the start of a game.</summary>
    public const int StartingSpareLives = 2;

    /// <summary>Score that awards the first extra life.</summary>
    public const int FirstExtraLife = 8000;

    /// <summary>Score distance between subsequent extra lives.</summary>
    public const int ExtraLifeStep = 14000;

    /// <summary>Points for luring Coily off the pyramid with a disk.</summary>
    public const int CoilyLurePoints = 500;

    /// <summary>Points for catching the green ball.</summary>
    public const int GreenBallPoints = 100;

    /// <summary>Points for catching Slick or Sam.</summary>
    public const int SlickSamPoints = 300;

    /// <summary>Bonus for each disk left unused at the end of a round.</summary>
    public const int UnusedDiskPoints = 50;

    /// <summary>Frames of a Q*bert jump.</summary>
    public const int PlayerJumpFrames = 18;

    /// <summary>Frames of a disk ride up to the apex.</summary>
    public const int RideFrames = 120;

    /// <summary>Frames enemies stay frozen after the green ball is caught.</summary>
    public const int FreezeFrames = 180;

    /// <summary>Frames of the curse pause after Q*bert is caught.</summary>
    public const int DyingFrames = 120;

    /// <summary>Frames of the flashing pyramid after a round is cleared.</summary>
    public const int RoundCompleteFrames = 150;

    /// <summary>Frames of the level introduction screen.</summary>
    public const int LevelIntroFrames = 180;

    /// <summary>Frames the game-over screen stays up.</summary>
    public const int GameOverFrames = 240;

    private const int _enemyJumpFrames = 18;
    private const int _spawnDropFrames = 30;
    private const int _hatchFrames = 40;
    private const int _firstSpawnFrames = 90;
    private const int _maxEnemies = 5;
    private const int _baseRoundBonus = 1000;
    private const int _roundBonusStep = 250;
    private const int _maxRoundBonus = 5000;

    private readonly Random _random;
    private readonly List<Enemy> _enemies = new();
    private readonly List<Disk> _disks = new();
    private readonly List<Sound> _sounds = new();
    private Direction? _input;
    private Cell? _lure;
    private Cell _respawn;
    private int _nextExtraLife;
    private int _spawnTimer;
    private int _roundsCompleted;

    /// <summary>
    /// Initializes a game on the title screen.
    /// </summary>
    /// <param name="random">Randomness source; pass a seeded instance for reproducible runs.</param>
    public Game(Random random)
    {
        _random = random;
        Pyramid = new(ColorRule.Single);
    }

    /// <summary>Gets the current phase.</summary>
    public GameState State { get; private set; }

    /// <summary>Gets the frames left in a timed phase (intro, dying, round complete, game over).</summary>
    public int StateTimer { get; private set; }

    /// <summary>Gets the frames simulated since construction; drives animations.</summary>
    public int Frame { get; private set; }

    /// <summary>Gets the current score.</summary>
    public int Score { get; private set; }

    /// <summary>Gets or sets the best score; the host loads it from storage.</summary>
    public int HighScore { get; set; }

    /// <summary>Gets the lives left after the current one.</summary>
    public int SpareLives { get; private set; }

    /// <summary>Gets the 1-based level.</summary>
    public int Level { get; private set; } = 1;

    /// <summary>Gets the 1-based round inside the level.</summary>
    public int Round { get; private set; } = 1;

    /// <summary>Gets the bonus awarded by the last cleared round.</summary>
    public int LastBonus { get; private set; }

    /// <summary>Gets the frames left before frozen enemies move again.</summary>
    public int FreezeTimer { get; private set; }

    /// <summary>Gets a value indicating whether the simulation is paused.</summary>
    public bool IsPaused { get; private set; }

    /// <summary>Gets the cubes of the current round.</summary>
    public Pyramid Pyramid { get; private set; }

    /// <summary>Gets Q*bert.</summary>
    public Actor Player { get; } = new();

    /// <summary>Gets the enemies on screen.</summary>
    public IReadOnlyList<Enemy> Enemies => _enemies;

    /// <summary>Gets the disks of the current round.</summary>
    public IReadOnlyList<Disk> Disks => _disks;

    /// <summary>Gets the index of the round in the whole game, used to pick the color scheme.</summary>
    public int RoundIndex => (Level - 1) * RoundsPerLevel + Round - 1;

    /// <summary>Gets or sets the direction the player is holding, or <c>null</c> for none.</summary>
    /// <exception cref="ArgumentException">The direction is not one of the four diagonals.</exception>
    public Direction? Input
    {
        get => _input;
        set => _input = value is Direction.Left or Direction.Right
            ? throw new ArgumentException($"Q*bert moves only diagonally, not {value}", nameof(value))
            : value;
    }

    private int EnemyPause => Math.Max(6, 30 - (Level - 1) * 3 - (Round - 1) * 2);

    private int CoilyPause => EnemyPause + 6;

    private int SpawnInterval => Math.Max(60, 200 - (Level - 1) * 15 - (Round - 1) * 10);

    /// <summary>
    /// Maps a level to its color rule; from level 5 the toggle rules alternate.
    /// </summary>
    /// <param name="level">1-based level.</param>
    /// <returns>The color rule for that level.</returns>
    public static ColorRule RuleFor(int level) => level switch
    {
        1 => ColorRule.Single,
        2 => ColorRule.Double,
        3 => ColorRule.Toggle,
        4 => ColorRule.DoubleRevert,
        _ => level % 2 == 1 ? ColorRule.Toggle : ColorRule.DoubleRevert
    };

    /// <summary>
    /// Handles the start button: begins a game from the title, or skips the level intro.
    /// </summary>
    public void PressStart()
    {
        if (State == GameState.Title)
        {
            NewGame();
        }
        else if (State == GameState.LevelIntro)
        {
            StartRound();
        }
    }

    /// <summary>
    /// Pauses or resumes play; ignored outside the playing phase.
    /// </summary>
    public void TogglePause()
    {
        if (State == GameState.Playing)
        {
            IsPaused = !IsPaused;
        }
    }

    /// <summary>
    /// Returns the sounds raised since the last call and forgets them.
    /// </summary>
    /// <returns>Sounds in the order they were raised.</returns>
    public Sound[] DrainSounds()
    {
        var sounds = _sounds.ToArray();
        _sounds.Clear();
        return sounds;
    }

    /// <summary>
    /// Simulates one frame.
    /// </summary>
    public void Update()
    {
        if (IsPaused)
        {
            return;
        }

        Frame++;
        if (State == GameState.Playing)
        {
            UpdatePlaying();
        }
        else if (State != GameState.Title && --StateTimer <= 0)
        {
            EndTimedState();
        }
    }

    internal void AddEnemy(Enemy enemy) => _enemies.Add(enemy);

    internal void SetDisks(params Cell[] cells)
    {
        _disks.Clear();
        _disks.AddRange(cells.Select(cell => new Disk(cell)));
    }

    private void NewGame()
    {
        Score = 0;
        SpareLives = StartingSpareLives;
        Level = 1;
        Round = 1;
        _roundsCompleted = 0;
        _nextExtraLife = FirstExtraLife;
        Play(Sound.Start);
        EnterTimedState(GameState.LevelIntro, LevelIntroFrames);
    }

    private void StartRound()
    {
        Pyramid = new(RuleFor(Level));
        ClearEnemies();
        PlaceDisks(Level < 3 ? 2 : 3);
        Player.Place(new Cell(0, 0));
        State = GameState.Playing;
    }

    private void PlaceDisks(int count)
    {
        _disks.Clear();
        while (_disks.Count < count)
        {
            int row = _random.Next(1, Pyramid.Rows - 1);
            var cell = _random.Next(2) == 0 ? new Cell(row, -1) : new Cell(row, row + 1);
            if (_disks.All(disk => disk.Cell != cell))
            {
                _disks.Add(new Disk(cell));
            }
        }
    }

    private void EnterTimedState(GameState state, int frames)
    {
        State = state;
        StateTimer = frames;
    }

    private void EndTimedState()
    {
        switch (State)
        {
            case GameState.LevelIntro:
                StartRound();
                break;
            case GameState.Dying:
                LoseLife(_respawn);
                break;
            case GameState.RoundComplete:
                NextRound();
                break;
            case GameState.GameOver:
                State = GameState.Title;
                break;
        }
    }

    private void NextRound()
    {
        if (++Round <= RoundsPerLevel)
        {
            StartRound();
            return;
        }

        Round = 1;
        Level++;
        EnterTimedState(GameState.LevelIntro, LevelIntroFrames);
    }

    private void UpdatePlaying()
    {
        UpdatePlayer();
        if (State != GameState.Playing)
        {
            return;
        }

        if (FreezeTimer > 0)
        {
            FreezeTimer--;
        }
        else
        {
            UpdateEnemies();
            UpdateSpawner();
        }

        CheckCollisions();
    }

    private void UpdatePlayer()
    {
        switch (Player.Motion)
        {
            case Motion.Idle:
                if (_input is { } direction)
                {
                    Player.JumpTo(direction, PlayerJumpFrames);
                    Play(Sound.Jump);
                }

                break;
            case Motion.Falling:
                Player.Advance();
                if (Player.IsOffScreen)
                {
                    LoseLife(Player.From);
                }

                break;
            default:
                if (Player.Advance())
                {
                    OnPlayerLanded();
                }

                break;
        }
    }

    private void OnPlayerLanded()
    {
        var cell = Player.Cell;
        if (cell.IsOnPyramid)
        {
            _lure = null;
            AddScore(Pyramid.Step(cell));
            if (Pyramid.IsComplete)
            {
                CompleteRound();
            }

            return;
        }

        var disk = _disks.FirstOrDefault(d => d.Cell == cell && !d.Used);
        if (disk is null)
        {
            Player.StartFalling();
            Play(Sound.Fall);
            return;
        }

        disk.Used = true;
        _lure = cell;
        Player.Ride(RideFrames);
        Play(Sound.Disk);
    }

    private void UpdateEnemies()
    {
        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            if (UpdateEnemy(_enemies[i]))
            {
                _enemies.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Advances one enemy.
    /// </summary>
    /// <param name="enemy">Enemy to update.</param>
    /// <returns><c>true</c> when the enemy left the screen and must be removed.</returns>
    private bool UpdateEnemy(Enemy enemy)
    {
        switch (enemy.Motion)
        {
            case Motion.Falling:
                enemy.Advance();
                return enemy.IsOffScreen;
            case Motion.Idle:
                Think(enemy);
                return false;
            default:
                if (enemy.Advance())
                {
                    OnEnemyLanded(enemy);
                }

                return false;
        }
    }

    private void Think(Enemy enemy)
    {
        if (--enemy.Wait > 0)
        {
            return;
        }

        if (enemy.Kind == EnemyKind.PurpleBall && enemy.Cell.Row == Pyramid.Rows - 1)
        {
            enemy.Hatch();
            enemy.Wait = CoilyPause;
            Play(Sound.Hatch);
            return;
        }

        var move = EnemyBrain.ChooseMove(enemy, ChaseTarget(), _random);
        if (move is { } direction)
        {
            enemy.JumpTo(direction, _enemyJumpFrames);
            enemy.Wait = enemy.Kind == EnemyKind.Coily ? CoilyPause : EnemyPause;
        }
    }

    /// <summary>
    /// Selects the cell Coily heads for: the disk Q*bert escaped on, else the last cube he stood on.
    /// </summary>
    /// <returns>The chase target.</returns>
    private Cell ChaseTarget() => _lure ?? (Player.Cell.IsOnPyramid ? Player.Cell : Player.From);

    private void OnEnemyLanded(Enemy enemy)
    {
        if (!enemy.CanStandOn(enemy.Cell))
        {
            enemy.StartFalling();
            if (enemy.Kind == EnemyKind.Coily)
            {
                AddScore(CoilyLurePoints);
                Play(Sound.CoilyFall);
            }

            return;
        }

        if (enemy.Kind is EnemyKind.Slick or EnemyKind.Sam)
        {
            Pyramid.Revert(enemy.Cell);
        }
        else if (enemy.Kind == EnemyKind.PurpleBall && enemy.Cell.Row == Pyramid.Rows - 1)
        {
            enemy.Wait = _hatchFrames;
        }
    }

    private void UpdateSpawner()
    {
        if (--_spawnTimer > 0)
        {
            return;
        }

        _spawnTimer = SpawnInterval;
        if (_enemies.Count < _maxEnemies)
        {
            _enemies.Add(Spawn(PickSpawnKind()));
        }
    }

    /// <summary>
    /// Chooses which enemy enters next, honouring per-kind limits and the level that unlocks each kind.
    /// </summary>
    /// <returns>The kind to spawn.</returns>
    private EnemyKind PickSpawnKind()
    {
        List<EnemyKind> options = [EnemyKind.RedBall];
        if (!Has(EnemyKind.PurpleBall) && !Has(EnemyKind.Coily))
        {
            options.Add(EnemyKind.PurpleBall);
        }

        // ponytail: one-in-three gate keeps the freeze ball rare; tune per level if it feels off.
        if ((Level > 1 || Round >= 2) && !Has(EnemyKind.GreenBall) && _random.Next(3) == 0)
        {
            options.Add(EnemyKind.GreenBall);
        }

        bool late = Level > 1 || Round >= 3;
        AddIfAbsent(options, EnemyKind.Slick, late);
        AddIfAbsent(options, EnemyKind.Sam, late);
        AddIfAbsent(options, EnemyKind.Ugg, Level >= 2);
        AddIfAbsent(options, EnemyKind.Wrongway, Level >= 2);
        return options[_random.Next(options.Count)];
    }

    private void AddIfAbsent(List<EnemyKind> options, EnemyKind kind, bool unlocked)
    {
        if (unlocked && !Has(kind))
        {
            options.Add(kind);
        }
    }

    private bool Has(EnemyKind kind) => _enemies.Any(enemy => enemy.Kind == kind);

    private Enemy Spawn(EnemyKind kind)
    {
        var enemy = new Enemy(kind) { Wait = EnemyPause };
        switch (kind)
        {
            case EnemyKind.Ugg:
                enemy.Place(new Cell(Pyramid.Rows, Pyramid.Rows));
                break;
            case EnemyKind.Wrongway:
                enemy.Place(new Cell(Pyramid.Rows, 0));
                break;
            default:
                enemy.DropOnto(new Cell(1, _random.Next(2)), _spawnDropFrames);
                break;
        }

        return enemy;
    }

    private void CheckCollisions()
    {
        if (Player.CollisionCell is not { } playerCell)
        {
            return;
        }

        for (int i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            if (enemy.CollisionCell != playerCell)
            {
                continue;
            }

            if (enemy.IsDeadly)
            {
                Die(playerCell);
                return;
            }

            _enemies.RemoveAt(i);
            Catch(enemy.Kind);
        }
    }

    private void Catch(EnemyKind kind)
    {
        if (kind == EnemyKind.GreenBall)
        {
            FreezeTimer = FreezeFrames;
            AddScore(GreenBallPoints);
        }
        else
        {
            AddScore(SlickSamPoints);
        }

        Play(Sound.Catch);
    }

    private void Die(Cell where)
    {
        _respawn = where.IsOnPyramid ? where : Player.From;
        EnterTimedState(GameState.Dying, DyingFrames);
        Play(Sound.Curse);
    }

    private void LoseLife(Cell respawn)
    {
        ClearEnemies();
        if (SpareLives == 0)
        {
            EnterTimedState(GameState.GameOver, GameOverFrames);
            Play(Sound.GameOver);
            return;
        }

        SpareLives--;
        Player.Place(respawn);
        State = GameState.Playing;
    }

    private void CompleteRound()
    {
        ClearEnemies();
        LastBonus = Math.Min(_maxRoundBonus, _baseRoundBonus + _roundBonusStep * _roundsCompleted)
            + _disks.Count(disk => !disk.Used) * UnusedDiskPoints;
        _roundsCompleted++;
        AddScore(LastBonus);
        EnterTimedState(GameState.RoundComplete, RoundCompleteFrames);
        Play(Sound.RoundComplete);
    }

    private void ClearEnemies()
    {
        _enemies.Clear();
        _lure = null;
        FreezeTimer = 0;
        _spawnTimer = _firstSpawnFrames;
    }

    private void AddScore(int points)
    {
        Score += points;
        HighScore = Math.Max(HighScore, Score);
        while (Score >= _nextExtraLife)
        {
            SpareLives++;
            _nextExtraLife += ExtraLifeStep;
            Play(Sound.ExtraLife);
        }
    }

    private void Play(Sound sound) => _sounds.Add(sound);
}
