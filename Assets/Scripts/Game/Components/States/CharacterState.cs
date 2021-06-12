using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace QuarksWorld
{
    [RequireComponent(typeof(HealthState))]
    [DisallowMultipleComponent]
    public class CharacterState : NetworkBehaviour
    {
        [NonSerialized] [SyncVar] public int heroTypeIndex;

        [NonSerialized] public float eyeHeight = 1.8f;
        [NonSerialized] public string characterName;
        [NonSerialized] public HeroTypeAsset heroTypeData;
        [NonSerialized] public int teamId = -1;
        [NonSerialized] public float altitude;
        [NonSerialized] public Collider groundCollider;
        [NonSerialized] public Vector3 groundNormal;
    }
}
