using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [CreateAssetMenu(fileName = "CharacterDefinition", menuName = "QuarksWorld/Character/CharacterDefinition")]
    public class CharacterDefinition : ScriptableObject
    {
        public WeakAssetReference prefabServer;
        public WeakAssetReference prefabClient;
        public WeakAssetReference prefab1P;
    }
}
