﻿using Kingmaker.Blueprints;
using System;
using HarmonyLib;
using Kingmaker.UnitLogic.Class.Kineticist;
using Kingmaker.Enums;
using Kingmaker.Blueprints.Items.Weapons;
using Kingmaker.UnitLogic.Class.Kineticist.ActivatableAbility;
using Kingmaker.Items;
using Kingmaker.Blueprints.Items.Armors;
using Shared;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace DarkCodex
{
    [PatchInfo(Severity.Harmony, "Patch: Fix Polymorph Gather", "makes it so polymorphed creatures can use Gather Power and creatures with hands Kinetic Blade", false)]
    [HarmonyPatch]
    public class Patch_FixPolymorphGather
    {
        [HarmonyPatch(typeof(RestrictionCanGatherPower), nameof(RestrictionCanGatherPower.IsAvailable))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler1(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original)
        {
            var data = new TranspilerTool(instructions, generator, original);
            data.ReplaceAllCalls(typeof(UnitBody), nameof(UnitBody.IsPolymorphed), Patch1);
            return data;
        }
        public static bool Patch1(UnitBody __instance)
        {
            return false;
        }

        [HarmonyPatch(typeof(RestrictionCanUseKineticBlade), nameof(RestrictionCanUseKineticBlade.IsAvailable), [typeof(UnitDescriptor)])]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase original) => Transpiler1(instructions, generator, original);

        //[HarmonyPatch(typeof(RestrictionCanGatherPower), nameof(RestrictionCanGatherPower.IsAvailable))]
        //[HarmonyPrefix]
        public static bool Prefix1(RestrictionCanGatherPower __instance, ref bool __result)
        {
            UnitPartKineticist unitPartKineticist = __instance.Owner.Get<UnitPartKineticist>();
            if (!unitPartKineticist)
            {
                __result = false;
                return false;
            }
            UnitBody body = __instance.Owner.Body;
            if (body.PrimaryHand.MaybeItem is ItemEntityWeapon weapon
                && !weapon.IsMonkUnarmedStrike
                && (weapon == null || weapon.Blueprint.Category != WeaponCategory.KineticBlast))
            {
                __result = false;
                return false;
            }
            ItemEntity weapon2 = body.SecondaryHand.MaybeItem;
            if (weapon2 != null)
            {
                ArmorProficiencyGroup? armorProficiencyGroup = body.SecondaryHand.MaybeShield?.Blueprint.Type.ProficiencyGroup;
                if (armorProficiencyGroup != null)
                {
                    if (!(armorProficiencyGroup.GetValueOrDefault() == ArmorProficiencyGroup.TowerShield & armorProficiencyGroup != null))
                    {
                        __result = unitPartKineticist.CanGatherPowerWithShield;
                        return false;
                    }
                }
                __result = false;
                return false;
            }
            __result = true;
            return false;
        }

        //[HarmonyPatch(typeof(RestrictionCanUseKineticBlade), nameof(RestrictionCanUseKineticBlade.IsAvailable), new Type[0])]
        //[HarmonyPrefix]
        public static bool Prefix2(RestrictionCanUseKineticBlade __instance, ref bool __result)
        {
            var unit = __instance.Owner;
            var body = unit.Body;
            if (body.IsPolymorphed && !body.IsPolymorphKeepSlots || !body.HandsAreEnabled)
            {
                __result = false;
                return false;
            }
            UnitPartKineticist unitPartKineticist = unit.Get<UnitPartKineticist>();
            if (!unitPartKineticist)
            {
                __result = false;
                return false;
            }
            ItemEntityWeapon maybeWeapon = body.PrimaryHand.MaybeWeapon;
            BlueprintItemWeapon blueprintItemWeapon = maybeWeapon?.Blueprint;
            bool flag = blueprintItemWeapon.GetComponent<WeaponKineticBlade>() != null;
            if (body.PrimaryHand.MaybeItem != null && !flag)
            {
                __result = false;
                return false;
            }
            AddKineticistBlade addKineticistBlade = __instance.Fact.Blueprint.Buff.GetComponent<AddKineticistBlade>().Or(null);
            BlueprintItemWeapon blueprintItemWeapon2 = addKineticistBlade?.Blade;
            if (blueprintItemWeapon2 == null)
            {
                __result = false;
                return false;
            }
            if (blueprintItemWeapon != blueprintItemWeapon2 || !unitPartKineticist.IsBladeActivated)
            {
                WeaponKineticBlade weaponKineticBlade = blueprintItemWeapon2.GetComponent<WeaponKineticBlade>().Or(null);
                KineticistAbilityBurnCost? kineticistAbilityBurnCost = null;
                if (((AbilityKineticist.CalculateAbilityBurnCost(weaponKineticBlade?.GetActivationAbility(unit)) != null) ? kineticistAbilityBurnCost.GetValueOrDefault().Total : 0) > unitPartKineticist.LeftBurnThisRound)
                {
                    __result = false;
                    return false;
                }
            }
            __result = true;
            return false;
        }
    }
}
