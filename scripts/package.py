"""生成便携包和干净源码包 / Build portable and clean source archives."""
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED

ROOT = Path(__file__).resolve().parents[1]
VERSION = "1.5.0-beta.4"
ROOT_FILES = ["README.md", "README.en.md", "README.zh-Hant.md", "README.ja.md", "README.es.md", "README.ko.md",
              "LICENSE", "CONTRIBUTING.md", "THIRD_PARTY_NOTICES.md", "CHANGELOG.md", "AGENTS.md", "Dockerfile.windows", ".gitignore", ".gitattributes"]
SOURCE_DIRS = ["src", "locales", "assets", "scripts", "tests", "docs", ".github", "browser-extension", "examples"]


def source_files():
    files = [ROOT / name for name in ROOT_FILES]
    for directory in SOURCE_DIRS:
        files.extend(path for path in (ROOT / directory).rglob("*") if path.is_file()
                     and "__pycache__" not in path.parts and path.suffix not in {".pyc", ".zip", ".bak", ".tmp", ".exe", ".pdb"})
    for path in files:
        assert path.is_file(), path
        assert path.name != "settings.json" and "test-data" not in path.parts, path
    return sorted(set(files))


def archive(destination, entries):
    with ZipFile(destination, "w", ZIP_DEFLATED) as output:
        for source, name in entries:
            output.write(source, name)
    with ZipFile(destination) as output:
        assert output.testzip() is None
        assert len(output.namelist()) == len(entries)
        for source, name in entries:
            assert output.read(name) == source.read_bytes(), name
            assert not name.startswith((".build/", "dist/")) and "settings.json" not in name
    return {"name": destination.name, "files": len(entries), "bytes": destination.stat().st_size,
            "sha256": hashlib.sha256(destination.read_bytes()).hexdigest()}


def main():
    output = ROOT / "dist"
    output.mkdir(exist_ok=True)
    files = source_files()
    for name in ("SlavesEncourager.exe", "SlavesEncourager.exe.config", "StockOcr.exe", "StockOcr.exe.config"):
        assert (output / name).is_file(), "Build the application first"
    portable_entries = [(output / name, name) for name in ("SlavesEncourager.exe", "SlavesEncourager.exe.config", "StockOcr.exe", "StockOcr.exe.config")]
    portable_entries.extend((file, file.relative_to(ROOT).as_posix()) for file in files
                            if file.suffix == ".md" or file.name == "LICENSE" or file.relative_to(ROOT).parts[0] in {"assets", "docs", "browser-extension", "examples"})
    source_entries = [(file, file.relative_to(ROOT).as_posix()) for file in files]
    results = [archive(output / f"SlavesEncourager-{VERSION}-windows-portable.zip", portable_entries),
               archive(output / f"SlavesEncourager-{VERSION}-source.zip", source_entries)]
    (output / "SHA256SUMS.txt").write_text("".join(f"{entry['sha256']}  {entry['name']}\n" for entry in results), encoding="utf-8")
    (ROOT / ".build/package-manifest.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(json.dumps(results, indent=2))


if __name__ == "__main__":
    main()
