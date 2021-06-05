using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class PlayerState : NetworkBehaviour
    {
        [SyncVar]
        public int playerId;
        [SyncVar]
        public string playerName;
        [SyncVar] 
        public int teamIndex;
        [SyncVar] 
        public int score;
        [SyncVar] 
        public GameObject controlledEntity;
        [SyncVar] 
        public bool gameModeSystemInitialized;

        // These are only sync'hed to owning client
        public bool displayScoreBoard;
        public bool displayGameScore;
        public bool displayGameResult;
        public string gameResult;

        public bool displayGoal;
        public Vector3 goalPosition;
        public uint goalDefendersColor;
        public uint goalAttackersColor;
        public uint goalAttackers;
        public uint goalDefenders;
        public string goalString;
        public string actionString;
        public float goalCompletion;
    }
}
