namespace Core;

/// <summary>
/// The library the other three reference. A body edit here is the case that separates a held
/// stamp from a per-build one: under a held stamp this project's reference assembly does not
/// change, so nothing above it recompiles.
/// </summary>
public static class Numbers
{
    public static int Double(int value) => value * 2;
}
