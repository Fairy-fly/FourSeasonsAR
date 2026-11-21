using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public sealed partial class SpringPage
{
    RectTransform album, index, leaf;
    GameObject albumPaper;
    Text progress, detailName, detailStory, announcement;
    Image detailPortrait;
    float announcementUntil;

    sealed class AlbumEntry
    {
        public SpringSpirit character;
        public Button button;
        public Image portrait;
        public Text season, caption;
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
        Label(Bounds(index,"册名",.09f,.82f,.91f,.93f),"四季花册",52,Ink,brushFont);
        Ornament(Bounds(index,"花钿",.23f,.805f,.77f,.826f));
        progress = Label(Bounds(index,"进度",.1f,.748f,.9f,.804f),"",21,Ink);
        entries = new AlbumEntry[visit.characters.Length];
        for (int i = 0; i < entries.Length; ++i)
        {
            var character = visit.characters[i];
            float x = i % 2 == 0 ? .075f : .52f;
            float y = i < 2 ? .456f : .164f;
            var card = Bounds(index,character.referenceName,x,y,x+.405f,y+.265f);
            var edge = Surface(card,Honey,true);
            var paper = Inset(card,"笺纸",1.4f);
            Surface(paper,new Color32(255,252,245,255),true);
            var portrait = Portrait(Bounds(paper,"立绘",.06f,.245f,.94f,.95f));
            portrait.sprite = character.portrait.sprite;
            var seal = Label(Bounds(paper,"季印",.17f,.29f,.83f,.93f),
                character.characterName.Substring(0,1),52,new Color32(163,127,111,255),brushFont);
            Line(paper,.19f,.258f,.81f,.262f,Honey);
            var caption = Label(Bounds(paper,"题名",.04f,.025f,.96f,.25f),"",23,Ink);
            MakeButton(card,edge,() => OpenDetail(character));
            entries[i] = new AlbumEntry { character=character, button=card.GetComponent<Button>(),
                portrait=portrait, season=seal, caption=caption };
        }
        AlbumButton(index,"返回封面",.30f,.050f,.70f,.118f,() => ShowScanner(false));

        leaf = Bounds(album,"角色详情",0,0,1,1);
        leaf.gameObject.AddComponent<FlowerPageEntrance>();
        detailName = Label(Bounds(leaf,"题名",.07f,.815f,.93f,.94f),"",54,Ink,brushFont);
        Ornament(Bounds(leaf,"花钿",.22f,.80f,.78f,.822f));
        detailPortrait = Portrait(Bounds(leaf,"立绘",.09f,.30f,.91f,.78f));
        var note = Bounds(leaf,"角色花笺",.075f,.163f,.925f,.283f);
        Surface(note,Honey,true);
        Surface(Inset(note,"笺纸",1.5f),new Color32(255,252,245,255),true);
        detailStory = Label(Inset(note,"花语",14),"",23,Ink);
        AlbumButton(leaf,"返回花册",.30f,.05f,.70f,.118f,OpenAlbum);
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
            entry.season.gameObject.SetActive(!known);
            entry.caption.text = known ? entry.character.characterName : "待寻访";
        }
        progress.text = $"已收集  {count} / {entries.Length}";
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
