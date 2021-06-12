using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QuarksWorld
{

    [CreateAssetMenu(menuName = "QuarksWorld/Hero/HeroTypeRegistry", fileName = "HeroTypeRegistry")]
    public class HeroTypeRegistry : RegistryBase
    {
        public List<HeroTypeAsset> entries = new List<HeroTypeAsset>();

#if UNITY_EDITOR

        public override void PrepareForBuild()
        {
            Debug.Log("HeroTypeRegistry");

            entries.Clear();
            var guids = AssetDatabase.FindAssets("t:HeroTypeAsset");
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<HeroTypeAsset>(path);
                Debug.Log("   Adding definition:" + definition);
                entries.Add(definition);
            }

            EditorUtility.SetDirty(this);
        }

        public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
        {
            foreach (var setup in entries)
            {
                foreach (var item in setup.weapons)
                {
                    if (serverBuild && item.prefabServer.IsSet())
                        guids.Add(item.prefabServer.GetGuidStr());
                    if (!serverBuild && item.prefabClient.IsSet())
                        guids.Add(item.prefabClient.GetGuidStr());
                    if (!serverBuild && item.prefab1P.IsSet())
                        guids.Add(item.prefab1P.GetGuidStr());
                }
            }
        }
#endif
    }
}
