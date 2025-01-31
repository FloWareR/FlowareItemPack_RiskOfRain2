using System.Reflection;
using UnityEngine;

namespace FlowareItemPack
{
    public class Assets
    {
        public Sprite icon;
        public GameObject prefab;
        
        public void PopulateAssets(string iconName, string prefabName)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowareItemPack.import"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                icon = bundle.LoadAsset<Sprite>($"assets/import/icons/{iconName}.png");
                prefab = bundle.LoadAsset<GameObject>($"assets/import/prefabs/{prefabName}.prefab");
            }
        }
    }
}
