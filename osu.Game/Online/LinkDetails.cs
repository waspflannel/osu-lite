// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Online
{
    public enum LinkAction
    {
        External,
        OpenBeatmap,
        OpenBeatmapSet,
        OpenChannel,
        OpenEditorTimestamp,
        JoinRoom,
        Spectate,
        OpenUserProfile,
        SearchBeatmapSet,
        OpenWiki,
        Custom,
        OpenChangelog,
        FilterBeatmapSetGenre,
        FilterBeatmapSetLanguage,
    }

    public class LinkDetails
    {
        public readonly LinkAction Action;

        public readonly object Argument;

        public LinkDetails(LinkAction action, object argument)
        {
            Action = action;
            Argument = argument;
        }
    }

    public class Link : IComparable<Link>
    {
        public string Url;
        public int Index;
        public int Length;
        public LinkAction Action;
        public object Argument;

        public Link(string url, int startIndex, int length, LinkAction action, object argument)
        {
            Url = url;
            Index = startIndex;
            Length = length;
            Action = action;
            Argument = argument;
        }

        public bool Overlaps(Link otherLink) => Overlaps(otherLink.Index, otherLink.Length);

        public bool Overlaps(int otherIndex, int otherLength) => Index < otherIndex + otherLength && otherIndex < Index + Length;

        public int CompareTo(Link? otherLink) => Index > otherLink?.Index ? 1 : -1;
    }
}
