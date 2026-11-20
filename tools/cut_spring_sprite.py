"""Extract the front view from the supplied spring turnaround (no image synthesis)."""
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage


def main():
    root = Path(__file__).resolve().parents[1]
    source = Image.open(root / "assets/turnarounds/spring_three_views.jpg").convert("RGB")
    area = (93, 17, 350, 529)
    sprite = source.crop(area)
    rgb = np.asarray(sprite, dtype=float)
    distance = np.max(np.abs(rgb - (239, 236, 225)), axis=2)
    # Clip printed labels, the decorative plant and the ground shadow.
    outline = Image.new("L", source.size)
    points = [(95,183),(115,146),(99,114),(104,67),(153,47),(182,27),(218,19),
              (259,19),(276,45),(313,49),(328,110),(301,141),(345,176),(348,220),
              (328,252),(333,279),(309,285),(327,307),(305,312),(310,348),
              (265,365),(253,394),(267,439),(301,447),(318,486),(317,509),
              (301,521),(250,526),(162,525),(132,518),(130,490),(151,468),
              (182,452),(206,447),(190,411),(180,358),(149,357),(121,340),
              (133,309),(150,283),(117,293),(102,269),(105,238),(134,228),
              (98,217)]
    ImageDraw.Draw(outline).polygon(points, fill=255)
    mask = (distance > 12) & (np.asarray(outline.crop(area)) > 0)
    mask = ndimage.binary_closing(mask, iterations=1)
    labels, count = ndimage.label(mask)
    main_shape = 1 + np.argmax(np.bincount(labels.ravel(), minlength=count + 1)[1:])
    mask = labels == main_shape
    # Preserve the large openings between hair and body, fill enclosed highlights.
    holes, _ = ndimage.label(ndimage.binary_fill_holes(mask) & ~mask)
    sizes = np.bincount(holes.ravel())
    mask |= (holes > 0) & (sizes[holes] < 140)
    # Pale face, petals on the skirt and knees share colors with the paper.
    interiors = Image.new("L", source.size)
    brush = ImageDraw.Draw(interiors)
    for polygon in [
        [(173,126),(233,108),(259,138),(245,167),(211,190),(182,169)],
        [(199,281),(245,275),(272,315),(256,331),(174,327)],
        [(184,335),(210,333),(212,378),(207,409),(213,443),(201,440),(194,394)],
        [(219,334),(242,335),(239,380),(246,411),(230,424),(217,369)],
        [(144,498),(298,491),(303,508),(282,516),(166,515)]
    ]:
        brush.polygon(polygon, fill=255)
    mask |= np.asarray(interiors.crop(area)) > 0
    sprite.putalpha(Image.fromarray((mask * 255).astype("uint8")).filter(ImageFilter.GaussianBlur(.55)))
    target = root / "derived/characters/spring_front.png"
    target.parent.mkdir(exist_ok=True, parents=True)
    sprite.crop(sprite.getbbox()).save(target)


if __name__ == "__main__":
    main()
