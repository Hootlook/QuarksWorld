using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "QuarksWorld/Weapon/WeaponDefinition")]
    public class WeaponDefinition : ScriptableObject
    {
        public WeakAssetReference prefabServer;
        public WeakAssetReference prefabClient;
        public WeakAssetReference prefab1P;
    }
}
