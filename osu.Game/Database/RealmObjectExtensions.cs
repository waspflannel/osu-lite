// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Input.Bindings;
using osu.Game.Models;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using Realms;

namespace osu.Game.Database
{
    public static class RealmObjectExtensions
    {
        /// <summary>
        /// Create a detached copy of the each item in the collection.
        /// </summary>
        /// <remarks>
        /// Items which are already detached (ie. not managed by realm) will not be modified.
        /// </remarks>
        /// <param name="items">A list of managed <see cref="RealmObject"/>s to detach.</param>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <returns>A list containing non-managed copies of provided items.</returns>
        public static List<T> Detach<T>(this IEnumerable<T> items) where T : RealmObjectBase
        {
            var list = new List<T>();

            foreach (var obj in items)
                list.Add(obj.Detach());

            return list;
        }

        /// <summary>
        /// Create a detached copy of the item.
        /// </summary>
        /// <remarks>
        /// If the item if already detached (ie. not managed by realm) it will not be detached again and the original instance will be returned. This allows this method to be potentially called at multiple levels while only incurring the clone overhead once.
        /// </remarks>
        /// <param name="item">The managed <see cref="RealmObject"/> to detach.</param>
        /// <typeparam name="T">The type of object.</typeparam>
        /// <returns>A non-managed copy of provided item. Will return the provided item if already detached.</returns>
        public static T Detach<T>(this T item) where T : RealmObjectBase
        {
            if (!item.IsManaged)
                return item;

            return (T)detachObject(item);
        }

        private static RealmObjectBase detachObject(RealmObjectBase item)
        {
            switch (item)
            {
                case BeatmapSetInfo beatmapSet:
                    return detachBeatmapSet(beatmapSet);

                case BeatmapInfo beatmap:
                    return detachBeatmap(beatmap);

                case ScoreInfo score:
                    return detachScore(score);

                case BeatmapMetadata metadata:
                    return detachMetadata(metadata);

                case BeatmapDifficulty difficulty:
                    return detachDifficulty(difficulty);

                case BeatmapUserSettings userSettings:
                    return detachUserSettings(userSettings);

                case RulesetInfo ruleset:
                    return detachRuleset(ruleset);

                case RealmFile file:
                    return detachRealmFile(file);

                case RealmNamedFileUsage fileUsage:
                    return detachFileUsage(fileUsage);

                case RealmKeyBinding keyBinding:
                    return detachKeyBinding(keyBinding);

                default:
                    return shallowClone((RealmObject)item);
            }
        }

        private static BeatmapSetInfo detachBeatmapSet(BeatmapSetInfo source)
        {
            var result = new BeatmapSetInfo(null)
            {
                ID = source.ID,
                DateAdded = source.DateAdded,
                DeletePending = source.DeletePending,
                Hash = source.Hash,
                Protected = source.Protected,
            };

            foreach (var b in source.Beatmaps)
            {
                var detachedBeatmap = detachBeatmap(b, false);
                detachedBeatmap.BeatmapSet = result;
                result.Beatmaps.Add(detachedBeatmap);
            }

            foreach (var f in source.Files)
                result.Files.Add(detachFileUsage(f));

            return result;
        }

        private static BeatmapInfo detachBeatmap(BeatmapInfo source, bool detachParent = true)
        {
            var result = new BeatmapInfo(
                detachRuleset(source.Ruleset),
                detachDifficulty(source.Difficulty),
                detachMetadata(source.Metadata))
            {
                ID = source.ID,
                DifficultyName = source.DifficultyName,
                Length = source.Length,
                BPM = source.BPM,
                Hash = source.Hash,
                MD5Hash = source.MD5Hash,
                StarRating = source.StarRating,
                Hidden = source.Hidden,
                EndTimeObjectCount = source.EndTimeObjectCount,
                TotalObjectCount = source.TotalObjectCount,
                LastPlayed = source.LastPlayed,
                BeatDivisor = source.BeatDivisor,
            };

            var userSettings = new BeatmapUserSettings();
            copyProperties(source.UserSettings, userSettings);
            result.UserSettings = userSettings;

            if (detachParent && source.BeatmapSet != null)
            {
                var set = detachBeatmapSet(source.BeatmapSet);

                for (int i = 0; i < set.Beatmaps.Count; i++)
                {
                    if (set.Beatmaps[i].Equals(result))
                    {
                        set.Beatmaps[i] = result;
                        break;
                    }
                }

                result.BeatmapSet = set;
            }

            return result;
        }

