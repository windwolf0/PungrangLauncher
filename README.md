# PungrangLauncher

풍랑빌 공식 Minecraft Windows 런처 프로젝트입니다.

## 게임 대상
- Minecraft Java Edition 1.21.1
- NeoForge 21.1.265
- Windows x64

## 저장소 구성
- `Launcher/`: WPF Windows 런처 소스
- `docs/`: GitHub Pages 공지 및 모드팩 메타데이터
- `.github/workflows/pages.yml`: Pages 자동 배포
- `.github/workflows/windows-build.yml`: Windows x64 CI 빌드 및 artifact 업로드

## 개발 상태
현재 초기 WPF 앱 골격, 풍랑빌 테마, 공지 JSON 조회, Minecraft Java 서버 상태 조회 UI가 추가되어 있습니다.

아직 Microsoft 계정 인증, 게임/NeoForge 설치 및 실행, 모드팩 업데이트 적용, Velopack 배포는 구현·검증되지 않았습니다. 게임 실행 버튼은 의도적으로 비활성화되어 있습니다.

## 빌드
Windows 10/11 개발 PC와 .NET 10 SDK에서:

```powershell
dotnet restore Launcher/PungrangLauncher.csproj
dotnet build Launcher/PungrangLauncher.csproj -c Release
dotnet publish Launcher/PungrangLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o publish
```

GitHub Actions의 **Build Windows launcher** workflow도 push 시 자동 실행되며, 성공하면 `PungrangLauncher-win-x64` artifact를 생성합니다.

## GitHub Pages
Pages 배포 소스는 **GitHub Actions**로 설정합니다. 배포 주소:
https://windwolf0.github.io/PungrangLauncher/

## 안전 및 배포 메모
- OAuth client secret, 사용자 토큰, 개인 키를 저장소에 커밋하지 않습니다.
- 실제 모드 파일은 라이선스와 배포 권한을 확인한 뒤 manifest에 등록합니다.
- manifest 파일에는 실제 SHA-256과 파일 크기를 사용해야 합니다.
- 게임 업데이트 시 `saves/` 및 사용자 `config/`를 보존해야 합니다.
- CI artifact는 테스트용 빌드 산출물이며, 정식 설치 프로그램이나 코드 서명된 릴리스가 아닙니다.
