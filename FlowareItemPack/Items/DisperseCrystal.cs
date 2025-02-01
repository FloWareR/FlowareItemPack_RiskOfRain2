﻿using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace FlowareItemPack.Items
{
    internal class DisperseCrystal : BaseItem
    {
        public override ItemDef ItemDef { get; } = ScriptableObject.CreateInstance<ItemDef>();
        private GameObject visualEffectPrefab;
        private GameObject effect;

        public override void Initialize()
        {
            var assets = new Assets();
            assets.PopulateAssets("matches", "matches");

            ItemDef.name = "disperse_crystal";
            ItemDef.nameToken = "FLOWARE_DISPERSECRYSTAL_NAME";
            ItemDef.pickupToken = "FLOWARE_DISPERSECRYSTAL_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_DISPERSECRYSTAL_DESC";
            ItemDef.loreToken = "FLOWARE_DISPERSECRYSTA_LORE";

            LanguageAPI.Add("FLOWARE_DISPERSECRYSTAL_NAME", "Disperse Crystal");
            LanguageAPI.Add("FLOWARE_DISPERSECRYSTAL_PICKUP", "Deal bonus damage to enemies at long distance corrupts all Focus Crystals");
            LanguageAPI.Add("FLOWARE_DISPERSECRYSTAL_DESC", "Grants a damage boost to enemies further away. Displays a pulsating effect around the player.");
            LanguageAPI.Add("FLOWARE_DISPERSECRYSTA_LORE", "A shard that resonates with the distant echoes of battle.");

            ItemDef.tier = ItemTier.VoidTier1;
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();

            ItemDef.pickupIconSprite = assets.icon;
            ItemDef.pickupModelPrefab = assets.prefab;
            ItemDef.tags = new ItemTag[] { };
            ItemDef.canRemove = true;
            ItemDef.hidden = false;

            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(ItemDef, displayRules));

            visualEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Items/NearbyDamageBonus/DiamondDamageBonusEffect.prefab").WaitForCompletion();
            if (visualEffectPrefab != null) Debug.LogWarning("NULLOS WEEWEE");
            Hook();
        }

        public override void Hook()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += OnInventoryChanged;
            On.RoR2.HealthComponent.TakeDamage += OnDamageDealt;
        }

        public override void Unhook()
        {
            On.RoR2.CharacterBody.OnInventoryChanged -= OnInventoryChanged;
            On.RoR2.HealthComponent.TakeDamage -= OnDamageDealt;
        }

        private void OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (!self.inventory) return;

            int itemCount = self.inventory.GetItemCount(ItemDef.itemIndex);
            if (itemCount <= 0) return;
            var focusCrystalCount = self.inventory.GetItemCount(ItemCatalog.FindItemIndex("NearbyDamageBonus"));
            if (focusCrystalCount > 0)
            {
                self.inventory.RemoveItem(ItemCatalog.FindItemIndex("NearbyDamageBonus"), focusCrystalCount);
                self.inventory.GiveItem(ItemDef.itemIndex, focusCrystalCount);
                Debug.Log($"Transformed {focusCrystalCount} Focus Crystals into Disperse Crystals.");
            }
        }


        private void OnDamageDealt(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (!NetworkServer.active)
            {
                orig(self, damageInfo);
                return;
            }

            if (damageInfo.attacker == null)
            {
                orig(self, damageInfo);
                return;
            }

            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody == null)
            {
                orig(self, damageInfo);
                return;
            }

            var inventory = attackerBody.inventory;
            if (inventory == null)
            {
                orig(self, damageInfo);
                return;
            }

            var itemCount = inventory.GetItemCount(ItemDef.itemIndex);
            if (itemCount <= 0)
            {
                orig(self, damageInfo);
                return;
            }

            var distance = Vector3.Distance(attackerBody.transform.position, self.transform.position);
            if (distance > 31f)
            {
                damageInfo.damage *= 1 + (0.25f * itemCount);
                damageInfo.damageColorIndex = DamageColorIndex.Void;
            }

            orig(self, damageInfo);
        }
    }
}
