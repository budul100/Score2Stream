from PIL import Image, ImageDraw, ImageFilter
import numpy as np
import random
import os
import cv2

# --- 7-segment display ---
def draw_7segment_digit(digit: int, width=64, height=96) -> "Image.Image":
    img = Image.new("L", (width, height), color=0)
    draw = ImageDraw.Draw(img)

    segments = {
        'a': (8,  4,  56, 12),   # top
        'b': (52, 8,  60, 44),   # top-right
        'c': (52, 52, 60, 88),   # bottom-right
        'd': (8,  84, 56, 92),   # bottom
        'e': (4,  52, 12, 88),   # bottom-left
        'f': (4,  8,  12, 44),   # top-left
        'g': (8,  44, 56, 52),   # middle
    }
    active = {
        0: 'abcdef',  1: 'bc',      2: 'abdeg',
        3: 'abcdg',   4: 'bcfg',    5: 'acdfg',
        6: 'acdefg',  7: 'abc',     8: 'abcdefg',
        9: 'abcdfg'
    }
    for seg, coords in segments.items():
        if seg in active[digit]:
            brightness = 220 + random.randint(-20, 20)
            draw.rectangle(coords, fill=brightness)
    return img

# --- Dot-matrix display (5x7 grid) ---
# Each digit as a bit pattern: 1 = lit, 0 = off
DOT_MATRIX = {
    0: [
        [  # Variant 1 (with diagonal dots)
            [0,1,1,1,0],
            [1,0,0,0,1],
            [1,0,0,1,1],
            [1,0,1,0,1],
            [1,1,0,0,1],
            [1,0,0,0,1],
            [0,1,1,1,0],
        ],
        [  # Variant 2 (classic)
            [0,1,1,1,0],
            [1,0,0,0,1],
            [1,0,0,0,1],
            [1,0,0,0,1],
            [1,0,0,0,1],
            [1,0,0,0,1],
            [0,1,1,1,0],
        ],
    ],
    1: [[
        [0,0,1,0,0],
        [0,1,1,0,0],
        [0,0,1,0,0],
        [0,0,1,0,0],
        [0,0,1,0,0],
        [0,0,1,0,0],
        [0,1,1,1,0],
    ]],
    2: [[
        [0,1,1,1,0],
        [1,0,0,0,1],
        [0,0,0,0,1],
        [0,0,0,1,0],
        [0,0,1,0,0],
        [0,1,0,0,0],
        [1,1,1,1,1],
    ]],
    3: [[
        [0,1,1,1,0],
        [1,0,0,0,1],
        [0,0,0,0,1],
        [0,0,1,1,0],
        [0,0,0,0,1],
        [1,0,0,0,1],
        [0,1,1,1,0],
    ]],
    4: [[   
        [0,0,0,1,0],
        [0,0,1,1,0],
        [0,1,0,1,0],
        [1,0,0,1,0],
        [1,1,1,1,1],
        [0,0,0,1,0],
        [0,0,0,1,0],
    ]],
    5: [[   
        [1,1,1,1,1],
        [1,0,0,0,0],
        [1,1,1,1,0],
        [0,0,0,0,1],
        [0,0,0,0,1],
        [1,0,0,0,1],
        [0,1,1,1,0],
    ]],
    6: [[
        [0,0,1,1,0],
        [0,1,0,0,0],
        [1,0,0,0,0],
        [1,1,1,1,0],
        [1,0,0,0,1],
        [1,0,0,0,1],
        [0,1,1,1,0],
    ]],
    7: [[
        [1,1,1,1,1],
        [0,0,0,0,1],
        [0,0,0,1,0],
        [0,0,1,0,0],
        [0,1,0,0,0],
        [0,1,0,0,0],
        [0,1,0,0,0],
    ]],
    8: [[
        [0,1,1,1,0],
        [1,0,0,0,1],
        [1,0,0,0,1],
        [0,1,1,1,0],
        [1,0,0,0,1],
        [1,0,0,0,1],
        [0,1,1,1,0],
    ]],
    9: [[
        [0,1,1,1,0],
        [1,0,0,0,1],
        [1,0,0,0,1],
        [0,1,1,1,1],
        [0,0,0,0,1],
        [0,0,0,1,0],
        [0,1,1,0,0],
    ]],
}

def draw_dotmatrix_digit(digit: int, variant: list | None = None, width=64, height=96) -> Image.Image:
    img = Image.new("L", (width, height), color=0)
    draw = ImageDraw.Draw(img)

    if variant is None:
        variant = DOT_MATRIX[digit][0]
    pattern: list = variant  # type: ignore
    rows, cols = 7, 5
    dot_w = width  // (cols + 1)
    dot_h = height // (rows + 1)
    pad_x = (width  - cols * dot_w) // 2
    pad_y = (height - rows * dot_h) // 2
    radius = min(dot_w, dot_h) // 2 - 1

    for r, row in enumerate(pattern):
        for c, on in enumerate(row):
            if on:
                cx = pad_x + c * dot_w + dot_w // 2
                cy = pad_y + r * dot_h + dot_h // 2
                brightness = 220 + random.randint(-20, 20)
                draw.ellipse(
                    [cx - radius, cy - radius, cx + radius, cy + radius],
                    fill=brightness
                )
    return img

# --- Augmentation (same for both types) ---
def augment(img: Image.Image) -> Image.Image:
    arr = np.array(img, dtype=np.float32)

    arr *= random.uniform(0.6, 1.2)
    arr = np.clip(arr, 0, 255)

    noise = np.random.normal(0, random.uniform(0, 15), arr.shape)
    arr = np.clip(arr + noise, 0, 255).astype(np.uint8)

    img = Image.fromarray(arr)

    if random.random() > 0.5:
        img = img.filter(ImageFilter.GaussianBlur(radius=random.uniform(0.5, 1.5)))

    img_cv = np.array(img)
    h, w = img_cv.shape
    margin = 8
    src = np.float32(np.array([[0,0],[w,0],[w,h],[0,h]]))
    dst = np.float32(np.array([
        [random.uniform(0, margin), random.uniform(0, margin)],
        [w - random.uniform(0, margin), random.uniform(0, margin)],
        [w - random.uniform(0, margin), h - random.uniform(0, margin)],
        [random.uniform(0, margin), h - random.uniform(0, margin)],
    ]))
    M = cv2.getPerspectiveTransform(np.ascontiguousarray(src), np.ascontiguousarray(dst))
    img_cv = cv2.warpPerspective(img_cv, M, (w, h))

    return Image.fromarray(img_cv)

# --- Generation: both display types per digit ---
# 7-segment: single variant, 750 images
# Dot-matrix: all variants evenly distributed, 750 images total

for digit in range(10):
    os.makedirs(f"training-data/{digit}", exist_ok=True)

    # 7-segment
    for i in range(750):
        img = draw_7segment_digit(digit)
        img = augment(img)
        img.save(f"training-data/{digit}/7seg_{i:04d}.png")

    # Dot-matrix: iterate through all variants
    variants = DOT_MATRIX[digit]
    count_per_variant = 750 // len(variants)
    for v_idx, variant in enumerate(variants):
        for i in range(count_per_variant):
            img = draw_dotmatrix_digit(digit, variant=variant)
            img = augment(img)
            img.save(f"training-data/{digit}/dot_v{v_idx}_{i:04d}.png")

print("Done.")