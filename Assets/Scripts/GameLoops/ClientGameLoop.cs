using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuarksWorld
{
    public class ClientGameLoop : Game.IGameLoop
    {
        GameObject gameObject;

        [ConfigVar(Name = "cl.updaterate", DefaultValue = "30000", Description = "Max bytes/sec client wants to receive", Flags = ConfigVar.Flags.ClientInfo)]
        public static ConfigVar clientUpdateRate;

        [ConfigVar(Name = "cl.updateinterval", DefaultValue = "3", Description = "Snapshot sendrate requested by client", Flags = ConfigVar.Flags.ClientInfo)]
        public static ConfigVar clientUpdateInterval;

        [ConfigVar(Name = "cl.playername", DefaultValue = "MingeBag", Description = "Name of player", Flags = ConfigVar.Flags.ClientInfo | ConfigVar.Flags.Save)]
        public static ConfigVar clientPlayerName;

        public bool Init(string[] args)
        {
            gameObject = new GameObject(nameof(ClientGameLoop));
            gameObject.transform.SetParent(Game.game.transform);

            stateMachine = new StateMachine<ClientState>();
            stateMachine.Add(ClientState.Browsing, EnterBrowsingState, UpdateBrowsingState, LeaveBrowsingState);
            stateMachine.Add(ClientState.Connecting, EnterConnectingState, UpdateConnectingState, null);
            stateMachine.Add(ClientState.Loading, EnterLoadingState, UpdateLoadingState, null);
            stateMachine.Add(ClientState.Playing, EnterPlayingState, UpdatePlayingState, LeavePlayingState);


            RegisterMessages();

            Console.AddCommand("disconnect", CmdDisconnect, "Disconnect from server if connected", GetHashCode());

            GameDebug.Log("Client initialized");

            return true;
        }

        public void Shutdown()
        {
            NetworkClient.Disconnect();
            NetworkClient.Shutdown();

            ChangeScene("boot", SceneOperation.Normal);

            UnityEngine.Object.Destroy(gameObject);
        }

        public void Update() 
        {
            stateMachine.Update();
        }

        public void LateUpdate() { }

        public void FixedUpdate() { }

        void RegisterMessages()
        {
            NetworkClient.OnConnectedEvent = OnClientConnectInternal;
            NetworkClient.OnDisconnectedEvent = OnClientDisconnectInternal;
            NetworkClient.RegisterHandler<NotReadyMessage>(OnClientNotReadyMessageInternal);
            NetworkClient.RegisterHandler<SceneMessage>(OnClientSceneInternal, false);

            //if (playerPrefab != null)
            //    NetworkClient.RegisterPrefab(playerPrefab);

            //foreach (GameObject prefab in spawnPrefabs.Where(t => t != null))
            //    NetworkClient.RegisterPrefab(prefab);
        }

        void OnClientConnectInternal()
        {
            NetworkClient.connection.isAuthenticated = true;

            stateMachine.SwitchTo(ClientState.Connecting);
        }

        void OnClientDisconnectInternal()
        {
            NetworkClient.Disconnect();
            NetworkClient.Shutdown();

            stateMachine.SwitchTo(ClientState.Browsing);
        }

        void OnClientNotReadyMessageInternal(NotReadyMessage msg)
        {
            NetworkClient.ready = false;
        }

        void OnClientSceneInternal(SceneMessage msg)
        {
            if (NetworkClient.isConnected)
            {
                ChangeScene(msg.sceneName, msg.sceneOperation, msg.customHandling);
            }
        }

        SceneOperation clientSceneOperation = SceneOperation.Normal;

        void ChangeScene(string newSceneName, SceneOperation sceneOperation = SceneOperation.Normal, bool customHandling = false)
        {
            if (string.IsNullOrEmpty(newSceneName))
            {
                Debug.LogError("ClientChangeScene empty scene name");
                return;
            }

            // vis2k: pause message handling while loading scene. otherwise we will process messages and then lose all
            // the state as soon as the load is finishing, causing all kinds of bugs because of missing state.
            // (client may be null after StopClient etc.)
            // Debug.Log("ClientChangeScene: pausing handlers while scene is loading to avoid data loss after scene was loaded.");
            Transport.activeTransport.enabled = false;

            // Cache sceneOperation so we know what was requested by the
            // Scene message in OnClientChangeScene and OnClientSceneChanged
            clientSceneOperation = sceneOperation;

            // Let client prepare for scene change
            OnClientChangeScene(newSceneName, sceneOperation, customHandling);

            // scene handling will happen in overrides of OnClientChangeScene and/or OnClientSceneChanged
            if (customHandling)
            {
                FinishLoadScene();
                return;
            }

            switch (sceneOperation)
            {
                case SceneOperation.Normal:
                    loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName);
                    break;
                case SceneOperation.LoadAdditive:
                    // Ensure additive scene is not already loaded on client by name or path
                    // since we don't know which was passed in the Scene message
                    if (!SceneManager.GetSceneByName(newSceneName).IsValid() && !SceneManager.GetSceneByPath(newSceneName).IsValid())
                        loadingSceneAsync = SceneManager.LoadSceneAsync(newSceneName, LoadSceneMode.Additive);
                    else
                    {
                        Debug.LogWarning($"Scene {newSceneName} is already loaded");

                        // Re-enable the transport that we disabled before entering this switch
                        Transport.activeTransport.enabled = true;
                    }
                    break;
                case SceneOperation.UnloadAdditive:
                    // Ensure additive scene is actually loaded on client by name or path
                    // since we don't know which was passed in the Scene message
                    if (SceneManager.GetSceneByName(newSceneName).IsValid() || SceneManager.GetSceneByPath(newSceneName).IsValid())
                        loadingSceneAsync = SceneManager.UnloadSceneAsync(newSceneName, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects);
                    else
                    {
                        Debug.LogWarning($"Cannot unload {newSceneName} with UnloadAdditive operation");

                        // Re-enable the transport that we disabled before entering this switch
                        Transport.activeTransport.enabled = true;
                    }
                    break;
            }

            // don't change the client's current networkSceneName when loading additive scene content
            if (sceneOperation == SceneOperation.Normal)
                networkSceneName = newSceneName;
        }



        #region States

        void EnterBrowsingState() { }

        void UpdateBrowsingState() { }

        void LeaveBrowsingState() { }

        void EnterConnectingState()
        {
            GameDebug.Assert(clientState == ClientState.Browsing, "Expected ClientState to be browsing");
            clientState = ClientState.Connecting;
        }

        void UpdateConnectingState()
        {
            switch (NetworkClient.connectState)
            {
                case ConnectState.Connected:
                    gameMessage = "Waiting for map info";
                    break;
                case ConnectState.Connecting:
                    // Do nothing; just wait for either success or failure
                    break;
                case ConnectState.Disconnected:
                    if (connectRetryCount < 2)
                    {
                        connectRetryCount++;
                        gameMessage = string.Format("Trying to connect to {0} (attempt #{1})...", targetServer, connectRetryCount);
                        GameDebug.Log(gameMessage);
                        networkClient.Connect(targetServer);
                    }
                    else
                    {
                        gameMessage = "Failed to connect to server";
                        GameDebug.Log(gameMessage);
                        networkClient.Disconnect();
                        stateMachine.SwitchTo(ClientState.Browsing);
                    }
                    break;
            }
        }

        void EnterLoadingState()
        {
            throw new NotImplementedException();
        }

        void UpdateLoadingState()
        {
            // Handle disconnects
            if (!NetworkClient.isConnected)
            {
                gameMessage = disconnectReason != null ? string.Format("Disconnected from server ({0})", disconnectReason) : "Disconnected from server (lost connection)";
                disconnectReason = null;
                stateMachine.SwitchTo(ClientState.Browsing);
            }

            // Wait until we got level info
            if (levelName == null)
                return;

            // Load if we are not already loading
            var level = Game.game.levelManager.currentLevel;
            if (level == null || level.name != levelName)
            {
                if (!Game.game.levelManager.LoadLevel(levelName))
                {
                    disconnectReason = string.Format("could not load requested level '{0}'", levelName);
                    NetworkClient.Disconnect();
                    return;
                }
                level = Game.game.levelManager.currentLevel;
            }

            // Wait for level to be loaded
            if (level.state == LevelState.Loaded)
                stateMachine.SwitchTo(ClientState.Playing);
        }

        void EnterPlayingState()
        {
            throw new NotImplementedException();
        }

        void UpdatePlayingState()
        {
            throw new NotImplementedException();
        }

        void LeavePlayingState()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Commands

        public void CmdConnect(string[] args)
        {
            throw new NotImplementedException();
        }

        void CmdDisconnect(string[] args)
        {

        }

        #endregion

        enum ClientState { Browsing, Connecting, Loading, Playing }

        StateMachine<ClientState> stateMachine;

        ClientState clientState;

        string levelName;

        string disconnectReason = null;
        string gameMessage = $"Welcome to {Application.productName} !";
    }
}
