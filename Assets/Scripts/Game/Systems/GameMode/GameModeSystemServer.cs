using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

namespace QuarksWorld.Systems
{
    public interface IGameMode
    {
        void Initialize(GameWorld world, GameModeSystemServer gameModeSystemServer);
        void Shutdown();

        void Restart();
        void Update(Player[] players);

        void OnPlayerJoin(Player player);
        void OnPlayerRespawn(Player player, ref Vector3 position, ref Quaternion rotation);
        void OnPlayerKilled(Player victim, Player killer);
    }

    public class NullGameMode : IGameMode
    {
        public void Initialize(GameWorld world, GameModeSystemServer gameModeSystemServer) { }
        public void OnPlayerJoin(Player teamMember) { }
        public void OnPlayerKilled(Player victim, Player killer) { }
        public void OnPlayerRespawn(Player player, ref Vector3 position, ref Quaternion rotation) { }
        public void Restart() { }
        public void Shutdown() { }
        public void Update(Player[] players) { }
    }

    public class Team
    {
        public string name;
        public int score;
    }

    [DisableAutoCreation]
    public class GameModeSystemServer : SystemBase
    {
        [ConfigVar(Name = "mp.respawndelay", DefaultValue = "10", Description = "Time from death to respawning")]
        public static ConfigVar respawnDelay;

        [ConfigVar(Name = "mp.modename", DefaultValue = "deathmatch", Description = "Which gamemode to use")]
        public static ConfigVar modeName;

        EntityQuery playersEntityQuery;
        EntityQuery teamBaseEntityQuery;
        EntityQuery spawnPointEntityQuery;

        public readonly GameModeState gameModeState;

        public List<Team> teams = new List<Team>();
        public List<TeamBase> teamBases = new List<TeamBase>();

        public GameModeSystemServer(GameWorld gameWorld, BundledResourceManager resourcesSystem)
        {
            this.gameWorld = gameWorld;
            resources = resourcesSystem;
            currentGameModeName = "";

            var gameModeStateAssetRef = resources.GetResourceRegistry<ReplicatedEntityRegistry>().entries[3].guid;
            var spectatorAssetRef = resources.GetResourceRegistry<ReplicatedEntityRegistry>().entries[1].guid;
            var characterAssetRef = resources.GetResourceRegistry<ReplicatedEntityRegistry>().entries[2].guid;

            gameModePrefab = (GameObject)resources.GetSingleAssetResource(gameModeStateAssetRef);
            gameModeState = this.gameWorld.Spawn<GameModeState>(gameModePrefab);
            gameModeState.name = gameModePrefab.name;

            gameModeState.teams = teams;

            spectatorPrefab = (GameObject)resources.GetSingleAssetResource(spectatorAssetRef);
            characterPrefab = (GameObject)resources.GetSingleAssetResource(characterAssetRef);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            playersEntityQuery = GetEntityQuery(typeof(Player));
            teamBaseEntityQuery = GetEntityQuery(typeof(TeamBase));
            spawnPointEntityQuery = GetEntityQuery(typeof(SpawnPoint));
        }


        public void Restart()
        {
            GameDebug.Log("Restarting...");
            var bases = TeamBase.List;
            teamBases.Clear();
            for (var i = 0; i < bases.Count; i++)
            {
                teamBases.Add(bases[i]);
            }

            for (int i = 0, c = teams.Count; i < c; ++i)
            {
                teams[i].score = -1;
            }

            var players = playersEntityQuery.ToComponentArray<Player>();
            for (int i = 0, c = players.Length; i < c; ++i)
            {
                var player = players[i];
                player.score = 0;
                player.displayGameScore = true;
                player.goalCompletion = -1.0f;
            }

            enableRespawning = true;

            gameMode.Restart();
        }

        public void Shutdown()
        {
            gameMode.Shutdown();

            gameWorld.RequestDespawn(gameModeState.gameObject);
        }

        float timerStart;
        ConfigVar timerLength;
        public void StartGameTimer(ConfigVar seconds, string message)
        {
            timerStart = UnityEngine.Time.time;
            timerLength = seconds;
            gameModeState.gameTimerMessage = message;
        }

        public int GetGameTimer()
        {
            return Mathf.Max(0, Mathf.FloorToInt(timerStart + timerLength.FloatValue - UnityEngine.Time.time));
        }

        public void SetRespawnEnabled(bool enable)
        {
            enableRespawning = enable;
        }

