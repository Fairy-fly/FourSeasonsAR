using UnityEngine;
using UnityEngine.UI;

public sealed partial class SpringPage
{
    RectTransform worldPage, summon, worldControls, swap;
    Text worldMessage;

    void BuildWorldPage()
    {
        summon=AlbumButton(leaf,"来到现实",.29f,.185f,.71f,.239f,()=>visit.Invite(detailId));
        worldPage=Bounds(safe,"现实同行",0,0,1,1);
        worldControls=Bounds(worldPage,"控制栏",.04f,.905f,.96f,.98f);
        AlbumButton(worldControls,"返回花册",0,0,.248f,1,()=>visit.LeaveWorld());
        AlbumButton(worldControls,"找回",.267f,0,.458f,1,()=>visit.Travel?.FindActor());
        AlbumButton(worldControls,"重新放置",.477f,0,.754f,1,()=>visit.Travel?.ResetSite());
        swap=AlbumButton(worldControls,"下一位",.774f,0,1,1,()=>visit.Travel?.SwitchRole());
        var caption=Bounds(worldPage,"同行提示",.06f,.050f,.94f,.122f);
        Surface(caption,Glass,true);
        worldMessage=Label(Inset(caption,"提示",4),"",18,Parchment);
        worldPage.gameObject.SetActive(false);
    }
    public bool WorldConsumes(Vector2 point)=>worldControls && RectTransformUtility.RectangleContainsScreenPoint(worldControls,point);
    public void WorldHint(string text){if(worldMessage.text!=text)worldMessage.text=text;}

    public void WorldDisplay(bool open)
    {
        if(open)
        {
            swap.gameObject.SetActive(CompanionProfile.OwnedCount(visit.Album)>=2);
            modelViewer.Close();home.SetActive(false);coverLayer.SetActive(false);
            scanner.SetActive(false);albumPaper.SetActive(false);album.gameObject.SetActive(false);
        }
        worldPage.gameObject.SetActive(open);
    }
    public void WorldReturn(string role)
    {
        WorldDisplay(false);
        OpenAlbum();
        if(string.IsNullOrEmpty(role) || !visit.Album.Contains(role))return;
        if(role==AlbumModelViewer.TimeKeeper || role==AlbumModelViewer.StarDreamer)
        {
            OpenHiddenDetail(role);
            return;
        }
        foreach(var c in visit.characters)
            if(c.referenceName==role){OpenDetail(c);break;}
    }
    void ArrangeActions(string id)
    {
        bool canInvite=visit.Album.Contains(id) && FlowerCompanion.IsAvailable(id);
        summon.gameObject.SetActive(canInvite);
        if(!canInvite)return;
        detailBack.anchorMin=new Vector2(.035f,.028f);detailBack.anchorMax=new Vector2(.325f,.101f);
        modelSwitch.anchorMin=new Vector2(.35f,.028f);modelSwitch.anchorMax=new Vector2(.64f,.101f);
        summon.anchorMin=new Vector2(.665f,.028f);summon.anchorMax=new Vector2(.965f,.101f);
    }
}
