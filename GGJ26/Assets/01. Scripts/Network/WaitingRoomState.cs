using System;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class WaitingRoomState : NetworkBehaviour, INetworkRunnerCallbacks
{
    [SerializeField] private FusionLauncher launcher;

    [Networked, Capacity(4)] private NetworkArray<PlayerRef> players { get; }
    [Networked, Capacity(4)] private NetworkArray<NetworkBool> readyStates { get; }

    public event Action<int, int, bool> ReadyStateChanged;

    private int lastReadyCount = -1;
    private int lastPlayerCount = -1;

    public bool IsHost => (Object != null && Object.HasStateAuthority) || (Runner != null && Runner.IsSharedModeMasterClient);

    public override void Spawned()
    {
        if (launcher == null)
        {
            launcher = FindFirstObjectByType<FusionLauncher>();
        }

        if (Runner != null)
        {
            Runner.AddCallbacks(this);
        }

        if (Object.HasStateAuthority)
        {
            InitializePlayers();
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        runner.RemoveCallbacks(this);
    }

    private void Update()
    {
        int playerCount = GetPlayerCountEffective();
        int readyCount = GetReadyCountEffective();
        bool allReady = IsAllReadyEffective(playerCount, readyCount);

        if (playerCount != lastPlayerCount || readyCount != lastReadyCount)
        {
            lastPlayerCount = playerCount;
            lastReadyCount = readyCount;
            ReadyStateChanged?.Invoke(readyCount, playerCount, allReady);
        }
    }

    public void ToggleLocalReady()
    {
        if (Runner == null || Object == null)
        {
            return;
        }

        if (IsHost)
        {
            return;
        }

        bool current = GetReady(Runner.LocalPlayer);
        RpcSetReady(Runner.LocalPlayer, !current);
    }

    public void RequestStartGame()
    {
        if (IsHost == false)
        {
            return;
        }

        if (CanStartGame())
        {
            launcher?.StartGameScene();
        }
    }

    private void InitializePlayers()
    {
        ClearSlots();

        foreach (var player in Runner.ActivePlayers)
        {
            AddPlayer(player);
        }

        SetReadyInternal(Runner.LocalPlayer, true);
    }

    private void ClearSlots()
    {
        for (int i = 0; i < players.Length; i++)
        {
            players.Set(i, default);
            readyStates.Set(i, false);
        }
    }

    private int FindIndex(PlayerRef player)
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (IsPlayerValid(players[i]) && players[i] == player)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindEmptySlot()
    {
        for (int i = 0; i < players.Length; i++)
        {
            if (IsPlayerValid(players[i]) == false)
            {
                return i;
            }
        }

        return -1;
    }

    private void AddPlayer(PlayerRef player)
    {
        if (FindIndex(player) >= 0)
        {
            return;
        }

        int index = FindEmptySlot();
        if (index < 0)
        {
            return;
        }

        players.Set(index, player);
        readyStates.Set(index, false);
    }

    private void RemovePlayer(PlayerRef player)
    {
        int index = FindIndex(player);
        if (index < 0)
        {
            return;
        }

        players.Set(index, default);
        readyStates.Set(index, false);
    }

    private void SetReadyInternal(PlayerRef player, bool ready)
    {
        int index = FindIndex(player);
        if (index < 0)
        {
            AddPlayer(player);
            index = FindIndex(player);
        }

        if (index >= 0)
        {
            readyStates.Set(index, ready);
        }
    }

    private bool GetReady(PlayerRef player)
    {
        int index = FindIndex(player);
        if (index < 0)
        {
            return false;
        }

        return readyStates[index];
    }

    private int GetPlayerCount()
    {
        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (IsPlayerValid(players[i]))
            {
                count++;
            }
        }

        return count;
    }

    private int GetReadyCount()
    {
        int count = 0;
        for (int i = 0; i < players.Length; i++)
        {
            if (IsPlayerValid(players[i]) && readyStates[i])
            {
                count++;
            }
        }

        return count;
    }

    public int GetPlayerCountPublic()
    {
        return GetPlayerCountEffective();
    }

    public int GetReadyCountPublic()
    {
        return GetReadyCountEffective();
    }

    public bool CanStartGame()
    {
        int playerCount = GetPlayerCountEffective();
        int readyCount = GetReadyCountEffective();
        return IsAllReadyEffective(playerCount, readyCount);
    }

    private bool AllReady()
    {
        int playerCount = GetPlayerCount();
        int minPlayers = launcher != null ? Mathf.Max(1, launcher.MinPlayersToStart) : 1;
        if (playerCount < minPlayers)
        {
            return false;
        }

        if (playerCount == 1)
        {
            return true;
        }

        return GetReadyCount() == playerCount;
    }

    private int GetPlayerCountEffective()
    {
        if (Object != null && Object.IsValid)
        {
            return GetPlayerCount();
        }

        if (Runner == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var _ in Runner.ActivePlayers)
        {
            count++;
        }

        return count;
    }

    private int GetReadyCountEffective()
    {
        if (Object != null && Object.IsValid)
        {
            return GetReadyCount();
        }

        return GetPlayerCountEffective();
    }

    private bool IsAllReadyEffective(int playerCount, int readyCount)
    {
        int minPlayers = launcher != null ? Mathf.Max(1, launcher.MinPlayersToStart) : 1;
        if (playerCount < minPlayers)
        {
            return false;
        }

        if (playerCount == 1)
        {
            return true;
        }

        return readyCount == playerCount;
    }

    private static bool IsPlayerValid(PlayerRef player)
    {
        return player.RawEncoded != 0;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RpcSetReady(PlayerRef player, NetworkBool ready)
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        SetReadyInternal(player, ready);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        AddPlayer(player);
        if (player == Runner.LocalPlayer)
        {
            SetReadyInternal(player, true);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        RemovePlayer(player);
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}
