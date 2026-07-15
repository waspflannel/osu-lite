// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.

using System;
using System.Collections.Generic;
using System.Linq;
using MessagePack;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Extensions;

namespace osu.Game.Rulesets.Mods
{
    [MessagePackObject]
    public class SerialisedMod : IEquatable<SerialisedMod>
    {
        [JsonProperty("acronym")]
        [Key(0)]
        public string Acronym { get; set; } = string.Empty;

        [JsonProperty("settings")]
        [Key(1)]
        [MessagePackFormatter(typeof(ModSettingsDictionaryFormatter))]
        public Dictionary<string, object> Settings { get; set; } = new Dictionary<string, object>();

        [JsonConstructor]
        [SerializationConstructor]
        public SerialisedMod()
        {
        }

        public SerialisedMod(Mod mod)
        {
            Acronym = mod.Acronym;

            foreach (var (_, property) in mod.GetSettingsSourceProperties())
            {
                var bindable = (IBindable)property.GetValue(mod)!;

                if (!bindable.IsDefault)
                    Settings.Add(property.Name.ToSnakeCase(), bindable.GetUnderlyingSettingValue());
            }
        }

        public bool Equals(SerialisedMod? other) => other != null && Acronym == other.Acronym && Settings.SequenceEqual(other.Settings, ModSettingsEqualityComparer.Default);

        private class ModSettingsEqualityComparer : IEqualityComparer<KeyValuePair<string, object>>
        {
            public static ModSettingsEqualityComparer Default { get; } = new ModSettingsEqualityComparer();

            public bool Equals(KeyValuePair<string, object> x, KeyValuePair<string, object> y) => x.Key == y.Key && EqualityComparer<object>.Default.Equals(x.Value.GetUnderlyingSettingValue(), y.Value.GetUnderlyingSettingValue());

            public int GetHashCode(KeyValuePair<string, object> obj) => HashCode.Combine(obj.Key, obj.Value.GetUnderlyingSettingValue());
        }
    }
}
