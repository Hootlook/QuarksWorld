using System.Collections.Generic;
using UnityEngine;

using QuarksWorld.Systems;

namespace QuarksWorld
{
    public class GameModeState : MonoBehaviour
    {
        public int gameTimerSeconds;
        public string gameTimerMessage;
        public List<Team> teams;
    }
}
