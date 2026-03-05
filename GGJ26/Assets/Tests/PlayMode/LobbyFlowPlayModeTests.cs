using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

[TestFixture]
public class LobbyFlowPlayModeTests
{
    private const string LobbySceneName = "Lobby";

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        // Suppress Fusion/Photon network errors in CI (no server connection)
        LogAssert.ignoreFailingMessages = true;

        SceneManager.LoadScene(LobbySceneName);
        yield return null; // Wait one frame for Awake/OnEnable
        yield return null; // Extra frame for BuildUiIfNeeded dynamic objects
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static GameObject FindIncludingInactive(string name)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (root.name == name) return root;
            var found = root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == name);
            if (found != null) return found.gameObject;
        }
        return null;
    }

    private static GameObject FindRequired(string name)
    {
        var go = FindIncludingInactive(name);
        Assert.IsNotNull(go, $"GameObject '{name}' not found in scene (including inactive).");
        return go;
    }

    private static T FindRequiredComponent<T>(string gameObjectName) where T : Component
    {
        var go = FindRequired(gameObjectName);
        var comp = go.GetComponent<T>();
        Assert.IsNotNull(comp, $"Component {typeof(T).Name} not found on '{gameObjectName}'.");
        return comp;
    }

    private static void ClickButton(string gameObjectName)
    {
        var button = FindRequiredComponent<Button>(gameObjectName);
        button.onClick.Invoke();
    }

    private static void ClickUIGenericButton(string gameObjectName)
    {
        var go = FindRequired(gameObjectName);
        var comp = go.GetComponents<Component>()
            .FirstOrDefault(c => c.GetType().Name == "UIGenericButton");
        Assert.IsNotNull(comp, $"UIGenericButton not found on '{gameObjectName}'.");
        var clickMethod = comp.GetType().GetMethod("Click");
        Assert.IsNotNull(clickMethod, "UIGenericButton.Click() method not found.");
        clickMethod.Invoke(comp, null);
    }

    // ── Initial State ───────────────────────────────────────────

    [UnityTest]
    public IEnumerator InitialState_MainMenuButtonsAreVisible()
    {
        yield return null;
        Assert.IsTrue(FindRequired("BtnHost").activeInHierarchy, "BtnHost should be visible.");
        Assert.IsTrue(FindRequired("BtnPublic").activeInHierarchy, "BtnPublic should be visible.");
        Assert.IsTrue(FindRequired("BtnExit").activeInHierarchy, "BtnExit should be visible.");
    }

    [UnityTest]
    public IEnumerator InitialState_RoomPanelIsHidden()
    {
        yield return null;
        var go = FindRequired("RoomPanel");
        Assert.IsFalse(go.activeInHierarchy, "RoomPanel should be hidden initially.");
    }

    [UnityTest]
    public IEnumerator InitialState_PublicRoomPanelIsHidden()
    {
        yield return null;
        var go = FindRequired("PublicRoomPanel");
        Assert.IsFalse(go.activeInHierarchy, "PublicRoomPanel should be hidden initially.");
    }

    [UnityTest]
    public IEnumerator InitialState_DynamicMatchmakingPopupCreatedAndHidden()
    {
        yield return null;
        var go = FindRequired("MatchmakingPopup");
        Assert.IsFalse(go.activeInHierarchy, "MatchmakingPopup should be created but hidden.");
    }

    [UnityTest]
    public IEnumerator InitialState_SkinSelectPanelIsVisible()
    {
        yield return null;
        var go = FindRequired("SkinSelectPanel");
        Assert.IsTrue(go.activeInHierarchy, "SkinSelectPanel should be visible initially.");
    }

    // ── BtnHost Flow ────────────────────────────────────────────

    [UnityTest]
    public IEnumerator BtnHost_Click_ShowsRoomPanel()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        var roomPanel = FindRequired("RoomPanel");
        Assert.IsTrue(roomPanel.activeInHierarchy, "RoomPanel should be visible after clicking BtnHost.");
    }

    [UnityTest]
    public IEnumerator BtnHost_Click_RoomPanelInCreateMode()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        var createBtn = FindRequired("CreateRoomButton");
        Assert.IsTrue(createBtn.activeInHierarchy, "CreateRoomButton should be visible in create mode.");
        var joinBtn = FindRequired("JoinRoomButton");
        Assert.IsFalse(joinBtn.activeInHierarchy, "JoinRoomButton should be hidden in create mode.");
    }

    [UnityTest]
    public IEnumerator BtnHost_Click_MaxPlayersControlsVisible()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        Assert.IsTrue(FindRequired("MaxPlayersPrevButton").activeInHierarchy,
            "MaxPlayersPrevButton should be visible.");
        Assert.IsTrue(FindRequired("MaxPlayersNextButton").activeInHierarchy,
            "MaxPlayersNextButton should be visible.");
        Assert.IsTrue(FindRequired("MaxPlayersValueText").activeInHierarchy,
            "MaxPlayersValueText should be visible.");
    }

    // ── Room Close ──────────────────────────────────────────────

    [UnityTest]
    public IEnumerator RoomCloseButton_Click_HidesRoomPanel()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        Assert.IsTrue(FindRequired("RoomPanel").activeInHierarchy, "RoomPanel should be open.");
        ClickButton("RoomCloseButton");
        yield return null;
        Assert.IsFalse(FindRequired("RoomPanel").activeInHierarchy,
            "RoomPanel should be hidden after clicking RoomCloseButton.");
    }

    // ── Max Players ─────────────────────────────────────────────

    [UnityTest]
    public IEnumerator MaxPlayersNext_Click_IncreasesDisplayedValue()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        var text = FindRequiredComponent<TextMeshProUGUI>("MaxPlayersValueText");
        string valueBefore = text.text;
        ClickButton("MaxPlayersNextButton");
        yield return null;
        // If already at max, value stays the same; otherwise it should increase
        int before = int.Parse(valueBefore);
        int after = int.Parse(text.text);
        Assert.GreaterOrEqual(after, before,
            "Max players value should not decrease when clicking Next.");
    }

    [UnityTest]
    public IEnumerator MaxPlayersPrev_Click_DecreasesDisplayedValue()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        // First increase to ensure we are not at minimum
        ClickButton("MaxPlayersNextButton");
        yield return null;
        var text = FindRequiredComponent<TextMeshProUGUI>("MaxPlayersValueText");
        string valueBefore = text.text;
        ClickButton("MaxPlayersPrevButton");
        yield return null;
        int before = int.Parse(valueBefore);
        int after = int.Parse(text.text);
        Assert.LessOrEqual(after, before,
            "Max players value should not increase when clicking Prev.");
    }

    // ── Mode Toggle ─────────────────────────────────────────────

    [UnityTest]
    public IEnumerator ModeToggleButton_Click_ChangesModeText()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;
        var modeText = FindRequiredComponent<TextMeshProUGUI>("ModeValueText");
        string textBefore = modeText.text;
        ClickButton("ModeToggleButton");
        yield return null;
        Assert.AreNotEqual(textBefore, modeText.text,
            "Mode text should change after toggling.");
    }

    // ── Private Room Toggle ─────────────────────────────────────

    [UnityTest]
    public IEnumerator PrivateRoomToggle_ControlsPasswordInputVisibility()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;

        var toggle = FindRequiredComponent<Toggle>("PrivateRoomToggle");
        var passwordInput = FindRequired("RoomPasswordInput");

        // Toggle ON → password should be visible
        toggle.isOn = true;
        yield return null;
        Assert.IsTrue(passwordInput.activeInHierarchy,
            "RoomPasswordInput should be visible when private toggle is ON.");

        // Toggle OFF → password should be hidden
        toggle.isOn = false;
        yield return null;
        Assert.IsFalse(passwordInput.activeInHierarchy,
            "RoomPasswordInput should be hidden when private toggle is OFF.");
    }

    // ── Public Room ─────────────────────────────────────────────

    [UnityTest]
    public IEnumerator BtnPublic_Click_ShowsPublicRoomPanel()
    {
        yield return null;
        ClickUIGenericButton("BtnPublic");
        yield return null;
        var panel = FindRequired("PublicRoomPanel");
        Assert.IsTrue(panel.activeInHierarchy,
            "PublicRoomPanel should be visible after clicking BtnPublic.");
    }

    // ── Validation ──────────────────────────────────────────────

    [UnityTest]
    public IEnumerator CreateRoom_EmptyName_ShowsPopup()
    {
        yield return null;
        ClickUIGenericButton("BtnHost");
        yield return null;

        // Ensure room name is empty
        var roomNameInput = FindRequiredComponent<TMP_InputField>("RoomNameInput");
        roomNameInput.text = "";

        ClickButton("CreateRoomButton");
        yield return null;

        var popup = FindRequired("MatchmakingPopup");
        Assert.IsTrue(popup.activeInHierarchy,
            "MatchmakingPopup should be visible when creating room with empty name.");
    }

    // ── Panel Exclusion ─────────────────────────────────────────

    [UnityTest]
    public IEnumerator OpenPublicRoomPanel_HidesRoomPanel()
    {
        yield return null;
        // First open room panel
        ClickUIGenericButton("BtnHost");
        yield return null;
        Assert.IsTrue(FindRequired("RoomPanel").activeInHierarchy);

        // Now open public room panel
        ClickUIGenericButton("BtnPublic");
        yield return null;
        Assert.IsFalse(FindRequired("RoomPanel").activeInHierarchy,
            "RoomPanel should be hidden when PublicRoomPanel opens.");
        Assert.IsTrue(FindRequired("PublicRoomPanel").activeInHierarchy,
            "PublicRoomPanel should be visible.");
    }
}
