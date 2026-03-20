using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NetworkSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    public NetworkPrefabRef playerPrefab;

    
    public Transform[] spawnPoints;

    async void StartGame(GameMode mode)
    {
        var runner = gameObject.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;
        runner.AddCallbacks(this);
        runner.AddCallbacks(GetComponent<NetworkInputHandler>());

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            Vector3 spawnPos;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                spawnPos = spawnPoints[player.RawEncoded % spawnPoints.Length].position;
            }
            else
            {
               
                float randomX = UnityEngine.Random.Range(-2f, 2f);
                spawnPos = new Vector3(randomX, 48f, 0);
            }

            runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        }
    }

    //private void OnGUI()
    //{
    //    if (FindObjectOfType<NetworkRunner>() == null)
    //    {
    //        if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 50, 200, 40), "HOST")) StartGame(GameMode.Host);
    //        if (GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 10, 200, 40), "CLIENT")) StartGame(GameMode.Client);
    //    }
    //}
    
    [Header("UI References")]
    public GameObject lobbyPanel; 

    public void OnClickHost()
    {
        
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        StartGame(GameMode.Host);
    }

    public void OnClickClient()
    {
        
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        StartGame(GameMode.Client);
    }


    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] payload) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}