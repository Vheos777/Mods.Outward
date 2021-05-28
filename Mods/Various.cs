﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine.UI;
using System.Collections;



/* TO DO:
 * - hide armor extras (like scarf)
 * - prevent dodging right after hitting
 */
namespace ModPack
{
    public class Various : AMod, IUpdatable
    {
        #region const
        private const string FLINT_AND_STEEL_BREAK_NOTIFICATION = "Flint and Steel broke!";
        private const string INNS_QUEST_FAMILY_NAME = "Inns";
        private const float DEFAULT_ENEMY_HEALTH_RESET_HOURS = 24f;   // Character.HoursToHealthReset
        private const int ARMOR_TRAINING_ID = 8205220;
        private const int PRIMITIVE_SATCHEL_CAPACITY = 25;
        private const int TRADER_BACKPACK = 100;
        static private readonly Dictionary<AreaManager.AreaEnum, (string UID, Vector3[] Positions)> INN_STASH_POSITIONS_BY_CITY = new Dictionary<AreaManager.AreaEnum, (string, Vector3[])>
        {
            [AreaManager.AreaEnum.CierzoVillage] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-367.850f, -1488.250f, 596.277f),
                                                                                      new Vector3(-373.539f, -1488.250f, 583.187f) }),
            [AreaManager.AreaEnum.Berg] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-386.620f, -1493.132f, 773.86f),
                                                                             new Vector3(-372.410f, -1493.132f, 773.86f) }),
            [AreaManager.AreaEnum.Monsoon] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-371.628f, -1493.410f, 569.910f) }),
            [AreaManager.AreaEnum.Levant] = ("ZbPXNsPvlUeQVJRks3zBzg", new[] { new Vector3(-369.280f, -1502.535f, 592.850f),
                                                                               new Vector3(-380.530f, -1502.535f, 593.080f) }),
            [AreaManager.AreaEnum.Harmattan] = ("ImqRiGAT80aE2WtUHfdcMw", new[] { new Vector3(-178.672f, -1515.915f, 597.934f),
                                                                                  new Vector3(-182.373f, -1515.915f, 606.291f),
                                                                                  new Vector3(-383.484f, -1504.820f, 583.343f),
                                                                                  new Vector3(-392.681f, -1504.820f, 586.551f)}),
        };
        static private readonly Dictionary<TemperatureSteps, Vector2> DEFAULT_TEMPERATURE_DATA_BY_ENUM = new Dictionary<TemperatureSteps, Vector2>
        {
            [TemperatureSteps.Coldest] = new Vector2(-45, -1),
            [TemperatureSteps.VeryCold] = new Vector2(-30, 14),
            [TemperatureSteps.Cold] = new Vector2(-20, 26),
            [TemperatureSteps.Fresh] = new Vector2(-14, 38),
            [TemperatureSteps.Neutral] = new Vector2(0, 50),
            [TemperatureSteps.Warm] = new Vector2(14, 62),
            [TemperatureSteps.Hot] = new Vector2(20, 80),
            [TemperatureSteps.VeryHot] = new Vector2(28, 92),
            [TemperatureSteps.Hottest] = new Vector2(40, 101),
        };
        #endregion
        #region enum
        [Flags]
        private enum ArmorSlots
        {
            None = 0,
            Head = 1 << 1,
            Chest = 1 << 2,
            Feet = 1 << 3,
        }
        [Flags]
        private enum TitleScreens
        {
            Vanilla = 1 << 1,
            TheSoroboreans = 1 << 2,
            TheThreeBrothers = 1 << 3,
        }
        private enum TitleScreenCharacterVisibility
        {
            Enable = 1,
            Disable = 2,
            Randomize = 3,
        }
        #endregion

        // Settings
        static private ModSetting<bool> _enableCheats;
        static private ModSetting<string> _enableCheatsHotkey;
        static private ModSetting<bool> _skipStartupVideos;
        static private ModSetting<ArmorSlots> _armorSlotsToHide;
        static private ModSetting<bool> _removeCoopScaling;
        static private ModSetting<bool> _removeDodgeInvulnerability;
        static private ModSetting<bool> _healEnemiesOnLoad;
        static private ModSetting<bool> _multiplicativeStacking;
        static private ModSetting<int> _armorTrainingPenaltyReduction;
        static private ModSetting<bool> _applyArmorTrainingToManaCost;
        static private ModSetting<bool> _loadArrowsFromInventory;
        static private ModSetting<Vector2> _remapBackpackCapacities;
        static private ModSetting<int> _waterskinCapacity;
        static private ModSetting<int> _innRentDuration;
        static private ModSetting<bool> _innStashes;
        static private ModSetting<float> _baseStaminaRegen;
        static private ModSetting<int> _chanceToBreakFlintAndSteel;
        static private ModSetting<bool> _moreGatheringTools;
        static private ModSetting<Vector2> _gatheringDurabilityCost;
        static private ModSetting<TitleScreens> _titleScreenRandomize;
        static private ModSetting<TitleScreenCharacterVisibility> _titleScreenHideCharacters;
        static private ModSetting<bool> _temperatureToggle;
        static private Dictionary<TemperatureSteps, ModSetting<Vector2>> _temperatureDataByEnum;
        override protected void Initialize()
        {
            _enableCheats = CreateSetting(nameof(_enableCheats), false);
            _enableCheatsHotkey = CreateSetting(nameof(_enableCheatsHotkey), "");
            _skipStartupVideos = CreateSetting(nameof(_skipStartupVideos), false);
            _armorSlotsToHide = CreateSetting(nameof(_armorSlotsToHide), ArmorSlots.None);
            _removeCoopScaling = CreateSetting(nameof(_removeCoopScaling), false);
            _removeDodgeInvulnerability = CreateSetting(nameof(_removeDodgeInvulnerability), false);
            _healEnemiesOnLoad = CreateSetting(nameof(_healEnemiesOnLoad), false);
            _multiplicativeStacking = CreateSetting(nameof(_multiplicativeStacking), false);
            _armorTrainingPenaltyReduction = CreateSetting(nameof(_armorTrainingPenaltyReduction), 50, IntRange(0, 100));
            _applyArmorTrainingToManaCost = CreateSetting(nameof(_applyArmorTrainingToManaCost), false);
            _loadArrowsFromInventory = CreateSetting(nameof(_loadArrowsFromInventory), false);
            _remapBackpackCapacities = CreateSetting(nameof(_remapBackpackCapacities), new Vector2(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK));
            _waterskinCapacity = CreateSetting(nameof(_waterskinCapacity), 5, IntRange(1, 18));
            _innRentDuration = CreateSetting(nameof(_innRentDuration), 12, IntRange(1, 168));
            _innStashes = CreateSetting(nameof(_innStashes), false);
            _baseStaminaRegen = CreateSetting(nameof(_baseStaminaRegen), 2.4f, FloatRange(0, 10));
            _chanceToBreakFlintAndSteel = CreateSetting(nameof(_chanceToBreakFlintAndSteel), 0, IntRange(0, 100));
            _moreGatheringTools = CreateSetting(nameof(_moreGatheringTools), false);
            _gatheringDurabilityCost = CreateSetting(nameof(_gatheringDurabilityCost), new Vector2(0f, 5f));
            _titleScreenRandomize = CreateSetting(nameof(_titleScreenRandomize), (TitleScreens)0);
            _titleScreenHideCharacters = CreateSetting(nameof(_titleScreenHideCharacters), TitleScreenCharacterVisibility.Enable);
            _temperatureToggle = CreateSetting(nameof(_temperatureToggle), false);
            _temperatureDataByEnum = new Dictionary<TemperatureSteps, ModSetting<Vector2>>();
            foreach (var step in Utility.GetEnumValues<TemperatureSteps>())
                if (step != TemperatureSteps.Count)
                    _temperatureDataByEnum.Add(step, CreateSetting(nameof(_temperatureDataByEnum) + step, DEFAULT_TEMPERATURE_DATA_BY_ENUM[step]));

            foreach (var questFamily in QuestEventDictionary.m_sections)
                if (questFamily.Name == INNS_QUEST_FAMILY_NAME)
                {
                    _innRentQuestFamily = questFamily;
                    break;
                }

            _enableCheats.AddEvent(() => Global.CheatsEnabled = _enableCheats);
            AddEventOnConfigClosed(() =>
            {
                foreach (var player in Players.Local)
                    UpdateBaseStaminaRegen(player.Stats);
                TryUpdateTemperatureData();
            });
        }
        override protected void SetFormatting()
        {
            _enableCheats.Format("Enable cheats");
            Indent++;
            {
                _enableCheatsHotkey.Format("Hotkey");
                Indent--;
            }
            _enableCheats.Description = "aka Debug Mode";
            _skipStartupVideos.Format("Skip startup videos");
            _skipStartupVideos.Description = "Saves ~3 seconds each time you launch the game";
            _armorSlotsToHide.Format("Armor slots to hide");
            _armorSlotsToHide.Description = "Used to hide ugly helmets (purely visual)";

            _removeCoopScaling.Format("Remove multiplayer scaling");
            _removeCoopScaling.Description = "Enemies in multiplayer will have the same stats as in singleplayer";
            _removeDodgeInvulnerability.Format("Remove dodge invulnerability");
            _removeDodgeInvulnerability.Description = "You can get hit during the dodge animation\n" +
                                                      "(even without a backpack)";
            _healEnemiesOnLoad.Format("Heal enemies on load");
            _healEnemiesOnLoad.Description = "Every loading screen fully heals all enemies";
            _multiplicativeStacking.Format("Multiplicative stacking");
            _multiplicativeStacking.Description = "Some stats will stack multiplicatively instead of additvely\n" +
                                                  "(movement speed, stamina cost, mana cost)";
            Indent++;
            {
                _armorTrainingPenaltyReduction.Format("\"Armor Training\" penalty reduction", _multiplicativeStacking);
                _armorTrainingPenaltyReduction.Description = "How much of equipment's movement speed and stamina cost penalties should \"Armor Training\" ignore";
                _applyArmorTrainingToManaCost.Format("\"Armor Training\" affects mana cost", _multiplicativeStacking);
                _applyArmorTrainingToManaCost.Description = "\"Armor Training\" will also lower equipment's mana cost penalties";
                Indent--;
            }
            _loadArrowsFromInventory.Format("Load arrows from inventory");
            _loadArrowsFromInventory.Description = "Whenever you shoot your bow, the lost arrow is instantly replaced with one from your backpack or pouch (in that order).";
            _remapBackpackCapacities.Format("Remap backpack capacities");
            _remapBackpackCapacities.Description = "X   -   Primitive Satchel's capacity\n" +
                                                   "Y   -   Trader Backpack's capacity\n" +
                                                   "(all other backpacks will have their capacities scaled accordingly)";
            _waterskinCapacity.Format("Waterskin capacity");
            _waterskinCapacity.Description = "Have one big waterskin instead of a few small ones so you don't have to swap quickslots";
            _innRentDuration.Format("Inn rent duration");
            _innRentDuration.Description = "Pay the rent once, sleep for up to a week (in hours)";
            _innStashes.Format("Inn stashes");
            _innStashes.Description = "Each inn room will have a stash, linked with the player's house stash\n" +
                                      "(exceptions: the first rooms in Monsoon's inn and Harmattan's Victorious Light inn)";
            _baseStaminaRegen.Format("Base stamina regen");
            _chanceToBreakFlintAndSteel.Format("Chance to break \"Flint and Steel\"");
            _chanceToBreakFlintAndSteel.Description = "Each time you use Flint and Steel, there's a X% chance it will break";
            _moreGatheringTools.Format("More gathering tools");
            _moreGatheringTools.Description = "Any Spear can fish and any 2-Handed Mace can mine\n" +
                                              "The tool is searched for in your bag, then pouch, then equipment\n" +
                                              "If there is more than 1 valid tool, the cheapest one is chosen first";
            _gatheringDurabilityCost.Format("Gathering tools durability cost");
            _gatheringDurabilityCost.Description = "X   -   flat amount\n" +
                                                   "Y   -   percent of max";
            _titleScreenRandomize.Format("Randomize title screen");
            _titleScreenRandomize.Description = "Every time you start the game, one of the chosen title screens will be loaded at random (untick all for default)";
            Indent++;
            {
                _titleScreenHideCharacters.Format("Characters");
                _titleScreenHideCharacters.Description = "If you think the character are ruining the view :)\n" +
                                                         "(requires game restart)";
                Indent--;
            }
            _temperatureToggle.Format("Temperature");
            _temperatureToggle.Description = "Change each environmental temperature level's value and cap:\n" +
                                             "X   -   value; how much cold/hot weather defense you need to nullify this temperature level\n" +
                                             "Y   -   cap; min/max player temperature at this environmental temperature level\n" +
                                             "\n" +
                                             "Player temperatures cheatsheet:\n" +
                                             "Very cold   -   25\n" +
                                             "Cold   -   40\n" +
                                             "Neutral   -   50\n" +
                                             "Hot   -   60\n" +
                                             "Very Hot   -   75)";
            Indent++;
            {
                foreach (var step in Utility.GetEnumValues<TemperatureSteps>())
                    if (step != TemperatureSteps.Count)
                        _temperatureDataByEnum[step].Format(step.ToString(), _temperatureToggle);
                Indent--;
            }
        }
        override protected string Description
        => "• Mods (small and big) that didn't get their own section yet :)";
        override protected string SectionOverride
        => SECTION_VARIOUS;
        override public void LoadPreset(Presets.Preset preset)
        {
            switch (preset)
            {
                case Presets.Preset.Vheos_CoopSurvival:
                    ForceApply();
                    _enableCheats.Value = false;
                    _enableCheatsHotkey.Value = KeyCode.Keypad0.ToString();
                    _skipStartupVideos.Value = true;
                    _removeCoopScaling.Value = true;
                    _removeDodgeInvulnerability.Value = true;
                    _healEnemiesOnLoad.Value = true;
                    _multiplicativeStacking.Value = true;
                    _armorTrainingPenaltyReduction.Value = 50;
                    _applyArmorTrainingToManaCost.Value = true;
                    _loadArrowsFromInventory.Value = true;
                    _remapBackpackCapacities.Value = new Vector2(20, 60);
                    _waterskinCapacity.Value = 9;
                    _innRentDuration.Value = 120;
                    _innStashes.Value = true;
                    _chanceToBreakFlintAndSteel.Value = 25;
                    _moreGatheringTools.Value = true;
                    _gatheringDurabilityCost.Value = new Vector2(15, 3);
                    _temperatureToggle.Value = true;
                    {
                        _temperatureDataByEnum[TemperatureSteps.Coldest].Value = new Vector2(-50, 50 - (50 + 1));
                        _temperatureDataByEnum[TemperatureSteps.VeryCold].Value = new Vector2(-40, 50 - (50 - 1));
                        _temperatureDataByEnum[TemperatureSteps.Cold].Value = new Vector2(-30, 50 - (25 + 1));
                        _temperatureDataByEnum[TemperatureSteps.Fresh].Value = new Vector2(-20, 50 - (10 + 1));
                        _temperatureDataByEnum[TemperatureSteps.Neutral].Value = new Vector2(0, 50);
                        _temperatureDataByEnum[TemperatureSteps.Warm].Value = new Vector2(+20, 50 + (10 + 1));
                        _temperatureDataByEnum[TemperatureSteps.Hot].Value = new Vector2(+30, 50 + (25 + 1));
                        _temperatureDataByEnum[TemperatureSteps.VeryHot].Value = new Vector2(+40, 50 + (50 - 1));
                        _temperatureDataByEnum[TemperatureSteps.Hottest].Value = new Vector2(+50, 50 + (50 + 1));
                    }
                    break;

                case Presets.Preset.IggyTheMad_TrueHardcore:
                    break;
            }
        }
        public void OnUpdate()
        {
            if (_enableCheatsHotkey.Value.ToKeyCode().Pressed())
                _enableCheats.Value = !_enableCheats;
        }

        // Utility
        static private QuestEventFamily _innRentQuestFamily;
        static private bool ShouldArmorSlotBeHidden(EquipmentSlot.EquipmentSlotIDs slot)
        => slot == EquipmentSlot.EquipmentSlotIDs.Helmet && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Head)
        || slot == EquipmentSlot.EquipmentSlotIDs.Chest && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Chest)
        || slot == EquipmentSlot.EquipmentSlotIDs.Foot && _armorSlotsToHide.Value.HasFlag(ArmorSlots.Feet);
        static private bool HasLearnedArmorTraining(Character character)
        => character.Inventory.SkillKnowledge.IsItemLearned(ARMOR_TRAINING_ID);
        static public bool IsAnythingEquipped(EquipmentSlot slot)
        => slot != null && slot.HasItemEquipped;
        static public bool IsNotLeftHandUsedBy2H(EquipmentSlot slot)
        => !(slot.SlotType == EquipmentSlot.EquipmentSlotIDs.LeftHand && slot.EquippedItem.TwoHanded);
        static private bool TryApplyMultiplicativeStacking(CharacterEquipment equipment, ref float result, Func<EquipmentSlot, float> getStatValue, bool invertedPositivity = false, bool applyArmorTraining = false)
        {
            #region quit
            if (!_multiplicativeStacking)
                return true;
            #endregion

            float invCoeff = invertedPositivity ? -1f : +1f;
            bool canApplyArmorTraining = applyArmorTraining && HasLearnedArmorTraining(equipment.m_character);

            result = 1f;
            foreach (var slot in equipment.m_equipmentSlots)
                if (IsAnythingEquipped(slot) && IsNotLeftHandUsedBy2H(slot))
                {
                    float armorTrainingCoeff = canApplyArmorTraining && getStatValue(slot) > 0f ? 1f - _armorTrainingPenaltyReduction / 100f : 1f;
                    result *= 1f + getStatValue(slot) / 100f * invCoeff * armorTrainingCoeff;
                }
            result -= 1f;
            result *= invCoeff;
            return false;
        }
        static private void UpdateBaseStaminaRegen(CharacterStats characterStats)
        => characterStats.m_staminaRegen.BaseValue = _baseStaminaRegen;
        static private void TryUpdateTemperatureData()
        {
            #region quit
            if (!_temperatureToggle)
                return;
            #endregion

            if (EnvironmentConditions.Instance.TryAssign(out var environmentConditions))
                foreach (var step in Utility.GetEnumValues<TemperatureSteps>())
                    if (step != TemperatureSteps.Count)
                    {
                        environmentConditions.BodyTemperatureImpactPerStep[step] = _temperatureDataByEnum[step].Value.x;
                        environmentConditions.TemperatureCaps[step] = _temperatureDataByEnum[step].Value.y;
                    }
        }

        // Hooks
