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
    AlbumModelViewer modelViewer;
    GameObject modelBackdrop;
    RectTransform detailBack, modelSwitch;
    Text switchLabel;
    string detailId, detailExcerpt;
    Button encounterTouch;
    static readonly Color32[] SeasonInk = {
        new(130,71,89,255),new(51,103,96,255),new(140,82,53,255),new(79,98,135,255)
    };
    static readonly Color32[] SeasonPaper = {
        new(251,232,233,255),new(230,241,225,255),new(250,232,209,255),new(229,237,250,255)
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
        var title=Bounds(index,"册名",.18f,.865f,.82f,.953f).gameObject.AddComponent<InkTitle>();
        title.Configure(Resources.Load<Texture2D>("Album/title-album"),Ink,Honey,1.18f);
        progress = Label(Bounds(index,"进度",.1f,.811f,.9f,.855f),"",18,Ink);
        entries = new AlbumEntry[visit.characters.Length];
        for (int i = 0; i < entries.Length; ++i)
        {
            var character = visit.characters[i];
            float x = i % 2 == 0 ? .055f : .515f;
            float y = i < 2 ? .532f : .261f;
            var card = Bounds(index,character.referenceName,x,y,x+.43f,y+.254f);
            var edge = Surface(card,Honey,true);
            var paper = Inset(card,"笺纸",3);
            var tone = SeasonInk[(int)character.season];
            var face=Surface(paper,Parchment,true).SetGradient(Color.Lerp(tone,Parchment,.82f),Color.Lerp(tone,Parchment,.55f));
            StyleCards(edge,face);
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
            var caption = Label(Bounds(paper,"题名",.04f,.005f,.96f,.20f),"",25,Color.Lerp(tone,Ink,.58f),brushFont);
            MakeButton(card,edge,() => OpenDetail(character));
            entries[i] = new AlbumEntry { character=character, button=card.GetComponent<Button>(),
                portrait=portrait, seal=medallion.gameObject, caption=caption };
        }
        HiddenSeat(index,"时见",.055f,true);
        HiddenSeat(index,"星眠",.515f,false);
        AlbumButton(index,"返回封面",.32f,.025f,.68f,.091f,() => ShowScanner(false));

        leaf = Bounds(album,"角色详情",0,0,1,1);
        leaf.gameObject.AddComponent<FlowerPageEntrance>();
        detailName = Label(Bounds(leaf,"题名",.07f,.845f,.93f,.940f),"",54,Ink,brushFont);
        var framed = Bounds(leaf,"画境",.052f,.235f,.948f,.805f);
        seasonFrame = framed.gameObject;
        var sceneBorder=Surface(framed,Honey,true);
        var interior = Inset(framed,"季景",3);
        var sceneFace=Surface(interior,Parchment,true);
        StyleCards(sceneBorder,sceneFace);
        detailScenery=Bounds(interior,"底纸",0,0,1,1).gameObject.AddComponent<AlbumScenery>();
        detailPortrait = Portrait(Bounds(leaf,"立绘",.09f,.245f,.91f,.801f));
        encounterTouch=detailPortrait.gameObject.AddComponent<FlowerButton>();
        encounterTouch.targetGraphic=detailPortrait;
        encounterTouch.transition=Selectable.Transition.None;
        encounterTouch.onClick.AddListener(ReceiveHidden);
        detailAir = Air(Bounds(leaf,"景深",.059f,.239f,.941f,.802f),AlbumAtmosphere.Motif.Petal);
        var veil = Bounds(leaf,"展柜衬纸",.059f,.239f,.941f,.802f);
        Paint(veil,new Color32(248,237,227,145));
        modelBackdrop = veil.gameObject;
        modelBackdrop.SetActive(false);
        var viewport = Bounds(leaf,"模型展窗",.059f,.239f,.941f,.802f);
        viewport.gameObject.SetActive(false);
        modelViewer = viewport.gameObject.AddComponent<AlbumModelViewer>();
        var note = Bounds(leaf,"角色花笺",.055f,.117f,.945f,.22f);
        Surface(note,Honey,true);
        storyPlaque = Surface(Inset(note,"笺纸",1.2f),Parchment,true);
        PaperCorners(note);
        detailStory = Label(Bounds(note,"花语",.105f,.09f,.895f,.91f),"",23,Ink);
        detailBack = AlbumButton(leaf,"返回花册",.30f,.05f,.70f,.118f,OpenAlbum);
        modelSwitch = AlbumButton(leaf,"查看 3D",.522f,.028f,.948f,.101f,ToggleModel);
        switchLabel = modelSwitch.Find("按钮文字").GetComponent<Text>();
    }

    void ToggleModel()
    {
        if (!AlbumModelViewer.HasModel(detailId)) return;
        bool open = !modelViewer.gameObject.activeSelf;
        if (open) modelViewer.Open(detailId);
        else modelViewer.Close();
        modelBackdrop.SetActive(open && seasonFrame.activeSelf);
        detailPortrait.gameObject.SetActive(!open);
        detailAir.gameObject.SetActive(!open);
        detailStory.resizeTextMaxSize = !open && !seasonFrame.activeSelf ? 34 : 23;
        detailStory.fontSize = detailStory.resizeTextMaxSize;
        detailStory.text = open ? "拖动旋转   双指缩放" : detailExcerpt;
        switchLabel.text = open ? "查看 2D" : "查看 3D";
    }

    static void SetSeasonScene(AlbumScenery art,SpringSpirit.Season season) =>
        art.SelectPaper(SeasonPaper[(int)season]);

    static AlbumAtmosphere Air(RectTransform stage,AlbumAtmosphere.Motif motif)
    {
        var air = Bounds(stage,"流光",0,0,1,1).gameObject.AddComponent<AlbumAtmosphere>();
        air.Select(motif);
        return air;
    }

    void HiddenSeat(RectTransform parent,string title,float x,bool pearl)
    {
        var seat=Bounds(parent,title,x,.111f,x+.43f,.245f);
        var edge=Surface(seat,Honey,true);
        var art=Inset(seat,"藏品",2.3f);
        StyleCards(edge,Surface(art,Parchment,true));
        Bounds(art,"底纸",0,0,1,1).gameObject.AddComponent<AlbumScenery>().SelectPaper(pearl ? PearlPaper : StarPaper);
        string id=pearl ? AlbumModelViewer.TimeKeeper : AlbumModelViewer.StarDreamer;
        Portrait(Bounds(art,"藏品立绘",.46f,.05f,.96f,.97f)).sprite=Resources.Load<Sprite>("Gallery/"+id+"/Portrait");
        Air(art,pearl ? AlbumAtmosphere.Motif.Pearl : AlbumAtmosphere.Motif.Star);
        var plaque=Bounds(art,"题签",.055f,.15f,.50f,.87f);
        Color ink=pearl ? new Color32(100,68,46,255) : new Color32(76,53,98,255);
        Label(Bounds(plaque,"款名",.03f,.36f,.97f,.94f),title,29,ink,brushFont);
        Label(Bounds(plaque,"款别",.04f,.05f,.96f,.36f),"隐藏款",15,ink);
        MakeButton(seat,edge,() => OpenHiddenDetail(id));
    }

    void OpenHiddenDetail(string id)
    {
        bool pearl=id==AlbumModelViewer.TimeKeeper;
        var portrait=Resources.Load<Sprite>("Gallery/"+id+"/Portrait");
        detailAir.Select(pearl ? AlbumAtmosphere.Motif.Pearl : AlbumAtmosphere.Motif.Star);
        ShowDetail(id,CompanionProfile.Find(id).Name,portrait,HiddenNote(id),brushFont);
    }

    static Image Portrait(RectTransform rect)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    RectTransform AlbumButton(RectTransform parent,string title,float x,float y,float r,float t,UnityAction click,bool coverEntry=false)
    {
        var rect = Bounds(parent,title,x,y,r,t);
        bool primary = title == "查看 3D";
        var rim = Surface(rect,Honey,true).SetGradient(new Color32(235,202,161,255),new Color32(163,112,81,255));
        var face = Surface(Inset(rect,"漆面",coverEntry ? 2.7f : 2.25f),Parchment,true);
        StyleButtons(rim,face);
        if(coverEntry) face.SetGradient(new Color32(155,104,126,255),new Color32(99,49,75,255));
        else if (primary) face.SetGradient(new Color32(66,117,104,255),new Color32(33,67,67,255));
        else face.SetGradient(new Color32(255,249,235,255),new Color32(224,202,169,255));
        if (primary) switchFace = face;
        Label(Bounds(rect,"按钮文字",.04f,.07f,.96f,.93f),title,23,primary || coverEntry ? Parchment : Ink);
        MakeButton(rect,rim,click);
        if (!primary) return rect;
        var flare = Bounds(rect,"金屑",0,0,1,1).gameObject.AddComponent<EnamelFlare>();
        flare.enabled = false;
        rect.GetComponent<FlowerButton>().flare = flare;
        return rect;
    }

    void OpenAlbum()
    {
        if (!visit.CanOpenAlbum) return;
        modelViewer.Close();
        SetAlbumGround();
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

    bool AllSeasonsCollected()=>System.Array.TrueForAll(visit.characters,c=>visit.Album.Contains(c.referenceName));
    string HiddenNote(string id)=>visit.Album.Contains(id)?"已收录 · 隐藏款":AllSeasonsCollected()?"点击收录进四季花册~":"收集四季后可收录";

    void ReceiveHidden()
    {
        if(!leaf.gameObject.activeSelf || modelViewer.gameObject.activeSelf ||
            (detailId!=AlbumModelViewer.TimeKeeper && detailId!=AlbumModelViewer.StarDreamer) ||
            !AllSeasonsCollected() || !visit.Album.Collect(detailId))return;
        detailAir.Sample(.8f);
        detailExcerpt=detailStory.text=HiddenNote(detailId);
        encounterTouch.interactable=false;
        detailPortrait.raycastTarget=false;
        ArrangeActions(detailId);
        Debug.Log("Hidden encounter collected "+detailId);
    }

    void OpenDetail(SpringSpirit character)
    {
        if (!visit.Album.Contains(character.referenceName)) return;
        SetSeasonScene(detailScenery,character.season);
        detailAir.Select((AlbumAtmosphere.Motif)character.season);
        ShowDetail(character.referenceName,character.characterName,character.portrait.sprite,character.excerpt,brushFont);
    }

    void ShowDetail(string id,string title,Sprite portrait,string excerpt,Font titleFont)
    {
        detailId = id;
        detailExcerpt = excerpt;
        modelViewer.Close();
        modelBackdrop.SetActive(false);
        detailPortrait.gameObject.SetActive(true);
        detailAir.gameObject.SetActive(true);
        bool hasModel = AlbumModelViewer.HasModel(id);
        modelSwitch.gameObject.SetActive(hasModel);
        switchLabel.text = "查看 3D";
        detailBack.anchorMin = new Vector2(hasModel ? .052f : .30f,.028f);
        detailBack.anchorMax = new Vector2(hasModel ? .478f : .70f,.101f);
        modelSwitch.anchorMin=new Vector2(.522f,.028f);
        modelSwitch.anchorMax=new Vector2(.948f,.101f);
        bool hidden = id==AlbumModelViewer.TimeKeeper || id==AlbumModelViewer.StarDreamer;
        encounterTouch.interactable = hidden && AllSeasonsCollected() && !visit.Album.Contains(id);
        detailPortrait.raycastTarget=encounterTouch.interactable;
        ArrangeActions(id);
        DressDetail(id);
        detailName.font = titleFont;
        detailName.text = title;
        detailPortrait.sprite = portrait;
        detailStory.resizeTextMaxSize = seasonFrame.activeSelf ? 23 : 34;
        detailStory.fontSize = detailStory.resizeTextMaxSize;
        detailStory.text = excerpt;
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
