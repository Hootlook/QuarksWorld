using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    [CreateAssetMenu(fileName = "AssetRegistryRoot", menuName = "QuarksWorld/Resource/AssetRegistryRoot", order = 10000)]
    public class AssetRegistryRoot : ScriptableObject
    {
        public bool serverBuild;
        public ScriptableObject[] assetRegistries;
    }
}

