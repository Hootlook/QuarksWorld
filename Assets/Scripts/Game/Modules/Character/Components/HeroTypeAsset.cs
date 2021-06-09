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
        public float eyeHeight = 1.8f;
        public WeakAssetReference abilities;
        public CharacterDefinition character;
        public WeaponDefinition[] weapons;
    }
}
