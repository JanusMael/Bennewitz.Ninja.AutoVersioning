using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[DebuggerDisplay("{DebuggerDisplay}")]
public sealed class EnumValue : IEquatable<EnumValue>, IComparable<EnumValue>, IComparable
{
    public Type Type { get; }
    public string Name { get; }
    public Enum Value { get; }

    public bool IsFlag { get; }

    public string QualifiedName => $"{Type.Name}.{Name}";

    public Attribute[] Attributes { get; }

    public static EnumValue From<T>(T enumValue, bool? isFlag = null)
        where T : struct, Enum =>
        From((Enum) enumValue, isFlag);

    public static EnumValue From(Enum enumValue, bool? isFlag = null)
    {
        var enumType = enumValue.GetType();
        if (isFlag == null)
        {
            isFlag = enumType.HasFlagsAttribute();
        }
        var name = Enum.GetName(enumType, enumValue) ?? enumValue.ToString();
        return new EnumValue(enumType, name, enumValue, isFlag.Value);
    }

    private EnumValue(Type type, string name, Enum value, bool isFlag)
    {
        Type = type;
        Name = name;
        Value = value;
        IsFlag = isFlag;

        var fieldInfo = type.GetField(name);
        Attributes = fieldInfo?.GetCustomAttributes(inherit: false).Cast<Attribute>().ToArray() ?? Array.Empty<Attribute>();
    }

    public override string ToString() => Name;

    private string DebuggerDisplay => $"{QualifiedName}{(IsFlag ? " [bit flag]" : string.Empty)}";

    #region IEquatable
    public bool Equals(EnumValue? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ReferenceEquals(Type, other.Type) &&
               string.Equals(Name, other.Name, StringComparison.InvariantCulture) &&
               Value.Equals(other.Value);
    }

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) ||
        obj is EnumValue other && Equals(other);

    public override int GetHashCode()
    {
#if NETSTANDARD
        var hashCode = new HashCode();
        hashCode.Add(Type);
        hashCode.Add(Name, StringComparer.InvariantCulture);
        hashCode.Add(Value);
        return hashCode.ToHashCode();
#else
        return HashCodeUtility.GetCompositeHashCode(Type, Name, Value);
#endif
    }

    public static bool operator ==(EnumValue? left, EnumValue? right) =>
        Equals(left, right);

    public static bool operator !=(EnumValue? left, EnumValue? right) =>
        !Equals(left, right);
    #endregion

    #region IComparable
    public int CompareTo(EnumValue? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        if (ReferenceEquals(Type, other.Type))
        {
            var valueComparison = Value.CompareTo(other.Value);
            return valueComparison;
        }
        return StringComparer.InvariantCulture.Compare(
            Type.AssemblyQualifiedName,
            other.Type.AssemblyQualifiedName);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is EnumValue other ?
            CompareTo(other) :
            throw new ArgumentException(
                $"{nameof(Object)} must be of {nameof(System.Type)} {nameof(EnumValue)}");
    }

#pragma warning disable CS8604
    public static bool operator <(EnumValue? left, EnumValue? right) =>
        Comparer<EnumValue>.Default.Compare(left, right) < 0;

    public static bool operator >(EnumValue? left, EnumValue? right) =>
        Comparer<EnumValue>.Default.Compare(left, right) > 0;

    public static bool operator <=(EnumValue? left, EnumValue? right) =>
        Comparer<EnumValue>.Default.Compare(left, right) <= 0;

    public static bool operator >=(EnumValue? left, EnumValue? right) =>
        Comparer<EnumValue>.Default.Compare(left, right) >= 0;
#pragma warning restore CS8604
    #endregion
}