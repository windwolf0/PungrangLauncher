# PungrangLauncher

풍랑빌 공식 Minecraft 런처 프로젝트입니다.

## 목표 환경

- Minecraft: 1.21.1
- NeoForge: 21.1.265
- 대상 플랫폼: Windows x64
- 런처 업데이트: Velopack / GitHub Releases (구현 및 검증 예정)
- 공지 및 모드팩 메타데이터: GitHub Pages

## 저장소 구성

- `docs/index.html`: GitHub Pages 시작 페이지
- `docs/launcher/announcements.json`: 런처 공지 피드
- `docs/launcher/modpack/manifest.json`: 모드팩 manifest
- `.github/workflows/pages.yml`: Pages 자동 배포
- `.gitignore`: .NET 빌드 산출물 및 로컬 비밀정보 제외

## 다음 단계

1. Settings → Pages에서 배포 소스를 **GitHub Actions**로 설정합니다.
2. Pages 배포 성공 후 `https://windwolf0.github.io/PungrangLauncher/`를 확인합니다.
3. 승인된 모드 파일만 manifest에 HTTPS URL, 파일 크기, SHA-256과 함께 추가합니다.
4. Microsoft OAuth 비밀정보, 계정 토큰, 개인 키는 저장소에 커밋하지 않습니다.

> 현재 저장소에는 배포 기반 구조만 있습니다. 완성된 Windows 실행 파일이나 실제 모드팩 파일은 아직 포함되어 있지 않습니다.
