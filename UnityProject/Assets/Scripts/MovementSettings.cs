using UnityEngine;
using UnityEngine.UI;

public static class MovementPreference
{
    public const string Key="fourSeasons.world.experimentalWalk";
    public const float DefaultPaceFactor=1.28f;
    public static readonly Setting Speed=new("speed",60,150,10,100,"%");
    public static readonly Setting Clearance=new("hoverHeight",25,70,5,35," 毫米");
    public static readonly Setting Amplitude=new("hoverBob",0,15,3,9," 毫米");

    public readonly struct Setting
    {
        public readonly string Key, Unit;
        public readonly int Min, Max, Step, Default;
        public int Value=>Mathf.Clamp(PlayerPrefs.GetInt(Key,Default),Min,Max);

        public Setting(string key,int min,int max,int step,int initial,string unit)
        {
            Key="fourSeasons.world."+key;
            Unit=unit;
            Min=min;
            Max=max;
            Step=step;
            Default=initial;
        }

        public void Change(int direction)
        {
            PlayerPrefs.SetInt(Key,Mathf.Clamp(Value+direction*Step,Min,Max));
            PlayerPrefs.Save();
        }
    }

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
    TuningRow speedRow,heightRow,bobRow;

    sealed class TuningRow
    {
        public MovementPreference.Setting setting;
        public Text value;
        public Button less,more;

        public void Refresh(bool enabled)
        {
            int current=setting.Value;
            value.text=current+setting.Unit;
            value.color=enabled ? Ink : new Color32(138,112,116,255);
            less.interactable=enabled && current>setting.Min;
            more.interactable=enabled && current<setting.Max;
        }
    }

    void BuildMovementSettings(RectTransform heading)
    {
        AlbumButton(heading,"设置",.052f,.94f,.287f,.99f,OpenMovementSettings,true);
        movementSettings=Bounds(safe,"移动设置",0,0,1,1);
        movementSettings.gameObject.AddComponent<FlowerPageEntrance>();
        Label(Bounds(movementSettings,"标题",.08f,.864f,.92f,.958f),"设置",45,Ink);
        Label(Bounds(movementSettings,"类别",.10f,.805f,.90f,.855f),"现实同行 · 移动方式",22,Ink);
        floatCard=MovementCard("漂浮","默认 · 花灵贴着地面轻轻漂浮",.689f,.789f,false,
            out floatTitle,out floatSelection);
        walkCard=MovementCard("步行","实验功能 · 测试阶段",.571f,.671f,true,
            out walkTitle,out walkSelection);
        speedRow=BuildTuningRow("移动速度",.450f,MovementPreference.Speed,false);
        heightRow=BuildTuningRow("离地高度",.351f,MovementPreference.Clearance,true);
        bobRow=BuildTuningRow("起伏幅度",.252f,MovementPreference.Amplitude,true);
        Label(Bounds(movementSettings,"漂浮设置说明",.09f,.167f,.91f,.225f),
            "高度和幅度仅在漂浮时生效",17,Ink);
        AlbumButton(movementSettings,"返回封面",.286f,.068f,.714f,.134f,()=>ShowScanner(false));
        movementSettings.gameObject.SetActive(false);
    }

    TuningRow BuildTuningRow(string title,float bottom,MovementPreference.Setting setting,bool hoverOnly)
    {
        var row=Bounds(movementSettings,title,.085f,bottom,.915f,bottom+.084f);
        Surface(row,Honey,true);
        var paper=Inset(row,"笺纸",1.6f);
        Surface(paper,Parchment,true).SetGradient(new Color32(255,251,243,255),new Color32(246,231,221,255));
        var label=Label(Bounds(row,"项目",.049f,.10f,.39f,.90f),title,24,Ink);
        label.alignment=TextAnchor.MiddleLeft;
        var value=Label(Bounds(row,"数值",.565f,.10f,.815f,.90f),"",20,Ink);
        Button StepButton(bool increase,float left)
        {
            var rt=Bounds(row,increase?"增加":"减少",left,.18f,left+.14f,.82f);
            var rim=Surface(rt,Honey,true);
            Surface(Inset(rt,"漆面",2.2f),Parchment,true).SetGradient(
                new Color32(254,244,238,255),new Color32(235,212,222,255));
            Paint(Box(rt,"横笔",.5f,.5f,14,2),Plum);
            if(increase)Paint(Box(rt,"竖笔",.5f,.5f,2,14),Plum);
            MakeButton(rt,rim,()=>{
                if(hoverOnly && MovementPreference.ExperimentalWalk)return;
                setting.Change(increase?1:-1);
                RefreshMovementSettings();
            });
            var button=rt.GetComponent<Button>();
            button.targetGraphic=rt.Find("漆面").GetComponent<SpringSurface>();
            return button;
        }
        return new TuningRow {setting=setting,value=value,less=StepButton(false,.425f),more=StepButton(true,.815f)};
    }

    SpringSurface MovementCard(string title,string note,float bottom,float top,bool walk,out Text label,out Text selection)
    {
        var card=Bounds(movementSettings,title,.085f,bottom,.915f,top);
        var rim=Surface(card,Honey,true);
        var face=Surface(Inset(card,"笺纸",2.1f),Parchment,true);
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
        SetAlbumGround();
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
        speedRow.Refresh(true);
        heightRow.Refresh(!walk);
        bobRow.Refresh(!walk);
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
