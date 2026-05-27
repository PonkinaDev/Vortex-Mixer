using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private FusionSceneLoader _sceneLoader;
    [SerializeField] private NetworkObject _avatarSelectionPrefab;

    private const string SessionName = "ProtocolPanic";
    private const int GameSceneIndex = 1;
    private const int ExpectedPlayers = 2;
    private const float SelectionTimeout = 5f;

    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    public event Action OnConnectedAsHost;
    public event Action OnConnectedAsClient;
    public event Action<int> OnPlayerCountChanged;
    public event Action OnDisconnected;

    private readonly List<PlayerRef> _pendingPlayers = new();
    private bool _avatarSelectionSpawned;
    private bool _gameSceneReady;
    private bool _selectionsReady;
    private bool _playersSpawned;
    private Coroutine _waitForSelectionsRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        NetworkAvatarSelection.OnAllPlayersReady += HandleAllPlayersReady;
    }

    private void OnDisable()
    {
        NetworkAvatarSelection.OnAllPlayersReady -= HandleAllPlayersReady;
    }

    public async void StartHost()
    {
        await LaunchRunner(GameMode.Host);
    }

    public async void StartClient()
    {
        await LaunchRunner(GameMode.Client);
    }

    public async void Disconnect()
    {
        if (_runner == null)
            return;

        await ShutdownCurrentRunner(true);
    }

    private async Task LaunchRunner(GameMode mode)
    {
        if (_runner != null)
            await ShutdownCurrentRunner(false);

        if (this == null)
            return;

        ResetSessionState();

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();
        if (inputHandler != null)
            _runner.AddCallbacks(inputHandler);

        NetworkSceneManagerDefault sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        SceneRef currentScene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

        StartGameArgs args = new()
        {
            GameMode = mode,
            SessionName = SessionName,
            Scene = currentScene,
            SceneManager = sceneManager,
            PlayerCount = ExpectedPlayers
        };

        var result = await _runner.StartGame(args);

        if (!result.Ok)
        {
            await ShutdownCurrentRunner(false);
            return;
        }

        if (_sceneLoader != null)
            _sceneLoader.Initialize(_runner);

        if (_runner.IsServer)
            OnConnectedAsHost?.Invoke();
        else
            OnConnectedAsClient?.Invoke();
    }

    private async Task ShutdownCurrentRunner(bool notifyDisconnected)
    {
        if (_waitForSelectionsRoutine != null)
        {
            StopCoroutine(_waitForSelectionsRoutine);
            _waitForSelectionsRoutine = null;
        }

        if (_runner == null)
        {
            if (notifyDisconnected)
                OnDisconnected?.Invoke();

            return;
        }

        NetworkRunner runnerToShutdown = _runner;
        _runner = null;

        await runnerToShutdown.Shutdown();
        Destroy(runnerToShutdown);

        if (notifyDisconnected)
            OnDisconnected?.Invoke();
    }

    private void ResetSessionState()
    {
        _pendingPlayers.Clear();
        _avatarSelectionSpawned = false;
        _gameSceneReady = false;
        _selectionsReady = false;
        _playersSpawned = false;
        NetworkAvatarSelection.ClearPersistedSelections();
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        _pendingPlayers.Add(player);
        OnPlayerCountChanged?.Invoke(_pendingPlayers.Count);

        if (runner.IsServer && _pendingPlayers.Count >= ExpectedPlayers && !_avatarSelectionSpawned)
            SpawnAvatarSelection();
    }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer)
            return;

        int currentScene = SceneManager.GetActiveScene().buildIndex;
        if (currentScene != GameSceneIndex)
            return;
        _gameSceneReady = true;
        TrySpawnPlayers(runner);
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _pendingPlayers.Remove(player);
        OnPlayerCountChanged?.Invoke(_pendingPlayers.Count);

        if (!runner.IsServer)
            return;

        _playerSpawner.DespawnPlayer(runner, player);
    }

    private void SpawnAvatarSelection()
    {
        if (_runner == null || _avatarSelectionSpawned || _avatarSelectionPrefab == null)
            return;

        _avatarSelectionSpawned = true;
        _runner.Spawn(_avatarSelectionPrefab);
    }

    private void HandleAllPlayersReady()
    {
        _selectionsReady = true;

        if (_sceneLoader != null)
            _sceneLoader.LoadGameScene();
    }

    private void TrySpawnPlayers(NetworkRunner runner)
    {
        if (_playersSpawned)
            return;

        if (!_gameSceneReady || !_selectionsReady)
        {
            if (_gameSceneReady && !_selectionsReady)
                StartSelectionWaitRoutine(runner);

            return;
        }

        SpawnPlayers(runner);
    }

    private void StartSelectionWaitRoutine(NetworkRunner runner)
    {
        if (_waitForSelectionsRoutine != null)
            return;

        _waitForSelectionsRoutine = StartCoroutine(WaitForSelectionsAndSpawn(runner));
    }

    private IEnumerator WaitForSelectionsAndSpawn(NetworkRunner runner)
    {
        float timeout = SelectionTimeout;

        while (timeout > 0f)
        {
            if (HaveAllSelections())
            {
                SpawnPlayers(runner);
                _waitForSelectionsRoutine = null;
                yield break;
            }

            timeout -= Time.deltaTime;
            yield return null;
        }
        SpawnPlayers(runner);
        _waitForSelectionsRoutine = null;
    }

    private bool HaveAllSelections()
    {
        foreach (PlayerRef player in _pendingPlayers)
        {
            if (NetworkAvatarSelection.GetPersistedSelection(player) == -1)
                return false;
        }

        return true;
    }

    private void SpawnPlayers(NetworkRunner runner)
    {
        if (_playersSpawned)
            return;

        _playersSpawned = true;

        foreach (PlayerRef player in _pendingPlayers)
        {
            _playerSpawner.SpawnPlayer(runner, player);
        }
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        OnDisconnected?.Invoke();
    }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input) { }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }

    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
}