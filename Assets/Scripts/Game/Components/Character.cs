using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public class  Character : MonoBehaviour
    {
        public string characterName;
        public float eyeHeight = 1.8f;
        public int heroTypeIndex;
        public HeroTypeAsset heroTypeData;
        public int teamId = -1;
    }
}
