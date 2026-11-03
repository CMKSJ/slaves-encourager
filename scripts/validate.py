"""验证翻译完整性、项目链接与开源交付范围 / Validate translations, links and release scope."""
import csv
import json
import re
import struct
from pathlib import Path
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
LANGUAGES = ["zh-Hans", "zh-Hant", "en", "ja", "es", "ko"]


def validate():
    with (ROOT / "locales/catalog.tsv").open(encoding="utf-8", newline="") as stream:
        rows = list(csv.reader(stream, delimiter="\t"))
    assert rows[0] == ["key"] + LANGUAGES
    keys = {row[0] for row in rows[1:]}
    assert len(keys) == len(rows) - 1, "Duplicate translation keys"
    for row in rows[1:]:
        assert len(row) == 7 and all(value.strip() for value in row), row[0]
        tokens = set(re.findall(r"\{(?:\d+(?::[^}]+)?|amount|symbol|code)\}", row[1]))
        for value in row[2:]:
            assert set(re.findall(r"\{(?:\d+(?::[^}]+)?|amount|symbol|code)\}", value)) == tokens, row[0]
    referenced = set()
    for file in (ROOT / "src").rglob("*"):
        if file.suffix not in {".cs", ".xaml", ".csproj", ".manifest"}:
            continue
        text = file.read_text(encoding="utf-8")
        assert not text.startswith("\ufeff"), file
        if file.suffix != ".cs":
            ElementTree.fromstring(text)
        referenced.update(re.findall(r'L\.T\("([\w.]+)"', text))
        referenced.update(re.findall(r'\{DynamicResource ([a-z][\w.]*\.[\w.]+)\}', text))
        assert "FLIP" not in text, f"Old branding: {file}"
    assert not (referenced - keys), "Missing keys: " + str(referenced - keys)
    icon = (ROOT / "assets/app.ico").read_bytes()
    assert struct.unpack_from("<HHH", icon) == (0, 1, 7)
    icon_sizes = [256 if icon[6 + i * 16] == 0 else icon[6 + i * 16] for i in range(7)]
    assert icon_sizes == [16, 24, 32, 48, 64, 128, 256]
    for readme in ROOT.glob("README*.md"):
        text = readme.read_text(encoding="utf-8")
        for target in re.findall(r'\]\(([^)#]+)(?:#[^)]*)?\)', text):
            if "://" not in target and not target.startswith("mailto:"):
                assert (readme.parent / target).exists(), f"Broken link: {readme.name} -> {target}"
        for target in re.findall(r'<(?:img|a)[^>]+(?:src|href)="([^"]+)"', text):
            # 网络徽章保留远程地址，仅核对本地资源 / Keep remote badges external and check local resources.
            if "://" not in target and not target.startswith("#"):
                assert (readme.parent / target).exists(), f"Missing local resource: {target}"
    demo = (ROOT / "docs/demo.gif").read_bytes()
    assert demo[:6] == b"GIF89a" and struct.unpack_from("<HH", demo, 6) == (1000, 660), "Invalid README animation canvas"
    assert b"NETSCAPE2.0\x03\x01\x00\x00\x00" in demo, "README animation must loop"
    license_text = (ROOT / "LICENSE").read_text(encoding="utf-8")
    assert "MIT License" in license_text and "Permission is hereby granted" in license_text
    summary = {"languages": LANGUAGES, "translation_keys": len(keys), "referenced_keys": len(referenced), "xml": "PASS", "translation_tokens": "PASS", "icon_sizes": icon_sizes, "readme_links": "PASS", "readme_animation": {"width": 1000, "height": 660, "loop": "infinite", "bytes": len(demo)}, "license": "MIT"}
    output = ROOT / ".build"
    output.mkdir(exist_ok=True)
    (output / "static-validation.json").write_text(json.dumps(summary, indent=2), encoding="utf-8")
    return summary


if __name__ == "__main__":
    print(json.dumps(validate(), indent=2))
