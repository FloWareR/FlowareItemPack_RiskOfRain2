using HarmonyLib;
using On.RoR2.Items;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;



namespace FlowareItemPack.Items
{
    internal class PlaceholderVoid : BaseItem
    {
        public override ItemDef ItemDef { get; } = ScriptableObject.CreateInstance<ItemDef>();


        public override void Initialize()
        {
            var assets = new Core.Assets();
            assets.PopulateAssets("", "");

            ItemDef.name = "";
            ItemDef.nameToken = "FLOWARE_DISPERSEOBSIDIAN_NAME";
            ItemDef.pickupToken = "FLOWARE_DISPERSEOBSIDIAN_PICKUP";
            ItemDef.descriptionToken = "FLOWARE_DISPERSEOBSIDIAN_DESC";
            ItemDef.loreToken = "FLOWARE_DISPERSEOBSIDIAN_LORE";

            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_NAME", "");
            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_PICKUP", " <color=#ed7fcd>Corrupts all  </color>");
            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_DESC", " <color=#ed7fcd>Corrupts all  </color>");
            LanguageAPI.Add("FLOWARE_DISPERSEOBSIDIAN_LORE", "");

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

        }


        private void OnDamageDealt(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);

        }
    }
}
