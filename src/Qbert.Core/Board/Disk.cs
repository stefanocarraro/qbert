namespace Qbert.Core.Board;

/// <summary>
/// Spinning escape disk floating beside the pyramid; carries Q*bert back to the apex once.
/// </summary>
/// <param name="cell">Off-pyramid cell the disk occupies.</param>
public sealed class Disk(Cell cell)
{
    /// <summary>Gets the off-pyramid cell the disk occupies.</summary>
    public Cell Cell { get; } = cell;

    /// <summary>Gets or sets a value indicating whether the disk was already ridden this round.</summary>
    public bool Used { get; set; }
}
