/*
 * This file is largely based upon
 * https://github.com/Lordfirespeed/NuGet-GameLib-Dehumidifier/blob/20ec05e222b60cee6d6411116a1df5f42ee5d874/build/schema/EntriesDictionaryJsonConverter.cs
 * Copyright (c) 2024 Joe Clack
 * Joe Clack licenses the referenced file to the Sigurd Team under the GPL-3.0-OR-LATER license.
 *
 * Copyright (c) 2024 Sigurd Team
 * The Sigurd Team licenses this file to you under the GPL-3.0-OR-LATER license.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Build.Schema;

// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/converters-how-to?pivots=dotnet-8-0
public abstract class EntriesDictionaryJsonConverter<TDictionary, TKey, TValue> : JsonConverterFactory
    where TDictionary : IDictionary<TKey, TValue>
    where TKey: notnull
{
    public abstract TKey KeyForValue(TValue value);

    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(TDictionary).IsAssignableFrom(typeToConvert);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        JsonConverter converter = (JsonConverter)Activator.CreateInstance(
            typeof(EntriesDictionaryJsonConverterInner),
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            args: new object[] { options, (Delegate)KeyForValue },
            culture: null)!;

        return converter;
    }

    private class EntriesDictionaryJsonConverterInner : JsonConverter<TDictionary>
    {
        private readonly JsonConverter<IEnumerable<TValue>> _enumerableConverter;
        private readonly Type _valueEnumerableType;
        private readonly Func<TValue, TKey> _keyForValue;

        public EntriesDictionaryJsonConverterInner(JsonSerializerOptions options, Func<TValue, TKey> keyForValue)
        {
            // For performance, use the existing converter.
            _enumerableConverter = (JsonConverter<IEnumerable<TValue>>)options
                .GetConverter(typeof(IEnumerable<TValue>));

            _valueEnumerableType = typeof(IEnumerable<TValue>);
            _keyForValue = keyForValue;
        }

        public override TDictionary? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var sourceArray = _enumerableConverter.Read(ref reader, _valueEnumerableType, options);

            var dictionary = (TDictionary)Activator.CreateInstance(typeof(TDictionary))!;

            foreach (var value in sourceArray!)
            {
               dictionary.Add(_keyForValue(value), value);
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, TDictionary value, JsonSerializerOptions options)
        {
            _enumerableConverter.Write(writer, value.Values, options);
        }
    }
}
