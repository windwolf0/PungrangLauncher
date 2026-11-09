# PungrangLauncher 개발 로드맵

풍랑빌 공식 런처의 목표는 Minecraft Java Edition 1.21.1 / NeoForge 21.1.265 환경을 사용자가 직접 구성하지 않아도 설치·업데이트·실행할 수 있게 하는 것입니다.

## 개발 우선순위

| 순서 | 기능 | 상태 | 완료 기준 |
|---:|---|---|---|
| 1 | Microsoft 계정 로그인 | 코드 연결 완료 · Client ID 및 실사용 인증 검증 대기 | Microsoft 기기 코드 로그인, Xbox Live/XSTS 인증, Minecraft 프로필 및 보유 권한 확인, DPAPI 토큰 캐시, 로그아웃 |
| 2 | Minecraft 및 NeoForge 자동 설치 | 대기 | Java 런타임 확인/설치, Minecraft 1.21.1 설치, NeoForge 21.1.265 프로파일 설치 및 무결성 검증 |
| 3 | 게임 실행 및 메모리 설정 연결 | 대기 | 선택한 계정과 설치 프로파일로 실행, 최소/최대 메모리 설정 적용, 실행 상태 및 종료 감지 |
| 4 | 모드팩 자동 업데이트 | 대기 | 원격 manifest 버전 비교, SHA-256/파일 크기 검증, 변경 파일만 다운로드, 사용자 월드와 개인 설정 보존 |
| 5 | Velopack 런처 자동 업데이트 | 대기 | 업데이트 채널, 새 버전 확인, 설치/복구, 실패 시 안전한 롤백 |
| 6 | 정식 Windows 설치 EXE | 대기 | 설치/제거, 바로가기, 설치 경로 및 업데이트 연동, 서명 및 배포 검증 |

## 구현 원칙

- 대상 게임 버전은 Minecraft Java 1.21.1, NeoForge 21.1.265, Windows x64입니다.
- Microsoft 계정 비밀번호를 런처가 직접 수집하지 않습니다. MSAL 기기 코드 인증으로 Microsoft 웹 페이지에서 로그인합니다.
- OAuth client secret, 사용자 토큰, 개인 키는 저장소에 커밋하지 않습니다.
- MSAL 토큰 캐시는 Windows DPAPI CurrentUser 범위로 암호화하여 LocalAppData에 저장하고 로그아웃 시 삭제합니다.
- Minecraft 액세스 토큰은 게임 실행 기능 연결 전까지 현재 런처 프로세스 메모리에만 보관합니다.
- 게임 파일은 공식 manifest/다운로드 메타데이터를 기준으로 설치하고 해시를 검증합니다.
- 모드팩 업데이트는 사용자 월드와 개인 설정을 기본적으로 보존합니다.
- 모드 파일은 배포 권한과 라이선스가 확인된 파일만 manifest에 등록합니다.
- 런처 자체 업데이트와 Windows 설치 프로그램은 게임 설치/실행 기능이 검증된 뒤 적용합니다.

## 현재 상태

WPF 런처 기본 화면, 공지사항 조회, Minecraft 서버 상태 조회가 구현되어 있습니다.

Microsoft 로그인 코드는 MSAL 기기 코드 발급, Xbox Live 사용자 토큰 교환, XSTS, Minecraft Services 로그인, Java Edition entitlement 및 프로필 확인까지 연결했습니다. Windows x64 단일 EXE 빌드와 단일 파일 출력 검증은 GitHub Actions에서 성공했습니다.

실제 계정 인증은 Microsoft Entra 앱 등록 Client ID가 필요합니다. 현재 Client ID가 저장소 Actions 변수에 등록되지 않았기 때문에 로그인 버튼은 비활성화 상태입니다. Client ID를 등록한 빌드에서 실제 Microsoft/Xbox/Minecraft 인증을 성공해야 1번 항목을 완료로 처리합니다.

## 다음 작업

1. Microsoft Entra에서 앱을 등록하고 개인 Microsoft 계정과 Public client/device-code 흐름을 활성화합니다.
2. Repository variable `MICROSOFT_CLIENT_ID`에 Application (client) ID를 등록합니다. 이 값은 공개 식별자이므로 Secret이 아닌 Actions Variable로 설정합니다.
3. 새 EXE에서 Microsoft 로그인, Xbox Live/XSTS 교환, Java Edition 권한 및 프로필 확인을 실계정으로 검증합니다.
4. 1번 항목을 검증한 뒤 2번 Minecraft/NeoForge 자동 설치로 진행합니다.

설정 방법은 [Microsoft 계정 로그인 설정](MICROSOFT_ACCOUNT_SETUP.md)을 참고하세요.

## 릴리스 정책

- 초기 개발 빌드는 GitHub Actions 아티팩트로 검증합니다.
- v0.1.0 등의 정식 Release 태그는 해당 버전의 기능과 테스트 범위가 명확해진 뒤 생성합니다.
- 정식 Windows 설치 EXE는 6번 항목에서 Velopack 기반 설치/업데이트 구성을 확정한 뒤 배포합니다.
