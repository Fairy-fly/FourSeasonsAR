using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SpringPage
{
    public const string CardStyleKey="fourSeasons.appearance.card";
    public const string ButtonStyleKey="fourSeasons.appearance.button";
    static readonly string[] StyleNames={"花笺","圆角","折角"};
    readonly List<SpringSurface> cardFrames=new();
    readonly List<SpringSurface> themedButtons=new();
    readonly Text[] selectedCards=new Text[3];
    readonly Text[] selectedButtons=new Text[3];
    RectTransform appearanceSettings;

    static FrameStyle ChosenStyle(string key)
    {
        int value=PlayerPrefs.GetInt(key);
        return value>=0 && value<StyleNames.Length ? (FrameStyle)value : FrameStyle.Original;
    }

    void StyleCards(SpringSurface border,SpringSurface face)
    {
        var style=ChosenStyle(CardStyleKey);
        border.SetFrameStyle(style);
        face.SetFrameStyle(style);
        face.gameObject.AddComponent<Mask>();
        cardFrames.Add(border);
        cardFrames.Add(face);
    }

    void StyleButtons(params SpringSurface[] surfaces)
    {
        var style=ChosenStyle(ButtonStyleKey);
        foreach(var surface in surfaces)
        {
            surface.SetFrameStyle(style);
            themedButtons.Add(surface);
        }
    }

    void BuildAppearance()
    {
        appearanceSettings=Bounds(safe,"外观设置",0,0,1,1);
        appearanceSettings.gameObject.AddComponent<FlowerPageEntrance>();
        Label(Bounds(appearanceSettings,"标题",.08f,.849f,.92f,.951f),"外观样式",43,Ink);
        BuildStyleChoices("人物卡框",CardStyleKey,selectedCards,.510f,.767f,cardFrames);
        BuildStyleChoices("按钮样式",ButtonStyleKey,selectedButtons,.225f,.448f,themedButtons);
        AlbumButton(appearanceSettings,"返回设置",.286f,.088f,.714f,.152f,OpenMovementSettings);
        appearanceSettings.gameObject.SetActive(false);
    }

    void BuildStyleChoices(string title,string key,Text[] selections,float bottom,float top,List<SpringSurface> surfaces)
    {
        var strip=Bounds(appearanceSettings,title,.078f,bottom,.922f,top);
        var heading=Label(Bounds(strip,"标题",.016f,.756f,.98f,.988f),title,25,Ink);
        heading.alignment=TextAnchor.MiddleLeft;
        for(int i=0;i<StyleNames.Length;i++)
        {
            var style=(FrameStyle)i;
            float left=i*.342f;
            var sample=Bounds(strip,StyleNames[i],left,0,left+.316f,.728f);
            var rim=Surface(sample,Honey,true);
            var card=Inset(sample,"笺纸",1.75f);
            Surface(card,Parchment,true).SetGradient(new Color32(255,251,242,255),new Color32(244,228,219,255));
            bool isCard=key==CardStyleKey;
            var silhouette=Bounds(card,"样式预览",.20f,isCard?.37f:.45f,.80f,.90f);
            var border=Surface(silhouette,Honey,true).SetGradient(new Color32(225,188,145,255),new Color32(163,112,81,255));
            var paper=Surface(Inset(silhouette,"内边",3.6f),Parchment,true);
            border.SetFrameStyle(style);
            paper.SetFrameStyle(style);
            if(isCard)
            {
                paper.SetGradient(new Color32(253,233,228,255),new Color32(229,194,206,255));
                Blossom(Box(silhouette,"花纹",.5f,.50f,21,21));
            }
            else paper.SetGradient(new Color32(155,104,126,255),new Color32(99,49,75,255));
            Label(Bounds(card,"名称",.03f,.105f,.97f,.36f),StyleNames[i],19,Ink);
            selections[i]=Label(Bounds(card,"状态",.08f,.001f,.92f,.15f),"已选",14,Ink);
            MakeButton(sample,rim,()=>{
                PlayerPrefs.SetInt(key,(int)style);
                PlayerPrefs.Save();
                foreach(var surface in surfaces)surface.SetFrameStyle(style);
                ShowSelected(selections,style);
            });
        }
    }

    static void ShowSelected(Text[] selections,FrameStyle style)
    {
        for(int i=0;i<selections.Length;i++)
            selections[i].gameObject.SetActive(i==(int)style);
    }

    void OpenAppearance()
    {
        movementSettings.gameObject.SetActive(false);
        appearanceSettings.gameObject.SetActive(true);
        ShowSelected(selectedCards,ChosenStyle(CardStyleKey));
        ShowSelected(selectedButtons,ChosenStyle(ButtonStyleKey));
    }
}
