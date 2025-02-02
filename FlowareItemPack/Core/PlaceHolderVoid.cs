using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FlowareItemPack.Items
{
    public class PlaceHolderVoid : BaseItem
    {
        public override ItemDef ItemDef { get; } = ScriptableObject.CreateInstance<ItemDef>();

        public override void Initialize()
        {
            var assets = new Core.Assets();
            assets.PopulateAssets("matches", "matches");

            ItemDef.name = "void_matches";
            ItemDef.nameToken = "FLOWARE_VOIDMATCHES_NAME";
            ItemDef.pickupToken = "FLOWARE_VOIDMATCHES_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_VOIDMATCHES_DESC";
            ItemDef.loreToken = "FLOWARE_VOIDMATCHES_LORE";

            LanguageAPI.Add("FLOWARE_VOIDMATCHES_NAME", "");
            LanguageAPI.Add("FLOWARE_VOIDMATCHES_PICKUP", "");
            LanguageAPI.Add("FLOWARE_VOIDMATCHES_DESC", "");
            LanguageAPI.Add("FLOWARE_VOIDMATCHES_LORE", "");

            // Set item tier to Void Tier 1
            ItemDef.tier = ItemTier.VoidTier1;

            // Load the Void Tier 1 definition
#pragma warning disable Publicizer001
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            // Assign assets
            ItemDef.pickupIconSprite = assets.icon;
            ItemDef.pickupModelPrefab = assets.prefab;
            ItemDef.tags = new ItemTag[] { };

            // Item settings
            ItemDef.canRemove = true;
            ItemDef.hidden = false;

            // Register item with R2API
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(ItemDef, displayRules));

            // Register the item with the Void item pool
            if (ItemTierCatalog.availability.available)
            {
                ItemTierDef voidTierDef = ItemTierCatalog.GetItemTierDef(ItemTier.VoidTier1);
                if (voidTierDef != null)
                {
                    voidTierDef.dropletDisplayPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/VoidItemPickup.prefab").WaitForCompletion();
                }
            }

            // Hook into events
            Hook();
        }

        public override void Hook()
        {

        }

        public override void Unhook()
        {

        }

        private void OnServerDamageDealt(DamageReport report)
        {
          

        }
    }
}