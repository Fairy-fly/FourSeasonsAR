using UnityEngine;
using UnityEngine.UI;

public sealed partial class SpringPage
{
    AlbumScenery albumGround;
    Image albumWash;
    SpringSurface storyPlaque, switchFace;
    GameObject seasonFrame;

    static Texture2D EnamelTexture(string asset) => Resources.Load<Texture2D>("Enamel/" + asset);

    void SetAlbumGround(string asset)
    {
        albumGround.Select(EnamelTexture(asset),new Rect(0,0,1,1));
        albumWash.color = asset == "NightGarden" ? new Color32(82,74,102,109) :
            asset == "DayGarden" ? new Color32(251,241,225,122) : new Color32(255,249,237,187);
    }

    void DressDetail(string id)
    {
        bool pearl = id == AlbumModelViewer.TimeKeeper;
        bool night = id == AlbumModelViewer.StarDreamer;
        bool hidden = pearl || night;
        seasonFrame.SetActive(!hidden);
        SetAlbumGround(pearl ? "DayGarden" : night ? "NightGarden" : "Silk");
        detailName.color = night ? Parchment : Ink;
        if (night) storyPlaque.SetGradient(new Color32(244,240,248,255),new Color32(218,212,231,255));
        else storyPlaque.SetGradient(new Color32(255,252,241,255),new Color32(244,229,210,255));
        detailStory.color = night ? new Color32(66,51,81,255) : Ink;
        if (night) switchFace.SetGradient(new Color32(110,81,125,255),new Color32(61,45,86,255));
        else switchFace.SetGradient(new Color32(66,117,104,255),new Color32(33,67,67,255));
    }
}
