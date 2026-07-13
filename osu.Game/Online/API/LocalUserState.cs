// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;

namespace osu.Game.Online.API
{
    /// <summary>
    /// osu! lite is offline, so the local user is always a guest and there are no
    /// friends, blocks, or favourites to track.
    /// </summary>
    public partial class LocalUserState : Component, ILocalUserState
    {
        public IBindable<APIUser> User => localUser;
        public IBindableList<APIRelation> Friends => friends;
        public IBindableList<APIRelation> Blocks => blocks;
        public IBindableList<int> FavouriteBeatmapSets => favouriteBeatmapSets;

        private readonly Bindable<APIUser> localUser = new Bindable<APIUser>(createGuestUser());
        private readonly BindableList<APIRelation> friends = new BindableList<APIRelation>();
        private readonly BindableList<APIRelation> blocks = new BindableList<APIRelation>();
        private readonly BindableList<int> favouriteBeatmapSets = new BindableList<int>();

        private readonly Bindable<bool> configSupporter = new Bindable<bool>();

        public LocalUserState(IAPIProvider api, OsuConfigManager config)
        {
            config.BindWith(OsuSetting.WasSupporter, configSupporter);
        }

        private static APIUser createGuestUser() => new APIUser
        {
            Username = @"Guest",
            Id = APIUser.SYSTEM_USER_ID,
        };

        public void SetPlaceholderLocalUser(string username)
        {
            if (!localUser.IsDefault)
                return;

            localUser.Value = new APIUser
            {
                Username = username,
                IsSupporter = configSupporter.Value,
            };
        }

        public void SetLocalUser(APIMe me)
        {
            localUser.Value = me;
            configSupporter.Value = me.IsSupporter;
        }

        public void ClearLocalUser()
        {
            Schedule(() =>
            {
                localUser.Value = createGuestUser();
                configSupporter.Value = false;
                friends.Clear();
                blocks.Clear();
                favouriteBeatmapSets.Clear();
            });
        }

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
