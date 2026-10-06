"""Scale existing artwork; render replacements only for unusable legacy icons. Requires Pillow."""

import argparse
import json
from pathlib import Path, PureWindowsPath

from PIL import Image, ImageDraw, ImageOps

ROOT = Path(__file__).resolve().parents[2]
CATALOG = json.loads((ROOT / "src" / "store-apps.json").read_text(encoding="utf-8"))


def symbol_image(symbol, color, size):
    image = Image.new("RGBA", (512, 512))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((0, 0, 511, 511), radius=108, fill=color)
    ink = "#FFFFFF"
    width = 22
    if symbol == "news":
        draw.rounded_rectangle((126, 114, 386, 398), radius=16, outline=ink, width=width)
        draw.rectangle((164, 160, 234, 224), fill=ink)
        for y in (166, 212):
            draw.line((264, y, 348, y), fill=ink, width=width)
        for y in (266, 316, 358):
            draw.line((164, y, 348, y), fill=ink, width=width)
    elif symbol == "chat":
        draw.rounded_rectangle((110, 136, 402, 340), radius=48, outline=ink, width=width)
        draw.line((164, 331, 164, 392, 234, 337), fill=ink, width=width, joint="curve")
        for x in (180, 256, 332):
            draw.ellipse((x - 13, 226, x + 13, 252), fill=ink)
    elif symbol == "bookmark":
        draw.line((166, 122, 346, 122, 346, 394, 256, 328, 166, 394, 166, 122), fill=ink, width=width, joint="curve")
        draw.line((212, 192, 300, 192), fill=ink, width=width)
    elif symbol == "football":
        draw.ellipse((104, 158, 408, 354), outline=ink, width=width)
        draw.line((172, 256, 340, 256), fill=ink, width=18)
        for x in (204, 256, 308):
            draw.line((x, 226, x, 286), fill=ink, width=18)
    elif symbol == "text":
        draw.line((104, 368, 180, 136, 256, 368), fill=ink, width=width, joint="curve")
        draw.line((132, 292, 228, 292), fill=ink, width=width)
        draw.ellipse((290, 256, 380, 362), outline=ink, width=width)
        draw.line((380, 254, 380, 366, 404, 366), fill=ink, width=width)
    elif symbol == "play":
        draw.polygon(((136, 136), (136, 376), (310, 256)), fill=ink)
        draw.rounded_rectangle((346, 156, 380, 356), radius=8, fill=ink)
    else:
        raise ValueError(f"Unknown symbol: {symbol}")
    return image.resize((size, size), Image.Resampling.LANCZOS)


def source_image(app):
    if "iconSource" not in app:
        return None
    directory = ROOT / Path(*PureWindowsPath(app["project"]).parts).parent
    path = directory / Path(*PureWindowsPath(app["iconSource"]).parts)
    with Image.open(path) as image:
        return image.convert("RGBA")


def asset(app, width, height, unplated=False, source=None, listing=False):
    image = Image.new("RGBA", (width, height))
    icon_size = round(min(width, height) * (1 if listing else 0.9 if unplated else 0.75))
    if source is not None:
        # Preserve the artwork's colors, transparency and proportions, including light variants.
        icon = ImageOps.contain(source, (icon_size, icon_size), Image.Resampling.LANCZOS)
    else:
        icon = symbol_image(app["symbol"], app["color"], icon_size)
    image.alpha_composite(icon, ((width - icon.width) // 2, (height - icon.height) // 2))
    return image


def generate(check=False):
    for app in CATALOG["apps"]:
        directory = ROOT / Path(*PureWindowsPath(app["project"]).parts).parent
        source = source_image(app)
        outputs = {}
        sizes = {
            "Square44x44Logo": (44, 44),
            "Square150x150Logo": (150, 150),
            "Wide310x150Logo": (310, 150),
            "SplashScreen": (620, 300),
            "StoreLogo": (50, 50),
        }
        for name, (width, height) in sizes.items():
            for scale in (100, 200, 400):
                outputs[f"{name}.scale-{scale}.png"] = asset(app, width * scale // 100, height * scale // 100, source=source)
        for size in (16, 24, 32, 48, 256):
            for qualifier in ("altform-unplated", "altform-lightunplated"):
                outputs[f"Square44x44Logo.targetsize-{size}_{qualifier}.png"] = asset(app, size, size, unplated=True, source=source)
        for name, image in outputs.items():
            path = directory / "Assets" / "Package" / name
            if check:
                with Image.open(path) as existing:
                    if existing.size != image.size or existing.convert("RGBA").tobytes() != image.tobytes():
                        raise ValueError(f"Stale asset: {path}")
            else:
                path.parent.mkdir(parents=True, exist_ok=True)
                image.save(path)
        listing = ROOT / "doc" / "store" / "assets" / f"{app['id']}-store-logo.png"
        image = asset(app, 300, 300, source=source, listing=True)
        if check:
            with Image.open(listing) as existing:
                if existing.size != (300, 300) or existing.convert("RGBA").tobytes() != image.tobytes():
                    raise ValueError(f"Stale listing logo: {listing}")
        else:
            listing.parent.mkdir(parents=True, exist_ok=True)
            image.save(listing)
    print(f"{'Verified' if check else 'Generated'} package and listing assets for {len(CATALOG['apps'])} extensions.")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Verify committed images without changing them.")
    generate(parser.parse_args().check)
