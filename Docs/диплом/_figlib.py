# -*- coding: utf-8 -*-
"""Утилиты рисования схем для ВКР (matplotlib, кириллица через DejaVu Sans)."""
import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.patches import FancyBboxPatch, FancyArrowPatch, Rectangle
from matplotlib.lines import Line2D

plt.rcParams["font.family"] = "DejaVu Sans"
plt.rcParams["font.size"] = 11

# Палитра (спокойные «вузовские» цвета, согласованы с интерфейсом).
NAVY = "#1e4b8f"
BLUE = "#235fb4"
LBLUE = "#dce8f7"
LGREY = "#eef2f7"
GREY = "#7c8aa0"
GREEN = "#2e7d57"
LGREEN = "#dcefe5"
ORANGE = "#b9770e"
LORANGE = "#f7ecd6"
RED = "#a8324a"
LRED = "#f6dde2"
WHITE = "#ffffff"
DARK = "#1e293b"


def new_ax(w=10, h=6, xlim=(0, 100), ylim=(0, 100)):
    fig, ax = plt.subplots(figsize=(w, h))
    ax.set_xlim(*xlim)
    ax.set_ylim(*ylim)
    ax.axis("off")
    return fig, ax


def box(ax, x, y, w, h, text, fc=LBLUE, ec=NAVY, tc=DARK, fs=11, bold=False,
        rounded=True, lw=1.5, align="center", pad=0.0):
    style = "round,pad=0.02,rounding_size=2.2" if rounded else "square,pad=0.0"
    p = FancyBboxPatch((x, y), w, h, boxstyle=style, fc=fc, ec=ec, lw=lw, zorder=2)
    ax.add_patch(p)
    ha = {"center": "center", "left": "left", "right": "right"}[align]
    tx = x + w / 2 if align == "center" else (x + 2.0 if align == "left" else x + w - 2.0)
    ax.text(tx, y + h / 2, text, ha=ha, va="center", fontsize=fs,
            color=tc, fontweight="bold" if bold else "normal", zorder=3, wrap=True)
    return (x + w / 2, y + h / 2, x, y, w, h)


def title_box(ax, x, y, w, h, title, fc=NAVY, tc=WHITE, fs=12):
    return box(ax, x, y, w, h, title, fc=fc, ec=fc, tc=tc, fs=fs, bold=True)


def arrow(ax, p1, p2, color=NAVY, lw=1.6, style="-|>", ls="-", rad=0.0, mut=14):
    a = FancyArrowPatch(p1, p2, arrowstyle=style, color=color, lw=lw,
                        linestyle=ls, mutation_scale=mut,
                        connectionstyle=f"arc3,rad={rad}", zorder=1)
    ax.add_patch(a)
    return a


def label(ax, x, y, text, fs=9.5, color=GREY, bold=False, ha="center", va="center",
          bg=None, rot=0):
    bbox = dict(boxstyle="round,pad=0.2", fc=bg, ec="none") if bg else None
    ax.text(x, y, text, ha=ha, va=va, fontsize=fs, color=color,
            fontweight="bold" if bold else "normal", zorder=4, bbox=bbox, rotation=rot)


def save(fig, name, dpi=200):
    import os
    os.makedirs("figures", exist_ok=True)
    path = f"figures/{name}.png"
    fig.savefig(path, dpi=dpi, bbox_inches="tight", facecolor="white")
    plt.close(fig)
    return path
