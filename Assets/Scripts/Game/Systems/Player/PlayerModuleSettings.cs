using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [CreateAssetMenu(fileName = "PlayerModuleSettings", menuName = "QuarksWorld/Player/PlayerSystemSettings")]
    public class PlayerModuleSettings : ScriptableObject
    {
        public WeakAssetReference playerStatePrefab;
    }

}
