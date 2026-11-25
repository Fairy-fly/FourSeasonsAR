using UnityEngine;
using UnityEngine.UI;

// Share one neutral paper texture across full-bleed pages and tinted card backings.
public sealed class AlbumScenery : RawImage
{
    static Texture2D paper;

    public void SelectPaper(Color tint)
    {
        if (!paper) paper = MakePaper();
        texture = paper;
        color = tint;
        raycastTarget = false;
        RefreshCrop();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        RefreshCrop();
    }

    void RefreshCrop()
    {
        Rect rect = rectTransform.rect;
        if (!texture || rect.height <= 0 || rect.width <= 0) return;
        uvRect = SpringPage.CoverUV(rect.width/rect.height,1);
    }

    static Texture2D MakePaper()
    {
        const int size=512;
        var pixels=new Color32[size*size];
        var grain=new System.Random(2040);
        for(int y=0;y<size;y++)
        for(int x=0;x<size;x++)
        {
            float u=x/(float)(size-1),v=y/(float)(size-1);
            float cloud=Mathf.PerlinNoise(u*4.2f+.27f,v*5.3f+.13f);
            float fibers=Mathf.PerlinNoise(u*116+7,v*187+2);
            float fine=(float)grain.NextDouble();
            float light=Mathf.Exp(-5.2f*((u-.46f)*(u-.46f)+(v-.68f)*(v-.68f)));
            int value=Mathf.RoundToInt(232+8*cloud+8*fibers+4*fine+8*light);
            byte level=(byte)Mathf.Min(255,value);
            pixels[y*size+x]=new Color32(level,level,level,255);
        }
        var result=new Texture2D(size,size,TextureFormat.RGBA32,true)
            { name="Fine paper grain", wrapMode=TextureWrapMode.Clamp, filterMode=FilterMode.Trilinear,
                hideFlags=HideFlags.HideAndDontSave };
        result.SetPixels32(pixels);
        result.Apply(true,true);
        return result;
    }
}
