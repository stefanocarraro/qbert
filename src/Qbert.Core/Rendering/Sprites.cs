namespace Qbert.Core.Rendering;

/// <summary>
/// Pixel art for every character, drawn from scratch (no original arcade assets).
/// </summary>
public static class Sprites
{
    private static readonly Dictionary<char, uint> _colors = new()
    {
        ['O'] = Palette.Orange,
        ['o'] = Palette.DarkOrange,
        ['W'] = Palette.White,
        ['K'] = Palette.Black,
        ['R'] = Palette.Red,
        ['P'] = Palette.Purple,
        ['p'] = Palette.DarkPurple,
        ['G'] = Palette.Green
    };

    private static readonly string[] _ball =
    [
        "................",
        "................",
        "................",
        "................",
        "......XXXX......",
        "....XXXXXXXX....",
        "...XWWXXXXXXX...",
        "...XWXXXXXXXX...",
        "..XXXXXXXXXXXX..",
        "..XXXXXXXXXXXX..",
        "..XXXXXXXXXXXD..",
        "...XXXXXXXXXD...",
        "...XXXXXXXXDD...",
        "....XXXXXDDD....",
        "......DDDD......",
        "................"
    ];

    /// <summary>Q*bert seen from the front, snout to the left.</summary>
    public static readonly Sprite QbertFront = Parse(
    [
        "................",
        "......OOOOO.....",
        "....OOOOOOOOO...",
        "...OOWWWOWWWOO..",
        "..OOOWKWOWKWOOO.",
        "..OOOWKWOWKWOOO.",
        "..OOOOWOOOWOOOO.",
        "OOOOOOOOOOOOOOO.",
        "KKOOOOOOOOOOOOo.",
        "OOOOOOOOOOOOOoo.",
        "....OOOOOOOOoo..",
        ".....OOOOOOoo...",
        "......O...O.....",
        "......O...O.....",
        ".....OO..OO.....",
        "....OOO.OOO....."
    ]);

    /// <summary>Q*bert seen from behind, snout to the left.</summary>
    public static readonly Sprite QbertBack = Parse(
    [
        "................",
        "......OOOOO.....",
        "....OOOOOOOOO...",
        "...OOOOOOOOOOO..",
        "..OOOOOOOOOOOOO.",
        "..OOOOOOOOOOOOo.",
        "..OOOOOOOOOOOoo.",
        ".OOOOOOOOOOOOoo.",
        "KOOOOOOOOOOOOoo.",
        "OOO.OOOOOOOOooo.",
        "....OOOOOOOooo..",
        ".....OOOOOooo...",
        "......O...O.....",
        "......O...O.....",
        ".....OO..OO.....",
        "....OOO.OOO....."
    ]);

    /// <summary>Small Q*bert for the lives counter.</summary>
    public static readonly Sprite QbertMini = Parse(
    [
        "..OOOO..",
        ".OWOWOO.",
        "OOOOOOOO",
        "KOOOOOOo",
        "..OOOOo.",
        "..O..O..",
        ".OO.OO..",
        "........"
    ]);

    /// <summary>Coily facing left.</summary>
    public static readonly Sprite Coily = Parse(
    [
        "................",
        "......PPPP......",
        ".....PWWPPP.....",
        "....PPWKPPPP....",
        "...RRPPPPPPP....",
        "..R...PPPPP.....",
        ".......PPP......",
        "........PPP.....",
        ".......PPPP.....",
        "......PPP.......",
        ".....PPPPPPPP...",
        "....PPPPPPPPPP..",
        "....PP......PP..",
        "....PPPPPPPPPP..",
        ".....pppppppp...",
        "................"
    ]);

    /// <summary>Slick, with sunglasses.</summary>
    public static readonly Sprite Slick = Parse(
    [
        "................",
        "................",
        "................",
        "......GGGG......",
        ".....GGGGGG.....",
        "....GGGGGGGG....",
        "....KKKKKKKK....",
        "....GKKGGKKG....",
        "....GGGGGGGG....",
        ".....GRRRRG.....",
        "......GGGG......",
        ".....GG..GG.....",
        "....GG....GG....",
        "...GGG....GGG...",
        "................",
        "................"
    ]);

    /// <summary>Sam, Slick's partner without sunglasses.</summary>
    public static readonly Sprite Sam = Parse(
    [
        "................",
        "................",
        "................",
        "......GGGG......",
        ".....GGGGGG.....",
        "....GGGGGGGG....",
        "....GWWGGWWG....",
        "....GWKGGWKG....",
        "....GGGGGGGG....",
        ".....GRRRRG.....",
        "......GGGG......",
        ".....GG..GG.....",
        "....GG....GG....",
        "...GGG....GGG...",
        "................",
        "................"
    ]);

    /// <summary>Purple gremlin used for both Ugg and Wrongway, rotated onto the cube sides.</summary>
    public static readonly Sprite Gremlin = Parse(
    [
        "................",
        "...P........P...",
        "...PP......PP...",
        "....PPPPPPPP....",
        "...PPPPPPPPPP...",
        "..PPWWWPPWWWPP..",
        "..PPWKWPPWKWPP..",
        "..PPPPPPPPPPPP..",
        "..PPPRRRRRRPPP..",
        "...PPPWPPWPPP...",
        "....PPPPPPPP....",
        ".....pp..pp.....",
        "....ppp..ppp....",
        "................",
        "................",
        "................"
    ]);

    /// <summary>Deadly red ball.</summary>
    public static readonly Sprite RedBall = Ball(Palette.Red, Palette.DarkRed);

    /// <summary>Purple ball that hatches into Coily.</summary>
    public static readonly Sprite PurpleBall = Ball(Palette.Purple, Palette.DarkPurple);

    /// <summary>Green freeze ball.</summary>
    public static readonly Sprite GreenBall = Ball(Palette.Green, Palette.DarkGreen);

    private static Sprite Parse(string[] rows) => Sprite.Parse(rows, _colors);

    private static Sprite Ball(uint main, uint shade) => Sprite.Parse(_ball, new Dictionary<char, uint>
    {
        ['X'] = main,
        ['D'] = shade,
        ['W'] = Palette.White
    });
}
