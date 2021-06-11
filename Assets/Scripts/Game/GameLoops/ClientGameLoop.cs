using Mirror;
using QuarksWorld.Systems;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuarksWorld
{
    public class ClientGameWorld
    {
        public ClientGameWorld(GameWorld world, BundledResourceManager resourceSystem)
        {
            gameWorld = world;

            levelCameraSystem = new LevelCameraSystem();
            playerModule = new PlayerModuleClient(gameWorld);
            snapshotSystem = new SnapshotInterpolationClientSystem();
        }

        public void Shutdown()
        {
            levelCameraSystem.Shutdown();
            snapshotSystem.Shutdown();
        }

        public void Update()
        {
            // HandleTime(Time.deltaTime);

            gameWorld.worldTime = renderTime;
            gameWorld.FrameDuration = Time.deltaTime;
            gameWorld.lastServerTick = NetworkTime.time;

            levelCameraSystem.Update();
            snapshotSystem.Update();

            gameWorld.ProcessDespawns();
        }

        public void RegisterLocalPlayer(int playerId)
        {
            localPlayer = playerModule.RegisterLocalPlayer(playerId);
        }

        // public float frameTimeScale = 1.0f;
        // const int CommandClientBufferSize = 32;
        // void HandleTime(float deltaTime)
        // {
        //     // Update tick rate (this will only change runtime in test scenarios)
        //     // TODO (petera) consider use ConfigVars with Server flag for this
        //     if (m_NetworkClient.serverTickRate != predictedTime.TickRate)
        //     {
        //         predictedTime.TickRate = m_NetworkClient.serverTickRate;
        //         renderTime.TickRate = m_NetworkClient.serverTickRate;
        //     }

        //     // Sample input into current command
        //     //  The time passed in here is used to calculate the amount of rotation from stick position
        //     //  The command stores final view direction
        //     var userInputEnabled = Game.GetMousePointerLock();
        //     playerModule.SampleInput(userInputEnabled, Time.deltaTime, renderTime.tick);

        //     int prevTick = predictedTime.tick;

        //     // Increment time
        //     var deltaPredictedTime = deltaTime * frameTimeScale;
        //     predictedTime.AddDuration(deltaPredictedTime);

        //     // Adjust time to be synchronized with server
        //     int preferredBufferedCommandCount = 2;
        //     int preferredTick = NetworkTime.time + (int)(((m_NetworkClient.timeSinceSnapshot + NetworkTime.rtt) / 1000.0f) * gameWorld.worldTime.TickRate) + preferredBufferedCommandCount;

        //     bool resetTime = false;
        //     if (!resetTime && predictedTime.tick < preferredTick - 3)
        //     {
        //         GameDebug.Log(string.Format("Client hard catchup ... "));
        //         resetTime = true;
        //     }

        //     if (!resetTime && predictedTime.tick > preferredTick + 6)
        //     {
        //         GameDebug.Log(string.Format("Client hard slowdown ... "));
        //         resetTime = true;
        //     }

        //     frameTimeScale = 1.0f;
        //     if (resetTime)
        //     {
        //         GameDebug.Log(string.Format("CATCHUP ({0} -> {1})", predictedTime.tick, preferredTick));

        //         gameWorld.nextTickTime = Game.frameTime;
        //         predictedTime.tick = preferredTick;
        //         predictedTime.SetTime(preferredTick, 0);
        //     }
        //     else
        //     {
        //         int bufferedCommands = m_NetworkClient.lastAcknowlegdedCommandTime - NetworkTime.time;
        //         if (bufferedCommands < preferredBufferedCommandCount)
        //             frameTimeScale = 1.01f;

        //         if (bufferedCommands > preferredBufferedCommandCount)
        //             frameTimeScale = 0.99f;
        //     }

        //     // Increment interpolation time
        //     renderTime.AddDuration(deltaTime * frameTimeScale);

        //     // Force interp time to not exeede server time
        //     if (renderTime.tick >= NetworkTime.time)
        //     {
        //         renderTime.SetTime((int)NetworkTime.time, 0);
        //     }

        //     // hard catchup
        //     if (renderTime.tick < NetworkTime.time - 10)
        //     {
        //         renderTime.SetTime((int)NetworkTime.time - 8, 0);
        //     }

        //     // Throttle up to catch up
        //     if (renderTime.tick < NetworkTime.time - 1)
        //     {
        //         renderTime.AddDuration(deltaTime * 0.01f);
        //     }

        //     // If predicted time has entered a new tick the stored commands should be sent to server 
        //     if (predictedTime.tick > prevTick)
        //     {
        //         var oldestCommandToSend = Mathf.Max(prevTick, predictedTime.tick - CommandClientBufferSize);
        //         for (int tick = oldestCommandToSend; tick < predictedTime.tick; tick++)
        //         {
        //             playerModule.StoreCommand(tick);
        //             playerModule.SendCommand(tick);
        //         }

        //         playerModule.ResetInput(userInputEnabled);
        //         playerModule.StoreCommand(predictedTime.tick);
        //     }

        //     // Store command
        //     playerModule.StoreCommand(predictedTime.tick);
        // }

        GameTime renderTime = new GameTime(60);
        GameTime predictedTime = new GameTime(60);
        GameWorld gameWorld;
        LocalPlayer localPlayer;

        readonly PlayerModuleClient playerModule;
        readonly SnapshotInterpolationClientSystem snapshotSystem;
        readonly LevelCameraSystem levelCameraSystem;
    }

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

            stateMachine.SwitchTo(ClientState.Connecting);

            gameWorld = new GameWorld("ClientWorld");

            RegisterMessages();

            Console.AddCommand("disconnect", CmdDisconnect, "Disconnect from server if connected", GetHashCode());

            GameDebug.Log("Client initialized");

            return true;
        }

        public void Shutdown()
        {
            Console.RemoveCommandsWithTag(this.GetHashCode());

            NetworkClient.Disconnect();
            NetworkClient.Shutdown();

            gameWorld.Shutdown();

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
            NetworkClient.OnConnectedEvent = OnConnect;
            NetworkClient.OnDisconnectedEvent = OnDisconnect;
            NetworkClient.RegisterHandler<NotReadyMessage>(OnNotReady);
            NetworkClient.RegisterHandler<SceneMessage>(OnMapUpate, false);

            // if (playerPrefab != null)
            //    NetworkClient.RegisterPrefab(playerPrefab);
        }

        void OnConnect()
        {
            NetworkClient.connection.isAuthenticated = true;
        }

        void OnDisconnect()
        {
            NetworkClient.Disconnect();
            NetworkClient.Shutdown();

            stateMachine.SwitchTo(ClientState.Browsing);
        }

        void OnNotReady(NotReadyMessage msg)
        {
            NetworkClient.ready = false;
        }

        void OnMapUpate(SceneMessage msg)
        {
            levelName = msg.sceneName;

            if (stateMachine.CurrentState() != ClientState.Loading)
                stateMachine.SwitchTo(ClientState.Loading);
        }

        #region States

        void EnterBrowsingState() 
        {
            GameDebug.Assert(clientWorld == null);
            clientState = ClientState.Browsing;
        }

        void UpdateBrowsingState() { }

        void LeaveBrowsingState() { }

        void EnterConnectingState()
        {
            GameDebug.Assert(clientState == ClientState.Browsing, "Expected ClientState to be browsing");
            GameDebug.Assert(clientWorld == null, "Expected ClientWorld to be null");

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
                case ConnectState.None:
                case ConnectState.Disconnected:
                    if (connectRetryCount < 2)
                    {
                        connectRetryCount++;
                        gameMessage = string.Format("Trying to connect to {0} (attempt #{1})...", targetServer, connectRetryCount);
                        GameDebug.Log(gameMessage);
                        NetworkClient.Connect(targetServer);
                    }
                    else
                    {
                        gameMessage = "Failed to connect to server";
                        GameDebug.Log(gameMessage);
                        NetworkClient.Disconnect();
                        stateMachine.SwitchTo(ClientState.Browsing);
                    }
                    break;
            }
        }

        void EnterLoadingState()
        {
            Console.SetOpen(false);

            GameDebug.Assert(clientWorld == null);
            GameDebug.Assert(NetworkClient.isConnected);

            clientState = ClientState.Loading;
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
                Transport.activeTransport.enabled = false;

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
            {
                stateMachine.SwitchTo(ClientState.Playing);
                Transport.activeTransport.enabled = true;
            }
        }

        void EnterPlayingState()
        {
            GameDebug.Assert(clientWorld == null && Game.game.levelManager.IsCurrentLevelLoaded());

            resourceSystem = new BundledResourceManager(gameWorld, "BundledResources/Client");

            clientWorld = new ClientGameWorld(gameWorld, resourceSystem);

            clientWorld.RegisterLocalPlayer(NetworkClient.connection.connectionId);

            var assetRegistry = resourceSystem.GetResourceRegistry<NetworkedEntityRegistry>();

            foreach (var entry in assetRegistry.entries)
               NetworkClient.RegisterSpawnHandler(entry.guid.GetGuid(), resourceSystem.CreateEntity, gameWorld.RequestDespawn);

            if (!NetworkClient.ready) NetworkClient.Ready();

            NetworkClient.AddPlayer();

            clientState = ClientState.Playing;
        }

        void UpdatePlayingState()
        {
            if (!NetworkClient.isConnected)
            {
                gameMessage = disconnectReason != null ? string.Format("Disconnected from server ({0})", disconnectReason) : "Disconnected from server (lost connection)";
                stateMachine.SwitchTo(ClientState.Browsing);
                return;
            }

            clientWorld.Update();
        }

        void LeavePlayingState()
        {
            clientWorld.Shutdown();
            clientWorld = null;

            resourceSystem.Shutdown();

            gameWorld.Shutdown();
            gameWorld = new GameWorld("ClientWorld");

            Game.game.levelManager.LoadLevel("empty");
        }

        #endregion

        #region Commands

        public void CmdConnect(string[] args)
        {
            if (stateMachine.CurrentState() == ClientState.Browsing)
            {
                targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
                stateMachine.SwitchTo(ClientState.Connecting);
            }
            else if (stateMachine.CurrentState() == ClientState.Connecting)
            {
                NetworkClient.Disconnect();
                targetServer = args.Length > 0 ? args[0] : "127.0.0.1";
                connectRetryCount = 0;
            }
            else
            {
                GameDebug.Log("Unable to connect from this state: " + stateMachine.CurrentState().ToString());
            }
        }

        void CmdDisconnect(string[] args)
        {
            disconnectReason = "user manually disconnected";
            NetworkClient.Disconnect();
            stateMachine.SwitchTo(ClientState.Browsing);
        }

        #endregion

        enum ClientState { Browsing, Connecting, Loading, Playing }

        StateMachine<ClientState> stateMachine;

        ClientState clientState;

        string levelName;

        string disconnectReason;
        string gameMessage = $"Welcome to {Application.productName} !";
        string targetServer = "127.0.0.1";
        int connectRetryCount;
        
        BundledResourceManager resourceSystem;
        ClientGameWorld clientWorld;
        GameWorld gameWorld;
    }
}
