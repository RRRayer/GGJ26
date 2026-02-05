using Fusion;
using StarterAssets;
using UnityEngine;

public class FusionInputBridge : MonoBehaviour
{
    public void BuildInput(NetworkRunner runner, NetworkInput input)
    {
        if (runner == null || runner.IsRunning == false)
        {
            return;
        }

        var playerObject = GetLocalPlayerObject(runner);
        if (playerObject == null)
        {
            return;
        }

        var inputs = playerObject.GetComponent<StarterAssetsInputs>();
        var playerInput = playerObject.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (inputs == null)
        {
            inputs = FindFirstObjectByType<StarterAssetsInputs>();
        }
        if (playerInput == null && inputs != null)
        {
            playerInput = inputs.GetComponentInParent<UnityEngine.InputSystem.PlayerInput>();
        }

        PlayerInputData data = default;
        if (inputs != null)
        {
            data.Move = inputs.move;
            data.Look = inputs.look;
            data.Jump = inputs.jump;
            data.Sprint = inputs.sprint;
            data.danceIndex = GetDanceIndex(inputs, playerInput);
        }
        else
        {
            data.danceIndex = -1;
        }

        input.Set(data);

        if (inputs != null)
        {
            inputs.jump = false;
        }
    }

    private int GetDanceIndex(StarterAssetsInputs inputs, UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (playerInput != null && playerInput.actions != null && HasDanceAction(playerInput.actions))
        {
            if (IsPressedAnyMap(playerInput, "Dance1")) return 0;
            if (IsPressedAnyMap(playerInput, "Dance2")) return 1;
            if (IsPressedAnyMap(playerInput, "Dance3")) return 2;
            if (IsPressedAnyMap(playerInput, "Dance4")) return 3;
            if (IsPressedAnyMap(playerInput, "Dance5") || IsPressedAnyMap(playerInput, "CrazyDance") || IsPressedAnyMap(playerInput, "DanceCrazy")) return 4;
            return -1;
        }

        if (inputs == null)
        {
            return -1;
        }

        if (inputs.dance1) return 0;
        if (inputs.dance2) return 1;
        if (inputs.dance3) return 2;
        if (inputs.dance4) return 3;

        return -1;
    }

    private bool HasDanceAction(UnityEngine.InputSystem.InputActionAsset actions)
    {
        if (actions == null)
        {
            return false;
        }

        if (actions.FindAction("Dance1", false) != null) return true;
        if (actions.FindAction("Dance2", false) != null) return true;
        if (actions.FindAction("Dance3", false) != null) return true;
        if (actions.FindAction("Dance4", false) != null) return true;
        if (actions.FindAction("Dance5", false) != null) return true;
        if (actions.FindAction("CrazyDance", false) != null) return true;
        if (actions.FindAction("DanceCrazy", false) != null) return true;

        return false;
    }

    private bool IsPressedAnyMap(UnityEngine.InputSystem.PlayerInput playerInput, string actionName)
    {
        var actions = playerInput.actions;
        if (actions == null)
        {
            return false;
        }

        var action = actions.FindAction(actionName, false);
        if (action != null)
        {
            return action.IsPressed();
        }

        for (int i = 0; i < actions.actionMaps.Count; i++)
        {
            var map = actions.actionMaps[i];
            if (map == null)
            {
                continue;
            }

            action = map.FindAction(actionName, false);
            if (action != null && action.IsPressed())
            {
                return true;
            }
        }

        return false;
    }

    private bool IsPressed(UnityEngine.InputSystem.InputActionMap actionMap, string actionName)
    {
        if (actionMap == null)
        {
            return false;
        }

        var action = actionMap.FindAction(actionName, false);
        if (action == null)
        {
            return false;
        }

        return action.IsPressed();
    }

    private NetworkObject GetLocalPlayerObject(NetworkRunner runner)
    {
        if (runner != null && runner.TryGetPlayerObject(runner.LocalPlayer, out var obj) && obj != null)
        {
            return obj;
        }

        var networkObjects = FindObjectsByType<NetworkObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < networkObjects.Length; i++)
        {
            if (networkObjects[i] != null && networkObjects[i].HasInputAuthority)
            {
                return networkObjects[i];
            }
        }

        return null;
    }
}
