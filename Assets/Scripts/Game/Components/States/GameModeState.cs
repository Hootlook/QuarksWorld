using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using QuarksWorld.Systems;

namespace QuarksWorld
{
    public class GameModeState : NetworkBehaviour
    {
        [SyncVar] public int gameTimerSeconds;
        [SyncVar] public string gameTimerMessage;
        [SyncVar] public List<Team> teams;
    }
}
