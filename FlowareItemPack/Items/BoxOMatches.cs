using R2API;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace FlowareItemPack.Items
{
    public class BoxOMatches : BaseItem
    {
        public override ItemDef ItemDef { get; } = ScriptableObject.CreateInstance<ItemDef>();

        public override void Initialize()
        {
            var assets = new Core.Assets();
            assets.PopulateAssets("matches", "matches");

            ItemDef.name = "Matches";
            ItemDef.nameToken = "FLOWARE_MATCHES_NAME";
            ItemDef.pickupToken = "FLOWARE_MATCHES_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_MATCHES_DESC";
            ItemDef.loreToken = "FLOWARE_MATCHES_LORE";

            LanguageAPI.Add("FLOWARE_MATCHES_NAME", "Box O' Matches");
            LanguageAPI.Add("FLOWARE_MATCHES_PICKUP", "Light em' up, break em' down.");
            LanguageAPI.Add("FLOWARE_MATCHES_DESC", "5% chance (+5% per stack) to set enemies on fire for 2 seconds (+0.5s per stack), dealing 10% base damage.");
            LanguageAPI.Add("FLOWARE_MATCHES_LORE", "An old, half-empty box of matches. Each one a spark waiting to ignite chaos.");

            // Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow, NoTier
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001

            //myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            //myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            ItemDef.pickupIconSprite = assets.icon;
            ItemDef.pickupModelPrefab = assets.prefab;
            ItemDef.tags = new ItemTag[] { ItemTag.Damage, ItemTag.HalcyoniteShrine };

            ItemDef.canRemove = true;
            ItemDef.hidden = false;
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(ItemDef, displayRules));

            Hook();
        }
        public override void Hook()
        {
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
        }

        public override void Unhook()
        {
            GlobalEventManager.onServerDamageDealt -= GlobalEventManager_onServerDamageDealt;
        }


        private void GlobalEventManager_onServerDamageDealt(DamageReport report)
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
                var burnDuration = 2f * itemCount; // Burn for 2s + .5s per stack
                var burnDamage = (report.attackerBody.baseDamage * 0.1f); // Burn does 10% of base damage per tick
                var igniteTankCount = inventory.GetItemCount(igniteTankItemDef.itemIndex);
                var debuffToApply = igniteTankCount > 0 ? DotController.DotIndex.StrongerBurn : DotController.DotIndex.Burn;
                var dotController = DotController.FindDotController(victim.gameObject);

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
