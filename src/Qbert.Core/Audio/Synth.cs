namespace Qbert.Core.Audio;

/// <summary>
/// Generates the game's sound effects as 16-bit mono PCM, mirroring the Web Audio version in <c>qbert.js</c>.
/// </summary>
public static class Synth
{
    /// <summary>Samples per second of the generated audio.</summary>
    public const int SampleRate = 22050;

    private const int _wavHeaderBytes = 44;
    private const float _silence = 0.001f;

    private enum Waveform
    {
        Square,
        Triangle,
        Sawtooth
    }

    private readonly record struct Tone(
        float From,
        float To,
        float Duration,
        Waveform Wave = Waveform.Square,
        float Delay = 0,
        float Volume = 0.08f);

    /// <summary>
    /// Renders one sound effect.
    /// </summary>
    /// <param name="sound">Effect to render.</param>
    /// <returns>PCM samples at <see cref="SampleRate"/>.</returns>
    public static short[] Render(Sound sound) => Mix(Tones(sound).ToList());

    /// <summary>
    /// Wraps PCM samples in a WAV container.
    /// </summary>
    /// <param name="samples">16-bit mono samples at <see cref="SampleRate"/>.</param>
    /// <returns>A complete WAV file.</returns>
    public static byte[] ToWav(short[] samples)
    {
        int dataBytes = samples.Length * sizeof(short);
        using var stream = new MemoryStream(_wavHeaderBytes + dataBytes);
        using var writer = new BinaryWriter(stream);
        writer.Write("RIFF"u8);
        writer.Write(_wavHeaderBytes - 8 + dataBytes);
        writer.Write("WAVEfmt "u8);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)1);
        writer.Write(SampleRate);
        writer.Write(SampleRate * sizeof(short));
        writer.Write((short)sizeof(short));
        writer.Write((short)16);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        foreach (short sample in samples)
        {
            writer.Write(sample);
        }

        return stream.ToArray();
    }

    private static IEnumerable<Tone> Tones(Sound sound) => sound switch
    {
        Sound.Start => Melody([523, 659, 784, 1047], 0.1f),
        Sound.Jump => [new(300, 600, 0.06f, Volume: 0.04f)],
        Sound.Fall => [new(900, 60, 1.2f, Waveform.Triangle, Volume: 0.1f)],
        Sound.Curse => Curse(),
        Sound.Disk => Melody([392, 494, 587, 784, 988, 1175], 0.08f, Waveform.Triangle),
        Sound.Catch => Melody([880, 1320], 0.08f),
        Sound.CoilyFall => [new(700, 40, 1.4f, Waveform.Sawtooth, Volume: 0.07f)],
        Sound.Hatch => [new(120, 80, 0.25f, Waveform.Sawtooth, Volume: 0.06f)],
        Sound.RoundComplete => Melody([523, 587, 659, 698, 784, 880, 988, 1047], 0.09f, Waveform.Triangle),
        Sound.ExtraLife => Melody([1047, 1319, 1568, 2093], 0.07f),
        Sound.GameOver => Melody([392, 330, 262, 196], 0.25f, Waveform.Triangle),
        _ => throw new ArgumentOutOfRangeException(nameof(sound), sound, "No synth recipe for this sound")
    };

    private static IEnumerable<Tone> Melody(float[] notes, float step, Waveform wave = Waveform.Square) =>
        notes.Select((note, index) => new Tone(note, note, step, wave, index * step));

    /// <summary>
    /// Q*bert's gibberish: a burst of random chirps.
    /// </summary>
    /// <remarks>ponytail: fixed seed renders one curse at startup; render per play if variety matters.</remarks>
    /// <returns>The chirps.</returns>
    private static IEnumerable<Tone> Curse()
    {
        var random = new Random(1982);
        for (int i = 0; i < 10; i++)
        {
            float pitch = 200 + random.NextSingle() * 700;
            yield return new Tone(pitch, pitch * (0.6f + random.NextSingle() * 0.8f), 0.07f, Waveform.Sawtooth, i * 0.07f, 0.06f);
        }
    }

    private static short[] Mix(List<Tone> tones)
    {
        var mix = new float[(int)(tones.Max(tone => tone.Delay + tone.Duration) * SampleRate) + 1];
        foreach (var tone in tones)
        {
            AddTone(mix, tone);
        }

        return mix.Select(value => (short)(Math.Clamp(value, -1f, 1f) * short.MaxValue)).ToArray();
    }

    /// <summary>
    /// Adds one oscillator with exponential pitch and volume ramps, like Web Audio's <c>exponentialRampToValueAtTime</c>.
    /// </summary>
    /// <param name="mix">Buffer to add into.</param>
    /// <param name="tone">Tone to render.</param>
    private static void AddTone(float[] mix, Tone tone)
    {
        int start = (int)(tone.Delay * SampleRate);
        int length = (int)(tone.Duration * SampleRate);
        double phase = 0;
        for (int i = 0; i < length && start + i < mix.Length; i++)
        {
            float progress = i / (float)length;
            float frequency = tone.From * MathF.Pow(tone.To / tone.From, progress);
            float volume = tone.Volume * MathF.Pow(_silence / tone.Volume, progress);
            phase = (phase + frequency / SampleRate) % 1.0;
            mix[start + i] += volume * Oscillate(tone.Wave, (float)phase);
        }
    }

    private static float Oscillate(Waveform wave, float phase) => wave switch
    {
        Waveform.Square => phase < 0.5f ? 1f : -1f,
        Waveform.Triangle => 1f - 4f * MathF.Abs(phase - 0.5f),
        _ => 2f * phase - 1f
    };
}
