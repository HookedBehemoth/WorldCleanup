using UnityEngine;
using System.IO;
using System.Reflection;

namespace WorldCleanup {
    class Assets {
        private static GameObject s_Parent;
        private static AssetBundle s_AssetBundle;

        public static void Initialize() {
            s_Parent = new GameObject("MyPreloadedContents");
            Object.DontDestroyOnLoad(s_Parent);
            s_Parent.SetActive(false);

            s_Parent.transform.SetParent(QuickMenu.prop_QuickMenu_0.transform);

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("WorldCleanup.mod.assetbundle"))
            using (var tempStream = new MemoryStream((int)stream.Length)) {
                stream.CopyTo(tempStream);

                s_AssetBundle = AssetBundle.LoadFromMemory(tempStream.ToArray());
                s_AssetBundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            }
        }

        public static GameObject LoadGameObject(string str) {
            var objectFromBundle = s_AssetBundle.LoadAsset<GameObject>(str);
            var newObject = Object.Instantiate(objectFromBundle, s_Parent.transform);
            newObject.SetActive(true);
            newObject.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return newObject;
        }
    }
}
