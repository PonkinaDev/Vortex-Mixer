using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

// SRP: solo recolecta input local y lo entrega a Fusion
public class PlayerInputHandler : MonoBehaviour, INetworkRunnerCallbacks
{
    private Vector2 _moveInput;
    private bool _pickupPressed;

    private void Update()
    {
        // Movimiento
        _moveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        // Interacción
        // GetKeyDown = solo 1 frame
        _pickupPressed = Input.GetKeyDown(KeyCode.E);
    }

    // Fusion pide el input cada tick
    void INetworkRunnerCallbacks.OnInput(
        NetworkRunner runner,
        NetworkInput input
    )
    {
        PlayerInputData data = new PlayerInputData
        {
            MovementInput = _moveInput,
            PickupPressed = _pickupPressed
        };

        input.Set(data);

        // Reseteamos acciones únicas
        _pickupPressed = false;
    }

    // ─────────────────────────────────────────────────────────────────────
    // CALLBACKS VACÍOS REQUERIDOS POR FUSION
    // ─────────────────────────────────────────────────────────────────────

    void INetworkRunnerCallbacks.OnPlayerJoined(
        NetworkRunner runner,
        PlayerRef player
    ) { }

    void INetworkRunnerCallbacks.OnPlayerLeft(
        NetworkRunner runner,
        PlayerRef player
    ) { }

    void INetworkRunnerCallbacks.OnConnectedToServer(
        NetworkRunner runner
    ) { }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(
        NetworkRunner runner,
        NetDisconnectReason reason
    ) { }

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

    void INetworkRunnerCallbacks.OnSceneLoadDone(
        NetworkRunner runner
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