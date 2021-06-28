using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using QuarksWorld.Systems;
using UnityEngine.SceneManagement;

namespace QuarksWorld
{
    internal class ServerGameWorld
    {
        internal ServerGameWorld(GameWorld world, BundledResourceManager resourceSystem)
        {
            gameWorld = world;

            cameraSystem = new CameraSystem(gameWorld);
            levelCameraSystem = new LevelCameraSystem();
            playerModule = new PlayerModuleServer(gameWorld, resourceSystem);
            gameModeSystem = new GameModeSystemServer(gameWorld, resourceSystem);
            movableSystem = new MovableSystemServer();
        }

        internal void Shutdown()
        {
            levelCameraSystem.Shutdown();
            movableSystem.Shutdown();
            playerModule.Shutdown();
            gameModeSystem.Shutdown();
        }

        internal void Update()
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

        internal void SpawnPlayer(NetworkConnection conn)
        {
            playerModule.SpawnPlayer(conn);
        }

        internal void AssignPlayerTeam(int clientId, string team = "", string role = "")
        {
            PlayerState player = NetworkServer.connections[clientId].identity.GetComponent<PlayerState>();

            if (player != null) 
            {
                gameModeSystem.AssignTeam(player, team);
                gameModeSystem.AssignCharacter(player, role);
            }
        }

        GameWorld gameWorld;

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
            serverWorld?.SpawnPlayer(conn);
        }

        void OnUserCommand(NetworkConnection conn, UserCommand cmd)
        {
            var player = conn.identity.GetComponent<PlayerState>();

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

        void UpdateActiveState()
        {
            serverWorld.Update();
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