#pragma warning disable IDE0051 // Remove unused private members
        // Override title screen
        [HarmonyPatch(typeof(TitleScreenLoader), "LoadTitleScreen", new[] { typeof(OTWStoreAPI.DLCs) }), HarmonyPrefix]
        static bool TitleScreenLoader_LoadTitleScreen_Pre(TitleScreenLoader __instance, ref OTWStoreAPI.DLCs _dlc)
        {
            #region quit
            if (_titleScreenRandomize.Value == 0)
                return true;
            #endregion

            var DLCs = new List<OTWStoreAPI.DLCs>();
            foreach (var flag in Utility.GetEnumValues<TitleScreens>())
                if (_titleScreenRandomize.Value.HasFlag(flag))
                    switch (flag)
                    {
                        case TitleScreens.Vanilla: DLCs.Add(OTWStoreAPI.DLCs.None); break;
                        case TitleScreens.TheSoroboreans: DLCs.Add(OTWStoreAPI.DLCs.Soroboreans); break;
                        case TitleScreens.TheThreeBrothers: DLCs.Add(OTWStoreAPI.DLCs.DLC2); break;
                    }

            _dlc = DLCs.Random();
            return true;
        }

        [HarmonyPatch(typeof(TitleScreenLoader), "LoadTitleScreenCoroutine"), HarmonyPostfix]
        static IEnumerator TitleScreenLoader_LoadTitleScreenCoroutine_Post(IEnumerator original, TitleScreenLoader __instance)
        {
            while (original.MoveNext())
                yield return original.Current;

            #region quit
            if (_titleScreenHideCharacters.Value == TitleScreenCharacterVisibility.Enable)
                yield break;
            #endregion

            bool state = true;
            switch (_titleScreenHideCharacters.Value)
            {
                case TitleScreenCharacterVisibility.Disable: state = false; break;
                case TitleScreenCharacterVisibility.Randomize: state = System.DateTime.Now.Ticks % 2 == 0; break;
            }

            foreach (var characterVisuals in __instance.transform.GetAllComponentsInHierarchy<CharacterVisuals>())
                characterVisuals.GOSetActive(state);
        }

        // More gathering tools
        [HarmonyPatch(typeof(GatherableInteraction), "GetValidItem"), HarmonyPrefix]
        static bool GatherableInteraction_GetValidItem_Pre(GatherableInteraction __instance, ref Item __result, Character _character)
        {
            #region quit
            if (!_moreGatheringTools || !__instance.Gatherable.RequiredItem.TryAssign(out var requiredItem)
            || requiredItem.ItemID != "Mining Pick".ItemID() && requiredItem.ItemID != "Fishing Harpoon".ItemID())
                return true;
            #endregion

            // Cache
            Weapon.WeaponType requiredType = requiredItem.ItemID == "Fishing Harpoon".ItemID() ? Weapon.WeaponType.Spear_2H : Weapon.WeaponType.Mace_2H;
            List<Item> potentialTools = new List<Item>();

            // Search bag & pouch
            List<ItemContainer> containers = new List<ItemContainer>();
            if (_character.Inventory.EquippedBag.TryAssign(out var bag))
                containers.Add(bag.m_container);
            if (_character.Inventory.Pouch.TryAssign(out var pouch))
                containers.Add(pouch);

            foreach (var container in containers)
                if (potentialTools.IsEmpty())
                    foreach (var item in container.GetContainedItems())
                        if (item.TryAs(out Weapon weapon) && weapon.Type == requiredType && weapon.DurabilityRatio > 0)
                            potentialTools.Add(item);

            // Search equipment
            if (potentialTools.IsEmpty()
            && _character.Inventory.Equipment.m_equipmentSlots[(int)EquipmentSlot.EquipmentSlotIDs.RightHand].EquippedItem.TryAs(out Weapon mainWeapon)
            && mainWeapon.Type == requiredType && mainWeapon.DurabilityRatio > 0)
                potentialTools.Add(mainWeapon);

            // Choose tool
            Item chosenTool = null;
            if (potentialTools.IsNotEmpty())
            {
                int minValue = potentialTools.Min(tool => tool.RawCurrentValue);
                chosenTool = potentialTools.First(tool => tool.RawCurrentValue == minValue);
            }

            // Finalize
            __instance.m_validItem = chosenTool;
            __instance.m_isCurrentWeapon = chosenTool != null && chosenTool.IsEquipped;
            __result = chosenTool;
            return false;
        }

        // Gathering durability cost
        [HarmonyPatch(typeof(GatherableInteraction), "CharSpellTakeItem"), HarmonyPrefix]
        static bool GatherableInteraction_CharSpellTakeItem_Pre(GatherableInteraction __instance)
        {
            #region quit
            if (!__instance.m_validItem.TryAssign(out var item))
                return true;
            #endregion

            item.ReduceDurability(_gatheringDurabilityCost.Value.x + (_gatheringDurabilityCost.Value.y - 5) / 100f * item.MaxDurability);
            return true;
        }

        // Chance to break Flint and Steel
        [HarmonyPatch(typeof(Item), "OnUse"), HarmonyPostfix]
        static void Item_OnUse_Post(Item __instance)
        {
            #region quit
            if (__instance.ItemID != "Flint and Steel".ItemID()
            || UnityEngine.Random.value >= _chanceToBreakFlintAndSteel / 100f)
                return;
            #endregion

            __instance.RemoveQuantity(1);
            __instance.m_ownerCharacter.CharacterUI.ShowInfoNotification(FLINT_AND_STEEL_BREAK_NOTIFICATION);
        }

        // InnStash
        [HarmonyPatch(typeof(NetworkLevelLoader), "UnPauseGameplay"), HarmonyPostfix]
        static void NetworkLevelLoader_UnPauseGameplay_Post(NetworkLevelLoader __instance, string _identifier)
        {
            #region quit
            if (!_innStashes || _identifier != "Loading" || !AreaManager.Instance.CurrentArea.TryAssign(out var currentArea)
            || !INN_STASH_POSITIONS_BY_CITY.ContainsKey(currentArea.ID.As<AreaManager.AreaEnum>()))
                return;
            #endregion

            // Cache
            (string UID, Vector3[] Positions) = INN_STASH_POSITIONS_BY_CITY[currentArea.ID.As<AreaManager.AreaEnum>()];
            TreasureChest stash = (TreasureChest)ItemManager.Instance.GetItem(UID);
            stash.GOSetActive(true);

            int counter = 0;
            foreach (var position in Positions)
            {
                // Interactions
                Transform newInteractionHolder = GameObject.Instantiate(stash.InteractionHolder.transform);
                newInteractionHolder.name = $"InnStash{counter} - Interaction";
                newInteractionHolder.ResetLocalTransform();
                newInteractionHolder.position = position;
                InteractionActivator activator = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionActivator>();
                activator.UID += $"_InnStash{counter}";
                InteractionOpenChest openChest = newInteractionHolder.GetFirstComponentsInHierarchy<InteractionOpenChest>();
                openChest.m_container = stash;
                openChest.m_item = stash;
                openChest.StartInit();

                // Highlight
                Transform newHighlightHolder = GameObject.Instantiate(stash.CurrentVisual.ItemHighlightTrans);
                newHighlightHolder.name = $"InnStash{counter} - Highlight";
                newHighlightHolder.ResetLocalTransform();
                newHighlightHolder.BecomeChildOf(newInteractionHolder);
                newHighlightHolder.GetFirstComponentsInHierarchy<InteractionHighlight>().enabled = true;
                counter++;
            }
        }

        [HarmonyPatch(typeof(InteractionOpenChest), "OnActivate"), HarmonyPrefix]
        static bool InteractionOpenChest_OnActivate_Pre(InteractionOpenChest __instance)
        {
            #region quit
            if (!_innStashes || !__instance.m_chest.TryAssign(out var chest))
                return true;
            #endregion

            chest.GOSetActive(true);
            return true;
        }

        // Temperature data
        [HarmonyPatch(typeof(EnvironmentConditions), "Start"), HarmonyPostfix]
        static void EnvironmentConditions_Start_Post(EnvironmentConditions __instance)
        => TryUpdateTemperatureData();

        // Stamina regen
        [HarmonyPatch(typeof(PlayerCharacterStats), "OnStart"), HarmonyPostfix]
        static void PlayerCharacterStats_OnStart_Post(PlayerCharacterStats __instance)
        => UpdateBaseStaminaRegen(__instance);

        // Inn rent duration
        [HarmonyPatch(typeof(QuestEventData), "HasExpired"), HarmonyPrefix]
        static bool QuestEventData_HasExpired_Pre(QuestEventData __instance, ref int _gameHourAllowed)
        {
            if (__instance.m_signature.ParentSection == _innRentQuestFamily)
                _gameHourAllowed = _innRentDuration;
            return true;
        }

        // Waterskin capacity
        [HarmonyPatch(typeof(WaterContainer), "RefreshDisplay"), HarmonyPrefix]
        static bool WaterContainer_RefreshDisplay_Pre(WaterContainer __instance)
        {
            __instance.m_stackable.m_maxStackAmount = _waterskinCapacity;
            return true;
        }

        [HarmonyPatch(typeof(Item), "OnAwake"), HarmonyPostfix]
        static void Item_Awake_Post(Item __instance)
        {
            #region quit
            if (__instance.IsNot<WaterContainer>())
                return;
            #endregion

            __instance.m_stackable.m_maxStackAmount = _waterskinCapacity;
        }

        // Remap backpack capacities
        [HarmonyPatch(typeof(ItemContainer), "ContainerCapacity", MethodType.Getter), HarmonyPostfix]
        static void ItemContainer_ContainerCapacity_Post(ItemContainer __instance, ref float __result)
        {
            if (__instance.RefBag == null || __instance.m_baseContainerCapacity <= 0)
                return;

            __result = __result.Map(PRIMITIVE_SATCHEL_CAPACITY, TRADER_BACKPACK,
                                    _remapBackpackCapacities.Value.x, _remapBackpackCapacities.Value.y).Round();
        }

        // Load arrows from inventory
        [HarmonyPatch(typeof(WeaponLoadoutItem), "ReduceShotAmount"), HarmonyPrefix]
        static bool WeaponLoadoutItem_ReduceShotAmount_Pre(WeaponLoadoutItem __instance)
        {
            #region quit
            if (!_loadArrowsFromInventory
            || __instance.AmunitionType != WeaponLoadout.CompatibleAmmunitionType.WeaponType
            || __instance.CompatibleEquipment != Weapon.WeaponType.Arrow)
                return true;
            #endregion

            CharacterInventory inventory = __instance.m_projectileWeapon.OwnerCharacter.Inventory;
            int ammoID = inventory.GetEquippedAmmunition().ItemID;

            Item ammo = null;
            if (ammo == null && inventory.EquippedBag != null)
                ammo = inventory.EquippedBag.Container.GetItemFromID(ammoID);
            if (ammo == null)
                ammo = inventory.Pouch.GetItemFromID(ammoID);
            if (ammo == null)
                return true;

            ammo.RemoveQuantity(1);
            return false;
        }

        [HarmonyPatch(typeof(CharacterInventory), "GetAmmunitionCount"), HarmonyPostfix]
        static void CharacterInventory_GetAmmunitionCount_Post(CharacterInventory __instance, ref int __result)
        {
            #region quit
            if (!_loadArrowsFromInventory || __result == 0)
                return;
            #endregion

            __result += __instance.ItemCount(__instance.GetEquippedAmmunition().ItemID);
        }

        // Multiplicative stacking
        [HarmonyPatch(typeof(Stat), "GetModifier"), HarmonyPrefix]
        static bool Stat_GetModifier_Pre(Stat __instance, ref float __result, ref IList<Tag> _tags, ref int baseModifier)
        {
            #region quit
            if (!_multiplicativeStacking)
                return true;
            #endregion

            DictionaryExt<string, StatStack> multipliers = __instance.m_multiplierStack;
            __result = baseModifier;
            for (int i = 0; i < multipliers.Count; i++)
            {
                if (multipliers.Values[i].HasEnded)
                    multipliers.RemoveAt(i--);
                else if (multipliers.Values[i].SameTags(_tags))
                {
                    float value = multipliers.Values[i].EffectiveValue;
                    if (!__instance.NullifyPositiveStat || value <= 0f)
                        __result *= (1f + value);
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalMovementModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalMovementModifier_Pre(CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.MovementPenalty, true, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalStaminaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalStaminaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.StaminaUsePenalty, false, true);

        [HarmonyPatch(typeof(CharacterEquipment), "GetTotalManaUseModifier"), HarmonyPrefix]
        static bool CharacterEquipment_GetTotalManaUseModifier_Pre(CharacterEquipment __instance, ref float __result)
        => TryApplyMultiplicativeStacking(__instance, ref __result, slot => slot.EquippedItem.ManaUseModifier, false, _applyArmorTrainingToManaCost);

        // Skip startup video
        [HarmonyPatch(typeof(StartupVideo), "Awake"), HarmonyPrefix]
        static bool StartupVideo_Awake_Pre()
        {
            StartupVideo.HasPlayedOnce = _skipStartupVideos.Value;
            return true;
        }

        // Hide armor slots
        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPrefix]
        static bool CharacterVisuals_EquipVisuals_Pre(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return true;
            #endregion

            // save original hide flags for postfix
            __state = new bool[3];
            __state[0] = _visuals.HideFace;
            __state[1] = _visuals.HideHair;
            __state[2] = _visuals.DisableDefaultVisuals;
            // override hide flags
            if (ShouldArmorSlotBeHidden(_slotID))
            {
                _visuals.HideFace = false;
                _visuals.HideHair = false;
                _visuals.DisableDefaultVisuals = false;
            }
            return true;
        }

        [HarmonyPatch(typeof(CharacterVisuals), "EquipVisuals"), HarmonyPostfix]
        static void CharacterVisuals_EquipVisuals_Post(ref bool[] __state, ref EquipmentSlot.EquipmentSlotIDs _slotID, ref ArmorVisuals _visuals)
        {
            #region quit
            if (_armorSlotsToHide == ArmorSlots.None)
                return;
            #endregion

            // hide chosen pieces of armor
            if (ShouldArmorSlotBeHidden(_slotID))
                _visuals.Hide();

            // restore original hide flags
            _visuals.HideFace = __state[0];
            _visuals.HideHair = __state[1];
            _visuals.DisableDefaultVisuals = __state[2];
        }

        // Remove co-op scaling
        [HarmonyPatch(typeof(CoopStats), "ApplyToCharacter"), HarmonyPrefix]
        static bool CoopStats_ApplyToCharacter_Pre()
    => !_removeCoopScaling;

        [HarmonyPatch(typeof(CoopStats), "RemoveFromCharacter"), HarmonyPrefix]
        static bool CoopStats_RemoveFromCharacter_Pre()
        => !_removeCoopScaling;

        // Remove dodge invulnerability
        [HarmonyPatch(typeof(Character), "DodgeStep"), HarmonyPostfix]
        static void Character_DodgeStep_Post(ref Hitbox[] ___m_hitboxes, ref int _step)
        {
            #region quit
            if (!_removeDodgeInvulnerability)
                return;
            #endregion

            if (_step > 0 && ___m_hitboxes != null)
                foreach (var hitbox in ___m_hitboxes)
                    hitbox.gameObject.SetActive(true);
        }

        // Enemy health reset time
        [HarmonyPatch(typeof(Character), "LoadCharSave"), HarmonyPrefix]
        static bool Character_LoadCharSave_Pre(Character __instance)
        {
            #region quit
            if (!__instance.IsEnemy())
                return true;
            #endregion

            __instance.HoursToHealthReset = _healEnemiesOnLoad ? 0 : DEFAULT_ENEMY_HEALTH_RESET_HOURS;
            return true;
        }
    }
}
/*
 *         [Flags]
        private enum EquipmentStats
        {
            None = 0,
            All = ~0,

            Damage = 1 << 1,
            ImpactDamage = 1 << 2,
            Resistance = 1 << 3,
            ImpactResistance = 1 << 4,
            CorruptionResistance = 1 << 5,
            MovementSpeed = 1 << 6,
            StaminaCost = 1 << 7,
            ManaCost = 1 << 8,
            CooldownReduction = 1 << 9,
        }
 */

