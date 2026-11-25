using UnityEngine;

public sealed partial class SpringPage
{
    AlbumScenery albumGround;
    SpringSurface storyPlaque, switchFace;
    GameObject seasonFrame;
    static readonly Color32 PearlPaper=new(249,238,219,255);
    static readonly Color32 StarPaper=new(238,229,245,255);

    void SetAlbumGround() => albumGround.SelectPaper(new Color32(255,250,240,255));

    void DressDetail(string id)
    {
        bool pearl = id == AlbumModelViewer.TimeKeeper;
        bool night = id == AlbumModelViewer.StarDreamer;
        bool hidden = pearl || night;
        seasonFrame.SetActive(!hidden);
        albumGround.SelectPaper(hidden
            ? Color.Lerp(Parchment,pearl ? PearlPaper : StarPaper,.70f)
            : Color.Lerp(Parchment,detailScenery.color,.34f));
        detailName.color = night ? new Color32(76,53,98,255) : Ink;
        if (night) storyPlaque.SetGradient(new Color32(244,240,248,255),new Color32(218,212,231,255));
        else storyPlaque.SetGradient(new Color32(255,252,241,255),new Color32(244,229,210,255));
        detailStory.color = night ? new Color32(66,51,81,255) : Ink;
        if (night) switchFace.SetGradient(new Color32(110,81,125,255),new Color32(61,45,86,255));
        else switchFace.SetGradient(new Color32(66,117,104,255),new Color32(33,67,67,255));
    }
}
