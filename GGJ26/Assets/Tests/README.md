# Test Coverage

Lobby scene tests for the GGJ26 project. Tests validate both the **serialized scene structure** (EditMode) and **runtime UI behavior** (PlayMode).

## Overview

| Suite | File | Mode | Tests | Cases |
|-------|------|------|------:|------:|
| LobbySceneEditModeTests | `EditMode/LobbySceneEditModeTests.cs` | EditMode | 25 methods | 43 cases |
| LobbyFlowPlayModeTests | `PlayMode/LobbyFlowPlayModeTests.cs` | PlayMode | 16 methods | 16 cases |
| **Total** | | | **41 methods** | **59 cases** |

> "Cases" counts parameterized `[TestCase]` expansions. For example, one `[Test]` with 3 `[TestCase]` attributes counts as 1 method / 3 cases.

## EditMode Tests — LobbySceneEditModeTests

Opens the Lobby scene via `EditorSceneManager.OpenScene()` in `[OneTimeSetUp]`. No play mode, no Awake/OnEnable — validates **serialized scene state only**.

### Core Structure (6 cases)

| Test | What it verifies |
|------|-----------------|
| `MainCamera_ExistsInScene` | Main Camera GameObject exists |
| `MainCamera_HasCameraComponent` | Camera component attached |
| `DirectionalLight_ExistsInScene` | Directional Light GameObject exists |
| `DirectionalLight_HasLightComponent` | Light component attached |
| `EventSystem_ExistsInScene` | EventSystem with EventSystem component |
| `CanvasLobby_HasCanvasComponent` | CanvasLobby with Canvas component |

### Key Scripts (5 cases)

| Test | What it verifies |
|------|-----------------|
| `LobbyMatchmakingUI_ExistsInScene` | LobbyMatchmakingUI component on its GameObject |
| `FusionLauncher_ExistsInScene` | FusionLauncher component |
| `FusionSessionFlow_ExistsInScene` | FusionSessionFlow component |
| `AudioManager_ExistsInScene` | AudioManager component |
| `LobbyAudioController_ExistsInScene` | LobbyAudioController component |

### Main Menu Buttons (12 cases)

Each test is parameterized with `[TestCase]` for **BtnHost**, **BtnPublic**, **BtnExit** (3 buttons x 4 tests = 12 cases).

| Test | What it verifies |
|------|-----------------|
| `MainMenuButton_ExistsWithButtonComponent` | Button component attached |
| `MainMenuButton_IsChildOfBtnContainer` | Parent is BtnContainer |
| `MainMenuButton_HasUIGenericButton` | UIGenericButton component attached |
| `MainMenuButton_HasPersistentOnClickBinding` | At least one persistent onClick listener |

### BtnContainer (1 case)

| Test | What it verifies |
|------|-----------------|
| `BtnContainer_ExistsUnderCanvasLobby` | BtnContainer is a direct child of CanvasLobby |

### RoomPanel Structure (12 cases)

| Test | What it verifies |
|------|-----------------|
| `RoomPanel_ExistsUnderCanvasLobby` | RoomPanel is a child of CanvasLobby |
| `RoomPanel_StartsInactive` | RoomPanel.activeSelf == false |
| `RoomPanel_HasRequiredChild` | Parameterized x10: RoomNameInput, RoomPasswordInput, CreateRoomButton, MaxPlayersPrevButton, MaxPlayersNextButton, MaxPlayersValueText, ModeToggleButton, PrivateRoomToggle, RoomCloseButton, RoomTitle |

### Other Panels (2 cases)

| Test | What it verifies |
|------|-----------------|
| `PublicRoomPanel_ExistsAndStartsInactive` | PublicRoomPanel exists, activeSelf == false |
| `SkinSelectPanel_ExistsInScene` | SkinSelectPanel exists |

### Camera System (2 cases)

| Test | What it verifies |
|------|-----------------|
| `VirtualCamera_ExistsInScene` | Virtual Camera exists |
| `DollyTrack_ExistsInScene` | Dolly Track exists |

### Build Settings (1 case)

| Test | What it verifies |
|------|-----------------|
| `LobbyScene_IsInBuildSettingsAndEnabled` | Lobby scene is in EditorBuildSettings and enabled |

### JoinRoomButton (2 cases)

| Test | What it verifies |
|------|-----------------|
| `JoinRoomButton_ExistsInRoomPanel` | JoinRoomButton is a child of RoomPanel |
| `JoinRoomButton_StartsInactive` | JoinRoomButton.activeSelf == false (create mode default) |

