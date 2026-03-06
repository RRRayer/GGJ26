# Deathmatch 구현 태스크 분해 (파일/클래스 단위)

## Epic A. 모드 라우팅/세션

### A-1. Mode enum/상수 확장
- **파일**: `Assets/01. Scripts/Network/FusionLauncher.cs` (또는 모드 관리 파일)
- **작업**
  - `classic` 외 `deathmatch` 모드 식별 상수 추가
  - 룸 생성/입장 시 mode 파싱 유틸 통일
- **완료 기준**
  - 룸 mode가 `deathmatch`면 데스매치 컨트롤러 활성화

### A-2. 로비 UI 모드 선택 반영
- **파일**: `Assets/01. Scripts/UI/CreateRoomModalView.cs` (실제 파일명에 맞춤)
- **작업**
  - 모드 토글/드롭다운에 `Deathmatch` 추가
  - CustomProperties에 `"mode":"deathmatch"` 저장
- **완료 기준**
  - 로비 목록/입장 후 모드가 일관되게 전달됨

---

## Epic B. 플레이어 상태(목숨/킬/탈락)

### B-1. DeathmatchPlayerState 신규
- **신규 파일**: `Assets/01. Scripts/Network/Deathmatch/DeathmatchPlayerState.cs`
- **책임**
  - `Lives`, `Kills`, `LifeLostTotal`, `IsEliminated` 네트워크 상태 보관
  - StateAuthority에서만 상태 변경
- **API 초안**
  - `InitializeForMatch()`
  - `ApplyLifeDelta(int delta, DamageReason reason)`
  - `RegisterKill()`

### B-2. PlayerElimination 연동
- **파일**: `Assets/01. Scripts/Network/PlayerElimination.cs`
- **작업**
  - Deathmatch에서 `Lives<=0`일 때만 탈락 처리
  - 기존 관전자 전환 재사용
- **완료 기준**
  - 데스매치 탈락 시 기존 관전/카메라 흐름 정상

---

## Epic C. 근접 공격 시스템

### C-1. DeathmatchMeleeAttack 신규
- **신규 파일**: `Assets/01. Scripts/Network/Deathmatch/DeathmatchMeleeAttack.cs`
- **책임**
  - 좌클릭 입력 감지
  - 공격 쿨다운 관리
  - 히트 판정 요청(RPC) 및 StateAuthority 최종 처리
- **판정 규칙**
  - Player 우선, 그 다음 NPC
  - 플레이어 명중: 대상 `Lives=0`, 공격자 `Kills+1`, 공격자 `Lives=3`
  - NPC 명중: 공격자 `Lives-1`

### C-2. 입력 브리지 연동
- **파일**: `Assets/01. Scripts/Network/FusionInputBridge.cs`
- **작업**
  - Deathmatch 공격 버튼 플래그 추가/전달
- **완료 기준**
  - 네트워크 틱 중복 입력 없이 1회 클릭=1회 처리

---

## Epic D. 자기장 시스템

### D-1. DeathmatchZoneController 신규
- **신규 파일**: `Assets/01. Scripts/Network/Deathmatch/DeathmatchZoneController.cs`
- **책임**
  - 3분 타이머
  - 안전구역 반경 축소 함수 계산
  - 3초 틱으로 외부 플레이어 목숨 감소
- **데이터**
  - `SafeZoneCenter`, `SafeZoneRadius`, `MatchTimeRemaining`

### D-2. 자기장 시각화(최소)
- **신규/기존 파일**: `Assets/01. Scripts/UI/DeathmatchZoneWarningUI.cs` (신규 권장)
- **작업**
  - 자기장 밖 경고 텍스트/아이콘 표시

---

## Epic E. 승패/결과 판정

### E-1. DeathmatchMatchController 신규
- **신규 파일**: `Assets/01. Scripts/Network/Deathmatch/DeathmatchMatchController.cs`
- **책임**
  - 생존자 수 기반 종료 판정
  - 동시 사망 타이브레이커
    1) Kills desc
    2) LifeLostTotal asc
    3) 완전 동률=전원 패배
- **완료 기준**
  - 1명 생존 즉시 종료 / 0명 생존 시 규칙대로 승자 산출

### E-2. 결과 UI 반영
- **파일**: `Assets/01. Scripts/UI/GameResultController.cs` (또는 현재 결과 UI 스크립트)
- **작업**
  - 승자명/동률 패배 메시지 출력

---

## Epic F. HUD

### F-1. 데스매치 HUD 신규
- **신규 파일**: `Assets/01. Scripts/UI/UIDeathmatchHUD.cs`
- **표시 항목**
  - Lives(3칸), Kills, 타이머, 자기장 경고

### F-2. 씬 배선(MCP)
- **대상 씬**: `Assets/00. Scenes/GameScene.unity`
- **작업**
  - HUD 오브젝트 생성 및 컨트롤러 연결
  - Deathmatch 관련 컨트롤러 오브젝트 추가/레퍼런스 연결

---

## Epic G. 모드별 비활성 정책

### G-1. Classic 전용 기능 차단
- **파일 후보**
  - `Assets/01. Scripts/Network/StunGun.cs`
  - `Assets/01. Scripts/Network/Sabotage/SpectatorSabotageController.cs`
  - `Assets/01. Scripts/Events/GroupDanceMaskChanger.cs`
- **작업**
  - Deathmatch 모드에서는 술래/사보타지/가면 변경 루프 비활성

---

## 구현 순서(권장)

1. A-1, A-2 (모드 인입 보장)
2. B-1, B-2 (생명/탈락 기반)
3. C-1, C-2 (근접 공격)
4. D-1 (자기장 데미지)
5. E-1, E-2 (종료/결과)
6. F-1, F-2 (HUD)
7. G-1 (클래식 기능 분리 정리)

---

## 테스트 시나리오 티켓

### T-1. 2인 데스매치
- 플레이어 킬 시 즉시 승리/종료 확인

### T-2. 3인 + NPC 오타격
- NPC 타격 시 자해(목숨 감소) 확인

### T-3. 자기장 종료 테스트
- 3분 경과 후 안전구역 0, 동시 사망 타이브레이커 확인

### T-4. 완전 동률
- 킬/목숨손실 모두 동일하면 전원 패배 확인

