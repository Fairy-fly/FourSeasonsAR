using System;

public sealed class CompanionProfile
{
    public enum Kind { Spring, Summer, Autumn, Winter, Time, Star }

    public static readonly CompanionProfile[] All =
    {
        new(SpringVisit.ReferenceName, "春知意", Kind.Spring, .117f, 1.12838f, .39f, 1, 1, .28f),
        new("summer_huajian", "夏日花涧", Kind.Summer, .091f, 1.0781f, .34f, 1.06f, .80f, .62f),
        new("autumn_manwu", "秋叶漫舞", Kind.Autumn, .049f, 1.13658f, .45f, 1, 1, .46f),
        new("winter_xueling", "冬雪灵", Kind.Winter, .056f, 1.1014f, .31f, .80f, .78f, .73f),
        new(AlbumModelViewer.TimeKeeper, "时见", Kind.Time, .059f, 1.1038f, .28f, .79f, .62f, .80f),
        new(AlbumModelViewer.StarDreamer, "星眠", Kind.Star, .059f, 1.0794f, .37f, .89f, 1.04f, .67f),
    };

    CompanionProfile(string id, string name, Kind style, float sole, float crown, float pace,
        float stepLength, float lift, float release)
    {
        Id=id;
        Name=name;
        Style=style;
        Sole=sole;
        Crown=crown;
        Pace=pace;
        StepLength=stepLength;
        Lift=lift;
        Release=release;
    }

    public string Id { get; }
    public string Name { get; }
    public Kind Style { get; }
    public float Sole { get; }
    public float Crown { get; }
    public float Pace { get; }
    public float StepLength { get; }
    public float Lift { get; }
    public float Release { get; }

    public static CompanionProfile Find(string id) => Array.Find(All, profile => profile.Id==id);

    public static int OwnedCount(SeasonAlbum album)
    {
        int count=0;
        foreach(var profile in All)
            if(album.Contains(profile.Id))count++;
        return count;
    }

    public static CompanionProfile NextOwned(string current, SeasonAlbum album)
    {
        int start=Array.FindIndex(All, profile=>profile.Id==current);
        for(int offset=1;offset<All.Length;offset++)
        {
            var profile=All[(start+offset)%All.Length];
            if(album.Contains(profile.Id))return profile;
        }
        return null;
    }
}
