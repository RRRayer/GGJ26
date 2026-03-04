# Find Mask - Deathmatch Mode PRD

## 1) 개요

### 목적
- 기존 `Classic` 모드 외에 `Deathmatch` 모드를 추가한다.
- Deathmatch는 **모든 플레이어가 일반 플레이어 외형/역할**로 시작하며, 좌클릭으로 근접 공격을 수행한다.

### 성공 기준
- 룸 생성 시 `mode=deathmatch`로 시작 가능
- 생명(3목숨) 규칙, PvP/NPC 타격 패널티, 자기장(축소/도트 데미지), 승패 판정이 네트워크에서 일관되게 동작

---

## 2) 게임 규칙 (확정 요구사항)

1. 모든 플레이어는 시작 시 목숨 `3`개.
2. 근접 공격으로 **다른 플레이어 명중 시 대상 즉사(라운드 내 1목숨 소진 처리)**.
3. 근접 공격으로 **NPC 명중 시 공격자 목숨 -1**.
4. 목숨이 `0`이 되면 즉시 탈락(관전 전환).
5. 다른 플레이어를 탈락시키면 공격자 목숨을 `3`으로 즉시 복구.
6. 본인 제외 모든 플레이어가 탈락하면 승리/게임 종료.
7. 맵 자기장:
   - 자기장 밖: `3초마다 목숨 -1`
   - 제한시간 3분 동안 점진 축소
   - 3분 경과 시 안전구역 0(내부 구역 소멸)
8. 자기장 동시 사망 타이브레이커:
   - 1순위: 킬 수 높은 플레이어
   - 2순위: 누적 목숨 손실 적은 플레이어
   - 모두 동률: 모두 패배

---

## 3) 스코프

### In Scope
- 모드 선택에 `Deathmatch` 추가 (룸 생성/로비 표시/게임 진입)
- Deathmatch 전용 전투/생명/자기장/승패 시스템
- 결과 UI에 승자/동률 패배 노출

### Out of Scope (이번 단계)
- 신규 무기/애니메이션 대규모 리워크
- 데스매치 전용 맵 별도 제작
- 랭크/통계 서버 저장

---

## 4) 플레이 플로우

1. 방 생성에서 `Mode = Deathmatch` 선택
2. GameScene 진입 시 Deathmatch 규칙 활성화
3. 각 플레이어 초기화:
   - `lives=3`, `kills=0`, `lifeLost=0`, `isEliminated=false`
4. 전투/자기장 진행 중 상태 갱신
5. 종료 조건 충족 시 결과 화면

---

## 5) 시스템 설계

## 5.1 모드 라우팅
- 룸 CustomProperties `mode`에 `deathmatch` 추가
- 게임 시작 시 `GameModeService`(신규/기존 확장)에서 모드별 시스템 on/off
  - Classic 전용 로직(술래/일반/사보타지)은 Deathmatch에서 비활성

## 5.2 Deathmatch 상태 모델 (Networked)
- `DeathmatchPlayerState` (플레이어별)
  - `Lives` (0~3)
  - `Kills`
  - `LifeLostTotal` (타이브레이커용)
  - `IsEliminated`
- `DeathmatchMatchState` (매치 공통)
  - `MatchTimeRemaining` (초)
  - `SafeZoneRadius`
  - `SafeZoneCenter`
  - `IsFinished`
  - `WinnerPlayerRef` 또는 `IsDrawAllLose`

## 5.3 근접 공격
- 입력: 좌클릭 (InputAuthority)
- 판정: StateAuthority에서 최종 확정
  - 공격 원점/방향(카메라 또는 캐릭터 forward) 기반 `SphereCast` 또는 Overlap capsule
  - 우선순위: 플레이어 > NPC (동시 히트 시 플레이어 우선)
- 처리:
  - 플레이어 히트: 대상 `Lives=0` 처리(즉시 탈락), 공격자 `Kills+1`, 공격자 `Lives=3`
  - NPC 히트: 공격자 `Lives-1`

## 5.4 목숨/탈락 처리
- `ApplyLifeDelta(player, delta, reason)` 공통 함수로 단일 진입점 유지
- `Lives<=0` 시:
  - `IsEliminated=true`
  - 기존 관전자 카메라/이동 로직 재사용
  - 승패 체크 트리거

## 5.5 자기장
- 중심점: 맵 중심(초기 고정)
- 반경 함수:
  - `R(t) = R0 * (1 - t/180)` for `0<=t<=180`
  - `t>=180`이면 `R=0`
- 도트 데미지:
  - StateAuthority에서 3초 틱 타이머 관리
  - 자기장 밖 플레이어에게 `Lives-1` 적용

## 5.6 동시 사망 / 승자 결정
- 생존자 0명일 때(자기장 동시사 포함) 순위 계산:
  1) `Kills` 최대
  2) `LifeLostTotal` 최소
  3) 완전 동률이면 `DrawAllLose=true`
- 생존자 1명일 때 해당 플레이어 즉시 승리

---

## 6) 네트워크/권한 원칙 (Fusion)

- 입력 수집: InputAuthority
- 판정/상태 변경: StateAuthority only
- UI 표시: 각 클라이언트 Render 단계에서 Networked 상태 반영
- RPC는 요청용 최소화, 실제 결과는 Networked 변수로 동기화

---

## 7) UI/UX 요구사항

- HUD(Deathmatch 전용):
  - 현재 목숨(3칸 아이콘)
  - 킬 수
  - 자기장까지 거리/경고(밖에 있을 때 경고 표시)
  - 남은 시간(3:00 카운트다운)
- 결과창:
  - 승자 이름
  - 동률 전원 패배 시 명시 문구

---

## 8) 밸런스 기본값(초기)

- 목숨: 3
- 자기장 틱: 3초
- 근접 사거리: 2.0~2.5m
- 판정 반경: 0.5m
- 공격 쿨다운: 0.6s (연타 방지)

---

## 9) 예외/에러 처리

- 모드 값 누락/오염 시 `classic` 폴백 + Warning 로그
- 공격 대상 불명확(동시 다중 히트) 시 최근접 + 우선순위 규칙 적용
- 종료 직전 중복 판정 방지(`IsFinished` 가드)

---

## 10) 구현 작업 분해

1. 룸 모드 enum/프로퍼티 확장 (`deathmatch`)
2. Deathmatch 상태 컴포넌트 추가 (`DeathmatchPlayerState`, `DeathmatchMatchController`)
3. 근접 공격 컴포넌트 추가 (`DeathmatchMeleeAttack`)
4. 자기장 컨트롤러 추가 (`DeathmatchZoneController`)
5. 탈락/관전 연동 (기존 `PlayerElimination` 재사용)
6. 승패/타이브레이커 결과 처리
7. HUD/결과 UI 연결
8. 멀티 테스트(2인/3인/4인)

---

## 11) QA 체크리스트

- [ ] 방 생성에서 Deathmatch 선택 후 정상 시작
- [ ] 모든 플레이어 시작 목숨 3 확인
- [ ] 플레이어 명중 시 대상 즉시 탈락 + 공격자 목숨 3 복구
- [ ] NPC 명중 시 공격자 목숨 감소
- [ ] 목숨 0 시 즉시 탈락/관전 전환
- [ ] 자기장 밖 3초 틱 데미지 정상 적용
- [ ] 3분 후 안전구역 0 처리 확인
- [ ] 동시 사망 시 킬 수/목숨 손실 우선순위대로 승자 계산
- [ ] 완전 동률 시 전원 패배 처리

