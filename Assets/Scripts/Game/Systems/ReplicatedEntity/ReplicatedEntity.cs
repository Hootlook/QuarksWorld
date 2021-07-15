using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;
#endif
using Mirror;

namespace QuarksWorld.Old
{
    [ExecuteAlways, DisallowMultipleComponent]
    public class ReplicatedEntity : ReplicatedBehavior<ReplicatedEntityData>
    {
        public WeakAssetReference assetGuid;    // Guid of asset this entity is created from
        public string netID;                    // Guid of instance. Used for identifying replicated entities from the scene

        private void Awake()
        {
            state.id = -1;
            state.predictingPlayerId = -1;
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                SetUniqueNetID();
#endif
        }

#if UNITY_EDITOR
        public static Dictionary<string, ReplicatedEntity> netGuidMap = new Dictionary<string, ReplicatedEntity>();

        private void OnValidate()
        {
            if (EditorApplication.isPlaying)
                return;

            PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(this);
            if (prefabType == PrefabAssetType.Regular || prefabType == PrefabAssetType.Model)
            {
                netID = null;
            }
            else
                SetUniqueNetID();

            UpdateAssetGuid();
        }

        public bool SetAssetGUID(string guidStr)
        {
            var guid = new WeakAssetReference(guidStr);
            var currentGuid = assetGuid;
            if (!guid.Equals(currentGuid))
            {
                assetGuid = guid;
                PrefabUtility.SavePrefabAsset(gameObject);
                return true;
            }

            return false;
        }

        public void UpdateAssetGuid()
        {
            // Set type guid
            var stage = PrefabStageUtility.GetPrefabStage(gameObject);
            if (stage != null)
            {
                var guidStr = AssetDatabase.AssetPathToGUID(stage.assetPath);
                if (SetAssetGUID(guidStr))
                    EditorSceneManager.MarkSceneDirty(stage.scene);
            }
        }

        private void SetUniqueNetID()
        {
            // Generate new if fresh object
            if (netID == null || netID.Length == 0)
            {
                var guid = System.Guid.NewGuid();
                netID = guid.ToString();
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            // If we are the first add us
            if (!netGuidMap.ContainsKey(netID))
            {
                netGuidMap[netID] = this;
                return;
            }

            // Our guid is known and in use by another object??
            var oldReg = netGuidMap[netID];
            if (oldReg != null && oldReg.GetInstanceID() != GetInstanceID() && oldReg.netID == netID)
            {
                // If actually *is* another ReplEnt that has our netID, *then* we give it up (usually happens because of copy / paste)
                netID = System.Guid.NewGuid().ToString();
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }

            netGuidMap[netID] = this;
        }
#endif
    }

    public struct ReplicatedEntityData : IReplicatedComponent, IComponentBase
    {
        public int id;
        public int predictingPlayerId;

        public static IReplicatedBehaviorFactory CreateSerializerFactory()
        {
            return new ReplicatedSerializerFactory<ReplicatedEntityData>();
        }

        public void Serialize(ref SerializeContext context, NetworkWriter writer)
        {
            writer.WriteInt32(predictingPlayerId);
        }

        public void Deserialize(ref SerializeContext context, NetworkReader reader)
        {
            predictingPlayerId = reader.ReadInt32();
        }
    }
}

