"""Extract mobile textures from the same GLB material used by each Blender model."""
import io
import json
from pathlib import Path
import struct
from PIL import Image

root = Path(__file__).resolve().parents[2]
for path in sorted((root/"assets/models/hunyuan").glob("*.glb")):
    folder = root / "UnityProject/Assets/Resources/Gallery" / path.stem
    folder.mkdir(parents=True, exist_ok=True)
    with path.open("rb") as f:
        f.seek(12)
        size, kind = struct.unpack("<II", f.read(8))
        assert kind == 0x4E4F534A
        gltf = json.loads(f.read(size))
        size, kind = struct.unpack("<II", f.read(8))
        assert kind == 0x004E4942
        start = f.tell()
        material, = gltf["materials"]
        pbr = material["pbrMetallicRoughness"]
        for name, resolution, slot in (("Color", 2048, pbr["baseColorTexture"]),
                                       ("Normal", 1024, material["normalTexture"]),
                                       ("Surface", 1024, pbr["metallicRoughnessTexture"])):
            image = gltf["images"][gltf["textures"][slot["index"]]["source"]]
            view = gltf["bufferViews"][image["bufferView"]]
            f.seek(start + view.get("byteOffset", 0))
            with Image.open(io.BytesIO(f.read(view["byteLength"]))) as src:
                rgb = src.convert("RGB")
                rgb.thumbnail((resolution, resolution), Image.Resampling.LANCZOS)
                rgb.save(folder / (name + ".png"))
