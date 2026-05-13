#!/usr/bin/env python3
"""Render markdown guides in docs/ to standalone HTML pages."""

from __future__ import annotations

import argparse
import html
from pathlib import Path
import re
import sys

import markdown

EXCLUDED_MARKDOWN_FILENAMES = {
    "testing.md",
}


def build_html(title: str, body_html: str) -> str:
    safe_title = html.escape(title)
    return f"""<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>{safe_title}</title>
  <style>
    :root {{
      color-scheme: light dark;
      --bg: #0e1117;
      --panel: #161b22;
      --text: #e6edf3;
      --muted: #9da7b3;
      --link: #58a6ff;
      --border: #30363d;
      --code: #11161d;
      --mono: Consolas, "Cascadia Mono", "Courier New", monospace;
      --sans: "Segoe UI", "Noto Sans", Tahoma, sans-serif;
    }}
    @media (prefers-color-scheme: light) {{
      :root {{
        --bg: #f6f8fa;
        --panel: #ffffff;
        --text: #1f2328;
        --muted: #59636e;
        --link: #0969da;
        --border: #d0d7de;
        --code: #f6f8fa;
      }}
    }}
    html, body {{ margin: 0; padding: 0; }}
    body {{
      background: var(--bg);
      color: var(--text);
      font-family: var(--sans);
      line-height: 1.55;
      font-size: 16px;
    }}
    .page {{
      max-width: 1100px;
      margin: 24px auto;
      padding: 0 18px 28px;
    }}
    .doc {{
      background: var(--panel);
      border: 1px solid var(--border);
      border-radius: 12px;
      padding: 22px;
      box-sizing: border-box;
      overflow-wrap: anywhere;
    }}
    h1, h2, h3, h4 {{ line-height: 1.25; }}
    h1 {{ margin-top: 0; }}
    a {{
      color: var(--link);
      text-decoration: none;
    }}
    a:hover {{ text-decoration: underline; }}
    code {{
      font-family: var(--mono);
      font-size: 0.95em;
      background: var(--code);
      border: 1px solid var(--border);
      border-radius: 6px;
      padding: 0.1em 0.35em;
    }}
    pre {{
      background: var(--code);
      border: 1px solid var(--border);
      border-radius: 8px;
      padding: 12px;
      overflow: auto;
    }}
    pre code {{
      border: 0;
      background: transparent;
      padding: 0;
    }}
    table {{
      border-collapse: collapse;
      width: 100%;
      margin: 12px 0;
      font-size: 0.95em;
    }}
    th, td {{
      border: 1px solid var(--border);
      padding: 8px 10px;
      vertical-align: top;
      text-align: left;
    }}
    th {{
      background: color-mix(in srgb, var(--panel) 75%, var(--border));
    }}
    hr {{
      border: 0;
      border-top: 1px solid var(--border);
      margin: 20px 0;
    }}
    blockquote {{
      border-left: 4px solid var(--border);
      margin: 12px 0;
      padding: 4px 12px;
      color: var(--muted);
    }}
  </style>
</head>
<body>
  <main class="page">
    <article class="doc">
{body_html}
    </article>
  </main>
</body>
</html>
"""


def normalize_toc_block(text: str) -> str:
    """Convert plain link lines under '## Table of Contents' into markdown list items."""
    lines = text.splitlines()
    if not lines:
        return text

    out: list[str] = []
    i = 0
    while i < len(lines):
        line = lines[i]
        out.append(line)

        if line.strip() != "## Table of Contents":
            i += 1
            continue

        i += 1
        while i < len(lines) and lines[i].strip() == "":
            out.append(lines[i])
            i += 1

        toc_lines: list[str] = []
        while i < len(lines) and lines[i].strip() != "":
            toc_lines.append(lines[i].strip())
            i += 1

        looks_like_toc = bool(toc_lines) and all(
            item.startswith("[") and "](" in item and item.endswith(")")
            for item in toc_lines
        )

        if looks_like_toc:
            out.extend(f"- {item}" for item in toc_lines)
        else:
            out.extend(toc_lines)

    result = "\n".join(out)
    if text.endswith("\n"):
        result += "\n"
    return result


def normalize_key_blocks(text: str) -> str:
    """
    Normalize key-description blocks so they render as separate paragraphs/lists
    instead of a single combined line.
    """
    lines = text.splitlines()
    if not lines:
        return text

    out: list[str] = []
    in_fence = False
    key_line_re = re.compile(r"^\s*`[^`]+`\s*$")

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if stripped.startswith("```"):
            in_fence = not in_fence
            out.append(line)
            i += 1
            continue

        if in_fence:
            out.append(line)
            i += 1
            continue

        if stripped in {"Allowed values:", "Alias values:", "Alias keys:"}:
            if out and out[-1].strip() != "":
                out.append("")
            out.append(line)
            if i + 1 < len(lines) and lines[i + 1].lstrip().startswith("- "):
                out.append("")
            i += 1
            continue

        if key_line_re.match(stripped):
            out.append(line)
            if i + 1 < len(lines) and lines[i + 1].strip() != "":
                out.append("")
            i += 1
            continue

        out.append(line)
        i += 1

    result = "\n".join(out)
    if text.endswith("\n"):
        result += "\n"
    return result


