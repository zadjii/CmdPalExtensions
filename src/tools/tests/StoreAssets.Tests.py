"""Run with Python and Pillow; no test framework dependency."""

import importlib.util
from pathlib import Path, PureWindowsPath
import unittest

from PIL import Image, ImageColor, ImageOps

spec = importlib.util.spec_from_file_location("store_assets", Path(__file__).parents[1] / "Generate-StoreAssets.py")
assets = importlib.util.module_from_spec(spec)
spec.loader.exec_module(assets)


class StoreAssetsTests(unittest.TestCase):
    def test_existing_artwork_is_selected(self):
        expected = {
            "obsidian": "Assets\\obsidian-logo.png",
            "tmdb": "Assets\\Tmdb-312x276-logo.png",
            "icons": "Assets\\WinUI3Gallery.png",
        }
        actual = {app["id"]: app["iconSource"] for app in assets.CATALOG["apps"] if "iconSource" in app}
        self.assertEqual(actual, expected)
        for app in assets.CATALOG["apps"]:
            if app["id"] in expected:
                with self.subTest(app=app["id"]):
                    source = assets.source_image(app)
                    self.assertIsNotNone(source)
                    output = assets.asset(app, 300, 300, source=source, listing=True)
                    resized = ImageOps.contain(source, (300, 300), Image.Resampling.LANCZOS)
                    expected_image = Image.new("RGBA", (300, 300))
                    expected_image.alpha_composite(resized, ((300 - resized.width) // 2, (300 - resized.height) // 2))
                    self.assertEqual(output.tobytes(), expected_image.tobytes())

    def test_non_square_artwork_is_not_stretched_cropped_or_recolored(self):
        source = Image.new("RGBA", (200, 100), (20, 80, 160, 255))
        for width, height, unplated in ((100, 100, False), (310, 150, False), (32, 32, True)):
            with self.subTest(size=(width, height)):
                output = assets.asset({}, width, height, unplated=unplated, source=source)
                left, top, right, bottom = output.getbbox()
                size = round(min(width, height) * (0.9 if unplated else 0.75))
                self.assertEqual(right - left, size)
                self.assertEqual(bottom - top, round(size / 2))
                self.assertLessEqual(abs(left - (width - right)), 1)
                self.assertLessEqual(abs(top - (height - bottom)), 1)
                self.assertEqual(output.getpixel((width // 2, height // 2)), (20, 80, 160, 255))

    def test_start_menu_variants_keep_color_in_both_themes(self):
        for app in assets.CATALOG["apps"]:
            directory = assets.ROOT / Path(*PureWindowsPath(app["project"]).parts).parent
            for size in (16, 24, 32, 48, 256):
                with self.subTest(app=app["id"], size=size):
                    path = directory / "Assets" / "Package"
                    with Image.open(path / f"Square44x44Logo.targetsize-{size}_altform-unplated.png") as dark:
                        with Image.open(path / f"Square44x44Logo.targetsize-{size}_altform-lightunplated.png") as light:
                            self.assertEqual(dark.tobytes(), light.tobytes())
                        self.assertEqual(dark.size, (size, size))
                        self.assertEqual(dark.convert("RGBA").getpixel((0, 0))[3], 0)
                        if "iconSource" not in app:
                            color = ImageColor.getrgb(app["color"])
                            colored_pixels = sum(
                                alpha >= 240 and max(abs(r - color[0]), abs(g - color[1]), abs(b - color[2])) <= 12
                                for r, g, b, alpha in dark.convert("RGBA").getdata()
                            )
                            self.assertGreater(colored_pixels, size * size * 0.3, "Start icon lost its colorful background.")

    def test_bad_source_does_not_silently_generate_replacement(self):
        app = {
            "project": assets.CATALOG["apps"][0]["project"],
            "iconSource": "Assets\\missing-original.png",
            "color": "#FFFFFF",
            "symbol": "news",
        }
        with self.assertRaises(FileNotFoundError):
            assets.source_image(app)

    def test_checked_in_outputs_match_sources(self):
        assets.generate(check=True)


if __name__ == "__main__":
    unittest.main()
