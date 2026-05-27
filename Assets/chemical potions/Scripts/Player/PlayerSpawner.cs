using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour, IPlayerSpawner
{
    [SerializeField] private NetworkPrefabRef _fallbackPrefab;
    [SerializeField] private AvatarRegistry _registry;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();

    private static readonly Vector3[] SpawnPositions =
    {
        new(-3f, 1f, 1f),
        new(3f, 1f, 1f)
    };

    public int PlayerCount => _spawnedPlayers.Count;

public void SpawnPlayer(NetworkRunner runner, PlayerRef player)
{
    if (!CanSpawn(runner, player))
        return;

    NetworkPrefabRef prefab = GetPlayerPrefab(player);
    Vector3 spawnPosition = GetSpawnPosition();

    NetworkObject playerObject = runner.Spawn(
        prefab,
        spawnPosition,
        Quaternion.identity,
        player,
onBeforeSpawned: (r, obj) =>
{
    var cc = obj.GetComponent<CharacterController>();
    cc.enabled = false;
    obj.transform.position = spawnPosition;
    cc.enabled = true;
}
    );

    

    _spawnedPlayers[player] = playerObject;

;
}



    public void DespawnPlayer(NetworkRunner runner, PlayerRef player)
    {
        if (!_spawnedPlayers.TryGetValue(player, out NetworkObject playerObject))
            return;

        runner.Despawn(playerObject);
        _spawnedPlayers.Remove(player);
    }

    private bool CanSpawn(NetworkRunner runner, PlayerRef player)
    {
        if (runner == null)
            return false;

        if (!runner.IsServer)
            return false;

        if (_spawnedPlayers.ContainsKey(player))
            return false;

        return true;
    }

    private NetworkPrefabRef GetPlayerPrefab(PlayerRef player)
    {
        int avatarIndex = NetworkAvatarSelection.GetPersistedSelection(player);

        if (!HasValidAvatarSelection(avatarIndex))
            return _fallbackPrefab;

        AvatarDefinition avatar = _registry.Get(avatarIndex);

        if (avatar == null)
            return _fallbackPrefab;

        if (avatar.Prefab == NetworkPrefabRef.Empty)
            return _fallbackPrefab;

        return avatar.Prefab;
    }

    private bool HasValidAvatarSelection(int avatarIndex)
    {
        if (_registry == null)
            return false;

        if (avatarIndex < 0)
            return false;

        if (avatarIndex >= _registry.Count)
            return false;

        return true;
    }

    private Vector3 GetSpawnPosition()
    {
        int index = _spawnedPlayers.Count;

        if (index < SpawnPositions.Length)
            return SpawnPositions[index];

        return Vector3.zero;
    }
}