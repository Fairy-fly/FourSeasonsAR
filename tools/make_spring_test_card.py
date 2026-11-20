"""Build a print-matched spring tracking candidate without touching the supplied artwork."""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "assets/cards/spring_card.png"
OUTPUT = ROOT / "derived/cards/spring_track_v1.png"


def make_card():
    original = Image.open(SOURCE).convert("RGB")
    margin = 110
    width, height = original.size
    card = Image.new("RGB", (width + 2 * margin, height + 2 * margin), "#faece4")
    ink = ImageDraw.Draw(card)
    rng = random.Random(520)
    # Uneven branch silhouettes give each edge its own grayscale landmarks.
    palette = ["#503144", "#703a50", "#97546a", "#bb7984", "#81455a"]
    for start_y in range(-40, card.height + 120, 104):
        for edge in (0, card.width):
            direction = 1 if edge == 0 else -1
            origin = (edge, start_y + rng.randint(-30, 30))
            draw_sprig(ink, rng, origin, direction, palette)
    for start_x in range(-40, card.width + 120, 105):
        for edge in (0, card.height):
            origin = (start_x + rng.randint(-22, 22), edge)
            draw_sprig(ink, rng, origin, 1 if edge == 0 else -1, palette, vertical=True)
    ink.rectangle((margin - 4, margin - 4, width + margin + 3, height + margin + 3),
                  outline="#855064", width=4)
    card.paste(original, (margin, margin))
    return card


def draw_sprig(ink, rng, origin, direction, palette, vertical=False):
    length = rng.uniform(58, 106)
    lean = rng.uniform(-48, 48)

    def point(x, y):
        if vertical:
            x, y = y, x
        return round(origin[0] + x * direction), round(origin[1] + y * direction)

    ink.line([origin, point(length, lean)], fill=palette[0], width=4)
    for i in range(rng.randint(4, 7)):
        along = (i + 1) / 8
        x, y = length * along, lean * along
        tip = rng.uniform(12, 32) * (-1 if i % 2 else 1)
        ink.polygon([point(x, y), point(x + 6, y + tip * .75),
                     point(x + 20, y + tip), point(x + 13, y + tip * .2)],
                    fill=rng.choice(palette))
    cx, cy = point(length, lean)
    radius = rng.randint(6, 16)
    for i in range(5):
        angle = i * math.tau / 5
        x, y = cx + radius * math.cos(angle), cy + radius * math.sin(angle)
        ink.ellipse((x - radius * .65, y - radius * .65,
                     x + radius * .65, y + radius * .65), fill=rng.choice(palette[1:]))
    ink.ellipse((cx - 3, cy - 3, cx + 3, cy + 3), fill="#fbdb9d")


def make_square_card():
    # An irregular ink border supplies differently sized grayscale corners.
    rng = random.Random(419)
    sheet = Image.new("RGB", (800, 800), "#fff4eb")
    ink = ImageDraw.Draw(sheet)
    for _ in range(5000):
        x, y = rng.randrange(-20, 800), rng.randrange(-15, 800)
        size, value = rng.randrange(3, 22), rng.randrange(255)
        color = (int(45 + value * .8), int(18 + value * .8), int(38 + value * .76))
        points = [(x + rng.randrange(-size, size), y + rng.randrange(-size, size))
                  for _ in range(rng.randrange(3, 8))]
        ink.polygon(points, fill=color)
    with Image.open(SOURCE) as source:
        original = source.convert("RGB")
    original.thumbnail((520, 720))
    sheet.paste(original, ((800 - original.width) // 2, (800 - original.height) // 2))
    return sheet


def save_variant(card, output):
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        with Image.open(output) as existing:
            if existing.convert("RGB").tobytes() != card.tobytes() or existing.size != card.size:
                raise SystemExit("Output differs; choose a new versioned filename.")
    else:
        card.save(output)


if __name__ == "__main__":
    save_variant(make_card(), OUTPUT)
    save_variant(make_square_card(), OUTPUT.with_name("spring_track_v2.png"))
