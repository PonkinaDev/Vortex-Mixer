using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkAvatarSelection : NetworkBehaviour, IAvatarSelectionService
{
    private const int MaxPlayers = 4;
    private const int NoSelection = -1;

    [SerializeField] private AvatarRegistry _registry;

    [Networked, Capacity(MaxPlayers)]
    private NetworkArray<int> Selections => default;

    [Networked, Capacity(MaxPlayers)]
    private NetworkArray<NetworkBool> ReadyStates => default;

    private static readonly Dictionary<PlayerRef, int> PersistedSelections = new();

    private ChangeDetector _changeDetector;

    public static NetworkAvatarSelection Instance { get; private set; }

    public static event Action OnAllPlayersReady;
    public static event Action<NetworkAvatarSelection> OnInstanceReady;

    public event Action OnStateChanged;

    public int AvatarCount => _registry.Count;

    public override void Spawned()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;

        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (Object.HasStateAuthority)
            InitializeSelections();

        OnInstanceReady?.Invoke(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
            Instance = null;
    }

    public override void Render()
    {
        if (HasStateChanged())
            OnStateChanged?.Invoke();
    }

    public bool IsAvatarTaken(int avatarIndex)
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (Selections[i] == avatarIndex)
                return true;
        }

        return false;
    }

    public int GetPlayerSelection(PlayerRef player)
    {
        int slot = GetPlayerSlot(player);

        if (!IsValidSlot(slot))
            return NoSelection;

        return Selections[slot];
    }

    public AvatarDefinition GetAvatarDefinition(int index)
    {
        return _registry.Get(index);
    }

    public void RequestSelectAvatar(int avatarIndex)
    {
        RPC_SelectAvatar(Runner.LocalPlayer, avatarIndex);
    }

    public void RequestReady()
    {
        RPC_SetReady(Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SelectAvatar(PlayerRef player, int avatarIndex)
    {
        if (!CanSelectAvatar(player, avatarIndex))
            return;

        int slot = GetPlayerSlot(player);

        Selections.Set(slot, avatarIndex);
        ReadyStates.Set(slot, false);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetReady(PlayerRef player)
    {
        int slot = GetPlayerSlot(player);

        if (!HasSelection(slot))
            return;

        ReadyStates.Set(slot, true);

        if (AreAllPlayersReady())
            NotifyAllPlayersReady();
    }

    private void InitializeSelections()
    {
        for (int i = 0; i < MaxPlayers; i++)
        {
            Selections.Set(i, NoSelection);
            ReadyStates.Set(i, false);
        }
    }

    private bool HasStateChanged()
    {
        foreach (var _ in _changeDetector.DetectChanges(this))
            return true;

        return false;
    }

    private bool CanSelectAvatar(PlayerRef player, int avatarIndex)
    {
        int slot = GetPlayerSlot(player);

        if (!IsValidSlot(slot))
            return false;

        if (avatarIndex < 0 || avatarIndex >= AvatarCount)
            return false;

        if (IsAvatarTaken(avatarIndex))
            return false;

        return true;
    }

    private bool HasSelection(int slot)
    {
        if (!IsValidSlot(slot))
            return false;

        return Selections[slot] != NoSelection;
    }

    private bool AreAllPlayersReady()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            int slot = GetPlayerSlot(player);

            if (!HasSelection(slot))
                return false;

            if (!ReadyStates[slot])
                return false;
        }

        return true;
    }

    private void NotifyAllPlayersReady()
    {
        PersistSelections();
        RPC_NotifyAllPlayersReady();
    }

    private void PersistSelections()
    {
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            int slot = GetPlayerSlot(player);
            PersistedSelections[player] = Selections[slot];
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_NotifyAllPlayersReady()
    {
        OnAllPlayersReady?.Invoke();
    }

    public static int GetPersistedSelection(PlayerRef player)
    {
        if (PersistedSelections.TryGetValue(player, out int selection))
            return selection;

        return NoSelection;
    }

    public static void ClearPersistedSelections()
    {
        PersistedSelections.Clear();
    }

    private static int GetPlayerSlot(PlayerRef player)
    {
        return player.PlayerId - 1;
    }

    private static bool IsValidSlot(int slot)
    {
        return slot >= 0 && slot < MaxPlayers;
    }
}