---

## PlayMode Tests — LobbyFlowPlayModeTests

Loads the Lobby scene via `SceneManager.LoadScene()` in `[UnitySetUp]`. Awake/OnEnable runs, `BuildUiIfNeeded()` creates dynamic UI elements. Scene is **reloaded per test** for isolation. No networking is triggered.

### Initial State (5 tests)

| Test | What it verifies |
|------|-----------------|
| `InitialState_MainMenuButtonsAreVisible` | BtnHost, BtnPublic, BtnExit are all active in hierarchy |
| `InitialState_RoomPanelIsHidden` | RoomPanel is hidden on startup |
| `InitialState_PublicRoomPanelIsHidden` | PublicRoomPanel is hidden on startup |
| `InitialState_DynamicMatchmakingPopupCreatedAndHidden` | MatchmakingPopup (dynamic) is created but hidden |
| `InitialState_SkinSelectPanelIsVisible` | SkinSelectPanel is visible on startup |

### BtnHost Flow (3 tests)

| Test | What it verifies |
|------|-----------------|
| `BtnHost_Click_ShowsRoomPanel` | Clicking BtnHost opens RoomPanel |
| `BtnHost_Click_RoomPanelInCreateMode` | CreateRoomButton visible, JoinRoomButton hidden |
| `BtnHost_Click_MaxPlayersControlsVisible` | MaxPlayersPrevButton, MaxPlayersNextButton, MaxPlayersValueText visible |

### Room Close (1 test)

| Test | What it verifies |
|------|-----------------|
| `RoomCloseButton_Click_HidesRoomPanel` | RoomCloseButton hides RoomPanel |

### Max Players (2 tests)

| Test | What it verifies |
|------|-----------------|
| `MaxPlayersNext_Click_IncreasesDisplayedValue` | Next button does not decrease displayed value |
| `MaxPlayersPrev_Click_DecreasesDisplayedValue` | Prev button does not increase displayed value |

### Mode Toggle (1 test)

| Test | What it verifies |
|------|-----------------|
| `ModeToggleButton_Click_ChangesModeText` | ModeValueText changes after toggle click |

### Private Room Toggle (1 test)

| Test | What it verifies |
|------|-----------------|
| `PrivateRoomToggle_ControlsPasswordInputVisibility` | Toggle ON shows RoomPasswordInput, Toggle OFF hides it |

### Public Room (1 test)

| Test | What it verifies |
|------|-----------------|
| `BtnPublic_Click_ShowsPublicRoomPanel` | Clicking BtnPublic opens PublicRoomPanel |

### Validation (1 test)

| Test | What it verifies |
|------|-----------------|
| `CreateRoom_EmptyName_ShowsPopup` | Empty room name + CreateRoom shows MatchmakingPopup |

### Panel Exclusion (1 test)

| Test | What it verifies |
|------|-----------------|
| `OpenPublicRoomPanel_HidesRoomPanel` | Opening PublicRoomPanel hides RoomPanel |

---

## Running Tests

### Unity Editor

**Test Runner**: Window > General > Test Runner

- **EditMode** tab: runs without entering play mode
- **PlayMode** tab: enters play mode per test

### CLI (Unity Batch Mode)

```bash
# EditMode
unity -batchmode -nographics -runTests -testPlatform EditMode -testResults results-editmode.xml

# PlayMode
unity -batchmode -nographics -runTests -testPlatform PlayMode -testResults results-playmode.xml
```

### CI

Tests run automatically via `.github/workflows/unity-tests.yml` on push/PR to `main`.

---

## Assembly Definitions

| Asmdef | References |
|--------|-----------|
| `GGJ26.Tests.EditMode` | UnityEngine.TestRunner, UnityEditor.TestRunner, UnityEngine.UI, Unity.TextMeshPro |
| `GGJ26.Tests.PlayMode` | UnityEngine.TestRunner, UnityEngine.UI, Unity.TextMeshPro, Unity.InputSystem |

## Design Notes

- **EditMode** tests use `Resources.FindObjectsOfTypeAll<GameObject>()` to find objects (including inactive) without triggering Awake.
- **PlayMode** tests use a `FindIncludingInactive()` helper that walks scene roots recursively, since `GameObject.Find()` skips inactive objects.
- **UIGenericButton** is referenced by type name string (`GetType().Name`) to avoid adding a production asmdef dependency to the test assemblies.
- **No networking** is triggered in any test. Validation tests return before `StartMatchmaking` is called because `FusionLauncher` is not connected.
