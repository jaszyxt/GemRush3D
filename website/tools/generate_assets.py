"""Gem Rush 3D website asset generator.

Copies gameplay screenshots from Assets/Screenshots, derives the favicon set
from the game's identity icon, and composes the 1200x630 social share card in
the game's exact palette (ArtLib is law).

Run from the repo root:  python website/tools/generate_assets.py
Requires: Pillow
"""

from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

ROOT = Path(__file__).resolve().parents[2]
SRC_SHOTS = ROOT / "Assets" / "Screenshots"
SRC_ICON = ROOT / "Assets" / "Textures" / "AppIcon.png"
OUT = ROOT / "website" / "assets" / "img"

# ArtLib palette (docs/Art-Direction.md: icon colors are the marketing colors)
SKY_TOP = (64, 117, 237)      # #4075ED
SKY_BOT = (158, 212, 255)     # #9ED4FF
GRASS = (92, 184, 87)         # #5CB857
GOLD = (255, 214, 64)         # #FFD640
GEM_PINK = (250, 77, 191)     # #FA4DBF
GEM_PINK_LIGHT = (255, 179, 204)
CLOUD_WHITE = (247, 250, 255)
SUN = (255, 242, 184)
PUMPINK_OUTLINE = (30, 30, 40)

SHOTS = {
    "urp-after-gameplay-final.png": "gemrush-gameplay-first-steps.png",
    "urp-after-menu.png": "gemrush-menu.png",
    "urp-after-gameplay.png": "gemrush-gameplay-day.png",
    "urp-after-undercloud.png": "gemrush-undercloud.png",
    "bsides-nightfall.png": "gemrush-b-side-nightfall.png",
}

FONT_CANDIDATES = [
    "C:/Windows/Fonts/arialbd.ttf",
    "C:/Windows/Fonts/calibrib.ttf",
    "C:/Windows/Fonts/segoeuib.ttf",
    "/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf",
]


def load_font(size: int) -> ImageFont.FreeTypeFont:
    for path in FONT_CANDIDATES:
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def copy_screenshots() -> None:
    shots_dir = OUT / "screenshots"
    shots_dir.mkdir(parents=True, exist_ok=True)
    for src_name, dest_name in SHOTS.items():
        src = SRC_SHOTS / src_name
        img = Image.open(src).convert("RGB")
        img.save(shots_dir / dest_name, optimize=True)
        print(f"  {dest_name}: {img.width}x{img.height}")


def favicons() -> None:
    icon = Image.open(SRC_ICON).convert("RGBA")
    icons_dir = OUT / "icons"
    icons_dir.mkdir(parents=True, exist_ok=True)
    for size in (16, 32, 48, 180, 192, 512):
        resized = icon.resize((size, size), Image.LANCZOS)
        name = "apple-touch-icon.png" if size == 180 else f"icon-{size}.png"
        resized.save(icons_dir / name, optimize=True)
    icon.resize((16, 16), Image.LANCZOS).save(
        icons_dir / "favicon-16.png", optimize=True
    )
    icon.resize((32, 32), Image.LANCZOS).save(
        icons_dir / "favicon-32.png", optimize=True
    )
    icon.save(
        icons_dir / "favicon.ico",
        sizes=[(16, 16), (32, 32), (48, 48)],
    )
    print("  favicon set written")


def gem(dra: ImageDraw.ImageDraw, cx: int, cy: int, r: int) -> None:
    """Draw an octahedron-style sun-gem (two nested diamonds)."""
    dra.polygon(
        [(cx, cy - r), (cx + r * 0.75, cy), (cx, cy + r), (cx - r * 0.75, cy)],
        fill=GEM_PINK,
    )
    rr = int(r * 0.5)
    dra.polygon(
        [(cx, cy - rr), (cx + rr * 0.75, cy), (cx, cy + rr), (cx - rr * 0.75, cy)],
        fill=GEM_PINK_LIGHT,
    )


def cloud(layer: Image.Image, cx: int, cy: int, s: int, alpha: int = 200) -> None:
    c = Image.new("RGBA", (s * 3, s * 2), (0, 0, 0, 0))
    d = ImageDraw.Draw(c)
    col = (*CLOUD_WHITE, alpha)
    d.ellipse((0, s * 0.5, s * 1.2, s * 1.5), fill=col)
    d.ellipse((s * 0.6, 0, s * 2.4, s * 1.6), fill=col)
    d.ellipse((s * 1.8, s * 0.5, s * 3, s * 1.5), fill=col)
    layer.alpha_composite(c, (int(cx - s * 1.5), int(cy - s)))


