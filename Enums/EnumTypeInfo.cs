using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[DebuggerDisplay("{DebuggerDisplay}")]
public sealed class EnumTypeInfo : IEquatable<EnumTypeInfo>, IComparable<EnumTypeInfo>, IComparable
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public const long DefaultUnderlyingValue = 0;

    public Type Type { get; }
    public Type UnderlyingType { get; }
    public ImmutableArray<EnumValue> Values { get; }

    public EnumValue Default { get; }

    public bool HasFlagsAttribute { get; }

    public string Name => Type.Name;

    public string? Namespace => Type.Namespace;

    public bool HasValues => Values.Length > 0;

    public Attribute[] Attributes { get; }

    public static EnumTypeInfo FromEnum<T>()
        where T : struct, Enum =>
        FromEnum(typeof(T));

    public static EnumTypeInfo FromEnum(Type enumType)
    {
        if (!enumType.IsEnum)
        {
            throw new ArgumentException(
                $"'{nameof(enumType)}' must be a type derived from '{nameof(Enum)}'");
        }
        var underlyingTypeOfT = Enum.GetUnderlyingType(enumType);
        var enumValues = Enum.GetValues(enumType);

        var result = new EnumTypeInfo(enumType, underlyingTypeOfT, enumValues.ToArray<Enum>()!);
        return result;
    }

    private EnumTypeInfo(Type enumType, Type underlyingType, Enum[] enumValues)
    {
        Type = enumType;
        UnderlyingType = underlyingType;

        var hasFlagsAttribute = enumType.HasFlagsAttribute();
        HasFlagsAttribute = hasFlagsAttribute;

        Values = enumValues.Select(x => EnumValue.From(x, hasFlagsAttribute)).ToImmutableArray();

        var defaultValue = Values.FirstOrDefault(x => Convert.ToInt64(x.Value).Equals(DefaultUnderlyingValue));

        if (defaultValue == null)
        {
            var defaultUnderlyingValue = DefaultUnderlyingValue.ChangeType(underlyingType);
            var defaultValueAsEnum = defaultUnderlyingValue.ChangeType(enumType);
            defaultValue = EnumValue.From((Enum) defaultValueAsEnum);
        }

        Default = defaultValue;

        Attributes = enumType.GetCustomAttributes(inherit: false).Cast<Attribute>().ToArray();
    }

    public override string ToString() => Type.FullName ?? Name;

    private string DebuggerDisplay => $"{Type.FullName} ValueCount={Values.Length} Default={Default}{(HasFlagsAttribute ? " [bit flags]" : string.Empty)}";

    #region IEquatable
    public bool Equals(EnumTypeInfo? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return ReferenceEquals(Type, other.Type);
    }

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || obj is EnumTypeInfo other && Equals(other);

    public override int GetHashCode() =>

#if NETSTANDARD
        HashCode.Combine(Type);
#else
        HashCodeUtility.GetCompositeHashCode(Type);
#endif

    public static bool operator ==(EnumTypeInfo? left, EnumTypeInfo? right) =>
        Equals(left, right);

    public static bool operator !=(EnumTypeInfo? left, EnumTypeInfo? right) =>
        !Equals(left, right);
    #endregion

    #region IComparable

    public int CompareTo(EnumTypeInfo? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        if (ReferenceEquals(Type, other.Type))
        {
            return 0;
        }
        return StringComparer.InvariantCulture.Compare(
            Type.AssemblyQualifiedName,
            other.Type.AssemblyQualifiedName);
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is EnumTypeInfo other ?
            CompareTo(other) :
            throw new ArgumentException($"{nameof(Object)} must be of {Type} {nameof(EnumTypeInfo)}");
    }

#pragma warning disable CS8604
    public static bool operator <(EnumTypeInfo? left, EnumTypeInfo? right) =>
        Comparer<EnumTypeInfo>.Default.Compare(left, right) < 0;

    public static bool operator >(EnumTypeInfo? left, EnumTypeInfo? right) =>
        Comparer<EnumTypeInfo>.Default.Compare(left, right) > 0;

    public static bool operator <=(EnumTypeInfo? left, EnumTypeInfo? right) =>
        Comparer<EnumTypeInfo>.Default.Compare(left, right) <= 0;

    public static bool operator >=(EnumTypeInfo? left, EnumTypeInfo? right) =>
        Comparer<EnumTypeInfo>.Default.Compare(left, right) >= 0;
#pragma warning restore CS8604
    #endregion
}