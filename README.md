# PungrangLauncher

풍랑빌 공식 Minecraft Windows 런처 프로젝트입니다.

## 게임 대상
- Minecraft Java Edition 1.21.1
- NeoForge 21.1.265
- Windows x64

## 저장소 구성
- `Launcher/`: WPF Windows 런처 소스
- `docs/`: GitHub Pages 공지, 모드팩 메타데이터 및 개발 문서
- `.github/workflows/pages.yml`: Pages 자동 배포
- `.github/workflows/windows-build.yml`: Windows x64 단일 EXE 빌드 및 태그 기반 Release 업로드

## 개발 우선순위
1. Microsoft 계정 로그인
2. Minecraft 및 NeoForge 자동 설치
3. 게임 실행 및 메모리 설정 연결
4. 모드팩 자동 업데이트
5. Velopack 런처 자동 업데이트
6. 정식 Windows 설치 EXE

상세한 완료 기준과 보안 원칙은 [개발 로드맵](docs/DEVELOPMENT_ROADMAP.md)을 참고하세요.

## 현재 개발 상태
현재 WPF 런처 기본 화면, 풍랑빌 테마, 공지 JSON 조회, Minecraft Java 서버 상태 조회가 구현되어 있습니다.

Microsoft 계정 인증, 게임/NeoForge 설치 및 실행, 모드팩 자동 업데이트, Velopack 업데이트, 설치 프로그램은 아직 구현 및 검증 단계가 아닙니다. 게임 실행 버튼은 의도적으로 비활성화되어 있습니다.

## Microsoft 계정 로그인 준비
첫 번째 개발 항목은 Microsoft 계정 로그인입니다. Microsoft Entra 앱 등록과 Application (client) ID가 필요합니다. 비밀번호나 client secret을 런처에 포함하지 않습니다.

설정 방법은 [Microsoft 계정 로그인 준비 문서](docs/MICROSOFT_ACCOUNT_SETUP.md)를 참고하세요.

## 빌드
Windows 10/11 개발 PC와 .NET 10 SDK에서:

```powershell
dotnet restore Launcher/PungrangLauncher.csproj
dotnet build Launcher/PungrangLauncher.csproj -c Release
dotnet publish Launcher/PungrangLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:DebugSymbols=false -p:DebugType=None -o publish
```

GitHub Actions는 main 브랜치와 v* 태그에서 Windows x64 단일 EXE를 빌드합니다. v* 태그 빌드가 성공하면 Release에 `PungrangLauncher.exe`를 개별 파일로 첨부합니다. Actions artifact는 테스트용 다운로드이며, Release의 EXE와 구분됩니다.

## GitHub Pages
Pages 배포 소스는 **GitHub Actions**로 설정합니다.
https://windwolf0.github.io/PungrangLauncher/

## 안전 및 배포 원칙
- OAuth client secret, 사용자 토큰, 개인 키를 저장소에 커밋하지 않습니다.
- 실제 모드 파일은 라이선스와 배포 권한을 확인한 뒤 manifest에 등록합니다.
- manifest 파일에는 실제 SHA-256과 파일 크기를 사용해야 합니다.
- 모드팩 업데이트 시 사용자 월드(`saves/`)와 개인 설정(`config/`)을 보존합니다.
- 정식 Windows 설치 EXE는 Velopack 업데이트와 설치/제거 동작까지 검증한 뒤 배포합니다.
