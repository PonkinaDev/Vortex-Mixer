using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// SRP: SOLO maneja conexión y ciclo de vida del runner
public class NetworkManager : MonoBehaviour, INetworkService, INetworkRunnerCallbacks
{
    public static NetworkManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private FusionSceneLoader _sceneLoader;

    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner;

    // Eventos
    public event Action OnConnectedAsHost;
    public event Action OnConnectedAsClient;
    public event Action<int> OnPlayerCountChanged;
    public event Action OnDisconnected;

    private readonly List<PlayerRef> _pendingSpawns = new();

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

    // ─────────────────────────────────────────────────────────────────────────
    // HOST
    // ─────────────────────────────────────────────────────────────────────────

    public async void StartHost()
    {
        await LaunchRunner(GameMode.Host);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CLIENT
    // ─────────────────────────────────────────────────────────────────────────

    public async void StartClient()
    {
        await LaunchRunner(GameMode.Client);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DISCONNECT
    // ─────────────────────────────────────────────────────────────────────────

    public async void Disconnect()
    {
        if (_runner == null) return;

        await _runner.Shutdown();

        _runner = null;

        OnDisconnected?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // RUNNER
    // ─────────────────────────────────────────────────────────────────────────

    private async System.Threading.Tasks.Task LaunchRunner(GameMode mode)
    {
        // Si ya había runner, lo cerramos
        if (_runner != null)
        {
            await _runner.Shutdown();
            await System.Threading.Tasks.Task.Delay(100);
        }

        if (this == null) return;

        // Creamos runner
        _runner = gameObject.AddComponent<NetworkRunner>();

        _runner.ProvideInput = true;

        // IMPORTANTE:
        // Registramos callbacks
        _runner.AddCallbacks(this);

        PlayerInputHandler inputHandler = GetComponent<PlayerInputHandler>();

        if (inputHandler != null)
        {
            _runner.AddCallbacks(inputHandler);
        }

        // Scene Manager de Fusion
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Escena actual
        var currentScene = SceneRef.FromIndex(
            SceneManager.GetActiveScene().buildIndex
        );

        // Configuración de la sesión
        var args = new StartGameArgs
        {
            GameMode     = mode,
            SessionName  = "ProtocolPanic",
            Scene        = currentScene,
            SceneManager = sceneManager,
            PlayerCount  = 2
        };

        // Iniciamos
        var result = await _runner.StartGame(args);

        if (!result.Ok)
        {
            Debug.LogError($"[NetworkManager] Error: {result.ErrorMessage}");
            return;
        }

        // Inicializamos loader
        _sceneLoader.Initialize(_runner);

        bool isHost = _runner.IsServer;

        Debug.Log(
            $"[NetworkManager] Conectado como: {(isHost ? "Host" : "Client")}"
        );

        if (isHost)
            OnConnectedAsHost?.Invoke();
        else
            OnConnectedAsClient?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PLAYER JOIN
    // ─────────────────────────────────────────────────────────────────────────

    void INetworkRunnerCallbacks.OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        Debug.Log($"[NetworkManager] Jugador conectado: {player}");

        if (!runner.IsServer) return;

        // Guardamos jugador
        _pendingSpawns.Add(player);

        // Actualizamos UI
        OnPlayerCountChanged?.Invoke(_pendingSpawns.Count);

        // Si hay 2 jugadores -> cargar juego
        if (_pendingSpawns.Count >= 2)
        {
            Invoke(nameof(TriggerSceneLoad), 1.5f);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SCENE LOADED
    // ─────────────────────────────────────────────────────────────────────────

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[NetworkManager] Escena cargada.");

        if (!runner.IsServer) return;

        int currentScene = SceneManager.GetActiveScene().buildIndex;

        // Solo en Game.scene
        if (currentScene != 1) return;

        // Spawn jugadores
        foreach (PlayerRef player in _pendingSpawns)
        {
            _playerSpawner.SpawnPlayer(runner, player);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PLAYER LEFT
    // ─────────────────────────────────────────────────────────────────────────

    void INetworkRunnerCallbacks.OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player
    )
    {
        _pendingSpawns.Remove(player);

        _playerSpawner.DespawnPlayer(runner, player);

        OnPlayerCountChanged?.Invoke(_pendingSpawns.Count);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // LOAD GAME SCENE
    // ─────────────────────────────────────────────────────────────────────────

    private void TriggerSceneLoad()
    {
        _sceneLoader.LoadGameScene();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // CALLBACKS VACÍOS REQUERIDOS
    // ─────────────────────────────────────────────────────────────────────────

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason
    )
    {
        OnDisconnected?.Invoke();
    }

    void INetworkRunnerCallbacks.OnConnectFailed(
        NetworkRunner runner,
        NetAddress remoteAddress,
        NetConnectFailedReason reason
    ) { }

    void INetworkRunnerCallbacks.OnConnectRequest(
        NetworkRunner runner,
        NetworkRunnerCallbackArgs.ConnectRequest request,
        byte[] token
    ) { }

    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(
        NetworkRunner runner,
        Dictionary<string, object> data
    ) { }

    void INetworkRunnerCallbacks.OnHostMigration(
        NetworkRunner runner,
        HostMigrationToken hostMigrationToken
    ) { }

    void INetworkRunnerCallbacks.OnInput(
        NetworkRunner runner,
        NetworkInput input
    ) { }

    void INetworkRunnerCallbacks.OnInputMissing(
        NetworkRunner runner,
        PlayerRef player,
        NetworkInput input
    ) { }

    void INetworkRunnerCallbacks.OnObjectEnterAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    ) { }

    void INetworkRunnerCallbacks.OnObjectExitAOI(
        NetworkRunner runner,
        NetworkObject obj,
        PlayerRef player
    ) { }

    void INetworkRunnerCallbacks.OnReliableDataProgress(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        float progress
    ) { }

    void INetworkRunnerCallbacks.OnReliableDataReceived(
        NetworkRunner runner,
        PlayerRef player,
        ReliableKey key,
        ArraySegment<byte> data
    ) { }

    void INetworkRunnerCallbacks.OnSceneLoadStart(
        NetworkRunner runner
    ) { }

    void INetworkRunnerCallbacks.OnSessionListUpdated(
        NetworkRunner runner,
        List<SessionInfo> sessionList
    ) { }

    void INetworkRunnerCallbacks.OnShutdown(
        NetworkRunner runner,
        ShutdownReason shutdownReason
    ) { }

    void INetworkRunnerCallbacks.OnUserSimulationMessage(
        NetworkRunner runner,
        SimulationMessagePtr message
    ) { }
}