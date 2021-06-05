using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QuarksWorld
{
    public class BundledResourceManager
    {
        [ConfigVar(Name = "resources.forcebundles", DefaultValue = "0", Description = "Force use of bundles even in editor")]
        public static ConfigVar forceBundles;

        [ConfigVar(Name = "resources.verbose", DefaultValue = "0", Description = "Verbose logging about resources")]
        public static ConfigVar verbose;

        private string GetBundlePath()
        {
            string bundlePath = SimpleBundleManager.GetRuntimeBundlePath();
            return bundlePath;
        }

        public BundledResourceManager(GameWorld world, string registryName)
        {
            bool useBundles = !Application.isEditor || forceBundles.IntValue > 0;

            this.world = world;

#if UNITY_EDITOR
            if (!useBundles)
            {
                string assetPath = "Assets/" + registryName + ".asset";
                assetRegistryRoot = AssetDatabase.LoadAssetAtPath<AssetRegistryRoot>(assetPath);
                if (verbose.IntValue > 0)
                    GameDebug.Log("resource: loading resource: " + assetPath);
            }
#endif

            if (useBundles)
            {
                var bundlePath = GetBundlePath();
                var assetPath = bundlePath + "/" + registryName;

                if (verbose.IntValue > 0)
                    GameDebug.Log("resource: loading bundle (" + assetPath + ")");
                assetRegistryRootBundle = AssetBundle.LoadFromFile(assetPath);

                var registryRoots = assetRegistryRootBundle.LoadAllAssets<AssetRegistryRoot>();

                if (registryRoots.Length == 1)
                    assetRegistryRoot = registryRoots[0];
                else
                    GameDebug.LogError("Wrong number(" + registryRoots.Length + ") of registry roots in " + registryName);
            }

            // Update asset registry map
            if (assetRegistryRoot != null)
            {
                foreach (var registry in assetRegistryRoot.assetRegistries)
                {
                    if (registry == null)
                    {
                        continue;
                    }

                    System.Type type = registry.GetType();
                    assetRegistryMap.Add(type, registry);
                }
                assetResourceFolder = registryName + "_Assets";
            }
        }

        public void Shutdown()
        {
            foreach (var bundle in resources.Values)
            {
                // If we are in editor we may not have loaded these as bundles
                if (bundle.bundle != null)
                    bundle.bundle.Unload(false);
            }

            if (assetRegistryRootBundle != null)
                assetRegistryRootBundle.Unload(false);

            assetRegistryRoot = null;
            assetRegistryRootBundle = null;
            assetRegistryMap.Clear();
            assetResourceFolder = "";
            resources.Clear();
        }

        public T GetResourceRegistry<T>() where T : ScriptableObject
        {
            assetRegistryMap.TryGetValue(typeof(T), out ScriptableObject result);
            return (T)result;
        }

        
        public GameObject CreateEntity(Vector3 position, System.Guid assetId)
        {
            if (assetId == null)
            {
                GameDebug.LogError("Guid invalid");
                return null;
            }

            var reference = new WeakAssetReference(assetId.ToString());
            return CreateEntity(reference);
        }

        public GameObject CreateEntity(string guid)
        {
            if (guid == null || guid == "")
            {
                GameDebug.LogError("Guid invalid");
                return null;
            }

            var reference = new WeakAssetReference(guid);
            return CreateEntity(reference);
        }

        public GameObject CreateEntity(WeakAssetReference assetGuid)
        {
            var resource = GetSingleAssetResource(assetGuid);
            if (resource == null)
                return null;

            var prefab = resource as GameObject;
            if (prefab != null)
            {
                var gameObject = world.Spawn(prefab);
                return gameObject;
            }

            if (resource is NetworkedEntityFactory factory)
            {
                return factory.Create(this, world);
            }

            return null;
        }

        public Object GetSingleAssetResource(WeakAssetReference reference)
        {
            var def = new SingleResourceBundle();

            if (resources.TryGetValue(reference, out def))
            {
                return def.asset;
            }

            def = new SingleResourceBundle();
            var useBundles = !Application.isEditor || forceBundles.IntValue > 0;

            var guidStr = reference.GetGuidStr();

#if UNITY_EDITOR
            if (!useBundles)
            {
                var path = AssetDatabase.GUIDToAssetPath(guidStr);

                def.asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));

                if (def.asset == null)
                    GameDebug.LogWarning("Failed to load resource " + guidStr + " at " + path);
                if (verbose.IntValue > 0)
                    GameDebug.Log("resource: loading non-bundled asset " + path + "(" + guidStr + ")");
            }
#endif
            if (useBundles)
            {
                var bundlePath = GetBundlePath();
                def.bundle = AssetBundle.LoadFromFile(bundlePath + "/" + assetResourceFolder + "/" + guidStr);
                if (verbose.IntValue > 0)
                    GameDebug.Log("resource: loading bundled asset: " + assetResourceFolder + "/" + guidStr);
                var handles = def.bundle.LoadAllAssets();
                if (handles.Length > 0)
                    def.asset = handles[0];
                else
                    GameDebug.LogWarning("Failed to load resource " + guidStr);
            }

            resources.Add(reference, def);
            return def.asset;
        }

        class SingleResourceBundle
        {
            public AssetBundle bundle;
            public Object asset;
        }

        GameWorld world;
        AssetRegistryRoot assetRegistryRoot;
        AssetBundle assetRegistryRootBundle;
        Dictionary<System.Type, ScriptableObject> assetRegistryMap = new Dictionary<System.Type, ScriptableObject>();

        string assetResourceFolder = "";

        Dictionary<WeakAssetReference, SingleResourceBundle> resources = new Dictionary<WeakAssetReference, SingleResourceBundle>();
    }
}
