# Architecture Reference

## Scene Flow

Lobby → WaitingRoom (matchmaking/ready-up) → GameScene (gameplay)

Scene loading is managed by `FusionSessionFlow`, which creates/destroys the `NetworkRunner` and handles async scene transitions.

## Core Systems

### Networking (Fusion 2 Shared Mode)
- `FusionLauncher` — Entry point, singleton, implements `INetworkRunnerCallbacks`
- `FusionSessionFlow` — Session lifecycle and scene loading
- `FusionSpawnService` — Spawns players at grid positions and 27 NPCs (9 per color)
- `FusionRoleAssignmentService` — Deterministic seeker selection (lowest PlayerRef)
- `PlayerRole` — Mask color assignment (seeded Fisher-Yates shuffle)
- Authority pattern: state authority for game logic RPCs, input authority for per-player input

### Event System (ScriptableObject Channels)
All inter-system communication uses SO event channels (`VoidEventChannelSO`, `IntEventChannelSO`, `AudioCueEventChannelSO`, etc.). Systems subscribe in `OnEnable()` and unsubscribe in `OnDisable()`. Events fire via `RaiseEvent()`. Channel assets live in `Assets/02. ScriptableObjects/Events/`.

### Game State
Enum-driven (`None`, `Gameplay`, `Pause`, `Menu`, `CutScene`, `Ending`). `GameStateController` manages transitions, input toggling, and `Time.timeScale`.

### Dance System
`DanceEventPublisher` publishes mask-specific dances (8-12s interval, 3s duration) and group dances (30s interval, 10s duration). Dance index is announced before execution for synchronized animation across clients.

### Player System
- `PlayerStateManager` — Tracks all player roles/deaths via networked RPCs; has `PlayerStateFallback` for offline testing
- `PlayerElimination` — Handles death, spectator camera spawning
- `StunGun` — Seeker weapon (5s cooldown, 6m raycast)

### NPC System
`BaseNPC` (abstract) → `RedNPC`/`BlueNPC`/`GreenNPC` with NavMeshAgent pathfinding. `Local/` variants exist for testing without network. `NPCController` syncs position/animation over network.

### Audio
`AudioManager` singleton with object-pooled `SoundEmitter` instances. Playback requested through `AudioCueEventChannelSO` events, not direct calls.

### Save/Load
`SaveLoadSystem` SO + `FileManager` (JSON to disk). Auto-saves every 30s and on app pause.

## Key Patterns
- **SO event channels** for decoupled communication (never call systems directly)
- **Object pooling** via `PoolSO<T>` / `IFactory<T>` (used for audio emitters)
- **NetworkBehaviour** lifecycle: `Spawned()`, `FixedUpdateNetwork()`, `Render()`
- **Singleton** with destroy-duplicate guard
- **Deterministic** logic for cross-client consistency (seeded RNG for role/mask assignment)

## Directory Layout

```
Assets/
├── 00. Scenes/          # Lobby, WaitingRoom, GameScene
├── 01. Scripts/         # Runtime C# organized by feature
│   ├── Network/         # Fusion networking layer
│   ├── MaskNPC/         # NPC behaviors (+ Local/ offline variants)
│   ├── Game/            # PlayerState, StatsManager
│   ├── Events/          # Event channel SO definitions
│   ├── Audios/          # Audio management
│   ├── UI/              # Game/lobby UI controllers
│   └── ...              # Player, Input, Settings, SaveLoad, Pool, etc.
├── 02. ScriptableObjects/  # Configuration assets (events, audio, settings)
├── 03. Prefabs/         # Reusable GameObjects
├── 04. Audios/          # Audio clips (Music/, SFX/)
└── 05. Arts/            # Visual assets, animations, models
```

## Key Packages

- `com.unity.inputsystem` (1.17.0) — New Input System
- `com.unity.cinemachine` (2.10.5) — Camera
- `com.unity.ai.navigation` (2.0.9) — NavMesh
- `com.unity.render-pipelines.universal` (17.3.0) — URP
- `com.unity.visualeffectgraph` (17.0.4) — VFX
