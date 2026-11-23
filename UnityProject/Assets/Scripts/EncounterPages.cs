using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SpringPage
{
    RectTransform bubble, management;
    Text floatingName, floatingStory, floatingHint, confirmText;
    GameObject selection, confirmation;
    readonly Dictionary<string,string> roster = new();
    readonly Dictionary<string,Text> rows = new();
    readonly HashSet<string> selected = new();
    Image flight;
    Vector2 flightStart;
    float flightAt = -10;

    void BuildEncounters()
    {
        bubble=Box((RectTransform)scanner.transform,"随行花笺",0,0,150,69);
        Surface(bubble,new Color32(253,244,230,226),true);
        floatingName=Label(Bounds(bubble,"名字",.05f,.63f,.95f,.98f),"",21,Ink,brushFont);
        floatingStory=Label(Bounds(bubble,"花语",.05f,.31f,.95f,.65f),"",15,Ink);
        floatingHint=Label(Bounds(bubble,"点击提示",.03f,.025f,.97f,.34f),"",14,Plum);
        bubble.gameObject.SetActive(false);
        var flightRect=Box((RectTransform)scanner.transform,"收录飞瓣",0,0,17,17);
        flight=flightRect.gameObject.AddComponent<Image>();
        flight.raycastTarget=false;
        flight.preserveAspect=true;
        flight.enabled=false;
        AlbumButton(index,"···",.845f,.81f,.935f,.851f,OpenManagement);
        management=Bounds(index,"管理收录",0,0,1,1);
        var curtain=management.gameObject.AddComponent<Image>();
        curtain.color=new Color32(48,35,48,170);
        var panel=Bounds(management,"管理花笺",.075f,.21f,.925f,.79f);
        Surface(panel,Parchment,true);
        Label(Bounds(panel,"题名",.08f,.81f,.92f,.96f),"管理收录",33,Ink,brushFont);
        selection=Bounds(panel,"名单",.03f,.04f,.97f,.80f).gameObject;
        confirmation=Bounds(panel,"确认页",.05f,.04f,.95f,.80f).gameObject;
        foreach(var c in visit.characters) roster[c.referenceName]=c.characterName;
        roster[AlbumModelViewer.TimeKeeper]="时见";
        roster[AlbumModelViewer.StarDreamer]="星眠";
        int i=0;
        foreach(var entry in roster)
        {
            var id=entry.Key;
            float x=i%2==0 ? .04f:.52f, top=.97f-.19f*(i/2);
            var button=AlbumButton((RectTransform)selection.transform,entry.Value,x,top-.15f,x+.44f,top,()=>{
                if(!visit.Album.Contains(id))return;
                if(!selected.Add(id))selected.Remove(id);
                RefreshRows();
            });
            rows[id]=button.Find("按钮文字").GetComponent<Text>();
            i++;
        }
        AlbumButton((RectTransform)selection.transform,"取消",.05f,.02f,.46f,.19f,CloseManagement);
        AlbumButton((RectTransform)selection.transform,"重置所选",.54f,.02f,.95f,.19f,()=>{
            if(selected.Count==0)return;
            var names=new List<string>();
            foreach(var id in selected)names.Add(roster[id]);
            confirmText.text="重新收录以下角色？\n"+string.Join("、",names);
            selection.SetActive(false);
            confirmation.SetActive(true);
        });
        confirmText=Label(Bounds((RectTransform)confirmation.transform,"名单",.05f,.3f,.95f,.95f),"",22,Ink);
        AlbumButton((RectTransform)confirmation.transform,"返回",.04f,.02f,.46f,.19f,()=>{
            selection.SetActive(true);confirmation.SetActive(false);
        });
        AlbumButton((RectTransform)confirmation.transform,"确认重置",.54f,.02f,.96f,.19f,()=>{
            visit.Album.Reset(selected);
            CloseManagement();
            OpenAlbum();
        });
        CloseManagement();
    }

    void RefreshRows()
    {
        foreach(var row in rows)
        {
            bool owned=visit.Album.Contains(row.Key);
            row.Value.text=(selected.Contains(row.Key)?"+ ":"")+roster[row.Key];
            row.Value.transform.parent.GetComponent<Button>().interactable=owned;
            row.Value.color=owned?Ink:new Color32(147,136,130,255);
        }
    }
    void OpenManagement()
    {
        selected.Clear();RefreshRows();selection.SetActive(true);
        confirmation.SetActive(false);management.gameObject.SetActive(true);
    }
    void CloseManagement(){selected.Clear();management.gameObject.SetActive(false);}

    public void FollowSpirit(SpringSpirit spirit,bool direct,Camera camera)
    {
        if(!spirit || !direct || !camera || !scanner.activeSelf){bubble.gameObject.SetActive(false);return;}
        Vector3 shoulder=spirit.transform.TransformPoint(new Vector3(0,.078f,0));
        Vector3 viewport=camera.WorldToViewportPoint(shoulder);
        if(viewport.z<=0 || viewport.x<0 || viewport.x>1 || viewport.y<0 || viewport.y>1)
        {bubble.gameObject.SetActive(false);return;}
        var host=(RectTransform)scanner.transform;
        bool shortForm=viewport.z>.85f;
        bool owned=visit.Album.Contains(spirit.referenceName);
        floatingName.text=spirit.characterName;
        floatingStory.gameObject.SetActive(!shortForm);
        floatingStory.text=spirit.excerpt;
        floatingHint.text=owned?"轻触回应":"点击收录进四季花册~";
        float w=Mathf.Min(host.rect.width*.46f,172),h=shortForm?52:77;
        bubble.sizeDelta=new Vector2(w,h);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(host,camera.WorldToScreenPoint(shoulder),null,out var local);
        var point=local-host.rect.min;
        float direction=point.x<host.rect.width*.5f?1:-1;
        float shoulderGap=Mathf.Clamp(28/Mathf.Max(.2f,viewport.z),20,99);
        bubble.anchoredPosition=new Vector2(Mathf.Clamp(point.x+direction*(w/2+shoulderGap),w/2+3,host.rect.width-w/2-3),
            Mathf.Clamp(point.y+h*.2f,h/2+3,host.rect.height-h/2-3));
        bubble.gameObject.SetActive(true);
    }

    public void FlyToAlbum(SpringSpirit c)
    {
        flight.sprite=c.petal;
        flightStart=bubble.anchoredPosition;
        flightAt=Time.unscaledTime;
        flight.enabled=true;
    }
    void UpdateFlight()
    {
        if(!flight || !flight.enabled)return;
        float t=Mathf.Clamp01((Time.unscaledTime-flightAt)/.75f);
        var host=(RectTransform)scanner.transform;
        // The destination coincides with the collection notification.
        flight.rectTransform.anchoredPosition=Vector2.Lerp(flightStart,Vector2.Scale(host.rect.size,new Vector2(.66f,.89f)),t)+Vector2.up*(68*Mathf.Sin(Mathf.PI*t));
        flight.rectTransform.localRotation=Quaternion.Euler(0,0,t*220);
        if(t>=1)flight.enabled=false;
    }
}
