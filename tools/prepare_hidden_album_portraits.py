"""Rebuild the two 2D album sprites from the HD front references and archived alpha masks."""
from hashlib import sha256
from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
SOURCES = {
    "hidden_shijian": "a999e76c90d8c3f4ffde9ed81329a4fcde8ada3867ecb4d84baeb411cf8c1f4c",
    "hidden_xingmian": "0bb8af995d28af4c6c2eca84f447d66f4512164c33fa1564cee888dfe86a5c9c",
}


def prepare(role, digest):
    originals = ROOT / "assets/portraits"
    path = originals / (role + "_front.png")
    if sha256(path.read_bytes()).hexdigest() != digest:
        raise ValueError(f"Unexpected front reference for {role}")
    with Image.open(path) as source:
        rgb = source.convert("RGB")
    # The archived 1024px IS-Net matte preserves the gaps between locks and the fine halo.
    with Image.open(originals / (role + "_silhouette.png")) as mask:
        alpha = mask.convert("L").resize(rgb.size, Image.Resampling.BICUBIC)
    alpha = alpha.point(lambda v: 0 if v < 4 else v)
    left, top, right, bottom = alpha.getbbox()
    bounds = (max(0,left-8), max(0,top-8), min(rgb.width,right+8), min(rgb.height,bottom+8))
    portrait = rgb.crop(bounds)
    portrait.putalpha(alpha.crop(bounds))
    target = ROOT / "UnityProject/Assets/Resources/Gallery" / role / "Portrait.png"
    portrait.save(target)
    with Image.open(target) as saved:
        if saved.convert("RGB").tobytes() != rgb.crop(bounds).tobytes():
            raise ValueError(f"RGB mismatch for {role}")
    print(role, bounds, portrait.size)


if __name__ == "__main__":
    for role, digest in SOURCES.items():
        prepare(role, digest)