        private static ScoreInfo detachScore(ScoreInfo source)
        {
            var result = new ScoreInfo
            {
                ID = source.ID,
                Ruleset = detachRuleset(source.Ruleset),
                BeatmapHash = source.BeatmapHash,
                ClientVersion = source.ClientVersion,
                Hash = source.Hash,
                DeletePending = source.DeletePending,
                TotalScore = source.TotalScore,
                TotalScoreVersion = source.TotalScoreVersion,
                LegacyTotalScore = source.LegacyTotalScore,
                BackgroundReprocessingFailed = source.BackgroundReprocessingFailed,
                MaxCombo = source.MaxCombo,
                Accuracy = source.Accuracy,
                Date = source.Date,
                PP = source.PP,
                StatisticsJson = source.StatisticsJson,
                MaximumStatisticsJson = source.MaximumStatisticsJson,
                RankInt = source.RankInt,
                IsLegacyScore = source.IsLegacyScore,
                Combo = source.Combo,
                Passed = source.Passed,
            };

            foreach (var p in source.Pauses)
                result.Pauses.Add(p);

            foreach (var f in source.Files)
                result.Files.Add(detachFileUsage(f));

            if (source.BeatmapInfo != null)
                result.BeatmapInfo = detachBeatmap(source.BeatmapInfo);

            return result;
        }

        private static BeatmapMetadata detachMetadata(BeatmapMetadata source)
        {
            var result = new BeatmapMetadata();
            copyProperties(source, result);
            return result;
        }

        private static BeatmapDifficulty detachDifficulty(BeatmapDifficulty source)
        {
            var result = new BeatmapDifficulty();
            copyProperties(source, result);
            return result;
        }

        private static BeatmapUserSettings detachUserSettings(BeatmapUserSettings source)
        {
            var result = new BeatmapUserSettings();
            copyProperties(source, result);
            return result;
        }

        private static RulesetInfo detachRuleset(RulesetInfo source)
        {
            return new RulesetInfo
            {
                ShortName = source.ShortName,
                Name = source.Name,
                LastAppliedDifficultyVersion = source.LastAppliedDifficultyVersion,
            };
        }

        private static RealmFile detachRealmFile(RealmFile source)
        {
            var result = new RealmFile();
            copyProperties(source, result);
            return result;
        }

        private static RealmNamedFileUsage detachFileUsage(RealmNamedFileUsage source)
        {
            var result = new RealmNamedFileUsage(detachRealmFile(source.File!), source.Filename);
            return result;
        }

        private static RealmKeyBinding detachKeyBinding(RealmKeyBinding source)
        {
            var result = (RealmKeyBinding)Activator.CreateInstance(typeof(RealmKeyBinding), true)!;
            copyProperties(source, result);
            return result;
        }

        /// <summary>
        /// Copy changes in a detached beatmap back to realm.
        /// </summary>
        /// <param name="source">The detached beatmap to copy from.</param>
        /// <param name="destination">The live beatmap to copy to.</param>
        public static void CopyChangesToRealm(this BeatmapSetInfo source, BeatmapSetInfo destination)
            => copyChangesToRealm(source, destination);

        private static void copyChangesToRealm<T>(T source, T destination) where T : RealmObjectBase
        {
            switch (source)
            {
                case BeatmapSetInfo s when destination is BeatmapSetInfo d:
                    copyBeatmapSetChanges(s, d);
                    break;

                case BeatmapInfo s when destination is BeatmapInfo d:
                    copyBeatmapChanges(s, d);
                    break;

                default:
                    copyProperties(source, destination);
                    break;
            }
        }

