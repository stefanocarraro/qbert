# Q*bert clone in C# / .NET 10

An experiment in recreating a classic 1980s arcade game, Q*bert (Gottlieb, 1982), with the help of
[Claude](https://claude.com/claude-code) as a coding assistant. The whole project, from research and design
to code, tests and verification, was built in a conversation between a developer and Claude Code.

It is an unofficial fan project for learning purposes. All graphics and sounds are generated from code;
no original arcade assets are used.

The same game runs in two hosts:

- **Web**: Blazor WebAssembly, the game loop runs in C# inside the browser.
- **Desktop**: a native executable for Windows and Linux built on [Raylib-cs](https://github.com/ChrisDill/Raylib-cs).

## Gameplay

- 7-row pyramid of 28 cubes; change every cube top to the target color to clear the round.
- 4 rounds per level, with the color rules changing by level: one jump, two jumps, toggling cubes
  (a second jump reverts the cube) and two jumps with a revert on the third.
- Enemies: red balls, the purple ball that hatches into Coily, the green ball that freezes everyone,
  Slick and Sam (they revert cubes, catch them for points), Ugg and Wrongway (they walk on the cube sides).
- Spinning disks carry Q*bert back to the top and can lure Coily off the edge.
- Round bonus, extra lives at 8,000 points and every 14,000 after that, persistent high score.

## Controls

| Key | Action |
| --- | --- |
| Arrow keys (rotated 45 degrees: Up = up-right, Right = down-right, Down = down-left, Left = up-left) | Move |
| Numpad 7 / 9 / 1 / 3 | Move (up-left / up-right / down-left / down-right) |
| Enter or Space | Start |
| P | Pause |
| M | Mute |
| Esc | Quit (desktop) |

## Project layout

```
src/Qbert.Core      Game rules, enemies, pixel-art renderer, sound synthesis (pure C#, no platform code)
src/Qbert.Web       Blazor WebAssembly host (canvas, keyboard, Web Audio, localStorage via JS interop)
src/Qbert.Desktop   Raylib-cs host (window, keyboard, audio, high score file)
tests/Qbert.Core.Tests  xUnit tests
```

`Qbert.Core` renders each frame into a 256x240 RGBA framebuffer; each host only copies it to the screen,
feeds the keys in and plays the requested sounds.

## Download

Ready-to-run executables for Windows and Linux are attached to the
[latest release](https://github.com/stefanocarraro/qbert/releases/latest).

## Requirements

- .NET 10 SDK
- Desktop on Linux: glibc 2.38 or newer (Ubuntu 24.04+, Debian 13+, Fedora 39+), an X11 or Wayland
  session with OpenGL, and ALSA or PulseAudio. The bundled native raylib library needs that glibc version.

## Run

```
dotnet run --project src/Qbert.Web        # browser version
dotnet run --project src/Qbert.Desktop    # desktop version
dotnet test                               # unit tests
```

## Publish desktop executables

Single self-contained, trimmed files (about 15 MB each):

```
dotnet publish src/Qbert.Desktop -c Release -r win-x64   --self-contained -p:PublishSingleFile=true -o publish/win-x64
dotnet publish src/Qbert.Desktop -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64
```

## Known simplifications

- From level 5 on, the color rules alternate between toggle and two-jumps-with-revert.
- Enemy speeds and spawn rates are tuned by feel, not taken from the arcade ROM.
- Ugg and Wrongway share one sprite, rotated onto the cube side.

## Inspiration

- [Q*bert on Wikipedia](https://it.wikipedia.org/wiki/Q*bert)
- [alainsmet/qbert](https://github.com/alainsmet/qbert), JavaScript and canvas clone
- [leonicolas/ReQbert](https://github.com/leonicolas/ReQbert), HTML5 remake of the MSX version
- [SteveDunn/PacManBlazor](https://github.com/SteveDunn/PacManBlazor), arcade game architecture in Blazor

## License

Code released under the [MIT License](LICENSE).

Q*bert is a trademark of its respective owners. This project is not affiliated with or endorsed by them.
