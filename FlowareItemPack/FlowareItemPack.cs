using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FlowareItemPack
{
    // This is an example plugin that can be put in
    // BepInEx/plugins/ExamplePlugin/ExamplePlugin.dll to test out.
    // It's a small plugin that adds a relatively simple item to the game,
    // and gives you that item whenever you press F2.

    // This attribute specifies that we have a dependency on a given BepInEx Plugin,
    // We need the R2API ItemAPI dependency because we are using for adding our item to the game.
    // You don't need this if you're not using R2API in your plugin,
    // it's just to tell BepInEx to initialize R2API before this plugin so it's safe to use R2API.
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class FlowareItemPack : BaseUnityPlugin
    {
        // The Plugin GUID should be a unique ID for this plugin,
        // which is human readable (as it is used in places like the config).
        // If we see this PluginGUID as it is on thunderstore,
        // we will deprecate this mod.
        // Change the PluginAuthor and the PluginName !
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Floware";
        public const string PluginName = "FlowareItemPack";
        public const string PluginVersion = "1.1.0";

        // We need our item definition to persist through our functions, and therefore make it a class field.
        private static ItemDef myItemDef;

        // The Awake() method is run at the very start when the game is initialized.
        public void Awake()
        {
            // Init our logging class so that we can properly log for debugging
            Log.Init(Logger);
            Assets.PopulateAssets();

            myItemDef = ScriptableObject.CreateInstance<ItemDef>();
            myItemDef.name = "Matches";
            myItemDef.nameToken = "FLOWARE_MATCHES_NAME";
            myItemDef.pickupToken = "FLOWARE_MATCHES_PICKUP";
            myItemDef.descriptionToken = "FLOWARE_MATCHES_DESC";
            myItemDef.loreToken = "FLOWARE_MATCHES_LORE";

            LanguageAPI.Add("FLOWARE_MATCHES_NAME", "Box O' Matches");
            LanguageAPI.Add("FLOWARE_MATCHES_PICKUP", "Light em' up, break em' down.");
            LanguageAPI.Add("FLOWARE_MATCHES_DESC", "5% chance (+5% per stack) to set enemies on fire for 2 seconds (+0.5s per stack), dealing 10% base damage.");
            LanguageAPI.Add("FLOWARE_MATCHES_LORE", "An old, half-empty box of matches. Each one a spark waiting to ignite chaos.");

            // The tier determines what rarity the item is:
            // Tier1=white, Tier2=green, Tier3=red, Lunar=Lunar, Boss=yellow,
            // and finally NoTier is generally used for helper items, like the tonic affliction
#pragma warning disable Publicizer001 // Accessing a member that was not originally public. Here we ignore this warning because with how this example is setup we are forced to do this
            myItemDef._itemTierDef = Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();
#pragma warning restore Publicizer001
            // Instead of loading the itemtierdef directly, you can also do this like below as a workaround
            // myItemDef.deprecatedTier = ItemTier.Tier2;

            // You can create your own icons and prefabs through assetbundles, but to keep this boilerplate brief, we'll be using question marks.
            //myItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
            //myItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
            myItemDef.pickupIconSprite = Assets.icon;
            myItemDef.pickupModelPrefab = Assets.matchbox;

            // Can remove determines
            // if a shrine of order,
            // or a printer can take this item,
            // generally true, except for NoTier items.
            myItemDef.canRemove = true;

            // Hidden means that there will be no pickup notification,
            // and it won't appear in the inventory at the top of the screen.
            // This is useful for certain noTier helper items, such as the DrizzlePlayerHelper.
            myItemDef.hidden = false;

            // You can add your own display rules here,
            // where the first argument passed are the default display rules:
            // the ones used when no specific display rules for a character are found.
            // For this example, we are omitting them,
            // as they are quite a pain to set up without tools like https://thunderstore.io/package/KingEnderBrine/ItemDisplayPlacementHelper/
            var displayRules = new ItemDisplayRuleDict(null);
            ItemAPI.Add(new CustomItem(myItemDef, displayRules));
            GlobalEventManager.onServerDamageDealt += GlobalEventManager_onServerDamageDealt;
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
            var itemCount = inventory.GetItemCount(myItemDef.itemIndex);
            var victim = report.victimBody;
            var igniteTankItemDef = ItemCatalog.GetItemDef(ItemCatalog.FindItemIndex("StrengthenBurn"));

            if (itemCount > 0 && Util.CheckRoll(5f * itemCount, attacker.master)) // 5% chance to ignite (+5% per stack)
            {
                var burnDuration = 2f * itemCount; // Burn for 2s + .5s per stack
                var burnDamage = (report.attackerBody.baseDamage * 0.1f); // Burn does 15% of base damage per tick
                var igniteTankCount = inventory.GetItemCount(igniteTankItemDef.itemIndex);
                var debuffToApply = igniteTankCount > 0 ? DotController.DotIndex.StrongerBurn : DotController.DotIndex.Burn;
                var dotController = DotController.FindDotController(victim.gameObject);

                if (dotController != null && dotController.HasDotActive(debuffToApply))
                {
                    return;
                }

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

        // The Update() method is run on every frame of the game.
        private void Update()
        {
            // This if statement checks if the player has currently pressed F2.
            //if (Input.GetKeyDown(KeyCode.F2))
            //{
            //    // Get the player body to use a position:
            //    var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

            //    // And then drop our defined item in front of the player.

            //    Log.Info($"Player pressed F2. Spawning our custom item at coordinates {transform.position}");
            //    PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(myItemDef.itemIndex), transform.position, transform.forward * 20f);
            //}
        }

        public static class Assets
        {
            public static Sprite icon;
            public static GameObject matchbox;

            public static void PopulateAssets()
            {
                using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("FlowareItemPack.import"))
                {
                    var bundle = AssetBundle.LoadFromStream(stream);
                    icon = bundle.LoadAsset<Sprite>("assets/import/icons/matches.png");
                    matchbox = bundle.LoadAsset<GameObject>("assets/import/prefabs/matches.prefab");

                }
            }
        }
    }
}
