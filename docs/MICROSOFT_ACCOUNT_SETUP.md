# Microsoft 계정 로그인 설정

풍랑빌 런처는 MSAL.NET의 기기 코드(Device Code) 인증을 사용합니다. 런처에 표시되는 코드를 Microsoft 인증 페이지에 입력하며, 런처는 Microsoft 계정 비밀번호를 직접 받지 않습니다.

## Microsoft Entra 앱 등록

1. Microsoft Entra 관리 센터에서 런처용 앱 등록을 생성합니다.
2. 지원되는 계정 유형에서 개인 Microsoft 계정(MSA)을 포함하도록 선택합니다.
3. Authentication > Add a platform에서 Mobile and desktop applications를 선택합니다.
4. Redirect URI에 `https://login.microsoftonline.com/common/oauth2/nativeclient`를 추가합니다.
5. Advanced settings의 Allow public client flows를 Yes로 설정합니다.
6. Overview에서 Application (client) ID를 복사합니다.
7. client secret은 만들거나 런처에 포함하지 않습니다. 데스크톱 공개 클라이언트에는 secret을 배포할 수 없습니다.

사용자는 런처에 표시된 기기 코드를 Microsoft 인증 페이지에 입력합니다. MSAL의 기본 native-client redirect URI도 앱 등록에 추가해 둡니다.

공식 안내:
- https://learn.microsoft.com/entra/identity-platform/v2-oauth2-device-code
- https://learn.microsoft.com/entra/msal/dotnet/acquiring-tokens/desktop-mobile/acquiring-tokens-interactively
- https://learn.microsoft.com/en-us/xbox/gdk/docs/services/fundamentals/s2s-auth-calls/service-authentication/live-website-authentication?view=gdk-2604

## 런처 빌드에 Client ID 설정

개발 PC에서는 Client ID를 publish 시점에 어셈블리 메타데이터로 포함합니다.

```powershell
$env:PUNGRANG_MICROSOFT_CLIENT_ID = "앱 등록의 Application (client) ID"
dotnet publish Launcher/PungrangLauncher.csproj -c Release -r win-x64 --self-contained true -p:MicrosoftClientId="$env:PUNGRANG_MICROSOFT_CLIENT_ID" -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:IncludeAllContentForSelfExtract=true -p:DebugSymbols=false -p:DebugType=None -o publish
```

GitHub Actions Release 빌드에 Client ID를 포함하려면 저장소의 Settings > Secrets and variables > Actions > Variables에서 `MICROSOFT_CLIENT_ID`라는 Repository variable을 추가합니다. 이 값은 공개 클라이언트 식별자이므로 Secret이 아니라 Variable로 관리합니다. Windows 빌드 workflow가 이 값을 EXE에 포함합니다.

Client ID가 제공되지 않으면 런처의 로그인 버튼은 비활성화됩니다. Client ID가 있는 빌드에서는 사용자 PC에 환경 변수를 따로 설정하지 않아도 됩니다.

## 인증 흐름

1. Microsoft 계정: `XboxLive.signin` 및 `XboxLive.offline_access` 권한으로 기기 코드 인증
2. Xbox Live: Microsoft 액세스 토큰을 Xbox 사용자 토큰으로 교환
3. XSTS: Minecraft Services용 XSTS 토큰 발급
4. Minecraft Services: Minecraft 액세스 토큰 발급
5. Entitlements: Java Edition 이용 권한 확인
6. Profile: Minecraft 플레이어 프로필과 UUID 확인

## 보안 요구사항

- Microsoft 계정 암호를 런처 UI에서 입력받지 않습니다.
- OAuth client secret, 사용자 토큰, 개인 키를 저장소에 커밋하지 않습니다.
- MSAL 토큰 캐시는 Windows DPAPI의 CurrentUser 범위로 암호화하여 LocalAppData에 저장합니다.
- Minecraft 액세스 토큰은 현재 런처 프로세스 메모리에만 보관합니다. 파일이나 로그에 기록하지 않습니다.
- 로그아웃 시 MSAL 계정 캐시를 제거합니다.
- Xbox/Minecraft 인증 실패, Xbox 연령/가족 설정 오류, 게임 이용 권한 없음, 프로필 미생성 상태를 구분해 안내합니다.

## 유의 사항

Microsoft 계정 앱 등록만으로 Minecraft Services 접근이 자동 승인된다고 단정할 수 없습니다. 신규 앱 등록이 Minecraft Services에서 403을 반환한다면 Microsoft/Minecraft 측의 API 접근 승인 절차를 확인해야 합니다. 실제 사용자 인증 성공과 Java Edition 권한 확인은 등록된 Client ID로 테스트한 뒤에만 완료 처리합니다.
