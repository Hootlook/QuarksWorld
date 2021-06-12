using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    public class GameModeState : NetworkBehaviour
    {
        [SyncVar] public int gameTimerSeconds;
        [SyncVar] public string gameTimerMessage;
        [SyncVar] public string teamName0;
        [SyncVar] public string teamName1;
        [SyncVar] public int teamScore0;
        [SyncVar] public int teamScore1;
    }
}
