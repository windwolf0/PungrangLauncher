# Microsoft 계정 로그인 준비

Microsoft 계정 로그인은 런처가 암호를 직접 받지 않는 MSAL 기반 데스크톱 OAuth 흐름으로 구현합니다. WPF는 MSAL.NET 지원 데스크톱 플랫폼이며, 개인 Microsoft 계정도 지원하는 공개 클라이언트로 구성합니다.

## 필요한 사전 설정

1. Microsoft Entra 관리 센터에서 런처용 앱 등록을 생성합니다.
2. 지원되는 계정 유형에서 개인 Microsoft 계정을 포함하도록 선택합니다.
3. 앱 등록의 Authentication에서 Add a platform > Mobile and desktop applications를 선택합니다.
4. 시스템 브라우저 로그인을 사용할 수 있도록 Redirect URI에 `http://localhost`를 등록합니다.
5. Advanced settings의 Allow public client flows를 Yes로 설정합니다.
6. Overview에서 Application (client) ID를 복사합니다.
7. client secret은 만들거나 런처에 포함하지 않습니다.

공식 안내:
- https://learn.microsoft.com/entra/identity-platform/scenario-desktop-app-configuration
- https://learn.microsoft.com/entra/identity-platform/msal-authentication-flows

## 보안 요구사항

- Microsoft 계정 암호를 런처 UI에서 입력받지 않습니다.
- MSAL의 공개 클라이언트 인증 흐름을 사용하며 앱에 client secret을 포함하지 않습니다.
- 토큰 및 refresh token을 소스 코드, 설정 파일, 로그에 기록하지 않습니다.
- 토큰 캐시는 Windows 사용자 범위 보호 기능을 사용합니다.
- 로그아웃 시 로컬 계정 캐시를 제거합니다.
- Microsoft 계정 인증만 성공했다고 Minecraft 소유권/플레이 권한을 가정하지 않습니다. Xbox Live/XSTS 및 Minecraft 서비스 인증, 프로필/권한 확인을 별도로 수행합니다.

## 개발 진행에 필요한 값

앱 등록 후 Application (client) ID가 필요합니다. 이 값은 공개 클라이언트 식별자이며 client secret과 다릅니다. client ID가 준비되기 전에는 인증 서비스의 UI/구조와 테스트를 준비하되 실제 계정 인증 완료를 선언하지 않습니다.
