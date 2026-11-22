using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed partial class SpringPage
{
    RectTransform album, index, leaf;
    GameObject albumPaper;
    Text progress, detailName, detailStory, announcement;
    Image detailPortrait;
    AlbumScenery detailScenery;
    AlbumAtmosphere detailAir;
    Texture2D seasons, relics;
    static readonly Color32[] SeasonInk = {
        new(118,57,77,255),new(36,90,83,255),new(127,67,37,255),new(67,85,124,255)
    };
    float announcementUntil;

    sealed class AlbumEntry
    {
        public SpringSpirit character;
        public Button button;
        public Image portrait;
        public Text caption;
        public GameObject seal;
    }

    AlbumEntry[] entries;

    void BuildAlbum(RectTransform scan)
    {
        var toast = Bounds(scan,"收藏提示",.29f,.86f,.89f,.92f);
        Surface(toast,Glass);
        announcement = Label(Inset(toast,"提示",4),"",19,Parchment);

        album = Bounds(safe,"花册",0,0,1,1);
        index = Bounds(album,"目录",0,0,1,1);
        index.gameObject.AddComponent<FlowerPageEntrance>();
        seasons = Resources.Load<Texture2D>("Album/seasons");
        relics = Resources.Load<Texture2D>("Album/relics");
        var title=Bounds(index,"册名",.18f,.895f,.82f,.987f).gameObject.AddComponent<InkTitle>();
        title.Configure(Resources.Load<Texture2D>("Album/title-album"),Ink,Honey,1.18f);
        Ornament(Bounds(index,"花钿",.25f,.875f,.75f,.898f));
        progress = Label(Bounds(index,"进度",.1f,.819f,.9f,.873f),"",18,Ink);
        entries = new AlbumEntry[visit.characters.Length];
        for (int i = 0; i < entries.Length; ++i)
        {
            var character = visit.characters[i];
            float x = i % 2 == 0 ? .055f : .515f;
            float y = i < 2 ? .543f : .266f;
            var card = Bounds(index,character.referenceName,x,y,x+.43f,y+.262f);
            var edge = Surface(card,Honey,true);
            var paper = Inset(card,"笺纸",3);
            var tone = SeasonInk[(int)character.season];
            Surface(paper,tone,true);
            var stage = Bounds(paper,"季景",.014f,.207f,.986f,.988f);
            SetSeasonScene(stage.gameObject.AddComponent<AlbumScenery>(),character.season);
            var portrait = Portrait(Bounds(stage,"立绘",.055f,.014f,.945f,.99f));
            portrait.sprite = character.portrait.sprite;
            Air(stage,(AlbumAtmosphere.Motif)character.season);
            var medallion=Box(stage,"季印",.5f,.49f,67,67);
            Surface(medallion,Honey);
            Surface(Inset(medallion,"釉色",1.7f),tone);
            Label(Inset(medallion,"季名",5),character.characterName.Substring(0,1),38,Parchment,brushFont);
            Line(paper,.02f,.2f,.98f,.207f,Honey);
            var caption = Label(Bounds(paper,"题名",.04f,.005f,.96f,.20f),"",23,Parchment);
            MakeButton(card,edge,() => OpenDetail(character));
            entries[i] = new AlbumEntry { character=character, button=card.GetComponent<Button>(),
                portrait=portrait, seal=medallion.gameObject, caption=caption };
        }
        HiddenSeat(index,"时见",.055f,true);
        HiddenSeat(index,"星眠",.515f,false);
        AlbumButton(index,"返回封面",.32f,.025f,.68f,.091f,() => ShowScanner(false));

        leaf = Bounds(album,"角色详情",0,0,1,1);
        leaf.gameObject.AddComponent<FlowerPageEntrance>();
        detailName = Label(Bounds(leaf,"题名",.07f,.815f,.93f,.94f),"",54,Ink,brushFont);
        Ornament(Bounds(leaf,"花钿",.22f,.80f,.78f,.822f));
        var framed = Bounds(leaf,"画境",.052f,.292f,.948f,.79f);
        Surface(framed,Honey,true);
        var interior = Inset(framed,"季景",3);
        detailScenery = interior.gameObject.AddComponent<AlbumScenery>();
        detailPortrait = Portrait(Bounds(leaf,"立绘",.09f,.302f,.91f,.778f));
        detailAir = Air(Bounds(leaf,"景深",.059f,.296f,.941f,.786f),AlbumAtmosphere.Motif.Petal);
        var note = Bounds(leaf,"角色花笺",.075f,.163f,.925f,.283f);
        Surface(note,Honey,true);
        Surface(Inset(note,"笺纸",1.5f),new Color32(255,252,245,255),true);
        detailStory = Label(Inset(note,"花语",14),"",23,Ink);
        AlbumButton(leaf,"返回花册",.30f,.05f,.70f,.118f,OpenAlbum);
    }

    void SetSeasonScene(AlbumScenery art,SpringSpirit.Season season)
    {
        int n=(int)season;
        art.Select(seasons,new Rect((n%2)*.5f,n<2 ? .5f : 0,.5f,.5f));
    }

    static AlbumAtmosphere Air(RectTransform stage,AlbumAtmosphere.Motif motif)
    {
        var air = Bounds(stage,"流光",0,0,1,1).gameObject.AddComponent<AlbumAtmosphere>();
        air.Select(motif);
        return air;
    }

    void HiddenSeat(RectTransform parent,string title,float x,bool pearl)
    {
        var seat=Bounds(parent,title,x,.111f,x+.43f,.245f);
        Surface(seat,Honey,true);
        var art=Inset(seat,"藏品",2.3f);
        art.gameObject.AddComponent<AlbumScenery>().Select(relics,new Rect(0,pearl ? .5f : 0,1,.5f),.84f);
        Air(art,pearl ? AlbumAtmosphere.Motif.Pearl : AlbumAtmosphere.Motif.Star);
        var plaque=Bounds(art,"题签",.035f,.12f,.49f,.88f);
        Surface(plaque,pearl ? new Color32(64,49,43,223) : new Color32(37,27,59,225),true);
        Label(Bounds(plaque,"款名",.03f,.38f,.97f,.94f),title,27,Parchment);
        Line(plaque,.23f,.39f,.77f,.405f,Honey);
        Label(Bounds(plaque,"款别",.04f,.05f,.96f,.38f),"隐藏款",14,new Color32(248,218,170,255));
    }

    static Image Portrait(RectTransform rect)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    void AlbumButton(RectTransform parent,string title,float x,float y,float r,float t,UnityAction click)
    {
        var rect = Bounds(parent,title,x,y,r,t);
        var rim = Surface(rect,Honey);
        Surface(Inset(rect,"漆面",1.5f),Plum);
        Label(Inset(rect,"按钮文字",3),title,19,Parchment);
        MakeButton(rect,rim,click);
    }

    void OpenAlbum()
    {
        if (!visit.CanOpenAlbum) return;
        int count = 0;
        foreach (var entry in entries)
        {
            bool known = visit.Album.Contains(entry.character.referenceName);
            if (known) ++count;
            entry.button.interactable = known;
            entry.portrait.gameObject.SetActive(known);
            entry.seal.SetActive(!known);
            entry.caption.text = known ? entry.character.characterName : "待寻访";
        }
        progress.text = $"四季已收集  {count} / {entries.Length}";
        home.SetActive(false);
        coverLayer.SetActive(false);
        albumPaper.SetActive(true);
        album.gameObject.SetActive(true);
        index.gameObject.SetActive(true);
        leaf.gameObject.SetActive(false);
    }

    void OpenDetail(SpringSpirit character)
    {
        if (!visit.Album.Contains(character.referenceName)) return;
        detailName.text = character.characterName;
        detailPortrait.sprite = character.portrait.sprite;
        SetSeasonScene(detailScenery,character.season);
        detailAir.Select((AlbumAtmosphere.Motif)character.season);
        detailStory.text = character.excerpt;
        index.gameObject.SetActive(false);
        leaf.gameObject.SetActive(true);
    }

    public void AnnounceCollection(SpringSpirit character)
    {
        announcement.text = "已收入花册 · " + character.characterName;
        announcementUntil = Time.unscaledTime + 2.4f;
        announcement.transform.parent.gameObject.SetActive(true);
    }

    public void HideAnnouncement() => announcement.transform.parent.gameObject.SetActive(false);
}
