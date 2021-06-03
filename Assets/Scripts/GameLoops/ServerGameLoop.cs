using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using QuarksWorld.Systems;

namespace QuarksWorld
{
    public class ServerGameWorld
    {
        public ServerGameWorld()
        {
            levelCameraSystem = new LevelCameraSystem();
        }
        
        public void Shutdown()
        {
            levelCameraSystem.Shutdown();
        }

        public void Update()
        {
            levelCameraSystem.Update();
        }

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

        void OnAddPlayer(NetworkConnection conn, AddPlayerMessage msg) { }

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

            serverWorld = new ServerGameWorld();
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

        ServerGameWorld serverWorld;
    }
}
