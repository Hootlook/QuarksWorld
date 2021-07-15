using UnityEngine;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
#if UNITY_2018_3_OR_NEWER
using UnityEditor.Experimental.SceneManagement;
#endif
#endif

namespace QuarksWorld
{
    [DisallowMultipleComponent]
    public class ReplicatedEntity : MonoBehaviour
    {
        public WeakAssetReference assetGuid;    // Guid of asset this entity is created from
        public string assetId;                  // Guid of instance. Used for identifying replicated entities from the scene
        public uint id;

#if UNITY_EDITOR
        void OnValidate() => SetupIDs();

        void AssignAssetID(string path) => assetId = AssetDatabase.AssetPathToGUID(path);
        void AssignAssetID(GameObject prefab) => AssignAssetID(AssetDatabase.GetAssetPath(prefab));

        void SetupIDs()
        {
            if (Utils.IsPrefab(gameObject))
            {
                AssignAssetID(gameObject);
            }
            else if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                if (PrefabStageUtility.GetPrefabStage(gameObject) != null)
                {
                    AssignAssetID(PrefabStageUtility.GetCurrentPrefabStage().assetPath);
                }
            }
            else if (Utils.IsSceneObjectWithPrefabParent(gameObject, out GameObject prefab))
            {
                AssignAssetID(prefab);
            }
            else
            {
                if (!EditorApplication.isPlaying)
                {
                    assetId = "";
                }
            }
        }
#endif

        // #if UNITY_EDITOR
        //         public static Dictionary<string, ReplicatedEntity> netGuidMap = new Dictionary<string, ReplicatedEntity>();
        //         void Awake()
        //         {
        //             if (!EditorApplication.isPlaying)
        //                 SetUniqueNetID();
        //         }

        //         void OnValidate()
        //         {
        //             if (EditorApplication.isPlaying)
        //                 return;

        //             PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(this);
        //             if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Model)
        //             {
        //                 netID = null;
        //             }
        //             else
        //                 SetUniqueNetID();

        //             UpdateAssetGuid();
        //         }

        //         public bool SetAssetGUID(string guidStr)
        //         {
        //             var guid = new WeakAssetReference(guidStr);
        //             var currentGuid = assetGuid;
        //             if (!guid.Equals(currentGuid))
        //             {
        //                 assetGuid = guid;
        //                 PrefabUtility.SavePrefabAsset(gameObject);
        //                 return true;
        //             }

        //             return false;
        //         }

        //         public void UpdateAssetGuid()
        //         {
        //             var stage = PrefabStageUtility.GetPrefabStage(gameObject);
        //             if (stage != null)
        //             {
        //                 var guidStr = AssetDatabase.AssetPathToGUID(stage.assetPath);
        //                 if (SetAssetGUID(guidStr))
        //                     EditorSceneManager.MarkSceneDirty(stage.scene);
        //             }
        //         }

        //         void SetUniqueNetID()
        //         {
        //             // Generate new if fresh object
        //             if (netID == null || netID.Length == 0)
        //             {
        //                 var guid = System.Guid.NewGuid();
        //                 netID = guid.ToString();
        //                 EditorSceneManager.MarkSceneDirty(gameObject.scene);
        //             }

        //             // If we are the first add us
        //             if (!netGuidMap.ContainsKey(netID))
        //             {
        //                 netGuidMap[netID] = this;
        //                 return;
        //             }

        //             // Our guid is known and in use by another object??
        //             var oldReg = netGuidMap[netID];
        //             if (oldReg != null && oldReg.GetInstanceID() != GetInstanceID() && oldReg.netID == netID)
        //             {
        //                 // If actually *is* another ReplEnt that has our netID, *then* we give it up (usually happens because of copy / paste)
        //                 netID = System.Guid.NewGuid().ToString();
        //                 EditorSceneManager.MarkSceneDirty(gameObject.scene);
        //             }

        //             netGuidMap[netID] = this;
        //         }
        // #endif
    } 

    public struct ReplicatedData : IReplicates<ReplicatedEntity>
    {
        public int id;
        public int predictingPlayerId;

        public static IReplicatedSerializerFactory CreateSerializerFactory()
        {
            return new ReplicatedSerializerFactory<ReplicatedData>();
        }

        public void Serialize(ref SerializeContext context, NetworkWriter writer)
        {
            writer.Write(id);
            writer.Write(predictingPlayerId);
        }

        public void Deserialize(ref SerializeContext context, NetworkReader reader)
        {
            id = reader.ReadInt32();
            predictingPlayerId = reader.ReadInt32();
        }
    }
}
