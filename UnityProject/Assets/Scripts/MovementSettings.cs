using UnityEngine;
using UnityEngine.UI;

public static class MovementPreference
{
    public const string Key="fourSeasons.world.experimentalWalk";

    public static bool ExperimentalWalk
    {
        get=>PlayerPrefs.GetInt(Key,0)==1;
        set
        {
            PlayerPrefs.SetInt(Key,value?1:0);
            PlayerPrefs.Save();
        }
    }
}

public sealed partial class SpringPage
{
    RectTransform movementSettings;
    SpringSurface floatCard,walkCard;
    Text floatTitle,walkTitle,floatSelection,walkSelection;

    void BuildMovementSettings(RectTransform heading)
    {
        AlbumButton(heading,"设置",.052f,.94f,.206f,.99f,OpenMovementSettings);
        movementSettings=Bounds(safe,"移动设置",0,0,1,1);
        movementSettings.gameObject.AddComponent<FlowerPageEntrance>();
        Label(Bounds(movementSettings,"标题",.08f,.802f,.92f,.934f),"设置",47,Ink);
        Label(Bounds(movementSettings,"类别",.10f,.686f,.90f,.756f),"现实同行 · 移动方式",25,Ink);
        floatCard=MovementCard("漂浮","默认 · 花灵贴着地面轻轻漂浮",.485f,.654f,false,
            out floatTitle,out floatSelection);
        walkCard=MovementCard("步行","实验功能 · 测试阶段",.281f,.450f,true,
            out walkTitle,out walkSelection);
        AlbumButton(movementSettings,"返回封面",.286f,.105f,.714f,.171f,()=>ShowScanner(false));
        movementSettings.gameObject.SetActive(false);
    }

    SpringSurface MovementCard(string title,string note,float bottom,float top,bool walk,out Text label,out Text selection)
    {
        var card=Bounds(movementSettings,title,.085f,bottom,.915f,top);
        var rim=Surface(card,Honey,true);
        var face=Surface(Inset(card,"笺纸",1.7f),Parchment,true);
        label=Label(Bounds(face.rectTransform,"选项",.09f,.48f,.71f,.90f),title,31,Ink);
        label.alignment=TextAnchor.MiddleLeft;
        var detail=Label(Bounds(face.rectTransform,"说明",.09f,.12f,.91f,.46f),note,17,Ink);
        detail.alignment=TextAnchor.MiddleLeft;
        selection=Label(Bounds(face.rectTransform,"状态",.73f,.53f,.94f,.84f),"已选",17,Ink);
        MakeButton(card,rim,()=>{
            MovementPreference.ExperimentalWalk=walk;
            RefreshMovementSettings();
        });
        return face;
    }

    void OpenMovementSettings()
    {
        if(!visit.CanOpenAlbum)return;
        SetAlbumGround("Silk");
        albumPaper.SetActive(true);
        coverLayer.SetActive(false);
        home.SetActive(false);
        movementSettings.gameObject.SetActive(true);
        RefreshMovementSettings();
    }

    void RefreshMovementSettings()
    {
        bool walk=MovementPreference.ExperimentalWalk;
        StyleMovementOption(floatCard,floatTitle,floatSelection,!walk);
        StyleMovementOption(walkCard,walkTitle,walkSelection,walk);
    }

    static void StyleMovementOption(SpringSurface face,Text title,Text selection,bool selected)
    {
        face.SetGradient(
            selected?new Color32(241,224,232,255):new Color32(255,251,244,255),
            selected?new Color32(227,195,211,255):new Color32(245,232,216,255));
        selection.gameObject.SetActive(selected);
        title.color=selected?Plum:Ink;
    }
}
