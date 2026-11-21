"""Check that 回 retains both white spaces when the bundled UI font is rasterized."""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFont


def enclosed_spaces(font_path, size):
    font = ImageFont.truetype(str(font_path), size)
    tile = Image.new("L", (size * 2, size * 2))
    ImageDraw.Draw(tile).text((size // 2, size // 3), "回", font=font, fill=255)
    # Discard antialiasing, then flood the exterior to isolate closed spaces.
    mask = tile.point(lambda level: 255 if level >= 128 else 0)
    ImageDraw.floodfill(mask, (0, 0), 127)
    holes = 0
    for y in range(mask.height):
        for x in range(mask.width):
            if mask.getpixel((x, y)) == 0:
                holes += 1
                ImageDraw.floodfill(mask, (x, y), 127)
    return holes


if __name__ == "__main__":
    root = Path(__file__).resolve().parents[1]
    path = root / "UnityProject/Assets/Art/Typography/FlowerJourneyText.ttf"
    for size in (19, 22, 24, 36, 48):
        holes = enclosed_spaces(path, size)
        if holes != 2:
            raise SystemExit(f"U+56DE at {size}px: expected two enclosed spaces, found {holes}")
        print(f"U+56DE at {size}px: both enclosed spaces are open")
