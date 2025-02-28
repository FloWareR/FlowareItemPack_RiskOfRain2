using BepInEx;
using FlowareItemPack.Items;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace FlowareItemPack
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]

    public class FlowareItemPack : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Floware";
        public const string PluginName = "FlowareItemPack";
        public const string PluginVersion = "1.3.0";

        private readonly List<BaseItem> _items = new List<BaseItem>();

        public void Awake()
        {
            Log.Init(Logger);

            // Initialize all items
            _items.Add(new BoxOMatches());
            _items.Add(new DisperseObsidian());
            _items.Add(new OblivionRod());


            foreach (var item in _items)
            {
                item.Initialize();
                Debug.Log($"Initializing {item.ItemDef.name}");
            }
        }


        private void OnDestroy()
        {
            // Clean up all items
            foreach (var item in _items)
            {
                item.Unhook();
            }
        }
    }
}