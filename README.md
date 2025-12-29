# smate 빌드 및 실행 가이드


<br/>
<br/>

## 0. 사전 요구 사항
빌드를 시작하기 전에 아래 환경이 갖춰져 있어야 합니다.
 - Unity Editor: 6.2 이상 권장
 - Unity Hub: 프로젝트 관리 및 에디터 설치용
 - IDE: Visual Studio 2022 또는 VS Code


## 1. smate 빌드 및 실행 가이드
 - 레포지토리 클론
```bash
git clone https://github.com/사용자계정/smate.git
```
 - Unity Hub 실행 후 Add 버튼을 눌러 프로젝트 폴더를 선택합니다.
 - 리스트에 추가된 smate 프로젝트를 클릭하여 에디터를 엽니다.

<br/>
<br/>

## 2. 클라이언트 빌드 방법
```plaintext
Step 1 : File > Build Settings 메뉴를 엽니다. (Ctrl + Shift + B)

Step 2 : Scenes In Build 목록에 실행할 씬들이 포함되어 있는지 확인합니다. (없으면 Add Open Scenes)

Step 3 : 타겟 플랫폼(PC, Mac, Linux 또는 Android)을 선택합니다.

Step 4 : 하단의 Build 버튼을 누르고 결과물이 저장될 폴더를 지정합니다.
```

<br/>
<br/>

## 3. 백엔드 연결 설정 (Backend Integration)
### 이 프로젝트는 Flask 및 Spring Boot와 통신하므로, 빌드된 클라이언트가 서버를 찾을 수 있어야 합니다.

- 서버 주소 설정: Unity 내부의 API 관리 스크립트(예: NetworkManager.cs)에서 Flask 서버와 Spring Boot 서버의 IP 주소가 현재 구동 중인 서버 주소와 일치하는지 확인하세요.
  - Flask 서버 주소 예시: http://localhost:5000
  - Spring Boot 주소 예시: http://localhost:8080
  

## 4. 프로젝트 구조 (Directory Structure)
```
Assets/: 스크립트, 프리팹, UI 요소 등 핵심 리소스 보관

ProjectSettings/: 유니티 프로젝트의 전반적인 환경 설정

smate.sln: Visual Studio 프로젝트 솔루션 파일
```


