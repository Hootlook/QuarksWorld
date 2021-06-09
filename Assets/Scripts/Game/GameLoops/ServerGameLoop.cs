using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using QuarksWorld.Systems;

namespace QuarksWorld
{
    internal class ServerGameWorld
    {
        internal ServerGameWorld(GameWorld world, BundledResourceManager resourceSystem)
        {
            gameWorld = world;

            levelCameraSystem = new LevelCameraSystem();
            playerModule = new PlayerModuleServer(gameWorld, resourceSystem);
            gameModeSystem = new GameModeSystemServer(gameWorld, resourceSystem);
            snapshotSystem = new SnapshotInterpolationServerSystem();
            movableSystem = new MovableSystemServer();
        }

        internal void Shutdown()
        {
            levelCameraSystem.Shutdown();
            snapshotSystem.Shutdown();
            movableSystem.Shutdown();
            playerModule.Shutdown();
        }

        internal void Update()
        {
            gameWorld.worldTime.tick++;
            gameWorld.worldTime.tickDuration = gameWorld.worldTime.TickInterval;
            gameWorld.FrameDuration = gameWorld.worldTime.TickInterval;
            
            levelCameraSystem.Update();
            snapshotSystem.Update();
            movableSystem.Update();
            gameModeSystem.Update();
        }

        internal void SpawnPlayer(NetworkConnection conn)
        {
            playerModule.SpawnPlayer(conn);
        }

        GameWorld gameWorld;
        
        readonly PlayerModuleServer playerModule;
        readonly SnapshotInterpolationServerSystem snapshotSystem;
        readonly GameModeSystemServer gameModeSystem;
        readonly MovableSystemServer movableSystem;
        readonly LevelCameraSystem levelCameraSystem;
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

            NetworkServer.Listen(svMaxClients.IntValue);
            
            RegisterMessages();

            svName.Value ??= Application.productName;

            Console.AddCommand("load", CmdLoad, "Load a named scene", GetHashCode());

            CmdLoad(args);

            GameDebug.Log("Server initialized.");

            return true;
        }

        public void Shutdown()
        {
            GameDebug.Log("Server shutting down.");

            Console.RemoveCommandsWithTag(GetHashCode());

            stateMachine.Shutdown();

            NetworkServer.DisconnectAll();
            NetworkServer.Shutdown();

            Game.game.levelManager.UnloadLevel();      
            
            gameWorld.Shutdown();
            gameWorld = null;                      
        }

        public void Update()
        {
            stateMachine.Update();
        }

        public void FixedUpdate() { }

        public void LateUpdate() { }

        void RegisterMessages()
        {
            NetworkServer.OnConnectedEvent = OnConnect;
            NetworkServer.OnDisconnectedEvent = OnDisconnect;
            NetworkServer.RegisterHandler<AddPlayerMessage>(OnAddPlayer);

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
            
            resourceSystem = new BundledResourceManager(gameWorld, "BundledResources/Server");

            serverWorld = new ServerGameWorld(gameWorld, resourceSystem);
        }

        void UpdateActiveState()
        {
            serverWorld.Update();
        }

        void LeaveActiveState()
        {
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

        StateMachine<ServerState> stateMachine;
        BundledResourceManager resourceSystem;
        ServerGameWorld serverWorld;
        GameWorld gameWorld;
    }
}
