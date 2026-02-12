# 🎨 리소스 정리 (Art & Sound Assets)

> 게임 기획서 내용을 바탕으로 필요한 아트 및 사운드 리소스를 정리합니다.
> 최소한의 리소스로 핵심 재미를 구현하는 것을 목표로 합니다.

---
## 1. 3D 모델 및 아트 에셋 (3D Models & Art Assets)

### A. 캐릭터
- **기본 캐릭터 모델**: 마네킹 스타일 (플레이어, AI 공용). `StarterAsset`
- **가면**: Primitive Cube 형태. 색상별 (빨강, 초록, 파랑) 재질/머티리얼. `Assets\03. Prefabs\Mask\`

### B. 환경
- **맵**: `ProBuilder` 또는 기본 Cube 기반의 어두운 광장 `Assets\03. Prefabs\World\Map`
- **미러볼**: 단체 댄스 이벤트 시 공중에 나타나는 미러볼 모델. `Assets\05. Arts\Download\Tazer\World\`
- **천막**: 단체 댄스 이벤트 시 내려오는 천막 모델 또는 연출용 애셋. `Assets\05. Arts\Download\Tazer\World\`

### C. 기타
- **스턴건**: 술래 플레이어가 사용하는 무기 모델. `Assets\05. Arts\Download\Tazer\Prefabs\`

### D. UI 에셋
- **폰트**: 게임 전반에 사용될 폰트. `Pretendard`
- **키보드 키 이미지**: 조작 키 안내에 사용될 수 있는 키보드 아이콘. `Assets\05. Arts\UI\Keyboard＼`
- **댄스 액션 아이콘**: 1, 2, 3, 4번 댄스를 보여주는 사람 모양 아이콘. `Assets\05. Arts\UI\DancingIcon`

---
## 2. 애니메이션 (Animations)

### A. 기본 애니메이션 (Existing Base Animations)
- Idle (가만히 서 있기) `(기존 에셋 활용 예정)`
- Walk (걷기) `(기존 에셋 활용 예정)`
- Run (달리기) `(기존 에셋 활용 예정)`
- Jump (점프: JumpStart, InAir, Landing 포함) `(기존 에셋 활용 예정)`

### B. 파생 동작 (Derived/Combined Actions)
- **AI 행동 패턴**: `Idle`, `Walk`, `Run`, `Jump` 조합으로 구현.
- **가면/단체 댄스**: `Idle`, `Walk`, `Run`, `Jump` 조합을 통해 **4가지 댄스 동작**을 구현합니다. (예: 제자리에서 점프하며 손 흔들기, 짧게 걷다 멈춰서 박수 치기 등)
### D. 특수 액션
- **술래 스턴건 발사**: 스턴건을 발사하는 애니메이션.
- **일반 플레이어 피격**: 스턴건에 맞아 쓰러지는 애니메이션.
- **술래 페널티**: 고개 숙여 주변을 보지 못하는 애니메이션.
- **AI 피격**: 스턴건 피격 애니메이션 (기존 `Idle` 또는 `Walk` 애니메이션을 변형하여 구현).

---
## 3. VFX (Visual Effects)

- **가면 변경 효과**: 플레이어 가면 변경 시 발생하는 빛/플래시 효과.
- **스턴건 효과**: 발사 이펙트 (총구 섬광), 피격 이펙트. `피격 이펙트`
- **단체 댄스 연출**: 조명 반짝임, UI 플래시 등. `끝끝`

---
## 4. 오디오 (Audio Assets)

### A. BGM (Background Music) - `Assets\04. Audios\Clips\BGM`
- **평상시 BGM**: 일반 게임 플레이 시 배경 음악
- **단체 댄스 BGM**: 단체 댄스 이벤트 시 재생되는 음악
> **(개발자 참고)**: 이 BGM은 매번 처음부터 재생되는 것이 아니라, 이전 재생 위치를 기억했다가 다음 단체 댄스 때 이어서 재생되어야 합니다.

### B. SFX (Sound Effects) - `Assets\04. Audios\Clips\SFX`
- **스턴건**: 발사음, 명중음
- **가면 변경**: 가면 교체 시 효과음
- **AI 행동**: 만세 소리, 발자국 등
- **승패**: 승리/패배 사운드