def normalize_action_reference_blocks(text: str) -> str:
    """Preserve action/Desktop/Mobile reference blocks as hard line breaks."""
    lines = text.splitlines()
    if not lines:
        return text

    out = list(lines)
    in_fence = False

    def with_hard_break(value: str) -> str:
        return value if value.endswith("  ") else value + "  "

    i = 0
    while i < len(out):
        stripped = out[i].strip()
        if stripped.startswith("```"):
            in_fence = not in_fence
            i += 1
            continue

        if in_fence:
            i += 1
            continue

        if (
            i + 2 < len(out)
            and stripped.endswith(":")
            and out[i + 1].lstrip().startswith("Desktop:")
            and out[i + 2].lstrip().startswith("Mobile:")
        ):
            out[i] = with_hard_break(out[i])
            out[i + 1] = with_hard_break(out[i + 1])
            i += 3
            continue

        i += 1

    result = "\n".join(out)
    if text.endswith("\n"):
        result += "\n"
    return result


def normalize_escaped_headings(text: str) -> str:
    """Treat escaped markdown heading markers as headings when they start a line."""
    if not text:
        return text

    heading_re = re.compile(r"^(\s*)\\(#{1,6})(\s+.*)$", re.MULTILINE)
    return heading_re.sub(r"\1\2\3", text)


def render_markdown(input_path: Path, output_path: Path) -> None:
    text = input_path.read_text(encoding="utf-8-sig")
    text = normalize_toc_block(text)
    text = normalize_key_blocks(text)
    text = normalize_action_reference_blocks(text)
    text = normalize_escaped_headings(text)
    body = markdown.markdown(
        text,
        extensions=[
            "extra",
            "tables",
            "fenced_code",
            "sane_lists",
            "toc",
            "attr_list",
            "md_in_html",
        ],
        output_format="html5",
    )
    title = ""
    for line in text.splitlines():
        stripped = line.strip()
        if stripped.startswith("# "):
            title = stripped[2:].strip()
            break
    if not title:
        title = input_path.stem.replace("-", " ").title()
    full_html = build_html(title=title, body_html=body)
    if output_path.exists():
        current_html = output_path.read_text(encoding="utf-8")
        if current_html == full_html:
            return
    output_path.write_text(full_html, encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Render markdown docs into standalone HTML pages."
    )
    parser.add_argument(
        "inputs",
        nargs="*",
        help="Input markdown files. If omitted, all .md files in docs/ are rendered.",
    )
    parser.add_argument(
        "--output",
        help="Optional output path. Valid only when rendering a single input.",
    )
    parser.add_argument(
        "--all",
        action="store_true",
        help="Render all .md files in docs/ (default behavior when no inputs are given).",
    )
    return parser.parse_args()


def resolve_input_path(raw: str, script_dir: Path) -> Path:
    candidate = Path(raw)
    if candidate.is_absolute():
        return candidate
    if candidate.exists():
        return candidate.resolve()
    return (script_dir / candidate).resolve()


def render_all_docs(script_dir: Path) -> int:
    markdown_paths = sorted(
        (
            path
            for path in script_dir.rglob("*.md")
            if path.name.lower() not in EXCLUDED_MARKDOWN_FILENAMES
        ),
        key=lambda p: str(p.relative_to(script_dir)).lower(),
    )
    if not markdown_paths:
        print(f"No markdown files found in: {script_dir}", file=sys.stderr)
        return 1

    for excluded in EXCLUDED_MARKDOWN_FILENAMES:
        stale_output = script_dir / Path(excluded).with_suffix(".html")
        if stale_output.exists():
            stale_output.unlink()
            print(f"Removed excluded output: {stale_output}")

    for input_path in markdown_paths:
        output_path = input_path.with_suffix(".html")
        render_markdown(input_path, output_path)
        print(f"Rendered HTML: {output_path}")

        if input_path.parent != script_dir and input_path.parent.name.lower() == "en":
            legacy_output = script_dir / output_path.name
            render_markdown(input_path, legacy_output)
            print(f"Rendered legacy HTML: {legacy_output}")

    return 0


def main() -> int:
    args = parse_args()
    script_dir = Path(__file__).resolve().parent

    if args.output and len(args.inputs) != 1:
        print("--output can be used only with exactly one input file.", file=sys.stderr)
        return 1

    if args.all or len(args.inputs) == 0:
        return render_all_docs(script_dir)

    for index, raw_input in enumerate(args.inputs):
        input_path = resolve_input_path(raw_input, script_dir)
        if not input_path.exists():
            print(f"Input file not found: {input_path}", file=sys.stderr)
            return 1

        if args.output and index == 0:
            output_path = Path(args.output).resolve()
        else:
            output_path = input_path.with_suffix(".html")

        output_path.parent.mkdir(parents=True, exist_ok=True)
        render_markdown(input_path, output_path)
        print(f"Rendered HTML: {output_path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
