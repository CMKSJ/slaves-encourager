# 직장인 응원 / Slaves Encourager

고정된 홈 문구를 두 번 클릭하거나 버튼을 우클릭해 편집하세요 (`Ctrl+E`). 저장·취소·개별 문구 또는 현재 언어 복원을 지원하며 언어별로 저장됩니다. [설명](docs/HOME_TEXT.md).

열심히 보낸 시간을 작은 보상으로 바꿔 보세요. Windows용 수입 시계, 퇴근 카운트다운, 근무 달력과 목표 목록입니다.

[简体中文](README.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [Español](README.es.md) · **한국어**

<img src="docs/screenshots/ko-main.png" width="380" alt="한국어 화면">

## 사용 방법

Windows 10/11 및 .NET Framework 4.8이 필요합니다. ZIP을 풀고 `SlavesEncourager.exe`를 실행하세요. 계정과 인터넷이 필요 없으며 원격 측정 정보를 보내지 않습니다.

처음에는 데모 데이터가 표시됩니다. «처음 사용 설정»에서 월급과 근무 시간을 입력하고 달력에서 휴일과 추가 근무를 지정한 다음 저장하세요. 목표는 최대 20개까지 추가하고 순서를 바꿀 수 있습니다. 설정 오른쪽 위에서 6개 언어를 선택할 수 있으며 저장하면 유지됩니다. 취소하면 원래 언어로 돌아갑니다.

플립 시계, 슬라이드, 정적 숫자 효과를 선택하고 소수 자릿수, 구분 기호, 통화 기호와 표시 형식을 조정하세요. 주 창의 버튼 외 영역을 드래그하면 이동합니다. 창이 활성화된 상태에서 `Ctrl+M`은 미니 모드, `Ctrl+Shift+P`는 금액 숨기기입니다. «—»는 트레이로 숨기고 «×»는 종료합니다.

## 계산 및 저장

월급을 이번 달 근무일 수 또는 고정 21.75일 기준으로 나눕니다. 휴게 시간은 제외하고 지난 근무일은 만근으로 추정합니다. 세금, 상여금, 초과 근무 수당과 실제 근태는 계산하지 않습니다. 휴일은 회사 일정에 맞게 지정하세요. 야간 근무는 시작 날짜에 속하며 통화 선택은 기호만 바꿉니다.

목표에는 이번 달 추정 수입을 목록 순서대로 한 번씩 배분합니다. 매달 초기화되며 실제 저축 잔액이 아닙니다.

설정은 호환성을 위해 `%LOCALAPPDATA%\NiumaFlip\settings.json`에 저장합니다. 암호화되지 않으며 저장 시 `.bak` 사본을 남깁니다.

## 개발 및 라이선스

Windows 컨테이너를 우선 사용합니다: `./scripts/build.ps1`. 사용할 수 없다면 기존 컴파일러로 `./scripts/build.ps1 -Native -VisualCheck`를 실행하고 `python scripts/validate.py`, `python scripts/package.py`로 검사 및 패키징하세요.

[MIT 라이선스](LICENSE) · [개발 안내](docs/DEVELOPMENT.md) · [기여 안내](CONTRIBUTING.md) · [변경 기록](CHANGELOG.md)
