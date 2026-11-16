namespace Leaf;

/// <summary>
/// Nothing references this project, so a body edit here must recompile exactly one project.
/// </summary>
public static class Helper
{
    public static int Quadruple(int value) => Model.Record.Twice(Core.Numbers.Double(value));
}
