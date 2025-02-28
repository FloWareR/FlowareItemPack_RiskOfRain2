﻿using HarmonyLib;
using On.RoR2.Items;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;


using System;

namespace FlowareItemPack.Items
{
    internal class DisperseObsidian : BaseItem
    {
        public override ItemDef ItemDef { get; } = ScriptableObject.CreateInstance<ItemDef>();

        private GameObject effect;

        public override void Initialize()
        {
            var assets = new Core.Assets();
            assets.PopulateAssets("disperseobsidian", "disperseobsidian");

            ItemDef.name = "disperse_obsidian";
            ItemDef.nameToken = "FLOWARE_DISPERSEOBSIDIAN_NAME";
            ItemDef.pickupToken = "FLOWARE_DISPERSEOBSIDIAN_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_DISPERSEOBSIDIAN_DESC";
            ItemDef.loreToken = "FLOWARE_DISPERSEOBSIDIAN_LORE";

            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_NAME", "Disperse Obsidian");
            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_PICKUP", "Deal bonus damage to enemies at long distance. <color=#ed7fcd>Corrupts all Focus Crystals </color>");
            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_DESC", "Grants a damage boost against enemies further away. Displays a pulsating effect around the player. <color=#ed7fcd>Corrupts all Focus Crystals </color>");
            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_LORE", "A shard that resonates with the distant echoes of battle.");

            ItemDef.tier = ItemTier.VoidTier1;
            ItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();
            ItemDef.tags = new ItemTag[] { ItemTag.Damage };

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
            On.RoR2.Items.ContagiousItemManager.Init += SetTransformation;

        }

        public override void Unhook()
        {
            On.RoR2.CharacterMaster.OnInventoryChanged -= OnInventoryChanged;
            On.RoR2.HealthComponent.TakeDamage -= OnDamageDealt;
            On.RoR2.Items.ContagiousItemManager.Init -= SetTransformation;

        }

        private void SetTransformation(ContagiousItemManager.orig_Init orig)
        {

            var dlcType = ExpansionCatalog.expansionDefs[0];
            ItemDef.requiredExpansion = dlcType;

            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.NearbyDamageBonus,
                itemDef2 = ItemDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = 
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }

        private void OnInventoryChanged(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);

            if (!self || !self.inventory) return;

            CharacterBody body = self.GetBody();
            if (!body || !body.isPlayerControlled) return; 

            var itemCount = self.inventory.GetItemCount(ItemDef.itemIndex);

            if (itemCount > 0)
            {
                Debug.Log("DEBUG: Item added, creating effect.");
                InstantiateEffect(body);
            }
            else
            {
                Debug.Log("DEBUG: Item removed, destroying effect.");
                DestroyEffect();
            }
        }

        private void InstantiateEffect(CharacterBody self)
        {
            GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/NearbyDamageBonusIndicator");

            if (original == null)
            {
                Debug.LogError("Failed to load NearbyDamageBonusIndicator.");
                return;
            }

            GameObject spawnedObject = GameObject.Instantiate(original, self.corePosition, Quaternion.identity);

            if (spawnedObject == null)
            {
                Debug.LogError("Failed to instantiate NearbyDamageBonusIndicator.");
                return;
            }

            if (!spawnedObject.TryGetComponent(out NetworkIdentity netId))
            {
                netId = spawnedObject.AddComponent<NetworkIdentity>();
                Debug.LogWarning("Added NetworkIdentity to spawnedObject.");
            }
            Debug.Log("Attaching NetworkedBodyAttachment.");

            var scaleMultiplier = 2.3f; 
            spawnedObject.transform.localScale *= scaleMultiplier;


            MeshRenderer meshRenderer = spawnedObject.GetComponentInChildren<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Change the color to purple
                meshRenderer.material.color = Color.green;
            }
            else
            {
                Debug.LogError("No MeshRenderer found in NearbyDamageBonusIndicator.");
            }

            NetworkedBodyAttachment attachment = spawnedObject.GetComponent<NetworkedBodyAttachment>();
            if (attachment == null)
            {
                Debug.LogError("NearbyDamageBonusIndicator is missing NetworkedBodyAttachment.");
                return;
            }
            else
            {
                Debug.LogError("No Renderer found on NearbyDamageBonusIndicator.");
            }

            spawnedObject.transform.SetParent(self.transform, true);
            attachment.AttachToGameObjectAndSpawn(self.gameObject, null);
            effect = spawnedObject; // Store the effect for later 
        }

        private void DestroyEffect()
        {
            if (effect != null)
            {
                GameObject.Destroy(effect);
                effect = null;
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
            if (distance > 29.9f)
            {
                damageInfo.damage *= 1 + (0.2f * itemCount);
                damageInfo.damageColorIndex = DamageColorIndex.Void;
            }

            orig(self, damageInfo);
        }
    }
}
