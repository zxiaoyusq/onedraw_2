#!/usr/bin/env python3
"""Compose final T630 PNGs into a deterministic visual-review sheet."""

from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFont


WIDTH = 1920
HEIGHT = 1080


def load_rgba(root: Path, relative: str) -> Image.Image:
    return Image.open(root / relative).convert("RGBA")


def resize_to_height(image: Image.Image, height: int) -> Image.Image:
    width = max(1, round(image.width * height / image.height))
    return image.resize((width, height), Image.Resampling.LANCZOS)


def paste_bottom(canvas: Image.Image, image: Image.Image, center_x: int, bottom_y: int) -> None:
    canvas.alpha_composite(image, (center_x - image.width // 2, bottom_y - image.height))


def add_label(
    canvas: Image.Image,
    draw: ImageDraw.ImageDraw,
    font: ImageFont.FreeTypeFont,
    text: str,
    center_x: int,
    top_y: int,
) -> None:
    box = draw.textbbox((0, 0), text, font=font)
    text_width = box[2] - box[0]
    text_height = box[3] - box[1]
    left = center_x - text_width // 2 - 8
    draw.rounded_rectangle(
        (left, top_y, left + text_width + 16, top_y + text_height + 10),
        radius=6,
        fill=(12, 18, 24, 190),
        outline=(229, 197, 120, 220),
        width=1,
    )
    draw.text(
        (center_x - text_width // 2, top_y + 3 - box[1]),
        text,
        font=font,
        fill=(249, 237, 204, 255),
    )


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, type=Path)
    args = parser.parse_args()

    root = Path(__file__).resolve().parents[2]
    background = load_rgba(root, "Assets/_Game/Art/Backgrounds/bg_red_cave.png")
    crop_width = round(background.height * WIDTH / HEIGHT)
    crop_left = max(0, (background.width - crop_width) // 2)
    background = background.crop((crop_left, 0, crop_left + crop_width, background.height))
    background = background.resize((WIDTH, HEIGHT), Image.Resampling.LANCZOS)
    background = ImageEnhance.Brightness(background).enhance(0.72)
    canvas = background.copy()

    draw = ImageDraw.Draw(canvas, "RGBA")
    font_path = root / "Assets/_Game/Art/UI/Fonts/OneStrokeDemonUI-Regular.ttf"
    font = ImageFont.truetype(str(font_path), 22)
    title_font = ImageFont.truetype(str(font_path), 30)
    draw.rounded_rectangle((24, 18, 538, 64), 8, fill=(8, 13, 20, 210), outline=(229, 197, 120, 230), width=2)
    draw.text((42, 26), "T630 PROTOTYPE ART REVIEW", font=title_font, fill=(249, 237, 204, 255))

    actors = [
        ("Moyan", "Assets/_Game/Art/Characters/Moyan/moyan_idle.png", 185, 900, 275),
        ("Fire Fish", "Assets/_Game/Art/Enemies/fire_fish.png", 445, 900, 235),
        ("Wheel Zombie", "Assets/_Game/Art/Enemies/wheel_zombie.png", 690, 900, 250),
        ("Stone Turtle", "Assets/_Game/Art/Enemies/stone_turtle.png", 945, 910, 300),
        ("Soul Puppet", "Assets/_Game/Art/Enemies/soul_puppet.png", 1235, 905, 355),
        ("Tomb Armor King", "Assets/_Game/Art/Enemies/tomb_armor_king.png", 1635, 920, 455),
        ("Skeleton Ghost", "Assets/_Game/Art/Enemies/skeleton_ghost.png", 720, 435, 175),
        ("Talisman Bat", "Assets/_Game/Art/Enemies/talisman_bat.png", 1000, 405, 145),
    ]

    for label, relative, center_x, bottom_y, target_height in actors:
        actor = resize_to_height(load_rgba(root, relative), target_height)
        if bottom_y > 700:
            shadow_width = max(70, round(actor.width * 0.7))
            draw.ellipse(
                (
                    center_x - shadow_width // 2,
                    bottom_y - 13,
                    center_x + shadow_width // 2,
                    bottom_y + 12,
                ),
                fill=(0, 0, 0, 105),
            )
        paste_bottom(canvas, actor, center_x, bottom_y)
        add_label(canvas, draw, font, label, center_x, bottom_y + 16)

    slash = resize_to_height(
        load_rgba(root, "Assets/_Game/Art/VFX/Sprites/vfx_slash_arc.png"),
        245,
    )
    canvas.alpha_composite(slash, (805, 560))

    health = resize_to_height(load_rgba(root, "Assets/_Game/Art/UI/hud_health_frame.png"), 105)
    canvas.alpha_composite(health, (42, 82))
    ultimate = resize_to_height(load_rgba(root, "Assets/_Game/Art/UI/button_ultimate.png"), 145)
    switch = resize_to_height(load_rgba(root, "Assets/_Game/Art/UI/button_switch.png"), 125)
    settings = resize_to_height(load_rgba(root, "Assets/_Game/Art/UI/button_settings.png"), 92)
    canvas.alpha_composite(ultimate, (1515, 58))
    canvas.alpha_composite(switch, (1680, 74))
    canvas.alpha_composite(settings, (1810, 88))

    output = args.output.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(output, format="PNG", optimize=True)


if __name__ == "__main__":
    main()
