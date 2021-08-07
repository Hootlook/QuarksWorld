using System.Collections;
using System.Collections.Generic;
using QuarksWorld.Systems;
using UnityEngine;

namespace QuarksWorld
{
    public class PreviewGameMode
    {
        public int respawnDelay = 20;

        public PreviewGameMode(GameWorld world, PlayerState player, BundledResourceManager resources)
        {
            this.resources = resources;
            this.player = player;
            this.world = world;

            spawnPos = Vector3.up * 2f;
            spawnRot = Quaternion.identity;
        }

        public void Update()
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

            if (player.controlledEntity.TryGetComponent(out Health health))
            {
                if (!respawnPending && health.health == 0)
                {
                    respawnPending = true;
                    respawnTime = Time.time + respawnDelay;
                }

                if (respawnPending && Time.time > respawnTime)
                {
                    Spawn(false);
                    respawnPending = false;
                }
            }
        }

        void Spawn(bool keepCharPosition)
        {
            if (keepCharPosition && player.controlledEntity != null && player.controlledEntity.TryGetComponent(out Character character))
            {
                spawnPos = character.transform.position;
                spawnRot = character.transform.rotation;
            }
            else
                FindSpawnTransform();

            // Despawn old controlled
            if (player.controlledEntity != null)
            {
                if (player.controlledEntity.GetComponent<Character>())
                {
                    Object.Destroy(player.controlledEntity);
                }
            }

            if (player.characterType == Config.TeamSpectator)
            {
                SpawnSpectator(player, spawnPos, spawnRot);
            }
            else
                SpawnCharacter(player, spawnPos, spawnRot);
        }
     
        void FindSpawnTransform()
        {
            // Find random spawnpoint that matches teamIndex
            var spawnpoints = Object.FindObjectsOfType<SpawnPoint>();
            var offset = UnityEngine.Random.Range(0, spawnpoints.Length);
            for (var i = 0; i < spawnpoints.Length; ++i)
            {
                var sp = spawnpoints[(i + offset) % spawnpoints.Length];
                if (sp.teamIndex != player.teamIndex) continue;
                spawnPos = sp.transform.position;
                spawnRot = sp.transform.rotation;
                return;
            }
        }


        void SpawnSpectator(PlayerState owner, Vector3 position, Quaternion rotation)
        {
            var replicatedRegistry = resources.GetResourceRegistry<ReplicatedEntityRegistry>();
            owner.controlledEntity = resources.CreateEntity(position, replicatedRegistry.entries[1].guid.GetGuid());
        }

        void SpawnCharacter(PlayerState owner, Vector3 position, Quaternion rotation)
        {
            var heroTypeRegistry = resources.GetResourceRegistry<HeroTypeRegistry>();

            owner.characterType = owner.characterType < 0 ? 0 : owner.characterType;
            owner.characterType = Mathf.Min(owner.characterType, heroTypeRegistry.entries.Count - 1);
            var heroTypeAsset = heroTypeRegistry.entries[owner.characterType];

            var characterObj = resources.CreateEntity(position, heroTypeAsset.character.GetGuid());

            var character = characterObj.GetComponent<Character>();
            character.teamId = 0;
            character.heroTypeIndex = owner.characterType;
            character.heroTypeData = heroTypeAsset;

            var health = characterObj.GetComponent<Health>();
            health.SetMaxHealth(heroTypeAsset.health);

            owner.controlledEntity = characterObj;
        }

        BundledResourceManager resources;

        GameWorld world;

        PlayerState player;
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

            player = new GameObject(nameof(PlayerState)).AddComponent<PlayerState>();

            previewGameMode = new PreviewGameMode(gameWorld, player, resources);

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

        GameWorld gameWorld;

        PlayerState player;

        GameTime gameTime = new GameTime(60);
    }
}