/* POUCH
private const float POUCH_CAPACITY = 10f;
static private ModSetting<bool> _pouchToggle;
static private ModSetting<int> _pouchCapacity;
static private ModSetting<bool> _allowOverCapacity;

_pouchToggle = CreateSetting(nameof(_pouchToggle), false);
_pouchCapacity = CreateSetting(nameof(_pouchCapacity), POUCH_CAPACITY.Round(), IntRange(0, 100));
_allowOverCapacity = CreateSetting(nameof(_allowOverCapacity), true);

_pouchToggle.Format("Pouch");
Indent++;
{
    _pouchCapacity.Format("Pouch size", _pouchToggle);
    _allowOverCapacity.Format("Allow over capacity", _pouchToggle);
    Indent--;
}

[HarmonyPatch(typeof(CharacterInventory), "ProcessStart"), HarmonyPostfix]
static void CharacterInventory_ProcessStart_Post(CharacterInventory __instance, ref Character ___m_character)
{
    #region quit
    if (!_pouchToggle)
        return;
    #endregion

    ItemContainer pouch = __instance.Pouch;
    if (___m_character.IsPlayer() && pouch != null)
    {
        pouch.SetField("m_baseContainerCapacity", _pouchCapacity.Value, typeof(ItemContainer));
        pouch.AllowOverCapacity = _allowOverCapacity;
    }
}
*/

