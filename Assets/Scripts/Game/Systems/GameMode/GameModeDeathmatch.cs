using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld.Systems
{
    public class GameModeDeathmatch : IGameMode
    {
        [ConfigVar(Name = "mp.minplayers", DefaultValue = "2", Description = "Minimum players before match starts")]
        public static ConfigVar minPlayers;
        [ConfigVar(Name = "mp.prematchtime", DefaultValue = "20", Description = "Time before match starts")]
        public static ConfigVar preMatchTime;
        [ConfigVar(Name = "mp.postmatchtime", DefaultValue = "10", Description = "Time after match ends before new will begin")]
        public static ConfigVar postMatchTime;
        [ConfigVar(Name = "mp.roundlength", DefaultValue = "420", Description = "Deathmatch round length (seconds)")]
        public static ConfigVar roundLength;

        public void Initialize(GameWorld gameWorld, GameModeSystemServer gameModeSystemServer)
        {
            world = gameWorld;
            gameModeSystem = gameModeSystemServer;

            gameModeSystem.CreateTeam("Red");
            gameModeSystem.CreateTeam("Blue");

            Console.Write("Deathmatch game mode initialized");
        }

        public void Restart()
        {
            foreach (var t in gameModeSystem.teams)
                t.score = 0;
                
            phase = Phase.Countdown;
            gameModeSystem.StartGameTimer(preMatchTime, "PreMatch");
        }

        public void Shutdown() { }

        public void Update()
        {
            var gameModeState = gameModeSystem.gameModeState;

            var players = PlayerState.List;

            switch (phase)
            {
                case Phase.Countdown:
                    if (gameModeSystem.GetGameTimer() == 0)
                    {
                        if (players.Count < minPlayers.IntValue)
                        {
                            // gameModeSystem.chatSystem.SendChatAnnouncement("Waiting for more players.");
                            gameModeSystem.StartGameTimer(preMatchTime, "PreMatch");
                            GameDebug.Log("Waiting for more players.");
                        }
                        else
                        {
                            gameModeSystem.StartGameTimer(roundLength, "");
                            phase = Phase.Active;
                            // gameModeSystem.chatSystem.SendChatAnnouncement("Match started!");
                            GameDebug.Log("Match started !");
                        }
                    }
                    break;
                case Phase.Active:
                    if (gameModeSystem.GetGameTimer() == 0)
                    {
                        // Find winner team
                        var winTeam = -1;
                        var teams = gameModeSystem.teams;
                        // TODO (petera) Get rid of teams list and hardcode for teamsize 2 as all ui etc assumes it anyways.
                        if (teams.Count == 2)
                        {
                            winTeam = teams[0].score > teams[1].score ? 0 : teams[0].score < teams[1].score ? 1 : -1;
                        }

                        // TODO : For now we just kill all players when we restart 
                        // but we should change it to something less dramatic like taking
                        // control away from the player or something
                        for (int i = 0, c = players.Count; i < c; i++)
                        {
                            var playerState = players[i];
                            if (playerState.controlledEntity != null)
                            {
                                var healthState = playerState.GetComponent<Health>();
                                healthState.health = 0.0f;
                                healthState.deathTick = -1;
                            }
                            playerState.displayGameResult = true;
                            if (winTeam == -1)
                                playerState.gameResult = "TIE";
                            else
                                playerState.gameResult = (playerState.teamIndex == winTeam) ? "VICTORY" : "DEFEAT";
                            playerState.displayScoreBoard = false;
                            playerState.displayGoal = false;
                        }

                        phase = Phase.Ended;
                        gameModeSystem.SetRespawnEnabled(false);
                        gameModeSystem.StartGameTimer(postMatchTime, "PostMatch");
                        GameDebug.Log(winTeam > -1 ? $"Match over. {gameModeSystem.teams[winTeam].name}" : "Match over. Its a tie !");
                        // var l = 0;
                        // if (winTeam > -1)
                        //     l = StringFormatter.Write(ref _msgBuf, 0, "Match over. {0} wins!", gameModeSystem.teams[winTeam].name);
                        // else
                        //     l = StringFormatter.Write(ref _msgBuf, 0, "Match over. Its a tie!");
                        // gameModeSystem.chatSystem.SendChatAnnouncement(new CharBufView(_msgBuf, l));
                    }
                    break;
                case Phase.Ended:
                    if (gameModeSystem.GetGameTimer() == 0)
                    {
                        for (int i = 0, c = players.Count; i < c; i++)
                        {
                            var playerState = players[i];
                            playerState.displayGameResult = false;
                        }
                        gameModeSystem.Restart();
                    }
                    break;
            }

            // Allow character switch if in team base
            for (int i = 0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.controlledEntity == null)
                    continue;

                var position = player.controlledEntity.transform.position;
                player.allowedCharacterSwitch = false;
                foreach (var teamBase in gameModeSystem.teamBases)
                {
                    if (teamBase.teamIndex == player.teamIndex)
                    {
                        var inside = (teamBase.boxCollider.transform.InverseTransformPoint(position) - teamBase.boxCollider.center);
                        if (Mathf.Abs(inside.x) < teamBase.boxCollider.size.x * 0.5f && Mathf.Abs(inside.y) < teamBase.boxCollider.size.y * 0.5f && Mathf.Abs(inside.z) < teamBase.boxCollider.size.z * 0.5f)
                        {
                            player.allowedCharacterSwitch = true;
                            break;
                        }
                    }
                }
            }
        }

        public void OnPlayerJoin(PlayerState player)
        {
            player.score = 0;
            gameModeSystem.AssignTeamBalanced(player);
        }

        public void OnPlayerKilled(PlayerState victim, PlayerState killer)
        {
            if (killer != null)
            {
                if (killer.teamIndex != victim.teamIndex)
                {
                    killer.score++;
                    gameModeSystem.teams[killer.teamIndex].score++;
                }
            }
        }

        public void OnPlayerRespawn(PlayerState player, ref Vector3 position, ref Quaternion rotation)
        {
            gameModeSystem.GetRandomSpawnTransform(player.teamIndex, ref position, ref rotation);
        }

        enum Phase
        {
            Undefined,
            Countdown,
            Active,
            Ended,
        }
        Phase phase;

        GameWorld world;
        GameModeSystemServer gameModeSystem;
    }
}
