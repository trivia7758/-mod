"""
Convert battle BMP sprite sheets to PNG with transparent background.
Replaces purple RGB(247, 0, 255) with transparent pixels.

Usage: python convert_bmp_to_png.py
"""
import struct
import zlib
import os

FRAME_W = 48
FRAME_H = 48
PURPLE_R, PURPLE_G, PURPLE_B = 247, 0, 255
TOLERANCE = 20

SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
SPRITE_DIR = os.path.join(SCRIPT_DIR, "Assets", "_Project", "Resources", "Sprites", "Battle")


def read_bmp(filepath):
    """Read 8-bit indexed BMP, return (width, height, rgba_pixels)."""
    with open(filepath, "rb") as f:
        # BMP header
        magic = f.read(2)
        assert magic == b"BM", f"Not a BMP file: {filepath}"

        f.seek(10)
        data_offset = struct.unpack("<I", f.read(4))[0]

        # DIB header
        f.seek(14)
        dib_size = struct.unpack("<I", f.read(4))[0]
        width = struct.unpack("<i", f.read(4))[0]
        height = struct.unpack("<i", f.read(4))[0]  # can be negative (top-down)
        f.seek(28)
        bpp = struct.unpack("<H", f.read(2))[0]

        top_down = height < 0
        height = abs(height)

        # Read palette (at offset 54 for 40-byte DIB header)
        palette_offset = 14 + dib_size
        f.seek(palette_offset)
        palette = []
        num_colors = 256 if bpp == 8 else (1 << bpp)
        for _ in range(num_colors):
            b, g, r, _ = struct.unpack("BBBB", f.read(4))
            palette.append((r, g, b))

        # Read pixel data
        row_size = ((width * bpp // 8 + 3) // 4) * 4
        f.seek(data_offset)
        raw_rows = []
        for _ in range(height):
            row = f.read(row_size)
            raw_rows.append(row)

        # BMP is bottom-up by default
        if not top_down:
            raw_rows.reverse()

        # Convert to RGBA
        rgba = bytearray(width * height * 4)
        for y in range(height):
            for x in range(width):
                if bpp == 8:
                    idx = raw_rows[y][x]
                    r, g, b = palette[idx]
                elif bpp == 24:
                    offset = x * 3
                    b = raw_rows[y][offset]
                    g = raw_rows[y][offset + 1]
                    r = raw_rows[y][offset + 2]
                else:
                    r, g, b = 0, 0, 0

                # Check if purple background
                if (abs(r - PURPLE_R) <= TOLERANCE and
                    g <= TOLERANCE and
                    abs(b - PURPLE_B) <= TOLERANCE):
                    a = 0
                    r, g, b = 0, 0, 0
                else:
                    a = 255

                pos = (y * width + x) * 4
                rgba[pos] = r
                rgba[pos + 1] = g
                rgba[pos + 2] = b
                rgba[pos + 3] = a

        return width, height, bytes(rgba)


def write_png(filepath, width, height, rgba_data):
    """Write RGBA data as PNG file."""
    def make_chunk(chunk_type, data):
        chunk = chunk_type + data
        crc = struct.pack(">I", zlib.crc32(chunk) & 0xFFFFFFFF)
        return struct.pack(">I", len(data)) + chunk + crc

    # PNG signature
    signature = b"\x89PNG\r\n\x1a\n"

    # IHDR
    ihdr_data = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)  # 8-bit RGBA
    ihdr = make_chunk(b"IHDR", ihdr_data)

    # IDAT - build raw image data with filter bytes
    raw = bytearray()
    for y in range(height):
        raw.append(0)  # filter: None
        offset = y * width * 4
        raw.extend(rgba_data[offset:offset + width * 4])

    compressed = zlib.compress(bytes(raw), 9)
    idat = make_chunk(b"IDAT", compressed)

    # IEND
    iend = make_chunk(b"IEND", b"")

    with open(filepath, "wb") as f:
        f.write(signature + ihdr + idat + iend)


def process_file(base_name, expected_frames):
    bmp_path = os.path.join(SPRITE_DIR, base_name + ".bmp")
    png_path = os.path.join(SPRITE_DIR, base_name + ".png")

    if not os.path.exists(bmp_path):
        print(f"ERROR: {bmp_path} not found")
        return False

    print(f"Processing {bmp_path}...")
    width, height, rgba = read_bmp(bmp_path)
    actual_frames = height // FRAME_H
    print(f"  Size: {width}x{height}, Frames: {actual_frames} (expected {expected_frames})")

    # Count transparent pixels
    trans_count = sum(1 for i in range(3, len(rgba), 4) if rgba[i] == 0)
    total = width * height
    print(f"  Transparent: {trans_count}/{total} pixels ({100*trans_count//total}%)")

    write_png(png_path, width, height, rgba)
    print(f"  Saved: {png_path}")
    return True


if __name__ == "__main__":
    print("=== Battle Sprite BMP → PNG Converter ===")
    print(f"Sprite dir: {SPRITE_DIR}")
    print()

    ok1 = process_file("test_m", 11)
    ok2 = process_file("test_s", 5)

    if ok1 and ok2:
        print("\nDone! Now in Unity:")
        print("  1. Right-click test_m.png → Reimport")
        print("  2. Or run CaoCao > Process Battle Sprites to configure import settings")
    else:
        print("\nSome files failed. Check paths.")
