﻿namespace Vheos.Mods.Outward;

public class Damage : AMod
{
    #region Settings
    private static Dictionary<Team, DamageSettings> _settingsByTeam;
    protected override void Initialize()
    {
        _settingsByTeam = new();
        foreach (var team in Utility.GetEnumValues<Team>())
            _settingsByTeam[team] = new DamageSettings(this, team);
    }
    protected override void LoadPreset(string presetName)
    {
        switch (presetName)
        {
            case nameof(Preset.Vheos_CoopSurvival):
                ForceApply();
                _settingsByTeam[Team.Players].HealthDamage.Value = 50;
                _settingsByTeam[Team.Players].StabilityDamage.Value = 50;
                _settingsByTeam[Team.Players].FFHealthDamage.Value = 20;
                _settingsByTeam[Team.Players].FFStabilityDamage.Value = 40;

                _settingsByTeam[Team.Enemies].HealthDamage.Value = 80;
                _settingsByTeam[Team.Enemies].StabilityDamage.Value = 120;
                _settingsByTeam[Team.Enemies].FFHealthDamage.Value = 20;
                _settingsByTeam[Team.Enemies].FFStabilityDamage.Value = 40;
                break;
        }
    }
    #endregion

    #region Formatting
    protected override string SectionOverride
    => ModSections.Combat;
    protected override string Description
    => "• Change players/enemies health/stability damage multipliers" +
       "\n• Enable friendly fire between players" +
       "\n• Decrease friendly fire between enemies";
    protected override void SetFormatting()
    {
        foreach (var kvp in _settingsByTeam)
            kvp.Value.Format();
    }
    #endregion

    #region Utility
    private static bool IsPlayersFriendlyFireEnabled
        => _settingsByTeam[Team.Players].FFHealthDamage > 0
        || _settingsByTeam[Team.Players].FFStabilityDamage > 0;
    private static void TryOverrideElligibleFaction(ref bool result, Character defender, Character attacker)
    {
        if (result
        || defender == null
        || defender == attacker
        || !defender.IsAlly()
        || !IsPlayersFriendlyFireEnabled)
            return;

        result = true;
    }
    private enum Team
    {
        Players,
        Enemies,
    }
    private class DamageSettings : PerValueSettings<Damage, Team>
    {
        public ModSetting<int> HealthDamage, FFHealthDamage;
        public ModSetting<int> StabilityDamage, FFStabilityDamage;
        public DamageSettings(Damage mod, Team value, bool isToggle = false) : base(mod, value, isToggle)
        {
            HealthDamage = CreateSetting(nameof(HealthDamage), 100, _mod.IntRange(0, 200));
            StabilityDamage = CreateSetting(nameof(StabilityDamage), 100, _mod.IntRange(0, 200));
            FFHealthDamage = CreateSetting(nameof(FFHealthDamage), value == Team.Players ? 0 : 100, _mod.IntRange(0, 200));
            FFStabilityDamage = CreateSetting(nameof(FFStabilityDamage), value == Team.Players ? 0 : 100, _mod.IntRange(0, 200));
        }
        public override void Format()
        {
            base.Format();

            string lowerCaseTeam = _value.ToString().ToLower();
            Header.Description =
                $"Multipliers for damage dealt by {lowerCaseTeam}";
            using (Indent)
            {
                HealthDamage.Format("Health");
                StabilityDamage.Format("Stability");
                _mod.CreateHeader("Friendly Fire").Description =
                     $"Additional multipliers for damage dealt by {lowerCaseTeam} to other {lowerCaseTeam}";
                using (Indent)
                {
                    FFHealthDamage.Format("Health");
                    FFStabilityDamage.Format("Stability");
                }
            }
        }
    }
    #endregion

    #region Hooks
    [HarmonyPostfix, HarmonyPatch(typeof(Weapon), nameof(Weapon.ElligibleFaction), new[] { typeof(Character) })]
    private static void Weapon_ElligibleFaction_Post(Weapon __instance, ref bool __result, Character _character)
        => TryOverrideElligibleFaction(ref __result, _character, __instance.OwnerCharacter);

    [HarmonyPostfix, HarmonyPatch(typeof(MeleeHitDetector), nameof(MeleeHitDetector.ElligibleFaction), new[] { typeof(Character) })]
    private static void MeleeHitDetector_ElligibleFaction_Post(MeleeHitDetector __instance, ref bool __result, Character _character)
        => TryOverrideElligibleFaction(ref __result, _character, __instance.OwnerCharacter);

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.OnReceiveHitCombatEngaged))]
    private static bool Character_OnReceiveHitCombatEngaged_Pre(Character __instance, Character _dealerChar)
        => _dealerChar == null
        || !_dealerChar.IsAlly()
        || !IsPlayersFriendlyFireEnabled;

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.VitalityHit))]
    private static void Character_VitalityHit_Pre(Character __instance, Character _dealerChar, ref float _damage)
    {
        if (_dealerChar != null && _dealerChar.IsEnemy()
        || _dealerChar == null && __instance.IsAlly())
        {
            _damage *= _settingsByTeam[Team.Enemies].HealthDamage / 100f;
            if (__instance.IsEnemy())
                _damage *= _settingsByTeam[Team.Enemies].FFHealthDamage / 100f;
        }
        else
        {
            _damage *= _settingsByTeam[Team.Players].HealthDamage / 100f;
            if (__instance.IsAlly())
                _damage *= _settingsByTeam[Team.Players].FFHealthDamage / 100f;
        }
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Character), nameof(Character.StabilityHit))]
    private static void Character_StabilityHit_Pre(Character __instance, Character _dealerChar, ref float _knockValue)
    {
        if (_dealerChar != null && _dealerChar.IsEnemy()
        || _dealerChar == null && __instance.IsAlly())
        {
            _knockValue *= _settingsByTeam[Team.Enemies].StabilityDamage / 100f;
            if (__instance.IsEnemy())
                _knockValue *= _settingsByTeam[Team.Enemies].FFStabilityDamage / 100f;
        }
        else
        {
            _knockValue *= _settingsByTeam[Team.Players].StabilityDamage / 100f;
            if (__instance.IsAlly())
                _knockValue *= _settingsByTeam[Team.Players].FFStabilityDamage / 100f;
        }
    }
    #endregion
}