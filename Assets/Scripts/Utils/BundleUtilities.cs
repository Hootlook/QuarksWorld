using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace QuarksWorld
{
    public static class SimpleBundleManager
    {
        [ConfigVar(Name = "resources.runtimebundlepath", DefaultValue = "AssetBundles", Description = "Asset bundle folder", Flags = ConfigVar.Flags.ServerInfo)]
        public static ConfigVar runtimeBundlePath;

        public static string assetBundleFolder = "AssetBundles";

        public static string GetRuntimeBundlePath()
        {
            if (Application.isEditor)
                return "AutoBuild/" + assetBundleFolder;
            else
                return runtimeBundlePath.Value;
        }

        public static AssetBundle LoadLevelAssetBundle(string name)
        {
            var bundlePathName = GetRuntimeBundlePath() + "/" + name;

            GameDebug.Log("loading : " + bundlePathName);

            var cacheKey = name.ToLower();

            if (!levelBundles.TryGetValue(cacheKey, out AssetBundle result))
            {
                result = AssetBundle.LoadFromFile(bundlePathName);
                if (result != null)
                    levelBundles.Add(cacheKey, result);
            }

            return result;
        }

        public static void ReleaseLevelAssetBundle(string name)
        {
            // TODO (petera) : Implement unloading of asset bundles. Ideally not by name.
        }

        static Dictionary<string, AssetBundle> levelBundles = new Dictionary<string, AssetBundle>();
    }
}
