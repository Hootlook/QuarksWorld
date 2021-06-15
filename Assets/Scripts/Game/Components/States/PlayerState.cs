using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    [DisallowMultipleComponent]
    public class PlayerState : NetworkBehaviour
    {
        [SyncVar] public int id;
        [SyncVar] public string playerName;
        [SyncVar] public int teamIndex;
        [SyncVar] public int score;
        [SyncVar] public GameObject controlledEntity;
        [SyncVar] public bool gameModeSystemInitialized;

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

        public static bool ResolveLocalPlayer(ref PlayerState assignTo)
        {
            var localPlayer = PlayerState.List.Find(p => p.isLocalPlayer == true);

            if (localPlayer != null)
            {
                assignTo = localPlayer;
                return true;
            }

            return false;
        }

        public static PlayerState localPlayerState;

        void Start() => List.Add(this); 

        void OnDestroy() => List.Remove(this); 

        public override void OnStartAuthority() => localPlayerState = this;
        
        public override void OnStopAuthority() => localPlayerState = null;
        
        public static List<PlayerState> List = new List<PlayerState>();
    }
}
