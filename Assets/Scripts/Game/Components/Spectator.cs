using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    [DisallowMultipleComponent]
    public class Spectator : NetworkBehaviour
    {
        [SyncVar] public Vector3 position;
        [SyncVar] public Quaternion rotation;
    }
}
