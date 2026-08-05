from pathlib import Path

from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "src" / "Shutdown" / "Assets"
SIZES = tuple((size, size) for size in (16, 20, 24, 32, 40, 48, 64, 128, 256))


def render(color: str) -> Image.Image:
    scale = 4
    size = 256 * scale
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    stroke = 18 * scale

    draw.rounded_rectangle(
        (24 * scale, 36 * scale, 184 * scale, 168 * scale),
        radius=18 * scale,
        outline=color,
        width=stroke,
    )
    draw.line((104 * scale, 168 * scale, 104 * scale, 204 * scale), fill=color, width=stroke)
    draw.line((64 * scale, 212 * scale, 144 * scale, 212 * scale), fill=color, width=stroke)

    draw.line((112 * scale, 102 * scale, 226 * scale, 102 * scale), fill=color, width=stroke)
    draw.line((180 * scale, 58 * scale, 226 * scale, 102 * scale), fill=color, width=stroke)
    draw.line((180 * scale, 146 * scale, 226 * scale, 102 * scale), fill=color, width=stroke)

    return image.resize((256, 256), Image.Resampling.LANCZOS)


for tone, color in (("black", "#111111"), ("white", "#FFFFFF")):
    render(color).save(ASSETS / f"tray_rdp_{tone}.ico", format="ICO", sizes=SIZES)
