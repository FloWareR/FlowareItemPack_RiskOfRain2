using System.IO;
using System.Reflection;
using UnityEngine;

namespace FlowareItemPack
{
    public class Assets
    {
        public Sprite icon;
        public GameObject prefab;

        public static AssetBundle _assetBundle;


        public void PopulateAssets(string iconName, string prefabName)
        {
            if (_assetBundle == null)
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowareItemPack.import"))
                {
                    _assetBundle = AssetBundle.LoadFromStream(stream);
                }
            }


            icon = _assetBundle.LoadAsset<Sprite>($"assets/import/icons/{iconName}.png");
            prefab = _assetBundle.LoadAsset<GameObject>($"assets/import/prefabs/{prefabName}.prefab");
        }
    }
}
