using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace QuarksWorld
{
    public class Player : MonoBehaviour
    {
        float eyeHeight;

        public int id;
        public string playerName;
        public int teamIndex;
        public int score;
        public Entity controlledEntity;
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
        public float goalCompletion;

        // Non synchronized
        public bool allowedCharacterSwitch;
        public UserCommand command = UserCommand.defaultCommand;

        // Character control
        public int characterType = -1;
        public int requestedCharacterType = -1;
        public int requestedTeamIndex= -1;
    }
}