        protected override void OnUpdate()
        {
            // Handle change of game mode
            if (currentGameModeName != modeName.Value)
            {
                currentGameModeName = modeName.Value;

                switch (currentGameModeName)
                {
                    case "deathmatch":
                        gameMode = new GameModeDeathmatch();
                        break;
                    default:
                        gameMode = new NullGameMode();
                        break;
                }
                gameMode.Initialize(gameWorld, this);
                GameDebug.Log("New gamemode : '" + gameMode.GetType().Name + "'");
                Restart();
                return;
            }

            // Handle joining players
            var players = playersEntityQuery.ToComponentArray<Player>();
            for (int i = 0; i < players.Length; ++i)
            {
                var player = players[i];
                if (!player.gameModeSystemInitialized)
                {
                    player.score = 0;
                    player.displayGameScore = true;
                    player.goalCompletion = -1.0f;
                    player.characterType = 1000;
                    gameMode.OnPlayerJoin(player);
                    player.gameModeSystemInitialized = true;
                }
            }

            gameMode.Update(players);

            // General rules
            gameModeState.gameTimerSeconds = GetGameTimer();

            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];

                if (player.controlledEntity == Entity.Null)
                {
                    var position = new Vector3(0.0f, 0.2f, 0.0f);
                    var rotation = Quaternion.identity;
                    GetRandomSpawnTransform(player.teamIndex, ref position, ref rotation);

                    gameMode.OnPlayerRespawn(player, ref position, ref rotation);

                    if (player.teamIndex == Config.TeamSpectator)
                    {
                        // SpawnSpectator(player, position, rotation);
                    }
                    else
                    {
                        // SpawnCharacter(player, position, rotation);
                    }
                }

                // Has new entity been requested
                if (player.requestedCharacterType != -1 || player.requestedTeamIndex != -1)
                {
                    player.characterType = player.requestedCharacterType;
                    player.teamIndex = player.requestedTeamIndex;

                    if (player.controlledEntity != Entity.Null)
                    {
                        gameWorld.RequestDespawn(player.controlledEntity);

                        player.controlledEntity = Entity.Null;
                    }

                    player.requestedCharacterType = -1;
                    player.requestedTeamIndex = -1;
                    continue;
                }

