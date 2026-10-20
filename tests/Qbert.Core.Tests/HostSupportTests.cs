using System.Text;
using Qbert.Core.Audio;
using Qbert.Core.Board;

namespace Qbert.Core.Tests;

public class HostSupportTests
{
    [Fact]
    public void Driver_QuickTap_StillJumps()
    {
        var driver = new GameDriver(GameTests.Playing());

        driver.Press(Direction.DownLeft);
        driver.Release(Direction.DownLeft);
        driver.Advance(GameDriver.StepMs * 5);
        GameTests.Run(driver.Game, Game.PlayerJumpFrames);

        Assert.Equal(new Cell(1, 0), driver.Game.Player.Cell);
    }

    [Fact]
    public void Driver_TapIsConsumedOnce()
    {
        var driver = new GameDriver(GameTests.Playing());

        driver.Press(Direction.DownLeft);
        driver.Release(Direction.DownLeft);
        for (int i = 0; i < 3 * Game.PlayerJumpFrames; i++)
        {
            driver.Advance(GameDriver.StepMs);
        }

        Assert.Equal(new Cell(1, 0), driver.Game.Player.Cell);
    }

    [Fact]
    public void Driver_LatestHeldKeyWins()
    {
        var driver = new GameDriver(GameTests.Playing());

        driver.Press(Direction.DownLeft);
        driver.Press(Direction.DownRight);
        driver.Advance(GameDriver.StepMs);

        Assert.Equal(Direction.DownRight, driver.Game.Input);
    }

    [Fact]
    public void Driver_CapsLongStalls()
    {
        var driver = new GameDriver(GameTests.Playing());
        int before = driver.Game.Frame;

        driver.Advance(10_000);

        Assert.Equal(before + 5, driver.Game.Frame);
    }

    [Fact]
    public void Synth_RendersEverySound()
    {
        foreach (var sound in Enum.GetValues<Sound>())
        {
            var samples = Synth.Render(sound);

            Assert.NotEmpty(samples);
            Assert.Contains(samples, sample => sample != 0);
        }
    }

    [Fact]
    public void Synth_WavHasValidHeader()
    {
        short[] samples = [0, 100, -100];

        var wav = Synth.ToWav(samples);

        Assert.Equal(44 + samples.Length * 2, wav.Length);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(wav, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(wav, 8, 4));
        Assert.Equal(Synth.SampleRate, BitConverter.ToInt32(wav, 24));
        Assert.Equal(samples.Length * 2, BitConverter.ToInt32(wav, 40));
    }

    [Fact]
    public void HighScoreFile_RoundTripsAndCreatesFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), $"qbert-test-{Guid.NewGuid()}");
        var file = new HighScoreFile(Path.Combine(folder, "nested", "highscore.txt"));
        try
        {
            Assert.Equal(0, file.Load());

            file.Save(12345);

            Assert.Equal(12345, file.Load());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void HighScoreFile_CorruptContent_LoadsZero()
    {
        string path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "not a number");

            Assert.Equal(0, new HighScoreFile(path).Load());
        }
        finally
        {
            File.Delete(path);
        }
    }
}
