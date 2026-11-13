"""Сборка набора тайлов локации из сгенерированных в PixelLab исходников.

Запуск:  python3 build_biome.py <папка_с_исходниками> <config.json> <папка_вывода> <папка_лугового_набора>

Что делает:
  * приводит геометрию к игровой (ромб 128x64, стенка 40 px у нетронутых и
    фоновых тайлов, 30/20/10 px у стадий раскопок, центрирование в 128x128);
  * стенку для ВСЕХ тайлов берёт с одного якорного тайла (и наращивает её
    повторением слоёв, если исходник ниже нужного);
  * стадии раскопок собирает из верхних граней "раскопанных" тайлов,
    затемняя каждую следующую стадию на заданный процент;
  * сводит все тайлы к одной общей палитре;
  * перекрашивает тень под лентой забора под цвет грунта локации.
"""
from PIL import Image, ImageEnhance
import json, os, sys

W, DIAMOND_H = 128, 64
SIDE = {"intact": 40, "dug1": 30, "dug2": 20, "empty": 10}

def top_y(side): return (W - (DIAMOND_H + side)) // 2
def edge_y(x, t): return t + 32 + int(round(32 * (1 - abs(x - 63.5) / 64.0)))

def measure_side(img):
    """Высота стенки исходника по его габаритам (картинка центрирована)."""
    bb = img.getbbox()
    return W - 2 * 22 - DIAMOND_H if bb is None else None

def side_columns(anchor, anchor_side):
    """Для каждой колонки: список пикселей стенки сверху вниз и пиксель нижней обводки."""
    t = top_y(anchor_side); px = anchor.load(); cols = []
    for x in range(W):
        e = edge_y(x, t); col = []
        y = e
        while y < W and px[x, y][3] > 0:
            col.append(px[x, y]); y += 1
        if len(col) < 3: cols.append(([], None)); continue
        cols.append((col[:-1], col[-1]))          # тело стенки, обводка
    return cols

def draw_side(dst, cols, new_side):
    t = top_y(new_side)
    for x in range(W):
        body, outline = cols[x]
        if not body: continue
        e = edge_y(x, t)
        for k in range(new_side - 1):
            # слои повторяются по кругу, первые 2 строки (кромка под гранью) - только сверху
            src = body[k] if k < len(body) else body[2 + (k - len(body)) % max(1, len(body) - 2)]
            if e + k < W: dst[x, e + k] = src
        if e + new_side - 1 < W: dst[x, e + new_side - 1] = outline

def upper_part(img, src_side):
    """Всё, что выше нижней кромки ромба: верхняя грань и то, что на ней стоит."""
    t = top_y(src_side); out = Image.new("RGBA", (W, W)); s = img.load(); d = out.load()
    for x in range(W):
        e = edge_y(x, t)
        for y in range(0, min(e, W)):
            if s[x, y][3] > 0: d[x, y] = s[x, y]
    return out, t

def compose(src, src_side, cols, new_side, brightness=1.0):
    out = Image.new("RGBA", (W, W)); draw_side(out.load(), cols, new_side)
    up, t_src = upper_part(src, src_side)
    if brightness != 1.0:
        a = up.getchannel("A"); up = ImageEnhance.Brightness(up.convert("RGB")).enhance(brightness).convert("RGBA"); up.putalpha(a)
    dy = top_y(new_side) - t_src
    clipped = 0
    if dy < 0:
        bb = up.getbbox(); clipped = max(0, -dy - bb[1]) if bb else 0
    layer = Image.new("RGBA", (W, W)); layer.paste(up, (0, dy))
    out.alpha_composite(layer)
    return out, clipped

def unify_palette(images, colors):
    """Одна палитра на весь набор: близкие оттенки из разных генераций сливаются."""
    strip = Image.new("RGB", (W * len(images), W))
    for i, im in enumerate(images): strip.paste(im.convert("RGB"), (i * W, 0), im.getchannel("A"))
    # палитру строим только по непрозрачным пикселям
    opaque = [p[:3] for im in images for p in im.getdata() if p[3] > 0]
    sample = Image.new("RGB", (len(opaque), 1)); sample.putdata(opaque)
    pal = sample.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, dither=Image.Dither.NONE)
    out = []
    for im in images:
        q = im.convert("RGB").quantize(palette=pal, dither=Image.Dither.NONE).convert("RGBA")
        q.putalpha(im.getchannel("A").point(lambda a: 255 if a >= 128 else 0))
        out.append(q)
    return out

def recolor_fence_shadow(img, shadow_rgb):
    """Тёмно-зелёная тень под лентой (трава луга) -> тень цвета грунта локации."""
    out = img.copy(); px = out.load()
    for y in range(W):
        for x in range(W):
            r, g, b, a = px[x, y]
            if a > 0 and g > r and g > b: px[x, y] = (*shadow_rgb, a)
    return out

def main():
    src_dir, cfg_path, out_dir, meadow_dir = sys.argv[1:5]
    cfg = json.load(open(cfg_path, encoding="utf-8"))
    os.makedirs(out_dir, exist_ok=True)
    load = lambda name: Image.open(os.path.join(src_dir, name)).convert("RGBA")
    src_side = cfg["source_side"]
    cols = side_columns(load(cfg["anchor"]), src_side)

    names, images, report = [], [], []
    def add(name, src, side, brightness=1.0):
        img, clipped = compose(load(src), src_side, cols, side, brightness)
        names.append(name); images.append(img)
        if clipped: report.append(f"{name}: обрезано сверху {clipped} px ({src})")

    for i, src in enumerate(cfg["intact"], 1): add(f"intact_{i}", src, SIDE["intact"])
    for i, src in enumerate(cfg["back"], 1): add(f"back_{i}", src, SIDE["intact"])
    step = 1.0 - cfg["darken_per_stage"]
    for stage, power in (("dug1", 0), ("dug2", 1), ("empty", 2)):
        for i, src in enumerate(cfg[stage], 1): add(f"{stage}_{i}", src, SIDE[stage], step ** power)

    images = unify_palette(images, cfg["palette_colors"])
    for name, img in zip(names, images): img.save(os.path.join(out_dir, name + ".png"))

    for f in ("border.png", "border_edge.png", "border_edge_side.png"):
        recolor_fence_shadow(Image.open(os.path.join(meadow_dir, f)).convert("RGBA"),
                             tuple(cfg["fence_shadow_rgb"])).save(os.path.join(out_dir, f))
    print(f"готово: {len(names)} тайлов + 3 забора -> {out_dir}")
    for line in report: print("  !", line)

if __name__ == "__main__":
    main()
