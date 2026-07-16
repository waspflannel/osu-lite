// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.IO.Archives;
using osu.Game.Localisation;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Skinning;
using osu.Game.Storyboards;
using osu.Game.Utils;
using Realms;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Handles general operations related to global beatmap management.
    /// </summary>
    public class BeatmapManager : ModelManager<BeatmapSetInfo>, IModelImporter<BeatmapSetInfo>, IWorkingBeatmapCache
    {
        public ITrackStore BeatmapTrackStore { get; }

        private readonly BeatmapImporter beatmapImporter;

        private readonly WorkingBeatmapCache workingBeatmapCache;

        public override bool PauseImports
        {
            get => base.PauseImports;
            set
            {
                base.PauseImports = value;
                beatmapImporter.PauseImports = value;
            }
        }

        public BeatmapManager(Storage storage, RealmAccess realm, AudioManager audioManager, IResourceStore<byte[]> gameResources, GameHost? host = null,
                              WorkingBeatmap? defaultBeatmap = null)
            : base(storage, realm)
        {
            var userResources = new RealmFileStore(realm, storage).Store;

            BeatmapTrackStore = audioManager.GetTrackStore(userResources);

            beatmapImporter = CreateBeatmapImporter(storage, realm);
            beatmapImporter.PostNotification = obj => PostNotification?.Invoke(obj);

            workingBeatmapCache = CreateWorkingBeatmapCache(audioManager, gameResources, userResources, defaultBeatmap, host);
        }

        protected virtual WorkingBeatmapCache CreateWorkingBeatmapCache(AudioManager audioManager, IResourceStore<byte[]> resources, IResourceStore<byte[]> storage, WorkingBeatmap? defaultBeatmap,
                                                                        GameHost? host)
        {
            return new WorkingBeatmapCache(BeatmapTrackStore, audioManager, resources, storage, defaultBeatmap, host, Realm);
        }

        protected virtual BeatmapImporter CreateBeatmapImporter(Storage storage, RealmAccess realm) => new BeatmapImporter(storage, realm);

        /// <summary>
        /// Create a new beatmap set, backed by a <see cref="BeatmapSetInfo"/> model,
        /// with a single difficulty which is backed by a <see cref="BeatmapInfo"/> model
        /// and represented by the returned usable <see cref="WorkingBeatmap"/>.
        /// </summary>
        public WorkingBeatmap CreateNew(RulesetInfo ruleset, string creator)
        {
            var metadata = new BeatmapMetadata
            {
                Creator = creator,
            };

            var beatmapSet = new BeatmapSetInfo
            {
                DateAdded = DateTimeOffset.UtcNow,
                Beatmaps =
                {
                    new BeatmapInfo(ruleset, new BeatmapDifficulty(), metadata)
                }
            };

            foreach (BeatmapInfo b in beatmapSet.Beatmaps)
                b.BeatmapSet = beatmapSet;

            var imported = beatmapImporter.ImportModel(beatmapSet);

            if (imported == null)
                throw new InvalidOperationException("Failed to import new beatmap");

            return imported.PerformRead(s => GetWorkingBeatmap(s.Beatmaps.First()));
        }

        public bool Hide(BeatmapInfo beatmapInfo)
        {
            return Realm.Run(r =>
            {
                using (var transaction = r.BeginWrite())
                {
                    if (!beatmapInfo.IsManaged)
                        beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

                    if (!CanHide(beatmapInfo))
                        return false;

                    beatmapInfo.Hidden = true;
                    transaction.Commit();
                    return true;
                }
            });
        }

        public bool CanHide(BeatmapInfo beatmapInfo) => Realm.Run(r =>
        {
            if (!beatmapInfo.IsManaged)
                beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

            return beatmapInfo.BeatmapSet!.Beatmaps.Count(b => !b.Hidden) > 1;
        });

        /// <summary>
        /// Restore a beatmap difficulty.
        /// </summary>
        /// <param name="beatmapInfo">The beatmap difficulty to restore.</param>
        public void Restore(BeatmapInfo beatmapInfo)
        {
            Realm.Run(r =>
            {
                using (var transaction = r.BeginWrite())
                {
                    if (!beatmapInfo.IsManaged)
                        beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

                    beatmapInfo.Hidden = false;
                    transaction.Commit();
                }
            });
        }

        public void RestoreAll()
        {
            Realm.Run(r =>
            {
                using (var transaction = r.BeginWrite())
                {
                    foreach (var beatmap in r.All<BeatmapInfo>().Where(b => b.Hidden))
                        beatmap.Hidden = false;

                    transaction.Commit();
                }
            });
        }

        /// <summary>
        /// Returns a list of all usable <see cref="BeatmapSetInfo"/>s.
        /// IMPORTANT: This should not be used outside of tests. Consider using <see cref="RealmDetachedBeatmapStore"/> instead.
        /// </summary>
        /// <returns>A list of available <see cref="BeatmapSetInfo"/>.</returns>
        public List<BeatmapSetInfo> GetAllUsableBeatmapSets()
        {
            return Realm.Run(r =>
            {
                r.Refresh();
                return r.All<BeatmapSetInfo>().Where(b => !b.DeletePending).AsEnumerable().Detach();
            });
        }

        /// <summary>
        /// Perform a lookup query on available <see cref="BeatmapSetInfo"/>s.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The first result for the provided query, or null if no results were found.</returns>
        public Live<BeatmapSetInfo>? QueryBeatmapSet(Expression<Func<BeatmapSetInfo, bool>> query)
        {
            return Realm.Run(r => r.All<BeatmapSetInfo>().FirstOrDefault(query)?.ToLive(Realm));
        }

        /// <summary>
        /// Perform a lookup query on available <see cref="BeatmapInfo"/>s.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>The first result for the provided query, or null if no results were found.</returns>
        public BeatmapInfo? QueryBeatmap(Expression<Func<BeatmapInfo, bool>> query) => Realm.Run(r =>
            r.All<BeatmapInfo>().Filter($@"{nameof(BeatmapInfo.BeatmapSet)}.{nameof(BeatmapSetInfo.DeletePending)} == false").FirstOrDefault(query)?.Detach());

        /// <summary>
        /// Perform a lookup query on available <see cref="BeatmapInfo"/>s.
        /// Use this overload instead of <see cref="QueryBeatmap(System.Linq.Expressions.Expression{System.Func{osu.Game.Beatmaps.BeatmapInfo,bool}})"/>
        /// when Realm is unable to transform an expression to the internal Realm query syntax.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="arguments">The arguments for the query.</param>
        /// <returns>The first result for the provided query, or null if no results were found.</returns>
        public BeatmapInfo? QueryBeatmap(string query, params QueryArgument[] arguments) => Realm.Run(r =>
            r.All<BeatmapInfo>()
             .Filter($@"{nameof(BeatmapInfo.BeatmapSet)}.{nameof(BeatmapSetInfo.DeletePending)} == false")
             .Filter(query, arguments)
             .FirstOrDefault()?.Detach());

        /// <summary>
        /// Perform a lookup query on available <see cref="BeatmapInfo"/>s for a specific online ID.
        /// </summary>
        /// <returns>A matching local beatmap info if existing and in a valid state.</returns>
        public BeatmapInfo? QueryOnlineBeatmapId(int id) => Realm.Run(r =>
            r.All<BeatmapInfo>()
             .ForOnlineId(id)
             // See https://github.com/ppy/osu/issues/36234 for why this isn't a SingleOrDefault().
             .FirstOrDefault()
             ?.Detach()
        );

        /// <summary>
        /// A default representation of a WorkingBeatmap to use when no beatmap is available.
        /// </summary>
        public IWorkingBeatmap DefaultBeatmap => workingBeatmapCache.DefaultBeatmap;

        public void DeleteAllVideos()
        {
            Realm.Write(r =>
            {
                var items = r.All<BeatmapSetInfo>().Where(s => !s.DeletePending && !s.Protected);
                DeleteVideos(items.ToList());
            });
        }

        public void ResetAllOffsets()
        {
            Realm.Write(r =>
            {
                var items = r.All<BeatmapInfo>();

                foreach (var beatmap in items)
                {
                    if (beatmap.UserSettings.Offset != 0)
                        beatmap.UserSettings.Offset = 0;
                }

                PostNotification?.Invoke(new ProgressCompletionNotification { Text = MaintenanceSettingsStrings.AllOffsetsReset });
            });
        }

        public void Delete(Expression<Func<BeatmapSetInfo, bool>>? filter = null, bool silent = false)
        {
            Realm.Run(r =>
            {
                var items = r.All<BeatmapSetInfo>().Where(s => !s.DeletePending && !s.Protected);

                if (filter != null)
                    items = items.Where(filter);

                Delete(items.ToList(), silent);
            });
        }

        /// <summary>
        /// Delete a beatmap difficulty immediately.
        /// </summary>
        /// <remarks>
        /// There's no undoing this operation, as we don't have a soft-deletion flag on <see cref="BeatmapInfo"/>.
        /// This may be a future consideration if there's a user requirement for undeleting support.
        /// </remarks>
        public void DeleteDifficultyImmediately(BeatmapInfo beatmapInfo)
        {
            Realm.Write(r =>
            {
                if (!beatmapInfo.IsManaged)
                    beatmapInfo = r.Find<BeatmapInfo>(beatmapInfo.ID)!;

                Debug.Assert(beatmapInfo.BeatmapSet != null);
                Debug.Assert(beatmapInfo.File != null);

                var setInfo = beatmapInfo.BeatmapSet;

                DeleteFile(setInfo, beatmapInfo.File);
                setInfo.Beatmaps.Remove(beatmapInfo);
                r.Remove(beatmapInfo.Metadata);
                r.Remove(beatmapInfo);

                updateHashAndMarkDirty(setInfo);
                workingBeatmapCache.Invalidate(setInfo);
            });
        }

        /// <summary>
        /// Delete videos from a list of beatmaps.
        /// This will post notifications tracking progress.
        /// </summary>
        public void DeleteVideos(List<BeatmapSetInfo> items, bool silent = false)
        {
            if (items.Count == 0)
            {
                if (!silent)
                    PostNotification?.Invoke(new ProgressCompletionNotification { Text = MaintenanceSettingsStrings.NoVideosFoundToDelete });
                return;
            }

            var notification = new ProgressNotification
            {
                Progress = 0,
                Text = $"Preparing to delete all {HumanisedModelName} videos...",
                CompletionText = MaintenanceSettingsStrings.NoVideosFoundToDelete,
                State = ProgressNotificationState.Active,
            };

            if (!silent)
                PostNotification?.Invoke(notification);

            int i = 0;
            int deleted = 0;

            foreach (var b in items)
            {
                if (notification.State == ProgressNotificationState.Cancelled)
                    // user requested abort
                    return;

                var video = b.Files.FirstOrDefault(f => SupportedExtensions.VIDEO_EXTENSIONS.Any(ex => f.Filename.EndsWith(ex, StringComparison.OrdinalIgnoreCase)));

                if (video != null)
                {
                    DeleteFile(b, video);
                    deleted++;
                    notification.CompletionText = $"Deleted {deleted} {HumanisedModelName} video(s)!";
                }

                notification.Text = $"Deleting videos from {HumanisedModelName}s ({deleted} deleted)";

                notification.Progress = (float)++i / items.Count;
            }

            notification.State = ProgressNotificationState.Completed;
        }

        public void UndeleteAll()
        {
            Realm.Run(r => Undelete(r.All<BeatmapSetInfo>().Where(s => s.DeletePending).ToList()));
        }

        public Task<Live<BeatmapSetInfo>?> ImportAsUpdate(ProgressNotification notification, ImportTask importTask, BeatmapSetInfo original) =>
            beatmapImporter.ImportAsUpdate(notification, importTask, original);

        private void updateHashAndMarkDirty(BeatmapSetInfo setInfo)
        {
            setInfo.Hash = beatmapImporter.ComputeHash(setInfo);
        }

        public void MarkPlayed(BeatmapInfo beatmapSetInfo) => Realm.Run(r =>
        {
            using var transaction = r.BeginWrite();

            var beatmap = r.Find<BeatmapInfo>(beatmapSetInfo.ID)!;
            beatmap.LastPlayed = DateTimeOffset.Now;

            transaction.Commit();
        });

        public void MarkNotPlayed(BeatmapInfo beatmapSetInfo) => Realm.Run(r =>
        {
            using var transaction = r.BeginWrite();

            var beatmap = r.Find<BeatmapInfo>(beatmapSetInfo.ID)!;
            beatmap.LastPlayed = null;

            transaction.Commit();
        });

        #region Implementation of ICanAcceptFiles

        public Task Import(params string[] paths) => beatmapImporter.Import(paths);

        public Task Import(ImportTask[] tasks, ImportParameters parameters = default) => beatmapImporter.Import(tasks, parameters);

        public Task<IEnumerable<Live<BeatmapSetInfo>>> Import(ProgressNotification notification, ImportTask[] tasks, ImportParameters parameters = default) =>
            beatmapImporter.Import(notification, tasks, parameters);

        public Task<Live<BeatmapSetInfo>?> Import(ImportTask task, ImportParameters parameters = default, CancellationToken cancellationToken = default) =>
            beatmapImporter.Import(task, parameters, cancellationToken);

        public Live<BeatmapSetInfo>? Import(BeatmapSetInfo item, ArchiveReader? archive = null, CancellationToken cancellationToken = default) =>
            beatmapImporter.ImportModel(item, archive, default, cancellationToken);

        public IEnumerable<string> HandledExtensions => beatmapImporter.HandledExtensions;

        #endregion

        #region Implementation of IWorkingBeatmapCache

        /// <summary>
        /// Retrieve a <see cref="WorkingBeatmap"/> instance for the provided <see cref="BeatmapInfo"/>
        /// </summary>
        /// <param name="beatmapInfo">The beatmap to lookup.</param>
        /// <param name="refetch">Whether to force a refetch from the database to ensure <see cref="BeatmapInfo"/> is up-to-date.</param>
        /// <returns>A <see cref="WorkingBeatmap"/> instance correlating to the provided <see cref="BeatmapInfo"/>.</returns>
        public WorkingBeatmap GetWorkingBeatmap(BeatmapInfo? beatmapInfo, bool refetch = false)
        {
            if (beatmapInfo != null)
            {
                if (refetch)
                    workingBeatmapCache.Invalidate(beatmapInfo);

                // Detached beatmapsets don't come with files as an optimisation (see `RealmObjectExtensions.beatmap_set_mapper`).
                // If we seem to be missing files, now is a good time to re-fetch.
                bool missingFiles = beatmapInfo.BeatmapSet?.Files.Count == 0;

                if (beatmapInfo.IsManaged)
                {
                    beatmapInfo = beatmapInfo.Detach();
                }
                else if (refetch || missingFiles)
                {
                    Guid id = beatmapInfo.ID;
                    beatmapInfo = Realm.Run(r => r.Find<BeatmapInfo>(id)?.Detach()) ?? beatmapInfo;
                }

                Debug.Assert(beatmapInfo.IsManaged != true);
            }

            return workingBeatmapCache.GetWorkingBeatmap(beatmapInfo);
        }

        WorkingBeatmap IWorkingBeatmapCache.GetWorkingBeatmap(BeatmapInfo beatmapInfo) => GetWorkingBeatmap(beatmapInfo);
        void IWorkingBeatmapCache.Invalidate(BeatmapSetInfo beatmapSetInfo) => workingBeatmapCache.Invalidate(beatmapSetInfo);
        void IWorkingBeatmapCache.Invalidate(BeatmapInfo beatmapInfo) => workingBeatmapCache.Invalidate(beatmapInfo);

        public event Action<WorkingBeatmap>? OnInvalidated
        {
            add => workingBeatmapCache.OnInvalidated += value;
            remove => workingBeatmapCache.OnInvalidated -= value;
        }

        public override bool IsAvailableLocally(BeatmapSetInfo model)
        {
            throw new InvalidOperationException($"Use overload with {nameof(IBeatmapInfo)} parameter instead.");
        }

        public bool IsAvailableLocally(IBeatmapInfo model)
        {
            return Realm.Run(r => r.All<BeatmapInfo>()
                                   .Filter($@"{nameof(BeatmapInfo.BeatmapSet)}.{nameof(BeatmapSetInfo.DeletePending)} == false")
                                   .Filter($@"{nameof(BeatmapInfo.OnlineID)} == $0 AND {nameof(BeatmapInfo.MD5Hash)} == {nameof(BeatmapInfo.OnlineMD5Hash)}", model.OnlineID)
                                   .Any());
        }

        #endregion

        #region Implementation of IPostImports<out BeatmapSetInfo>

        public Action<IEnumerable<Live<BeatmapSetInfo>>>? PresentImport
        {
            set => beatmapImporter.PresentImport = value;
        }

        #endregion

        public override string HumanisedModelName => "beatmap";
    }

    /// <summary>
    /// Delegate type for beatmap processing callbacks.
    /// </summary>
    /// <param name="beatmapSet">The beatmap set to be processed.</param>
    public delegate void ProcessBeatmapDelegate(BeatmapSetInfo beatmapSet);
}
