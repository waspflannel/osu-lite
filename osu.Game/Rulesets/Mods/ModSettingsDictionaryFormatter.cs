// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.

using System.Buffers;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using MessagePack.Formatters;
using osu.Game.Configuration;

namespace osu.Game.Rulesets.Mods
{
    public class ModSettingsDictionaryFormatter : IMessagePackFormatter<Dictionary<string, object>?>
    {
        public void Serialize(ref MessagePackWriter writer, Dictionary<string, object>? value, MessagePackSerializerOptions options)
        {
            if (value == null) return;

            writer.WriteArrayHeader(value.Count);

            foreach (var kvp in value)
            {
                var stringBytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(kvp.Key));
                writer.WriteString(in stringBytes);
                PrimitiveObjectFormatter.Instance.Serialize(ref writer, kvp.Value.GetUnderlyingSettingValue(), options);
            }
        }

        public Dictionary<string, object> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var output = new Dictionary<string, object>();

            for (int i = 0, count = reader.ReadArrayHeader(); i < count; i++)
                output[reader.ReadString()!] = PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options)!;

            return output;
        }
    }
}
