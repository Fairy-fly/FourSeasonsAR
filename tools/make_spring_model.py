"""Build the spring study. Requires Pillow, NumPy, SciPy, PyMCubes and fast-simplification."""
from pathlib import Path

import mcubes
import fast_simplification
import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "UnityProject/Assets/Resources/Models"
W, H = 280, 520
FRONT_LEVELS = [0, 24, 184, 234, 350, 452, 521, 540]


def panel(sheet, center, levels, interiors=()):
    """Register the chin, waistband, skirt hem, shoes and pedestal to the front elevation."""
    source_y = np.interp(np.arange(H) + 12, FRONT_LEVELS, levels)
    x = np.arange(W) + center - W // 2
    rgb = np.stack([ndimage.map_coordinates(sheet[:, :, c], np.broadcast_arrays(
        source_y[:, None], x[None, :]), order=1) for c in range(3)], axis=2)
    background = np.median(sheet[312:325, 650:666, :], axis=(0, 1))
    mask = np.max(np.abs(rgb.astype(float) - background), axis=2) > 15
    mask[:8] = mask[513:] = False
    # The printed floor shadow extends beyond the pink plinth.
    floor = np.arange(H)[:, None] > 481
    mask &= ~(floor & (rgb[:, :, 0].astype(float) - rgb[:, :, 1] < 10))
    mask = ndimage.binary_closing(mask, iterations=2)
    labels, n = ndimage.label(mask)
    counts = np.bincount(labels.ravel(), minlength=n + 1)
    mask = labels == (1 + counts[1:].argmax())
    holes, n = ndimage.label(ndimage.binary_fill_holes(mask) & ~mask)
    areas = np.bincount(holes.ravel(), minlength=n + 1)
    mask |= (holes > 0) & (areas[holes] < 1150)
    # The cream dress, knees and pedestal are close to the paper color. Restore
    # their continuous interiors inside the visible outlines of this elevation.
    fill = Image.new("L",(W,H))
    pen = ImageDraw.Draw(fill)
    for polygon in interiors:
        pen.polygon([(px-center+W//2,np.interp(py,levels,FRONT_LEVELS)-12) for px,py in polygon],fill=255)
    mask |= np.asarray(fill)>0
    return rgb, mask


def sections(mask):
    """Give each horizontal silhouette interval its own rounded cross section."""
    result = np.full(mask.shape, 2.0)
    for row, line in enumerate(mask):
        breaks = np.flatnonzero(np.diff(np.r_[False, line, False]))
        for lo, hi in breaks.reshape(-1, 2):
            radius = (hi - lo) / 2
            result[row, lo:hi] = np.abs(np.arange(lo, hi) + .5 - (lo + hi) / 2) / radius
    return result


def projection_weights(side_mask, row, depth):
    """Use one stable paint blend per surface point, including at three-quarter views."""
    azimuth = np.zeros(side_mask.shape, dtype=float)
    for y, line in enumerate(side_mask):
        for lo, hi in np.flatnonzero(np.diff(np.r_[False,line,False])).reshape(-1,2):
            azimuth[y,lo:hi] = (np.arange(lo,hi)+.5-(hi+lo)/2) / ((hi-lo)/2)
    z = ndimage.map_coordinates(azimuth,[row,depth],order=1)
    amount = np.clip((np.abs(z)-.21)/.30,0,1)
    amount = amount*amount*(3-2*amount)
    return np.c_[np.where(z>=0,amount,0),1-amount,np.where(z<0,amount,0)]


def main():
    OUTPUT.mkdir(exist_ok=True, parents=True)
    source = np.asarray(Image.open(ROOT / "assets/turnarounds/spring_three_views.jpg").convert("RGB"))
    front, _ = panel(source, 221, FRONT_LEVELS)
    front_mask = np.zeros((H, W), dtype=bool)
    original = np.asarray(Image.open(ROOT / "derived/characters/spring_front.png").convert("RGBA"))
    front_mask[7:7+original.shape[0], 15:15+original.shape[1]] = original[:, :, 3] > 80
    side, side_mask = panel(source, 537, [0, 33, 198, 242, 358, 452, 521, 540], [
        [(507,94),(573,83),(609,118),(597,151),(584,178),(575,189),(553,187),(527,167),(496,148)],
        [(540,184),(558,183),(581,201),(590,208),(578,236),(579,255),(565,279),(524,283),(514,262),(527,211)],
        [(537,255),(576,255),(592,280),(610,313),(615,346),(579,351),(552,346),(522,350),(463,355),(491,310)],
        [(537,344),(576,348),(564,371),(550,399),(538,433),(529,451),(513,447),(522,417),(533,381)],
        [(508,431),(533,438),(544,455),(563,477),(557,489),(532,482),(492,459)],
        [(468,478),(501,472),(531,479),(581,473),(606,486),(614,505),(604,514),(573,519),(491,518),(456,510),(454,498)]
    ])
    back, back_mask = panel(source, 835, [0, 29, 190, 245, 358, 452, 521, 540])
    # Bleed the nearest pigment across the UV borders of the three photographs.
    atlas = []
    for rgb, mask in [(front, front_mask), (side, side_mask), (back, back_mask)]:
        nearest = ndimage.distance_transform_edt(~mask, return_distances=False, return_indices=True)
        atlas.append(rgb[tuple(nearest)])
    Image.fromarray(np.concatenate(atlas, axis=1)).save(OUTPUT / "SpringTurnaround.png")

    step = 3
    f = sections(front_mask)[::step, ::step]
    s = sections(side_mask)[::step, ::step]
    field = 1 - f[:, :, None] ** 2 - s[:, None, :] ** 2
    # At shoulder height, the side elevation separates the trailing curls from the torso.
    # Constrain their depth to avoid repeating the face and torso inside either curl.
    y, x, z = np.ogrid[:field.shape[0], :field.shape[1], :field.shape[2]]
    sides = np.abs(x * step - W / 2)
    hair_band = np.clip((y * step - 160) / 25, 0, 1) * np.clip((289 - y * step) / 23, 0, 1)
    curl = np.clip((sides - 45) / 24, 0, 1) * hair_band
    field = np.minimum(field, 1 - curl * ((z * step - 68) / 45) ** 2)
    torso = np.clip((55 - sides) / 20, 0, 1) * hair_band
    field = np.minimum(field, 1 - torso * ((z * step - 161) / 58) ** 2)
    field = ndimage.gaussian_filter(np.clip(field, -1, 1), .83)
    field[[0,-1],:,:] = field[:,[0,-1],:] = field[:,:,[0,-1]] = -1
    vertices, faces = mcubes.marching_cubes(field, 0)
    vertices, faces = fast_simplification.simplify(vertices, faces.astype(np.int32), target_count=22000)
    row, col, depth = (vertices * step).T
    # OBJ stores +Z towards the front. The gallery uses UVs for projection coordinates,
    # so its multi-view paint is independent of Unity's OBJ axis conversion.
    xyz = np.c_[(col - W/2) / 200, (H - row) / 200, (depth - W/2) / 200]
    # Front UV is measured in source pixels. A third coordinate (side depth) is in model space.
    uv = np.c_[(col+.5)/W, 1-(row+.5)/H]
    triangles = xyz[faces]
    if np.einsum("ij,ij->", triangles[:,0], np.cross(triangles[:,1],triangles[:,2])) < 0:
        faces = faces[:,::-1]
    with (ROOT / "derived/characters/SpringStudy.obj").open("w", encoding="ascii", newline="\n") as obj:
        obj.write("o SpringStudy\n")
        weights = projection_weights(side_mask,row,depth)
        np.savetxt(obj, np.c_[xyz,weights], fmt="v %.6f %.6f %.6f %.5f %.5f %.5f")
        np.savetxt(obj, uv, fmt="vt %.6f %.6f")
        obj.write("s 1\n")
        for a,b,c in faces+1:
            obj.write(f"f {a}/{a} {b}/{b} {c}/{c}\n")
    print(f"{len(vertices)} vertices, {len(faces)} triangles")


if __name__ == "__main__":
    main()
