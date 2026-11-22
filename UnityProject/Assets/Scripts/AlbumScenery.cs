using UnityEngine;
using UnityEngine.UI;

// Both atlases have full-bleed panels. Crop within the selected panel as the screen changes.
public sealed class AlbumScenery : RawImage
{
    Rect panel;
    float horizontalAlignment;

    public void Select(Texture2D atlas, Rect region,float horizontalAlignment = .5f)
    {
        texture = atlas;
        panel = region;
        this.horizontalAlignment = horizontalAlignment;
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
        if (!texture || panel.width == 0 || rect.height <= 0 || rect.width <= 0) return;
        var crop = SpringPage.CoverUV(rect.width/rect.height,
            texture.width*panel.width/(texture.height*panel.height));
        uvRect = new Rect(panel.x + (1-crop.width)*horizontalAlignment*panel.width,panel.y + crop.y*panel.height,
            crop.width*panel.width,crop.height*panel.height);
    }
}
