using UnityEngine;
using System.IO;
using System.Reflection;
using UnhollowerRuntimeLib;

namespace WorldCleanup
{
    class Assets
    {
        private static GameObject s_Parent;
        public static void Initialize()
        {
            s_Parent = new GameObject("MyPreloadedContents");
            UnityEngine.Object.DontDestroyOnLoad(s_Parent);
            s_Parent.SetActive(false);
            s_Parent.transform.SetParent(QuickMenu.prop_QuickMenu_0.transform);
        }

        public static AssetBundle LoadFromAssembly(string path)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path))
            using (var tempStream = new MemoryStream((int)stream.Length))
            {
                stream.CopyTo(tempStream);

                var bundle = AssetBundle.LoadFromMemory_Internal(tempStream.ToArray(), 0);
                bundle.hideFlags |= HideFlags.DontUnloadUnusedAsset;
                return bundle;
            }
        }
        public static GameObject LoadGameObject(AssetBundle bundle, string str)
        {
            var objectFromBundle = bundle.LoadAsset_Internal(str, Il2CppType.Of<GameObject>()).Cast<GameObject>();
            var newObject = UnityEngine.Object.Instantiate(objectFromBundle, s_Parent.transform);
            newObject.SetActive(true);
            newObject.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return newObject;
        }
    }
}
