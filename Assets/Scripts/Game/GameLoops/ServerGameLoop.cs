using UnityEngine.SceneManagement;
using System.Collections.Generic;
using QuarksWorld.Systems;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class ServerGameWorld
    {
        public int TickRate
        {
            get
            {
                return gameWorld.worldTime.TickRate;
            }
            set
            {
                gameWorld.worldTime.TickRate = value;
            }
        }
        public float TickInterval => gameWorld.worldTime.TickInterval;
        public int WorldTick => gameWorld.worldTime.tick;

        internal ServerGameWorld(GameWorld world, BundledResourceManager resources)
        {
            gameWorld = world;

            cameraSystem = new CameraSystem(gameWorld);
            levelCameraSystem = new LevelCameraSystem();
            replicatedSystem = new ReplicatedEntityModuleServer(gameWorld, resources);
            playerModule = new PlayerModuleServer(gameWorld, resources);
            gameModeSystem = new GameModeSystemServer(gameWorld, resources);
            movableSystem = new MovableSystemServer(gameWorld);
        }

        internal void Shutdown()
        {
            levelCameraSystem.Shutdown();
            replicatedSystem.Shutdown();
            movableSystem.Shutdown();
            playerModule.Shutdown();
            gameModeSystem.Shutdown();
        }

        internal void TickUpdate()
        {
            gameWorld.worldTime.tick++;
            gameWorld.worldTime.tickDuration = gameWorld.worldTime.TickInterval;
            gameWorld.FrameDuration = gameWorld.worldTime.TickInterval;
            
            levelCameraSystem.Update();
            movableSystem.Update();
            gameModeSystem.Update();
        }

        internal void LateUpdate()
        {
            cameraSystem.Execute();
        }

        internal void SpawnPlayer(int playerId)
        {
            playerModule.CreatePlayer(playerId, "ConnectingPlayer");
        }

        internal void AssignPlayerTeam(int clientId, string team = "", string role = "")
        {
            Player player = NetworkServer.connections[clientId].identity.GetComponent<Player>();

            if (player != null) 
            {
                gameModeSystem.AssignTeam(player, team);
                gameModeSystem.AssignCharacter(player, role);
            }
        }

        internal EntitySnapshot[] GenerateSnapshot(float deltaTime)
        {
            List<EntitySnapshot> entities = new List<EntitySnapshot>();

            foreach (var entity in replicatedSystem.entityCollection.replicatedData)
            {
                using (PooledNetworkWriter writer = NetworkWriterPool.GetWriter())
                {
                    if (entity.gameObject.TryGetComponent(out NetworkIdentity indentity))
                    {
                        replicatedSystem.GenerateEntitySnapshot((int)indentity.netId, writer);
                        entities.Add(new EntitySnapshot { id = (int)indentity.netId, data = writer.ToArray() });
                    }
                }
            }

            return entities.ToArray();
        }

        GameWorld gameWorld;

        readonly ReplicatedEntityModuleServer replicatedSystem;
        readonly PlayerModuleServer playerModule;
        readonly GameModeSystemServer gameModeSystem;
        readonly MovableSystemServer movableSystem;
        readonly LevelCameraSystem levelCameraSystem;
        readonly CameraSystem cameraSystem;        
    }

    public class ServerGameLoop : Game.IGameLoop
    {
        [ConfigVar(Name = "sv.maxclients", DefaultValue = "8", Description = "Maximum allowed clients")]
        public static ConfigVar svMaxClients;

        [ConfigVar(Name = "sv.name", DefaultValue = "", Description = "Servername")]
        public static ConfigVar svName;

        public bool Init(string[] args)
        {
            stateMachine = new StateMachine<ServerState>();
            stateMachine.Add(ServerState.Idle, null, UpdateIdleState, null);
            stateMachine.Add(ServerState.Loading, null, UpdateLoadingState, null);
            stateMachine.Add(ServerState.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);

            stateMachine.SwitchTo(ServerState.Idle);

            gameWorld = new GameWorld("ServerWorld");

            Transport.activeTransport.enabled = false;

            NetworkServer.Listen(svMaxClients.IntValue);
            
            RegisterMessages();

            svName.Value ??= Application.productName;

            SceneManager.activeSceneChanged += OnSceneChanged;

            Console.OnConsoleWrite += OnConsoleWrite; 

            Console.AddCommand("map", CmdLoad, "Load a named map", GetHashCode());
            Console.AddCommand("jointeam", RpcJoin, "Change team to the one specified", GetHashCode(), true);

            CmdLoad(args);

            GameDebug.Log("Server initialized.");

            return true;
        }

        public void Shutdown()
        {
            GameDebug.Log("Server shutting down.");

            Console.RemoveCommandsWithTag(GetHashCode());
            SceneManager.activeSceneChanged -= OnSceneChanged;

            stateMachine.Shutdown();

            NetworkServer.DisconnectAll();
            NetworkServer.Shutdown();

            Game.game.levelManager.UnloadLevel();      
            
            gameWorld.Shutdown();
            gameWorld = null;                      
        }

        float timer;
        public void Update()
        {
            if (serverWorld != null && serverWorld.TickRate != Game.serverTickRate.IntValue)
                serverWorld.TickRate = Game.serverTickRate.IntValue;

            stateMachine.Update();

            timer += Time.deltaTime;
            while (timer >= Time.fixedDeltaTime)
            {
                timer -= Time.fixedDeltaTime;

                Physics.Simulate(Time.fixedDeltaTime);
                Physics.SyncTransforms();
            }
        }

        public void FixedUpdate() {  }

        public void LateUpdate() 
        {
            serverWorld?.LateUpdate();
        }

        void RegisterMessages()
        {
            NetworkServer.OnConnectedEvent = OnConnect;
            NetworkServer.OnDisconnectedEvent = OnDisconnect;
            NetworkServer.RegisterHandler<AddPlayerMessage>(OnAddPlayer);
            NetworkServer.RegisterHandler<ConsoleMessage>(OnConsoleCommand);
            NetworkServer.RegisterHandler<UserCommand>(OnUserCommand);

            NetworkServer.ReplaceHandler<ReadyMessage>(OnReady);
        }

        void OnConnect(NetworkConnection conn)
        {
            conn.isAuthenticated = true;

            if (Game.game.levelManager.IsCurrentLevelLoaded())
            {
                SceneMessage msg = new SceneMessage() { sceneName = Game.game.levelManager.currentLevel.name };
                conn.Send(msg);
            }
        }

        void OnDisconnect(NetworkConnection conn)
        {
            NetworkServer.DestroyPlayerForConnection(conn);
        }

        void OnReady(NetworkConnection conn, ReadyMessage msg)
        {
            NetworkServer.SetClientReady(conn);
        }

        void OnAddPlayer(NetworkConnection conn, AddPlayerMessage msg)
        {
            serverWorld?.SpawnPlayer(conn.connectionId);
        }

        void OnUserCommand(NetworkConnection conn, UserCommand cmd)
        {
            var player = conn.identity.GetComponent<Player>();

            if (cmd.tick != gameWorld.worldTime.tick)
                return;

            if (player.controlledEntity != null)
            {
                player.command = cmd;
            }
        }

        void OnConsoleCommand(NetworkConnection conn, ConsoleMessage msg)
        {
            Console.EnqueueCommandNoHistory(msg.text, conn.connectionId);
        }

        void OnConsoleWrite(string message, int clientId)
        {
            // if command is from the server don't send
            if (clientId == 0 || string.IsNullOrEmpty(message))
                return;

            if (NetworkServer.connections.TryGetValue(clientId, out NetworkConnectionToClient conn))
                conn.Send(new ConsoleMessage { text = message });
        }

        void OnSceneChanged(Scene current, Scene next)
        {
            NetworkServer.SendToAll(new SceneMessage { sceneName = next.name });
        }

        #region States

        void UpdateIdleState() { }

        void UpdateLoadingState()
        {
            if (Game.game.levelManager.IsCurrentLevelLoaded())
                stateMachine.SwitchTo(ServerState.Active);
        }

        void EnterActiveState()
        {
            GameDebug.Assert(serverWorld == null);

            Transport.activeTransport.enabled = true;
            
            resources = new BundledResourceManager(gameWorld, "BundledResources/Server");

            serverWorld = new ServerGameWorld(gameWorld, resources);
        }
        
        float nextTickTime;
        void UpdateActiveState()
        {
            GameDebug.Assert(serverWorld != null);

            while (Game.frameTime > nextTickTime)
            {
                serverWorld.TickUpdate();

                // Profiler.BeginSample("GenerateSnapshots");

                // SnapshotMessage snapshot;
                // snapshot.tick = serverWorld.WorldTick;
                // snapshot.entities = serverWorld.GenerateSnapshot(Time.fixedDeltaTime);

                // Profiler.EndSample();
                
                // NetworkServer.SendToAll(snapshot, Channels.Unreliable);

                nextTickTime += serverWorld.TickInterval;
            }
        }

        void LeaveActiveState()
        {
            Transport.activeTransport.enabled = false;

            serverWorld.Shutdown();
            serverWorld = null;
        }

        #endregion

        #region Commands

        void CmdLoad(string[] args)
        {
            if (args.Length == 1)
                LoadLevel(args[0]);
            else if (args.Length == 2)
                LoadLevel(args[0], args[1]);
            else if (args.Length == 0)
                LoadLevel("GameScene");
        }

        void RpcJoin(string[] args)
        {
            if (args.Length == 2)
                serverWorld.AssignPlayerTeam(int.Parse(args[0]), args[1]);
            else if (args.Length == 3)
                serverWorld.AssignPlayerTeam(int.Parse(args[0]), args[1], args[2]);
            else if (args.Length == 0)
                GameDebug.LogError("Console haven't provided clientID");
        }

        #endregion

        void LoadLevel(string levelname, string gamemode = "deathmatch")
        {
            if (!Game.game.levelManager.CanLoadLevel(levelname))
            {
                GameDebug.Log("ERROR : Cannot load level : " + levelname);
                return;
            }

            Game.game.levelManager.LoadLevel(levelname);

            stateMachine.SwitchTo(ServerState.Loading);
        }

        enum ServerState { Idle, Loading, Active }

        class ClientInfo
        {
            public string playerName;
            public int updateInterval;
        }

        Dictionary<NetworkConnection, ClientInfo> Clients;

        StateMachine<ServerState> stateMachine;
        BundledResourceManager resources;
        ServerGameWorld serverWorld;
        GameWorld gameWorld;
    }
}
