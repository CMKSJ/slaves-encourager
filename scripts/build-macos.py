"""在原生 macOS runner 编译、验证、签名及打包 / Build, verify, ad-hoc sign and package on native macOS."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import plistlib
import shutil
import subprocess
import sys
from zipfile import ZipFile
from xml.etree import ElementTree

ROOT = Path(__file__).resolve().parents[1]
VERSION = "1.5.0-beta.4"


def run(*command, **kwargs):
    subprocess.run(command, cwd=ROOT, check=True, **kwargs)


def licenses(resources, runtime):
    # 随包保留实际解析依赖的许可，不输出开发机路径 / Retain resolved dependency licenses without developer paths.
    assets = json.loads((ROOT / "src/SlavesEncourager.Mac/obj/project.assets.json").read_text("utf-8"))
    folders = [Path(p) for p in assets["packageFolders"]]
    libraries = [(name, info) for name, info in assets["libraries"].items() if info["type"] == "package"]
    target = resources / "ThirdPartyLicenses"
    target.mkdir()
    manifest = []
    for name, info in libraries:
        package = next((folder / info["path"] for folder in folders if (folder / info["path"]).is_dir()), None)
        assert package is not None, name
        destination = target / name.replace("/", "-")
        destination.mkdir(exist_ok=True)
        nuspec = next(package.glob("*.nuspec"))
        metadata = ElementTree.parse(nuspec).getroot()
        terms = [element.text for element in metadata.iter() if element.tag.rsplit("}", 1)[-1] in {"license", "licenseUrl"}]
        copied = []
        for archive in package.glob("*.nupkg"):
            with ZipFile(archive) as z:
                for item in z.namelist():
                    base = Path(item).name
                    if not base or not any(word in base.lower() for word in ("license", "licence", "third-party", "thirdparty", "notice")) or len(z.read(item)) > 2000000:
                        continue
                    file = destination / (str(len(copied)) + "-" + base)
                    file.write_bytes(z.read(item)); copied.append(file.name)
        (destination / "metadata.json").write_text(json.dumps({"package": name, "license": terms, "sha512": info.get("sha512")}, indent=2) + "\n", encoding="utf-8")
        manifest.append({"package": name, "license": terms, "files": copied})
    # 自包含运行时不总列入 project.assets；额外保留运行时包许可 / Runtime packs may be absent from project.assets.
    for folder in folders:
        pack = folder / ("microsoft.netcore.app.runtime." + runtime)
        if pack.exists():
            for path in pack.rglob("*"):
                if path.is_file() and path.name.lower() in {"license.txt", "third-party-notices.txt"}:
                    shutil.copyfile(path, target / (path.parent.name + "-" + path.name))
    (target / "packages.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--runtime", choices=["osx-arm64", "osx-x64"], required=True)
    args = parser.parse_args()
    assert sys.platform == "darwin", "Use a native macOS runner for OCR, signing and launch verification"
    output = ROOT / "dist/macos" / args.runtime
    assert not output.exists(), "Use a fresh build directory; never overwrite a published bundle"
    output.mkdir(parents=True)
    app = output / "Slaves Encourager.app"
    contents = app / "Contents"
    binary = contents / "MacOS"
    resources = contents / "Resources"
    resources.mkdir(parents=True)
    project = "src/SlavesEncourager.Mac/SlavesEncourager.Mac.csproj"
    run("dotnet", "publish", project, "-c", "Release", "-r", args.runtime, "--self-contained", "true", "-o", str(binary))
    executable = binary / "SlavesEncourager.Mac"
    executable.chmod(0o755)
    target = "arm64-apple-macosx14.0" if args.runtime == "osx-arm64" else "x86_64-apple-macosx14.0"
    run("xcrun", "swiftc", "-O", "-target", target, str(ROOT / "scripts/MacOcr.swift"), "-o", str(binary / "StockOcr"), "-framework", "Vision", "-framework", "AppKit")
    run(str(executable), "--self-test")
    ocr = subprocess.check_output([str(binary / "StockOcr"), str(ROOT / "examples/holdings-sample.png")], text=True)
    ocr_file = ROOT / ".build" / (args.runtime + "-ocr.tsv")
    ocr_file.parent.mkdir(exist_ok=True)
    ocr_file.write_text(ocr, encoding="utf-8")
    run(str(executable), "--ocr-table-check", str(ocr_file))
    preview = ROOT / ".build/macos-preview" / args.runtime
    run(str(executable), "--ui-smoke", "--output", str(preview), timeout=120)
    assert (preview / "ui-result.txt").is_file()
    iconset = output / "App.iconset"
    iconset.mkdir()
    for size in (16, 32, 128, 256, 512):
        for multiple in (1, 2):
            dest = iconset / (f"icon_{size}x{size}" + ("@2x" if multiple == 2 else "") + ".png")
            run("sips", "-z", str(size * multiple), str(size * multiple), str(ROOT / "assets/app.png"), "--out", str(dest), stdout=subprocess.DEVNULL)
    run("iconutil", "-c", "icns", str(iconset), "-o", str(resources / "App.icns"))
    plist = {"CFBundleName": "Slaves Encourager", "CFBundleDisplayName": "Slaves Encourager",
        "CFBundleIdentifier": "io.github.thisemailrectrash-cpu.slaves-encourager.beta",
        "CFBundleExecutable": "SlavesEncourager.Mac", "CFBundleIconFile": "App.icns", "CFBundlePackageType": "APPL",
        "CFBundleShortVersionString": "1.5.0", "CFBundleVersion": "1.5.0.4", "LSMinimumSystemVersion": "14.0",
        "NSHighResolutionCapable": True, "NSHumanReadableCopyright": "MIT — Slaves Encourager contributors"}
    (contents / "Info.plist").write_bytes(plistlib.dumps(plist))
    licenses(resources, args.runtime)
    shutil.copytree(ROOT / "browser-extension", resources / "browser-extension")
    shutil.copyfile(ROOT / "LICENSE", resources / "LICENSE")
    shutil.copyfile(ROOT / "THIRD_PARTY_NOTICES.md", resources / "THIRD_PARTY_NOTICES.md")
    entitlements = output / "entitlements.plist"
    entitlements.write_bytes(plistlib.dumps({"com.apple.security.cs.allow-jit": True}))
    # 先逐文件签名，再签主程序与 bundle；不是 Developer ID 公证 / Sign files, then apphost/bundle; not Developer ID notarization.
    for path in sorted(binary.rglob("*")):
        if path.is_file() and path != executable:
            run("codesign", "--force", "--sign", "-", "--entitlements", str(entitlements), str(path))
    run("codesign", "--force", "--sign", "-", "--entitlements", str(entitlements), str(executable))
    run("codesign", "--force", "--sign", "-", "--entitlements", str(entitlements), str(app))
    run("codesign", "--verify", "--deep", "--strict", "--verbose=2", str(app))
    run(str(executable), "--self-test")
    run(str(executable), "--launch-check", timeout=45)
    # DMG 与 ZIP 都保留完整 bundle 和执行权限 / Both formats preserve the whole bundle and execute permissions.
    payload = output / "payload"
    payload.mkdir()
    shutil.copytree(app, payload / app.name, symlinks=True)
    (payload / "Applications").symlink_to("/Applications")
    shutil.copyfile(ROOT / "docs/MACOS.md", payload / "README-macOS.md")
    shutil.copytree(ROOT / "browser-extension", payload / "browser-extension")
    shutil.copyfile(ROOT / "LICENSE", payload / "LICENSE")
    shutil.copyfile(ROOT / "THIRD_PARTY_NOTICES.md", payload / "THIRD_PARTY_NOTICES.md")
    stem = f"SlavesEncourager-{VERSION}-macos-" + ("apple-silicon" if args.runtime == "osx-arm64" else "intel")
    run("hdiutil", "create", "-volname", "Slaves Encourager", "-srcfolder", str(payload), "-ov", "-format", "UDZO", str(output / (stem + ".dmg")))
    run("ditto", "-c", "-k", "--sequesterRsrc", "--keepParent", str(app), str(output / (stem + ".zip")))
    artifacts = [output / (stem + extension) for extension in (".dmg", ".zip")]
    manifest = [{"name": p.name, "bytes": p.stat().st_size, "sha256": hashlib.sha256(p.read_bytes()).hexdigest()} for p in artifacts]
    (output / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(manifest, indent=2))


if __name__ == "__main__":
    main()
