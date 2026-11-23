using UnityEngine;

public sealed class SeasonAlbum
{
    readonly string prefix;

    public SeasonAlbum(string prefix = "fourSeasons.album.v1.") => this.prefix = prefix;

    public bool Contains(string referenceId) => PlayerPrefs.GetInt(prefix + referenceId, 0) == 1;

    public void Reset(System.Collections.Generic.IEnumerable<string> ids)
    {
        foreach(var id in ids) PlayerPrefs.DeleteKey(prefix + id);
        PlayerPrefs.Save();
    }

    public bool Collect(string referenceId)
    {
        if (Contains(referenceId)) return false;
        PlayerPrefs.SetInt(prefix + referenceId, 1);
        PlayerPrefs.Save();
        return true;
    }
}
