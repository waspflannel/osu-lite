// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Database;

namespace osu.Game.Rulesets
{
    /// <summary>
    /// The application's one bundled ruleset. Its factory is supplied by Desktop to preserve the project dependency direction.
    /// </summary>
    public sealed class FixedRulesetStore : IRulesetStore
    {
        public RulesetInfo RulesetInfo { get; private set; } = null!;

        public IEnumerable<RulesetInfo> AvailableRulesets
        {
            get
            {
                yield return RulesetInfo;
            }
        }

        public FixedRulesetStore(RealmAccess realmAccess, Func<Ruleset> createRuleset)
        {
            ArgumentNullException.ThrowIfNull(realmAccess);
            ArgumentNullException.ThrowIfNull(createRuleset);

            var ruleset = createRuleset();
            RulesetInfo.SetFactory(createRuleset);

            realmAccess.Write(realm =>
            {
                var storedRuleset = realm.Find<RulesetInfo>(ruleset.RulesetInfo.ShortName);

                if (storedRuleset == null)
                {
                    storedRuleset = ruleset.RulesetInfo.Clone();
                    realm.Add(storedRuleset);
                }
                else
                {
                    storedRuleset.Name = ruleset.RulesetInfo.Name;
                }

                RulesetInfo = storedRuleset.Clone();
            });
        }

        public RulesetInfo? GetRuleset(int id) => RulesetInfo;

        public RulesetInfo? GetRuleset(string shortName) => string.Equals(shortName, RulesetInfo.ShortName, StringComparison.Ordinal) ? RulesetInfo : null;

        IRulesetInfo? IRulesetStore.GetRuleset(int id) => GetRuleset(id);
        IRulesetInfo? IRulesetStore.GetRuleset(string shortName) => GetRuleset(shortName);
        IEnumerable<IRulesetInfo> IRulesetStore.AvailableRulesets => AvailableRulesets;
    }
}