def og_image() -> None:
    W, H = 1200, 630
    img = Image.new("RGBA", (W, H))
    # Sky gradient
    top, bottom = SKY_TOP, SKY_BOT
    for y in range(H):
        t = y / H
        row = tuple(int(top[i] + (bottom[i] - top[i]) * t) for i in range(3))
        ImageDraw.Draw(img).line([(0, y), (W, y)], fill=(*row, 255))

    # Sun + clouds
    d = ImageDraw.Draw(img)
    d.ellipse((950, 40, 1110, 200), fill=SUN)
    cloud_layer = Image.new("RGBA", (W, H))
    cloud(cloud_layer, 160, 110, 70)
    cloud(cloud_layer, 480, 70, 46, alpha=160)
    cloud(cloud_layer, 1040, 330, 54, alpha=140)
    img.alpha_composite(cloud_layer)

    # Floating island at bottom
    d = ImageDraw.Draw(img)
    d.ellipse((-120, H - 150, W + 120, H + 210), fill=GRASS)
    d.ellipse((-60, H - 70, W + 60, H + 300), fill=(128, 94, 64))  # dirt #805E40

    # Pink gems sprinkled on the grass
    d = ImageDraw.Draw(img)
    gem(d, 560, 565, 30)
    gem(d, 645, 592, 20)
    gem(d, 1060, 570, 26)

    # Identity icon card, slightly tilted, soft shadow. The card carries its
    # own rounded-corner alpha, so it can be rotated and composited as-is.
    icon = Image.open(SRC_ICON).convert("RGBA").resize((330, 330), Image.LANCZOS)
    mask = Image.new("L", (330, 330), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, 330, 330), radius=50, fill=255)
    card_w = card_h = 340
    card = Image.new("RGBA", (card_w, card_h), (0, 0, 0, 0))
    card.paste(icon, (5, 5), mask)
    shadow = Image.new("RGBA", img.size, (0, 0, 0, 0))
    ImageDraw.Draw(shadow).rounded_rectangle(
        (812, 192, 812 + card_w, 192 + card_h), radius=52, fill=(20, 30, 60, 110)
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(18))
    img.alpha_composite(shadow)
    tilted = card.rotate(-4, expand=True, resample=Image.BICUBIC)
    img.alpha_composite(tilted, (818, 170))

    # Title
    d = ImageDraw.Draw(img)
    title_font = load_font(108)
    title = "GEM RUSH 3D"
    d.text((84, 180), title, font=title_font, fill=PUMPINK_OUTLINE,
           stroke_width=10, stroke_fill=PUMPINK_OUTLINE)
    d.text((84, 180), title, font=title_font, fill=GOLD)

    # Tagline
    tag_font = load_font(38)
    d.text((88, 320), "Pip vs. Gloomfang -", font=tag_font, fill=(*CLOUD_WHITE, 255))
    w1 = d.textlength("Pip vs. Gloomfang -", font=tag_font)
    d.text((88 + w1 + 18, 320), "a very small hero,", font=tag_font, fill=GOLD)
    d.text((88, 372), "a very large storm.", font=tag_font, fill=(*CLOUD_WHITE, 255))

    # Chips
    chip_font = load_font(26)
    chips = ["Free", "Android", "Windows", "Single-player"]
    cx = 88
    for label in chips:
        tw = d.textlength(label, font=chip_font)
        pad = 18
        d.rounded_rectangle(
            (cx, 470, cx + tw + pad * 2, 470 + 52), radius=26,
            fill=(255, 255, 255, 235),
        )
        d.text((cx + pad, 483), label, font=chip_font, fill=(40, 60, 120))
        cx += int(tw + pad * 2 + 16)

    img.convert("RGB").save(OUT / "og-image.png", optimize=True)
    print("  og-image.png 1200x630 written")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    print("Screenshots:")
    copy_screenshots()
    print("Favicons:")
    favicons()
    print("Social card:")
    og_image()
    print("Done.")


if __name__ == "__main__":
    main()
