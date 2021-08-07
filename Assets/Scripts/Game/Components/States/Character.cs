using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [DisallowMultipleComponent]
    public class  Character : MonoBehaviour
    {
        public int heroTypeIndex;
        public float eyeHeight = 1.8f;
        public string characterName;
        public HeroTypeAsset heroTypeData;
        public int teamId = -1;
        public float altitude;
        public Collider groundCollider;
        public Vector3 groundNormal;
    }
}
