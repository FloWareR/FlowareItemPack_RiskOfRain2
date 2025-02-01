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
            var assets = new Assets();
            assets.PopulateAssets("matches", "matches");

            ItemDef.name = "Matches";
            ItemDef.nameToken = "FLOWARE_MATCHES_NAME";
            ItemDef.pickupToken = "FLOWARE_MATCHES_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_MATCHES_DESC";
            ItemDef.loreToken = "FLOWARE_MATCHES_LORE";
            ItemDef.tags = new ItemTag[] { ItemTag.Damage };

            LanguageAPI.Add("FLOWARE_MATCHES_NAME", "Box O' Matches");
            LanguageAPI.Add("FLOWARE_MATCHES_PICKUP", "Light em' up, break em' down.");
            LanguageAPI.Add("FLOWARE_MATCHES_DESC", "5% chance (+5% per stack) to set enemies on fire for 2 seconds (+0.5s per stack), dealing 10% base damage.");
            LanguageAPI.Add("FLOWARE_MATCHES_LORE", "An old, half-empty box of matches. Each one a spark waiting to ignite chaos.");

            // Set item tier to Void Tier 1
            ItemDef.tier = ItemTier.VoidTier1;

            // Load the Void Tier 1 definition
#pragma warning disable Publicizer001
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            // Debugging: Verify the tier and tier def
            Debug.Log($"Item Tier: {ItemDef.tier}");
            Debug.Log($"Item Tier Def: {ItemDef._itemTierDef?.name}");

            // Assign assets
            ItemDef.pickupIconSprite = assets.icon;
            ItemDef.pickupModelPrefab = assets.prefab;

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
            GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
        }

        public override void Unhook()
        {
            GlobalEventManager.onServerDamageDealt -= OnServerDamageDealt;
        }

        private void OnServerDamageDealt(DamageReport report)
        {
            if (!report.attackerBody || !report.victimBody)
            {
                return;
            }
            if ((report.damageInfo.damageType & DamageType.DoT) != 0) return;

            var attacker = report.attackerBody;
            var inventory = attacker.inventory;
            if (!inventory) return;

            var itemCount = inventory.GetItemCount(ItemDef.itemIndex);
            var victim = report.victimBody;
            var igniteTankItemDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("StrengthenBurn"));

            if (itemCount > 0 && Util.CheckRoll(5f * itemCount, attacker.master)) // 5% chance to ignite (+5% per stack)
            {
                var burnDuration = 2f + 0.5f * (itemCount - 1); // Burn for 2s + 0.5s per stack
                var burnDamage = report.attackerBody.baseDamage * 0.1f; // Burn does 10% of base damage per tick
                var igniteTankCount = inventory.GetItemCount(igniteTankItemDef.itemIndex);
                var debuffToApply = igniteTankCount > 0 ? DotController.DotIndex.StrongerBurn : DotController.DotIndex.Burn;
                var dotController = DotController.FindDotController(victim.gameObject);

                if (dotController != null && dotController.HasDotActive(debuffToApply)) return;

                if (debuffToApply == DotController.DotIndex.StrongerBurn)
                {
                    burnDamage *= 4 * igniteTankCount;
                }

                DotController.InflictDot(
                    report.victimBody.gameObject, // Target
                    report.attackerBody.gameObject, // Attacker
                    debuffToApply, // Burn effect
                    burnDuration, // Duration
                    burnDamage // Damage per tick
                );
            }
        }
    }
}