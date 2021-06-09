using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public interface IGameMode
    {
        void Initialize(GameWorld world, GameModeSystemServer gameModeSystemServer);
        void Shutdown();

        void Restart();
        void Update();

        void OnPlayerJoin(PlayerState player);
        void OnPlayerRespawn(PlayerState player, ref Vector3 position, ref Quaternion rotation);
        void OnPlayerKilled(PlayerState victim, PlayerState killer);
    }

    public class NullGameMode : IGameMode
    {
        public void Initialize(GameWorld world, GameModeSystemServer gameModeSystemServer) { }
        public void OnPlayerJoin(PlayerState teamMember) { }
        public void OnPlayerKilled(PlayerState victim, PlayerState killer) { }
        public void OnPlayerRespawn(PlayerState player, ref Vector3 position, ref Quaternion rotation) { }
        public void Restart() { }
        public void Shutdown() { }
        public void Update() { }
    }

    public class Team
    {
        public string name;
        public int score;
    }

    public class GameModeSystemServer
    {
        [ConfigVar(Name = "mp.respawndelay", DefaultValue = "10", Description = "Time from death to respawning")]
        public static ConfigVar respawnDelay;
        [ConfigVar(Name = "mp.modename", DefaultValue = "deathmatch", Description = "Which gamemode to use")]
        public static ConfigVar modeName;

        public readonly GameModeState gameModeState;

        private readonly GameObject spectatorPrefab;
        private readonly GameObject characterPrefab;

        public List<Team> teams = new List<Team>();
        public List<TeamBase> teamBases = new List<TeamBase>();

        public GameModeSystemServer(GameWorld gameWorld, BundledResourceManager resourceSystem)
        {
            world = gameWorld;
            resources = resourceSystem;
            currentGameModeName = "";

            gameModePrefab = (GameObject)Resources.Load("Prefabs/GameModeState");
            gameModeState = world.Spawn<GameModeState>(gameModePrefab);

            var spectatorAssetRef = resources.GetResourceRegistry<NetworkedEntityRegistry>().entries[1].guid;
            var characterAssetRef = resources.GetResourceRegistry<NetworkedEntityRegistry>().entries[2].guid;

            spectatorPrefab = (GameObject)resources.GetSingleAssetResource(spectatorAssetRef);
            characterPrefab = (GameObject)resources.GetSingleAssetResource(characterAssetRef);
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

            var players = PlayerState.List;
            for (int i = 0, c = players.Count; i < c; ++i)
            {
                var player = players[i];
                player.score = 0;
                player.displayGameScore = true;
                player.goalCompletion = -1.0f;
                player.actionString = "";
            }

            enableRespawning = true;

            gameMode.Restart();

            // chatSystem.ResetChatTime();
        }

        public void Shutdown()
        {
            gameMode.Shutdown();

            Resources.UnloadAsset(gameModePrefab);

            world.RequestDespawn(gameModeState.gameObject);
        }

        float timerStart;
        ConfigVar timerLength;
        public void StartGameTimer(ConfigVar seconds, string message)
        {
            timerStart = Time.time;
            timerLength = seconds;
            gameModeState.gameTimerMessage = message;
        }

        public int GetGameTimer()
        {
            return Mathf.Max(0, Mathf.FloorToInt(timerStart + timerLength.FloatValue - Time.time));
        }

        public void SetRespawnEnabled(bool enable)
        {
            enableRespawning = enable;
        }

        char[] _msgBuf = new char[256];
        public void Update()
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
                    // case "assault":
                    //     gameMode = new GameModeAssault();
                    //     break;
                    default:
                        gameMode = new NullGameMode();
                        break;
                }
                gameMode.Initialize(world, this);
                GameDebug.Log("New gamemode : '" + gameMode.GetType().Name + "'");
                Restart();
                return;
            }

            // Handle joining players
            var playerStates = PlayerState.List;
            for (int i = 0; i < playerStates.Count; ++i)
            {
                var player = playerStates[i];
                if (!player.gameModeSystemInitialized)
                {
                    player.score = 0;
                    player.displayGameScore = true;
                    player.goalCompletion = -1.0f;
                    gameMode.OnPlayerJoin(player);
                    player.gameModeSystemInitialized = true;
                }
            }

            gameMode.Update();

            // General rules
            gameModeState.gameTimerSeconds = GetGameTimer();

            for (int i = 0; i < playerStates.Count; ++i)
            {
                var player = playerStates[i];
                var controlledEntity = player.controlledEntity;

                player.actionString = player.enableCharacterSwitch ? "Press H to change character" : "";

                // Spawn contolled entity (character) any missing
                if (controlledEntity == null)
                {
                    var position = new Vector3(0.0f, 0.2f, 0.0f);
                    var rotation = Quaternion.identity;
                    GetRandomSpawnTransform(player.teamIndex, ref position, ref rotation);

                    gameMode.OnPlayerRespawn(player, ref position, ref rotation);

                    if (player.characterType == 1000)
                    {
                        var spectatorObj = world.Spawn(spectatorPrefab, position, rotation);
                        Mirror.NetworkServer.Spawn(spectatorObj);
                        player.controlledEntity = spectatorObj;
                    }
                    else
                    {
                        var characterObj = world.Spawn(characterPrefab, position, rotation);
                        Mirror.NetworkServer.Spawn(characterObj);
                        player.controlledEntity = characterObj;
                    }

                    continue;
                }

                // Has new new entity been requested
                if (player.requestedCharacterType != -1)
                {
                    if (player.requestedCharacterType != player.characterType)
                    {
                        player.characterType = player.requestedCharacterType;
                        if (player.controlledEntity != null)
                        {

                            // Despawn current controlled entity. New entity will be created later
                            if (player.controlledEntity.GetComponent<Character>())
                            {
                                var position = new Vector3(0.0f, 0.2f, 0.0f);
                                var rotation = Quaternion.identity;
                                GetRandomSpawnTransform(player.teamIndex, ref position, ref rotation);

                                Mirror.NetworkServer.Destroy(player.controlledEntity);
                                world.RequestDespawn(player.controlledEntity);

                                var characterObj = world.Spawn(characterPrefab, position, rotation);
                                Mirror.NetworkServer.Spawn(characterObj);
                                player.controlledEntity = characterObj;
                            }
                            player.controlledEntity = null;
                        }
                    }
                    player.requestedCharacterType = -1;
                    continue;
                }

                var healthState = controlledEntity.GetComponent<HealthState>();
                if (healthState)
                {
                    // Is character dead ?
                    if (healthState.health == 0)
                    {
                        // Send kill msg
                        if (healthState.deathTick == world.worldTime.tick)
                        {
                            var killerEntity = healthState.killedBy;
                            var killerIndex = FindPlayerControlling(PlayerState.List, killerEntity);
                            PlayerState killerPlayer = null;
                            if (killerIndex != -1)
                            {
                                killerPlayer = playerStates[killerIndex];
                                var format = KillMessages[Random.Range(0, KillMessages.Length)];
                                // var l = StringFormatter.Write(ref _msgBuf, 0, format, killerPlayer.playerName, player.playerName, TeamColors[killerPlayer.teamIndex], TeamColors[player.teamIndex]);
                                // chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                                GameDebug.Log($"{killerPlayer.playerName} {format} {player.playerName}");
                            }
                            else
                            {
                                var format = SuicideMessages[Random.Range(0, SuicideMessages.Length)];
                                // var l = StringFormatter.Write(ref _msgBuf, 0, format, player.playerName, TeamColors[player.teamIndex]);
                                // chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                                GameDebug.Log($"{format} {player.playerName}");
                            }
                            gameMode.OnPlayerKilled(player, killerPlayer);
                        }

                        // Respawn dead players except if in ended mode
                        if (enableRespawning && (world.worldTime.tick - healthState.deathTick) * world.worldTime.TickInterval > respawnDelay.IntValue)
                        {
                            // Despawn current controlled entity. New entity will be created later
                            if (controlledEntity.GetComponent<Character>())
                                world.RequestDespawn(controlledEntity);

                            player.controlledEntity = null;
                        }
                    }
                }
            }
        }

        public void RequestNextChar(PlayerState player)
        {
            if (!player.enableCharacterSwitch)
                return;

            var heroTypeRegistry = resources.GetResourceRegistry<HeroTypeRegistry>();

            player.requestedCharacterType = (player.characterType + 1) % heroTypeRegistry.entries.Count;

            // chatSystem.SendChatMessage(player.playerId, "Switched to: " + heroTypeRegistry.entries[player.requestedCharacterType].name);
            GameDebug.Log($"Player {player.playerId} switched to: {heroTypeRegistry.entries[player.requestedCharacterType].name}");
        }

        public void CreateTeam(string name)
        {
            var team = new Team();
            team.name = name;
            teams.Add(team);

            // Update clients
            var idx = teams.Count - 1;
            if (idx == 0) gameModeState.teamName0 = name;
            if (idx == 1) gameModeState.teamName1 = name;
        }

        // Assign to team with fewest members
        public void AssignTeam(PlayerState player)
        {
            // Count team sizes
            var players = PlayerState.List;
            int[] teamCount = new int[teams.Count];
            for (int i = 0, c = players.Count; i < c; ++i)
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

        int FindPlayerControlling(List<PlayerState> players, int id)
        {
            if (id == -1)
                return -1;

            for (int i = 0, c = players.Count; i < c; ++i)
            {
                var playerState = players[i];
                if (playerState.playerId == id)
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

        readonly GameWorld world;
        readonly BundledResourceManager resources;
        readonly GameObject gameModePrefab;
        int[] prevTeamSpawnPointIndex = new int[2];
        bool enableRespawning = true;
        string currentGameModeName;
        IGameMode gameMode;
    }
}
