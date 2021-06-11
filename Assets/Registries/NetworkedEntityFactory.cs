using UnityEditor;
using UnityEngine;

namespace QuarksWorld
{
    public abstract class NetworkedEntityFactory : ScriptableObject
    {
        [HideInInspector]
        public WeakAssetReference guid;

        public abstract GameObject Create(BundledResourceManager resourceManager, GameWorld world);

#if UNITY_EDITOR

        private void OnValidate() => UpdateAssetGuid();

        public void SetAssetGUID(string guidStr)
        {
            var assetGuid = new WeakAssetReference(guidStr);
            if (!assetGuid.Equals(guid))
            {
                guid = assetGuid;
                EditorUtility.SetDirty(this);
            }
        }

        public void UpdateAssetGuid()
        {
            var path = AssetDatabase.GetAssetPath(this);
            if (path != null && path != "")
            {
                var guidStr = AssetDatabase.AssetPathToGUID(path);
                SetAssetGUID(guidStr);
            }
        }
#endif
    }

#if UNITY_EDITOR
    public class NetworkedEntityFactoryEditor<T> : Editor
        where T : NetworkedEntityFactory
    {
        public override void OnInspectorGUI()
        {
            var factory = target as T;
            GUILayout.Label("GUID:" + factory.guid.GetGuidStr());
        }
    }
#endif
}