        private static void copyBeatmapSetChanges(BeatmapSetInfo source, BeatmapSetInfo destination)
        {
            copyProperties(source, destination);

            foreach (var beatmap in source.Beatmaps)
            {
                var existingBeatmap = destination.Realm!.Find<BeatmapInfo>(beatmap.ID);

                if (existingBeatmap != null)
                {
                    if (!destination.Beatmaps.Contains(existingBeatmap))
                    {
                        System.Diagnostics.Debug.Fail("Beatmaps should never become detached under normal circumstances.");
                        Logger.Log("WARNING: One of the difficulties in a beatmap was detached from its set.", LoggingTarget.Database, LogLevel.Important);
                        destination.Beatmaps.Add(existingBeatmap);
                    }

                    copyProperties(beatmap, existingBeatmap);
                    copyProperties(beatmap.Difficulty, existingBeatmap.Difficulty);
                    copyProperties(beatmap.Metadata, existingBeatmap.Metadata);
                }
                else
                {
                    var newBeatmap = new BeatmapInfo
                    {
                        ID = beatmap.ID,
                        BeatmapSet = destination,
                        Ruleset = destination.Realm.Find<RulesetInfo>(beatmap.Ruleset.ShortName)!
                    };

                    destination.Beatmaps.Add(newBeatmap);
                    copyProperties(beatmap, newBeatmap);
                    copyProperties(beatmap.Difficulty, newBeatmap.Difficulty);
                    copyProperties(beatmap.Metadata, newBeatmap.Metadata);
                }
            }
        }

        private static void copyBeatmapChanges(BeatmapInfo source, BeatmapInfo destination)
        {
            copyProperties(source, destination);

            if (destination.Realm != null)
                destination.Ruleset = destination.Realm.Find<RulesetInfo>(source.Ruleset.ShortName)!;

            copyProperties(source.Difficulty, destination.Difficulty);
            copyProperties(source.Metadata, destination.Metadata);
        }

        private static void copyProperties(RealmObjectBase source, RealmObjectBase destination)
        {
            foreach (var prop in source.GetType().GetProperties())
            {
                if (!prop.CanWrite || !prop.CanRead) continue;
                if (prop.GetMethod?.IsPublic != true || prop.SetMethod?.IsPublic != true) continue;

                if (prop.GetCustomAttributes(typeof(IgnoredAttribute), false).Any()) continue;
                if (prop.GetCustomAttributes(typeof(BacklinkAttribute), false).Any()) continue;
                if (prop.GetCustomAttributes(typeof(IgnoreDataMemberAttribute), false).Any()) continue;
                if (prop.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false).Any()) continue;

                try
                {
                    prop.SetValue(destination, prop.GetValue(source));
                }
                catch
                {
                }
            }
        }

        private static RealmObjectBase shallowClone(RealmObject source)
        {
            var type = source.GetType();
            var result = (RealmObject)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
            copyProperties(source, result);
            return result;
        }

        public static List<Live<T>> ToLiveUnmanaged<T>(this IEnumerable<T> realmList)
            where T : RealmObject, IHasGuidPrimaryKey
        {
            return realmList.Select(l => new RealmLiveUnmanaged<T>(l)).Cast<Live<T>>().ToList();
        }

        public static Live<T> ToLiveUnmanaged<T>(this T realmObject)
            where T : RealmObject, IHasGuidPrimaryKey
        {
            return new RealmLiveUnmanaged<T>(realmObject);
        }

        public static Live<T> ToLive<T>(this T realmObject, RealmAccess realm)
            where T : RealmObject, IHasGuidPrimaryKey
        {
            return new RealmLive<T>(realmObject, realm);
        }

#pragma warning disable RS0030
        public static IDisposable QueryAsyncWithNotifications<T>(this IRealmCollection<T> collection, NotificationCallbackDelegate<T> callback)
            where T : RealmObjectBase
        {
            if (!RealmAccess.CurrentThreadSubscriptionsAllowed)
                throw new InvalidOperationException($"Make sure to call {nameof(RealmAccess)}.{nameof(RealmAccess.RegisterForNotifications)}");

            bool initial = true;
            return collection.SubscribeForNotifications((sender, changes) =>
            {
                if (initial)
                {
                    initial = false;

                    if (changes != null)
                    {
                        callback(sender, null);
                        return;
                    }
                }

                callback(sender, changes);
            });
        }
#pragma warning restore RS0030

        public static IDisposable? QueryAsyncWithNotifications<T>(this IQueryable<T> list, NotificationCallbackDelegate<T> callback)
            where T : RealmObjectBase
        {
            if (!(list is IRealmCollection<T> realmCollection))
                return null;

            return QueryAsyncWithNotifications(realmCollection, callback);
        }

        public static IDisposable? QueryAsyncWithNotifications<T>(this IList<T> list, NotificationCallbackDelegate<T> callback)
            where T : RealmObjectBase
        {
            if (!(list is IRealmCollection<T> realmCollection))
                return null;

            return QueryAsyncWithNotifications(realmCollection, callback);
        }
    }
}
