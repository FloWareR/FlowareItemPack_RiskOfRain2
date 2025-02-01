using BepInEx;
using FlowareItemPack.Items;
using R2API;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace FlowareItemPack
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class FlowareItemPack : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Floware";
        public const string PluginName = "FlowareItemPack";
        public const string PluginVersion = "1.1.4";

        private readonly List<BaseItem> _items = new List<BaseItem>();

        public void Awake()
        {
            Log.Init(Logger);

            _items.Add(new BoxOMatches());

            foreach (var item in _items)
            {
                item.Initialize();
            }
        }

        private void OnDestroy()
        {
            foreach (var item in _items)
            {
                item.Unhook();
            }
        }
    }
}