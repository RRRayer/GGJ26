using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[TestFixture]
public class LobbySceneEditModeTests
{
    private const string LobbyScenePath = "Assets/00. Scenes/Lobby.unity";

    [OneTimeSetUp]
    public void OpenLobbyScene()
    {
        EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private static GameObject FindRequired(string name)
    {
        var all = Resources.FindObjectsOfTypeAll<GameObject>();
        var go = all.FirstOrDefault(g => g.name == name && !EditorUtility.IsPersistent(g));
        Assert.IsNotNull(go, $"GameObject '{name}' not found in Lobby scene.");
        return go;
    }

    private static T FindRequiredComponent<T>(string gameObjectName) where T : Component
    {
        var go = FindRequired(gameObjectName);
        var comp = go.GetComponent<T>();
        Assert.IsNotNull(comp, $"Component {typeof(T).Name} not found on '{gameObjectName}'.");
        return comp;
    }

    // ── Core Structure ──────────────────────────────────────────

    [Test]
    public void MainCamera_ExistsInScene()
    {
        FindRequired("Main Camera");
    }

    [Test]
    public void MainCamera_HasCameraComponent()
    {
        FindRequiredComponent<Camera>("Main Camera");
    }

    [Test]
    public void DirectionalLight_ExistsInScene()
    {
        FindRequired("Directional Light");
    }

    [Test]
    public void DirectionalLight_HasLightComponent()
    {
        FindRequiredComponent<Light>("Directional Light");
    }

    [Test]
    public void EventSystem_ExistsInScene()
    {
        FindRequiredComponent<EventSystem>("EventSystem");
    }

    [Test]
    public void CanvasLobby_HasCanvasComponent()
    {
        FindRequiredComponent<Canvas>("CanvasLobby");
    }

    // ── Key Scripts ─────────────────────────────────────────────

    [Test]
    public void LobbyMatchmakingUI_ExistsInScene()
    {
        var go = FindRequired("LobbyMatchmakingUI");
        var comp = go.GetComponent("LobbyMatchmakingUI");
        Assert.IsNotNull(comp, "LobbyMatchmakingUI component not found.");
    }

    [Test]
    public void FusionLauncher_ExistsInScene()
    {
        var go = FindRequired("FusionLauncher");
        var comp = go.GetComponent("FusionLauncher");
        Assert.IsNotNull(comp, "FusionLauncher component not found.");
    }

    [Test]
    public void FusionSessionFlow_ExistsInScene()
    {
        var go = FindRequired("FusionSessionFlow");
        var comp = go.GetComponent("FusionSessionFlow");
        Assert.IsNotNull(comp, "FusionSessionFlow component not found.");
    }

    [Test]
    public void AudioManager_ExistsInScene()
    {
        var go = FindRequired("AudioManager");
        var comp = go.GetComponent("AudioManager");
        Assert.IsNotNull(comp, "AudioManager component not found.");
    }

    [Test]
    public void LobbyAudioController_ExistsInScene()
    {
        var go = FindRequired("LobbyAudioController");
        var comp = go.GetComponent("LobbyAudioController");
        Assert.IsNotNull(comp, "LobbyAudioController component not found.");
    }

    // ── Main Menu Buttons ───────────────────────────────────────

    [Test]
    [TestCase("BtnHost")]
    [TestCase("BtnPublic")]
    [TestCase("BtnExit")]
    public void MainMenuButton_ExistsWithButtonComponent(string buttonName)
    {
        FindRequiredComponent<Button>(buttonName);
    }

    [Test]
    [TestCase("BtnHost")]
    [TestCase("BtnPublic")]
    [TestCase("BtnExit")]
    public void MainMenuButton_IsChildOfBtnContainer(string buttonName)
    {
        var go = FindRequired(buttonName);
        Assert.IsNotNull(go.transform.parent, $"{buttonName} has no parent.");
        Assert.AreEqual("BtnContainer", go.transform.parent.name,
            $"{buttonName} should be a child of BtnContainer.");
    }

    [Test]
    [TestCase("BtnHost")]
    [TestCase("BtnPublic")]
    [TestCase("BtnExit")]
    public void MainMenuButton_HasUIGenericButton(string buttonName)
    {
        var go = FindRequired(buttonName);
        var comp = go.GetComponents<Component>()
            .FirstOrDefault(c => c.GetType().Name == "UIGenericButton");
        Assert.IsNotNull(comp, $"{buttonName} should have a UIGenericButton component.");
    }

    [Test]
    [TestCase("BtnHost")]
    [TestCase("BtnPublic")]
    [TestCase("BtnExit")]
    public void MainMenuButton_HasPersistentOnClickBinding(string buttonName)
    {
        var button = FindRequiredComponent<Button>(buttonName);
        Assert.Greater(button.onClick.GetPersistentEventCount(), 0,
            $"{buttonName} should have at least one persistent onClick binding.");
    }

    // ── BtnContainer ────────────────────────────────────────────

    [Test]
    public void BtnContainer_ExistsUnderCanvasLobby()
    {
        var canvas = FindRequired("CanvasLobby");
        var btnContainer = canvas.transform.Cast<Transform>()
            .FirstOrDefault(t => t.name == "BtnContainer");
        Assert.IsNotNull(btnContainer,
            "BtnContainer should exist as a direct child of CanvasLobby.");
    }

    // ── RoomPanel Structure ─────────────────────────────────────

    [Test]
    public void RoomPanel_ExistsUnderCanvasLobby()
    {
        var go = FindRequired("RoomPanel");
        Assert.IsNotNull(go.transform.parent, "RoomPanel has no parent.");
        Assert.AreEqual("CanvasLobby", go.transform.parent.name,
            "RoomPanel should be a child of CanvasLobby.");
    }

    [Test]
    public void RoomPanel_StartsInactive()
    {
        var go = FindRequired("RoomPanel");
        Assert.IsFalse(go.activeSelf, "RoomPanel should start inactive.");
    }

    [Test]
    [TestCase("RoomNameInput")]
    [TestCase("RoomPasswordInput")]
    [TestCase("CreateRoomButton")]
    [TestCase("MaxPlayersPrevButton")]
    [TestCase("MaxPlayersNextButton")]
    [TestCase("MaxPlayersValueText")]
    [TestCase("ModeToggleButton")]
    [TestCase("PrivateRoomToggle")]
    [TestCase("RoomCloseButton")]
    [TestCase("RoomTitle")]
    public void RoomPanel_HasRequiredChild(string childName)
    {
        var roomPanel = FindRequired("RoomPanel");
        var child = roomPanel.transform.Cast<Transform>()
            .FirstOrDefault(t => t.name == childName);
        Assert.IsNotNull(child, $"RoomPanel should have a child named '{childName}'.");
    }

    // ── Other Panels ────────────────────────────────────────────

    [Test]
    public void PublicRoomPanel_ExistsAndStartsInactive()
    {
        var go = FindRequired("PublicRoomPanel");
        Assert.IsFalse(go.activeSelf, "PublicRoomPanel should start inactive.");
    }

    [Test]
    public void SkinSelectPanel_ExistsInScene()
    {
        FindRequired("SkinSelectPanel");
    }

    // ── Camera System ───────────────────────────────────────────

    [Test]
    public void VirtualCamera_ExistsInScene()
    {
        FindRequired("Virtual Camera");
    }

    [Test]
    public void DollyTrack_ExistsInScene()
    {
        FindRequired("Dolly Track");
    }

    // ── Build Settings ──────────────────────────────────────────

    [Test]
    public void LobbyScene_IsInBuildSettingsAndEnabled()
    {
        var scenes = EditorBuildSettings.scenes;
        var lobbyScene = scenes.FirstOrDefault(s => s.path == LobbyScenePath);
        Assert.IsNotNull(lobbyScene, $"Lobby scene ({LobbyScenePath}) not found in Build Settings.");
        Assert.IsTrue(lobbyScene.enabled, "Lobby scene should be enabled in Build Settings.");
    }

    // ── JoinRoomButton ──────────────────────────────────────────

    [Test]
    public void JoinRoomButton_ExistsInRoomPanel()
    {
        var roomPanel = FindRequired("RoomPanel");
        var child = roomPanel.transform.Cast<Transform>()
            .FirstOrDefault(t => t.name == "JoinRoomButton");
        Assert.IsNotNull(child, "RoomPanel should have a child named 'JoinRoomButton'.");
    }

    [Test]
    public void JoinRoomButton_StartsInactive()
    {
        var go = FindRequired("JoinRoomButton");
        Assert.IsFalse(go.activeSelf, "JoinRoomButton should start inactive in create mode.");
    }
}
