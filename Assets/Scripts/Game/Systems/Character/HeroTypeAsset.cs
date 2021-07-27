using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [CreateAssetMenu(fileName = "HeroType", menuName = "QuarksWorld/Hero/HeroType")]
    public class HeroTypeAsset : ScriptableObject
    {
        public float health = 100;
        public float eyeHeight = 1.75f;
        public WeakAssetReference abilities;
        public WeakAssetReference character;
        public WeaponDefinition[] weapons;
    }
}
