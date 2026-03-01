using Fusion;
using StarterAssets;
using UnityEngine;

public class FusionInputBridge : MonoBehaviour
{
    private bool pendingSabotageArm1;
    private bool pendingSabotageArm2;
    private bool pendingSabotageArm3;
    private bool pendingSabotageExecute;

    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.digit1Key.wasPressedThisFrame) pendingSabotageArm1 = true;
            if (keyboard.digit2Key.wasPressedThisFrame) pendingSabotageArm2 = true;
            if (keyboard.digit3Key.wasPressedThisFrame) pendingSabotageArm3 = true;
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pendingSabotageExecute = true;
        }
    }

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
            data.npcDanceCommand = GetNpcDanceCommand(playerInput);
        }
        else
        {
            data.danceIndex = -1;
            data.npcDanceCommand = GetNpcDanceCommand(playerInput);
        }

        // Use pending one-shot flags only to avoid duplicate presses across multiple Fusion ticks in one frame.
        data.sabotageArm1 = ConsumePending(ref pendingSabotageArm1);
        data.sabotageArm2 = ConsumePending(ref pendingSabotageArm2);
        data.sabotageArm3 = ConsumePending(ref pendingSabotageArm3);
        data.sabotageExecute = ConsumePending(ref pendingSabotageExecute);

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

    private bool GetNpcDanceCommand(UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (playerInput != null && playerInput.actions != null)
        {
            if (WasPressedAnyMap(playerInput, "NpcDanceCommand")) return true;
            if (WasPressedAnyMap(playerInput, "CommandDanceNPC")) return true;
            if (WasPressedAnyMap(playerInput, "DanceNpcCommand")) return true;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard.eKey.wasPressedThisFrame;
    }

    private bool GetSabotageArmPressed(UnityEngine.InputSystem.PlayerInput playerInput, int slot)
    {
        if (playerInput != null && playerInput.actions != null)
        {
            if (WasPressedAnyMap(playerInput, $"Sabotage{slot}")) return true;
            if (WasPressedAnyMap(playerInput, $"SabotageArm{slot}")) return true;
        }

        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return slot switch
        {
            1 => keyboard.digit1Key.wasPressedThisFrame,
            2 => keyboard.digit2Key.wasPressedThisFrame,
            3 => keyboard.digit3Key.wasPressedThisFrame,
            _ => false
        };
    }

    private bool GetSabotageExecutePressed(UnityEngine.InputSystem.PlayerInput playerInput)
    {
        if (playerInput != null && playerInput.actions != null)
        {
            if (WasPressedAnyMap(playerInput, "SabotageExecute")) return true;
            if (WasPressedAnyMap(playerInput, "Fire")) return true;
            if (WasPressedAnyMap(playerInput, "Shoot")) return true;
        }

        var mouse = UnityEngine.InputSystem.Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    private static bool ConsumePending(ref bool flag)
    {
        if (flag == false)
        {
            return false;
        }

        flag = false;
        return true;
    }

    private bool WasPressedAnyMap(UnityEngine.InputSystem.PlayerInput playerInput, string actionName)
    {
        var actions = playerInput.actions;
        if (actions == null)
        {
            return false;
        }

        var action = actions.FindAction(actionName, false);
        if (action != null)
        {
            return action.WasPressedThisFrame();
        }

        for (int i = 0; i < actions.actionMaps.Count; i++)
        {
            var map = actions.actionMaps[i];
            if (map == null)
            {
                continue;
            }

            action = map.FindAction(actionName, false);
            if (action != null && action.WasPressedThisFrame())
            {
                return true;
            }
        }

        return false;
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
