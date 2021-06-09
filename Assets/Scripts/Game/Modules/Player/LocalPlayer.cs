using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class LocalPlayer : MonoBehaviour
    {
        public int playerId = -1;

        public PlayerState playerState;
        public GameObject controlledEntity;
        public UserCommand command = UserCommand.defaultCommand;

        [System.NonSerialized] public float m_debugMoveDuration;
        [System.NonSerialized] public float m_debugMovePhaseDuration;
        [System.NonSerialized] public float m_debugMoveTurnSpeed;
        [System.NonSerialized] public float m_debugMoveMag;
    }
}
