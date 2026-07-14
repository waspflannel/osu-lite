// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading.Tasks;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Localisation;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API
{
    /// <summary>
    /// osu! lite is fully offline. This provider never connects: the local user is always a guest,
    /// <see cref="IsLoggedIn"/> is always false, and API requests are not serviced.
    /// </summary>
    public partial class DummyAPIAccess : Component, IAPIProvider
    {
        public const int DUMMY_USER_ID = 1001;

        public DummyLocalUserState LocalUserState { get; } = new DummyLocalUserState();

        public string ScoreProcessingNoticeUrl { get; set; } = string.Empty;

        public Bindable<APIUser> LocalUser => LocalUserState.User;

        ILocalUserState IAPIProvider.LocalUserState => LocalUserState;
        IBindable<APIUser> IAPIProvider.LocalUser => LocalUser;

        public Language Language => Language.en;

        public string AccessToken => "token";

        public Guid SessionIdentifier { get; } = Guid.NewGuid();

        public bool IsLoggedIn => State.Value > APIState.Offline;

        public EndpointConfiguration Endpoints { get; } = new EndpointConfiguration
        {
            APIUrl = "http://localhost",
            WebsiteUrl = "http://localhost",
        };

        public int APIVersion => int.Parse(DateTime.Now.ToString("yyyyMMdd"));

        // osu! lite is offline: the API never connects, so IsLoggedIn is always false and the local user stays a guest.
        private readonly Bindable<APIState> state = new Bindable<APIState>(APIState.Offline);

        /// <summary>
        /// The current connectivity state of the API.
        /// </summary>
        public IBindable<APIState> State => state;

        public void Queue(APIRequest request)
        {
            request.AttachAPI(this);
            Schedule(() => request.Fail(new InvalidOperationException($@"{nameof(DummyAPIAccess)} is offline and cannot process requests.")));
        }

        void IAPIProvider.Schedule(Action action) => base.Schedule(action);

        public void Perform(APIRequest request) => request.AttachAPI(this);

        public Task PerformAsync(APIRequest request)
        {
            request.AttachAPI(this);
            return Task.CompletedTask;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // Ensure (as much as we can) that any pending tasks are run.
            Scheduler.Update();
        }

        public class DummyLocalUserState : ILocalUserState
        {
            public Bindable<APIUser> User { get; } = new Bindable<APIUser>(new APIUser
            {
                Username = @"Local user",
                Id = DUMMY_USER_ID,
            });

            public BindableList<APIRelation> Friends { get; } = new BindableList<APIRelation>();
            public BindableList<APIRelation> Blocks { get; } = new BindableList<APIRelation>();
            public BindableList<int> FavouriteBeatmapSets { get; } = new BindableList<int>();

            IBindable<APIUser> ILocalUserState.User => User;
            IBindableList<APIRelation> ILocalUserState.Friends => Friends;
            IBindableList<APIRelation> ILocalUserState.Blocks => Blocks;
            IBindableList<int> ILocalUserState.FavouriteBeatmapSets => FavouriteBeatmapSets;

            public void UpdateFriends()
            {
            }

            public void UpdateBlocks()
            {
            }

            public void UpdateFavouriteBeatmapSets()
            {
            }
        }
    }
}
