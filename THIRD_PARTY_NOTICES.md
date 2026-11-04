# Third-party and asset notes / 第三方与素材说明

- Application code is an independent implementation. No original author's source code, executable, icon or screenshots from the inspiration post are redistributed.
- The app uses Windows/.NET Framework/WPF APIs supplied by the operating system. Those components are not bundled in the portable ZIP and retain their own licenses.
- No third-party application packages, fonts or commercial icon libraries are bundled. Font names refer to fonts available on the user's Windows installation.
- The ox-and-clock icon was generated and recolored with OpenAI's built-in image generation tool for this project. See [icon provenance](docs/ICON.md). The asset is distributed as part of the MIT project; no claim of exclusive rights to AI-generated imagery is made.
- GitHub workflow actions are referenced from their official repositories; their code is not copied into this source tree. [Checkout](https://github.com/actions/checkout) and [upload-artifact](https://github.com/actions/upload-artifact) keep their upstream licenses.

The project's license is [MIT](LICENSE). Retain its copyright and permission notice when redistributing project code.

## Stock beta data

Yahoo Finance is an optional experimental remote data source, not a bundled library or guaranteed official API. Yahoo/provider terms govern data; the MIT license covers project code, not market-data redistribution. No actual quote payload is included in public examples. Windows.Media.Ocr is an installed Windows component; no OCR language packs are bundled or installed. The browser extension has no external scripts or libraries. See docs/STOCK_BETA.md.
