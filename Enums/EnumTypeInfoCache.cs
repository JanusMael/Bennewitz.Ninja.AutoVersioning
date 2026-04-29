using System;
using System.Collections.Concurrent;

namespace Bennewitz.Ninja.AutoVersioning.SourceGenerators;

public sealed class EnumTypeInfoCache
{
    private readonly ConcurrentDictionary<Type, EnumTypeInfo> _cache;
    private readonly Func<Type, bool>? _isValidForCache;

    public EnumTypeInfoCache(Func<Type, bool>? isValidForCache = null)
    {
        _cache = new();
        _isValidForCache = isValidForCache;
    }

    public EnumTypeInfo GetOrAdd(Type enumType)
    {
        if (!_cache.TryGetValue(enumType, out var enumTypeInfo))
        {
            _isValidForCache?.Invoke(enumType);

            //this throws if the incoming is not derived from Enum
            enumTypeInfo = EnumTypeInfo.FromEnum(enumType);

            _cache.TryAdd(enumType, enumTypeInfo);
        }

        return enumTypeInfo;
    }
}