                if (EntityManager.HasComponent<Health>(player.controlledEntity))
                {
                    var healthState = EntityManager.GetComponentObject<Health>(player.controlledEntity);

                    // Is character dead ?
                    if (healthState.health == 0)
                    {
                        // Send kill msg
                        if (healthState.deathTick == gameWorld.worldTime.tick)
                        {
                            int killerIndex = FindPlayerControlling(players, healthState.killedBy);
                            Player killerPlayer = null;
                            if (killerIndex != -1)
                            {
                                killerPlayer = players[killerIndex];
                                var format = KillMessages[Random.Range(0, KillMessages.Length)];
                                GameDebug.Log($"{killerPlayer.playerName} {format} {player.playerName}");
                            }
                            else
                            {
                                var format = SuicideMessages[Random.Range(0, SuicideMessages.Length)];
                                GameDebug.Log($"{format} {player.playerName}");
                            }

                            gameMode.OnPlayerKilled(player, killerPlayer);

                            // Respawn dead players except if in ended mode
                            if (enableRespawning && (gameWorld.worldTime.tick - healthState.deathTick) * gameWorld.worldTime.TickInterval > respawnDelay.IntValue)
                            {
                                // Despawn current controlled entity. New entity will be created later
                                if (EntityManager.HasComponent<Character>(player.controlledEntity))
                                {
                                    gameWorld.RequestDespawn(player.controlledEntity);
                                }

                                player.controlledEntity = Entity.Null;
                            }
                        }
                    }
                }
            }
        }

        public void CreateTeam(string name)
        {
            var team = new Team();
            team.name = name;
            teams.Add(team);
        }

        public void RequestNextChar(Player player)
        {
            if (!player.allowedCharacterSwitch)
                return;

            var heroTypeRegistry = resources.GetResourceRegistry<HeroTypeRegistry>();

            player.requestedCharacterType = (player.characterType + 1) % heroTypeRegistry.entries.Count;

            GameDebug.Log($"Player {player.id} switched to: {heroTypeRegistry.entries[player.requestedCharacterType].name}");
        }

        public void AssignCharacter(Player player, string characterName)
        {
            if (!player.allowedCharacterSwitch)
                return;

            var heroTypeRegistry = resources.GetResourceRegistry<HeroTypeRegistry>();

            var heroAsset = heroTypeRegistry.entries.Find(h => h.name == characterName);

            if (heroAsset == null)
            {
                GameDebug.LogWarning($"PlayerClass '{characterName}' doesn't exist");
                return;
            }

            player.requestedCharacterType = heroTypeRegistry.entries.IndexOf(heroAsset);

            Console.OutputString($"You'll be '{heroAsset.name}' next life", player.id);
        }

        public void AssignTeam(Player player, string teamName)
        {
            teamName = teamName.ToLower();

            if (teamName == "spectator")
            {
                player.requestedTeamIndex = Config.TeamSpectator;
                return;
            }

            Team team = teams.Find(t => t.name.ToLower() == teamName);

            if (team == null)
            {
                GameDebug.LogWarning($"Team '{teamName}' doesn't exist");
                return;
            }

            player.requestedTeamIndex = teams.IndexOf(team);

            Console.OutputString($"Your team will be '{team.name}' next life", player.id);
        }

        public void AssignTeamBalanced(Player player)
        {
            // Count team sizes
            var players = playersEntityQuery.ToComponentArray<Player>();
            int[] teamCount = new int[teams.Count];
            for (int i = 0, c = players.Length; i < c; ++i)
            {
                var idx = players[i].teamIndex;
                if (idx < teamCount.Length)
                    teamCount[idx]++;
            }

            // Pick smallest
            int joinIndex = -1;
            int smallestTeamSize = 1000;
            for (int i = 0, c = teams.Count; i < c; i++)
            {
                if (teamCount[i] < smallestTeamSize)
                {
                    smallestTeamSize = teamCount[i];
                    joinIndex = i;
                }
            }

            // Join 
            player.teamIndex = joinIndex < 0 ? 0 : joinIndex;
            GameDebug.Log("Assigned team " + joinIndex + " to player " + player);
        }

        int FindPlayerControlling(Player[] players, Entity entity)
        {
            if (entity == Entity.Null)
                return -1;

            for (int i = 0, c = players.Length; i < c; ++i)
            {
                var playerState = players[i];
                if (playerState.controlledEntity == entity)
                    return i;
            }
            return -1;
        }

        public bool GetRandomSpawnTransform(int teamIndex, ref Vector3 pos, ref Quaternion rot)
        {
            // Make list of spawnpoints for team 
            var teamSpawns = new List<SpawnPoint>();
            var spawnPoints = Object.FindObjectsOfType<SpawnPoint>();
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var spawnPoint = spawnPoints[i];
                if (spawnPoint.teamIndex == teamIndex)
                    teamSpawns.Add(spawnPoint);
            }

            if (teamSpawns.Count == 0)
                return false;

            var index = (prevTeamSpawnPointIndex[teamIndex] + 1) % teamSpawns.Count;
            prevTeamSpawnPointIndex[teamIndex] = index;
            pos = teamSpawns[index].transform.position;
            rot = teamSpawns[index].transform.rotation;

            GameDebug.Log("spawning at " + teamSpawns[index].name);

            return true;
        }

        static string[] KillMessages = new string[]
        {
        "<color={2}>{0}</color> killed <color={3}>{1}</color>",
        "<color={2}>{0}</color> terminated <color={3}>{1}</color>",
        "<color={2}>{0}</color> ended <color={3}>{1}</color>",
        "<color={2}>{0}</color> owned <color={3}>{1}</color>",
        };

        static string[] SuicideMessages = new string[]
        {
        "<color={1}>{0}</color> rebooted",
        "<color={1}>{0}</color> gave up",
        "<color={1}>{0}</color> slipped and accidently killed himself",
        "<color={1}>{0}</color> wanted to give the enemy team an edge",
        };

        static string[] TeamColors = new string[]
        {
        "#1EA00000", //"#FF19E3FF",
        "#1EA00001", //"#00FFEAFF",
        };

        readonly GameWorld gameWorld;

        readonly GameObject spectatorPrefab;
        readonly GameObject characterPrefab;

        readonly GameObject gameModePrefab;
        readonly BundledResourceManager resources;
        int[] prevTeamSpawnPointIndex = new int[2];
        bool enableRespawning = true;
        string currentGameModeName;
        IGameMode gameMode;
    }
}