/* Extra Controller Quickslots
[HarmonyPatch(typeof(QuickSlotPanel), "InitializeQuickSlotDisplays"), HarmonyPostfix]
static void QuickSlotPanel_InitializeQuickSlotDisplays_Post(QuickSlotPanel __instance, ref QuickSlotDisplay[] ___m_quickSlotDisplays)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    #endregion

    if (__instance.name == BOTH_TRIGGERS_PANEL_NAME)
        for (int i = 0; i < ___m_quickSlotDisplays.Length; i++)
            ___m_quickSlotDisplays[i].RefSlotID = i + 8;
}

[HarmonyPatch(typeof(LocalCharacterControl), "UpdateQuickSlots"), HarmonyPostfix]
static void LocalCharacterControl_UpdateQuickSlots_Pre(ref Character ___m_character)
{
    #region quit
    if (!_extraControllerQuickslots)
        return;
    if (___m_character == null || ___m_character.QuickSlotMngr == null || ___m_character.CharacterUI.IsMenuFocused)
        return;
    #endregion

    int playerID = ___m_character.OwnerPlayerSys.PlayerID;

    if (QuickSlotInstant9(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(8);
    else if (QuickSlotInstant10(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(9);
    else if (QuickSlotInstant11(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(10);
    else if (QuickSlotInstant12(playerID))
        ___m_character.QuickSlotMngr.QuickSlotInput(11);
}

        static void AddQuickSlot12()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform quickSlotsHolder = localPlayer.ControlledCharacter.GetComponent<CharacterQuickSlotManager>().QuickslotTrans;
        if (quickSlotsHolder.Find(QUICKSLOT_12_NAME) != null)
            continue;

        QuickSlot newQuickSlot = GameObject.Instantiate(quickSlotsHolder.Find("1"), quickSlotsHolder).GetComponent<QuickSlot>();
        newQuickSlot.name = QUICKSLOT_12_NAME;
        foreach (var quickSlot in quickSlotsHolder.GetComponents<QuickSlot>())
            quickSlot.ItemQuickSlot = false;
        localPlayer.ControlledCharacter.QuickSlotMngr.Awake();
    }
}

static void DrawExtraControllerQuickslotSwitcher()
{
    foreach (var localPlayer in Utility.LocalPlayers)
    {
        Transform panel = localPlayer.ControlledCharacter.CharacterUI.QuickSlotMenu.FindChild("PanelSwitcher/Controller/LT-RT").transform;
        if (panel.Find(BOTH_TRIGGERS_PANEL_NAME) != null)
            continue;

        // Instantiate
        Transform LT = panel.Find("LT").transform;
        Transform RT = panel.Find("RT").transform;
        foreach (var quickslotPlacer in LT.GetComponentsInChildren<EditorQuickSlotDisplayPlacer>())
            quickslotPlacer.IsTemplate = true;
        Transform LTRT = GameObject.Instantiate(LT.gameObject, panel).transform;
        LTRT.name = BOTH_TRIGGERS_PANEL_NAME;
        Transform imgLT = LTRT.Find("imgLT");
        Transform imgRT = GameObject.Instantiate(RT.Find("imgRT"), LTRT).transform;
        imgRT.name = "imgRT";

        // Change
        panel.Find("LeftDecoration").gameObject.SetActive(false);
        panel.Find("RightDecoration").gameObject.SetActive(false);
        LT.localPosition = new Vector2(-300f, 0);
        RT.localPosition = new Vector2(0f, 0);
        LTRT.localPosition = new Vector2(+300f, 0);
        imgLT.localPosition = new Vector2(-22.5f, 22.5f);
        imgRT.localPosition = new Vector2(+22.5f, 22.5f);
    }
}

*/
