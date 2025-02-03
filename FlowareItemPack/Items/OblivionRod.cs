using HarmonyLib;
using On.RoR2.Items;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using System.Collections.Generic;
using System.Collections;

namespace FlowareItemPack.Items
{
    internal class OblivionRod : BaseItem
    {
        public override ItemDef ItemDef { get; } = ScriptableObject.CreateInstance<ItemDef>();
        private static readonly float maxAllowedTime = 1.5f;  // Max time before applying damage
        private static readonly Dictionary<HealthComponent, EnemyState> enemyStates = new Dictionary<HealthComponent, EnemyState>();

        private class EnemyState
        {
            public float accumulatedDamage; // Damage to apply after the timer ends
            public Coroutine damageCoroutine; // Coroutine for damage application
        }

        public override void Initialize()
        {
            var assets = new Core.Assets();
            assets.PopulateAssets("oblivionrod", "oblivionrod");

            ItemDef.name = "oblivion_rod";
            ItemDef.nameToken = "FLOWARE_OBLIVIONROD_NAME";
            ItemDef.pickupToken = "FLOWARE_OBLIVIONROD_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_OBLIVIONROD_DESC";
            ItemDef.loreToken = "FLOWARE_OBLIVIONROD_LORE";

            LanguageAPI.Add("FLOWARE_OBLIVIONROD_NAME", "Oblivion Rod");
            LanguageAPI.Add("FLOWARE_OBLIVIONROD_PICKUP", " <color=#ed7fcd>Corrupts all crowbars </color>");
            LanguageAPI.Add("FLOWARE_OBLIVIONROD_DESC", " <color=#ed7fcd>Corrupts all crowbars </color>");
            LanguageAPI.Add("FLOWARE_OBLIVIONROD_LORE", "");

            ItemDef.tier = ItemTier.VoidTier1;
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();
            ItemDef.tags = new ItemTag[] { };

            ItemDef.pickupIconSprite = assets.icon;
            ItemDef.pickupModelPrefab = assets.prefab;

            ItemDef.canRemove = true;
            ItemDef.hidden = false;

            ItemAPI.Add(new CustomItem(ItemDef, new ItemDisplayRuleDict(null)));
            Hook();
        }

        public override void Hook()
        {
            On.RoR2.CharacterMaster.OnInventoryChanged += OnInventoryChanged;
            On.RoR2.HealthComponent.TakeDamage += OnDamageDealt;
            ContagiousItemManager.Init += SetTransformation;
        }

        public override void Unhook()
        {
            On.RoR2.CharacterMaster.OnInventoryChanged -= OnInventoryChanged;
            On.RoR2.HealthComponent.TakeDamage -= OnDamageDealt;
            ContagiousItemManager.Init -= SetTransformation;
        }

        private void SetTransformation(ContagiousItemManager.orig_Init orig)
        {
            var dlcType = ExpansionCatalog.expansionDefs[0];
            ItemDef.requiredExpansion = dlcType;

            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Crowbar,
                itemDef2 = ItemDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] =
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }

        private void OnInventoryChanged(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
        }

        private void OnDamageDealt(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo.attacker == null || !damageInfo.attacker.TryGetComponent<CharacterBody>(out var attackerBody))
            {
                orig(self, damageInfo);
                return;
            }

            var inventory = attackerBody.inventory;
            if (inventory == null || inventory.GetItemCount(ItemDef.itemIndex) <= 0)
            {
                orig(self, damageInfo);
                return;
            }

            Debug.Log($"Oblivion Rod: Enemy health = {self.health}/{self.fullHealth}, Is above 90%: {self.health >= self.fullHealth * 0.9f}");

            if (self.health >= self.fullHealth * 0.9f)
            {
                if (!enemyStates.TryGetValue(self, out var state))
                {
                    state = new EnemyState
                    {
                        accumulatedDamage = 0
                    };
                    enemyStates[self] = state;
                }

                state.accumulatedDamage += damageInfo.damage * (1 + (inventory.GetItemCount(ItemDef.itemIndex) * 0.35f));
                damageInfo.damageColorIndex = DamageColorIndex.Item;
                damageInfo.damage = 1; // Freeze damage
                if (state.damageCoroutine == null)
                {
                    GameObject coroutineObject = new GameObject("OblivionRodCoroutine");
                    MonoBehaviour mb = coroutineObject.AddComponent<CoroutineHelper>();
                    state.damageCoroutine = mb.StartCoroutine(ApplyAccumulatedDamageAfterDelay(self));
                }
            }
            else
            {
                enemyStates.Remove(self);
            }

            orig(self, damageInfo);
        }

        private IEnumerator ApplyAccumulatedDamageAfterDelay(HealthComponent target)
        {
            yield return new WaitForSeconds(maxAllowedTime);

            if (target != null && target.health >= 0)
            {
                var damageInfo = new DamageInfo
                {
                    attacker = null, // Set attacker if needed
                    damage = enemyStates[target].accumulatedDamage,
                    procCoefficient = 1f, // Adjust proc if necessary
                    position = target.transform.position
                };
                damageInfo.damageColorIndex = DamageColorIndex.Void;
                target.TakeDamage(damageInfo);
            }

            enemyStates[target].damageCoroutine = null;
            enemyStates.Remove(target);
        }


        private class CoroutineHelper : MonoBehaviour
        {
        }
    }
}
