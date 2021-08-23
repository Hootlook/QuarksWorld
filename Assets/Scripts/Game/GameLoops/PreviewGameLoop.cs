using System.Collections;
using System.Collections.Generic;
using QuarksWorld.Systems;
using Unity.Entities;
using UnityEngine;

namespace QuarksWorld
{
    [DisableAutoCreation]
    public class PreviewGameMode : SystemBase
    {
        public int respawnDelay = 20;

        public PreviewGameMode(GameWorld world, Player player) : base(world)
        {
            this.player = player;
            this.gameWorld = world;

            spawnPos = Vector3.up * 2f;
            spawnRot = Quaternion.identity;
        }

        protected override void OnUpdate()
        {
            if (player.requestedCharacterType != -1 && player.characterType != player.requestedCharacterType)
            {
                player.characterType = player.requestedCharacterType;
                player.requestedCharacterType = -1;

                GameDebug.Log(string.Format("PreviewGameMode. Respawning as char requested. New chartype:{0}", player.characterType));

                Spawn(true);
                return;
            }

            if (player.controlledEntity == null)
            {
                GameDebug.Log(string.Format("PreviewGameMode. Spawning as we have to char. Chartype:{0}", player.characterType));

                Spawn(false);
                return;
            }

            if (player.TryGetComponent(out Health health))
            {
                if (!respawnPending && health.health == 0)
                {
                    respawnPending = true;
                    respawnTime = UnityEngine.Time.time + respawnDelay;
                }

                if (respawnPending && UnityEngine.Time.time > respawnTime)
                {
                    Spawn(false);
                    respawnPending = false;
                }
            }
        }

        void Spawn(bool keepCharPosition)
        {
            if (keepCharPosition && player.controlledEntity != Entity.Null && player.TryGetComponent(out Character character))
            {
                spawnPos = character.transform.position;
                spawnRot = character.transform.rotation;
            }
            else
                FindSpawnTransform();

            // Despawn old controlled
            if (player.controlledEntity != null)
            {
                if (player.GetComponent<Character>())
                {
                    gameWorld.RequestDespawn(player.gameObject);
                }
            }

            // if (player.characterType == Config.TeamSpectator)
            // {
            //     // SpawnSpectator(player, spawnPos, spawnRot);
            // }
            // else
                // PlayerSpawnRequest.Create(, 0, spawnPos, spawnRot, player.controlledEntity);
        }
     
        void FindSpawnTransform()
        {
            // Find random spawnpoint that matches teamIndex
            var spawnpoints = Object.FindObjectsOfType<SpawnPoint>();
            var offset = Random.Range(0, spawnpoints.Length);
            for (var i = 0; i < spawnpoints.Length; ++i)
            {
                var sp = spawnpoints[(i + offset) % spawnpoints.Length];
                if (sp.teamIndex != player.teamIndex) continue;
                spawnPos = sp.transform.position;
                spawnRot = sp.transform.rotation;
                return;
            }
        }

        GameWorld gameWorld;

        Player player;
        Quaternion spawnRot;
        Vector3 spawnPos;

        bool respawnPending;
        float respawnTime;
    }

    public class PreviewGameLoop : Game.IGameLoop
    {
        public bool Init(string[] args)
        {
            stateMachine = new StateMachine<PreviewState>();
            stateMachine.Add(PreviewState.Loading, null, UpdateLoadingState, null);
            stateMachine.Add(PreviewState.Active, EnterActiveState, UpdateActiveState, LeaveActiveState);

            // Console.AddCommand("nextchar", CmdNextHero, "Select next character", GetHashCode());
            // Console.AddCommand("nextteam", CmdNextTeam, "Select next character", GetHashCode());
            // Console.AddCommand("spectator", CmdSpectatorCam, "Select spectator cam", GetHashCode());
            // Console.AddCommand("respawn", CmdRespawn, "Force a respawn. Optional argument defines now many seconds untill respawn", GetHashCode());

            Console.SetOpen(false);

            gameWorld = new GameWorld("World[PreviewGameLoop]");

            previewGameMode = gameWorld.GetECSWorld().AddSystem(new PreviewGameMode(gameWorld, player));
            
            if (args.Length > 0)
            {
                Game.game.levelManager.LoadLevel(args[0]);
                stateMachine.SwitchTo(PreviewState.Loading);
            }
            else
            {
                stateMachine.SwitchTo(PreviewState.Active);
            }

            GameDebug.Log("Preview initialized");
            return true;
        }

        public void Shutdown()
        {
            GameDebug.Log("PreviewGameState shutdown");
            Console.RemoveCommandsWithTag(GetHashCode());

            stateMachine.Shutdown();

            Game.game.levelManager.UnloadLevel();

            gameWorld.Shutdown();
        }

        public void Update()
        {
            stateMachine.Update();
        }

        public void FixedUpdate() { }

        public void LateUpdate() { }

        public void PreviewTickUpdate()
        {
            gameWorld.worldTime = gameTime;
            gameWorld.FrameDuration = gameTime.tickDuration;

            previewGameMode.Update();

            gameWorld.ProcessDespawns();
        }

        void UpdateLoadingState()
        {
            if (Game.game.levelManager.IsCurrentLevelLoaded())
                stateMachine.SwitchTo(PreviewState.Active);
        }

        void EnterActiveState()
        {
            resources = new BundledResourceManager(gameWorld, "BundledResources/Client");

            player = playerModuleServer.CreatePlayer(0, "LocalHero");

            Game.SetMousePointerLock(true);
        }

        void UpdateActiveState()
        {
            if (gameTime.TickRate != Game.serverTickRate.IntValue)
                gameTime.TickRate = Game.serverTickRate.IntValue;

            if (Game.Input.GetKeyUp(KeyCode.H))
            {
                // CmdNextHero(null);
            }
            if (Game.Input.GetKeyUp(KeyCode.T))
            {
                // CmdNextTeam(null);
            }

            while (Game.frameTime > gameWorld.nextTickTime)
            {
                gameTime.tick++;
                gameTime.tickDuration = gameTime.TickInterval;

                PreviewTickUpdate();
                gameWorld.nextTickTime += gameWorld.worldTime.TickInterval;
            }
        }

        void LeaveActiveState()
        {
            resources.Shutdown();
        }

        enum PreviewState { Loading, Active }
        StateMachine<PreviewState> stateMachine;

        BundledResourceManager resources;

        PreviewGameMode previewGameMode;
        PlayerModuleServer playerModuleServer;

        GameWorld gameWorld;

        Player player;

        GameTime gameTime = new GameTime(60);
    }
}
