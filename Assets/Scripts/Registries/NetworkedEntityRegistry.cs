using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QuarksWorld
{
    [CreateAssetMenu(fileName = "NetworkedEntityRegistry", menuName = "QuarksWorld/NetworkedEntity/NetworkedEntityRegistry")]
    public class NetworkedEntityRegistry : RegistryBase
    {
        [Serializable]
        public class Entry
        {
            public WeakAssetReference guid;
            // Each entry has either a asset reference or factory. Never both
            public WeakAssetReference prefab = new WeakAssetReference();
            public NetworkedEntityFactory factory;
        }

        public List<Entry> entries = new List<Entry>();

        public void LoadAllResources(BundledResourceManager resourceManager)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                resourceManager.GetSingleAssetResource(entries[i].guid);
            }
        }

        // public GameObject Create(GameWorld world, NetworkedEntity repEntity)
        // {
        //     var prefab = repEntity.gameObject;

        //     if (prefab == null)
        //     {
        //         GameDebug.LogError("Cant create. Not gameEntityType. GameEntityTypeDefinition:" + name);
        //         return null;
        //     }

        //     var gameObject = world.Spawn(prefab);
        //     gameObject.name = string.Format("{0}", prefab.name);

        //     return gameObject;
        // }

        public Entry GetEntry(WeakAssetReference guid)
        {
            var index = GetEntryIndex(guid);
            if (index != -1)
                return entries[index];
            return null;
        }

        public int GetEntryIndex(WeakAssetReference guid)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].guid.Equals(guid))
                {
                    return i;
                }
            }
            return -1;
        }


#if UNITY_EDITOR

        public override void PrepareForBuild()
        {
            //Debug.Log("NetworkedEntityRegistry");

            //entries.Clear();

            //var guids = AssetDatabase.FindAssets("t:GameObject");
            //foreach (var guid in guids)
            //{
            //    var path = AssetDatabase.GUIDToAssetPath(guid);
            //    var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            //    var replicated = go.GetComponent<NetworkedEntity>();
            //    if (replicated == null)
            //        continue;   

            //    replicated.SetAssetGUID(guid);

            //    Debug.Log("   Adding guid:" + guid + " prefab:" + path);

            //    var guidData = new WeakAssetReference(guid);
            //    entries.Add(new Entry
            //    {
            //        guid = guidData,
            //        prefab = new WeakAssetReference(guid)
            //    });
            //}

            //guids = AssetDatabase.FindAssets("t:NetworkedEntityFactory");
            //foreach (var guid in guids)
            //{
            //    var path = AssetDatabase.GUIDToAssetPath(guid);
            //    var factory = AssetDatabase.LoadAssetAtPath<NetworkedEntityFactory>(path);

            //    factory.SetAssetGUID(guid);

            //    Debug.Log("   Adding guid:" + guid + " factory:" + factory);

            //    var guidData = new WeakAssetReference(guid);
            //    entries.Add(new Entry
            //    {
            //        guid = guidData,
            //        factory = factory
            //    });
            //}

            //EditorUtility.SetDirty(this);
        }

        public static NetworkedEntityRegistry GetNetworkedEntityRegistry()
        {
            var registryGuids = AssetDatabase.FindAssets("t:NetworkedEntityRegistry");
            if (registryGuids == null || registryGuids.Length == 0)
            {
                GameDebug.LogError("Failed to find NetworkedEntityRegistry");
                return null;
            }
            if (registryGuids.Length > 1)
            {
                GameDebug.LogError("There should only be one NetworkedEntityRegistry in project");
                return null;
            }

            var guid = registryGuids[0];
            var registryPath = AssetDatabase.GUIDToAssetPath(guid);
            var registry = AssetDatabase.LoadAssetAtPath<NetworkedEntityRegistry>(registryPath);
            return registry;
        }

        public override void GetSingleAssetGUIDs(List<string> guids, bool serverBuild)
        {
            foreach (var entry in entries)
            {
                if (entry.factory != null)
                {
                    var factoryPath = AssetDatabase.GetAssetPath(entry.factory);
                    var factoryGuid = AssetDatabase.AssetPathToGUID(factoryPath);
                    guids.Add(factoryGuid);

                    continue;
                }


                if (!entry.prefab.IsSet())
                    continue;

                guids.Add(entry.prefab.GetGuidStr());
            }
        }

        public override bool Verify()
        {
            var verified = true;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (!entry.prefab.IsSet())
                {
                    Debug.Log("Entry:" + i + " is free");
                    continue;
                }

                if (entry.prefab.IsSet() && entry.factory != null)
                {
                    Debug.Log("Entry:" + i + " registered with both prefab and factory");
                    verified = false;
                    continue;
                }

                if (entry.factory != null)
                {
                    var factoryPath = AssetDatabase.GetAssetPath(entry.factory);
                    if (factoryPath == null || factoryPath == "")
                    {
                        Debug.Log("Cant find path for factory:" + entry.factory);
                        verified = false;
                    }

                    if (!verified)
                        continue;
                }
                else
                {
                    var p = AssetDatabase.GUIDToAssetPath(entry.prefab.GetGuidStr());
                    if (p == null || p == "")
                    {
                        Debug.Log("Cant find path for guid:" + entry.prefab.GetGuidStr());
                        verified = false;
                        continue;
                    }

                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(p);
                    if (go == null)
                    {
                        Debug.Log("Cant load asset for guid:" + entry.prefab.GetGuidStr() + " path:" + p);
                        verified = false;
                        continue;
                    }

                    // var repEntity = go.GetComponent<NetworkedEntity>();
                    // if (repEntity == null)
                    // {
                    //     Debug.Log(go + " has no GameEntityType component");
                    //     verified = false;
                    //     continue;
                    // }
                }
            }

            return verified;
        }
#endif
    }
}
