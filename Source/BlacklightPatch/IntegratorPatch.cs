using RimWorld;
using Verse;
using HarmonyLib;
using WVC_XenotypesAndGenes;

namespace BlacklightPatch
{
    [StaticConstructorOnStartup]
    public static class BlacklightPatchInit
    {
        static BlacklightPatchInit() //our constructor
        {
            new Harmony("Yoesph.BlacklightPatch").PatchAll();
        }
    }
    
    [HarmonyPatch(typeof(CompAbilityEffect_ArchiverIntegrator), "Valid")]
    public static class Patch_ArchiverIntegrator_Valid
    {
        // Return false = skip original, true = run original
        public static bool Prefix(LocalTargetInfo target, bool throwMessages, ref bool __result, CompAbilityEffect_ArchiverIntegrator __instance)
        {
            Pawn pawn = target.Pawn;
            if (pawn == null) { __result = false; return false; }

            // Keep the lodger check
            if (QuestUtility.IsQuestLodger(__instance.parent.pawn) || QuestUtility.IsQuestLodger(pawn))
            {
                if (throwMessages)
                    Messages.Message("WVC_XaG_PawnIsQuestLodgerMessage".Translate(pawn.Named("PAWN")), pawn, MessageTypeDefOf.RejectInput, false);
                __result = false; return false;
            }

            // Keep the human check
            if (!pawn.IsHuman())
            {
                if (throwMessages)
                    Messages.Message("WVC_PawnIsAndroidCheck".Translate(), __instance.parent.pawn, MessageTypeDefOf.RejectInput, false);
                __result = false; return false;
            }

            // CHANGED: only allow downed or deathresting — opinion no longer matters
            if (!pawn.Downed && !pawn.Deathresting)
            {
                if (throwMessages)
                    Messages.Message("MessageCantUseOnResistingPerson".Translate(__instance.parent.def.Named("ABILITY")), pawn, MessageTypeDefOf.RejectInput, false);
                __result = false; return false;
            }

            // Skip the original method entirely, call base manually via __result = true
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Gene_ChimeraArchiverCopy), "TickInterval")]
    public static class Patch_Gene_ChimeraArchiverCopy_TickInterval
    {
        public static bool Prefix(int delta, Gene_ChimeraArchiverCopy __instance)
        {
            //Change interval of new genes from 2.3 days to 4 hours
            if (!__instance.pawn.IsHashIntervalTick(10000, delta) || __instance.pawn.Faction != Faction.OfPlayer)
                return false;
            __instance.SelfCopy();
            return false;
        }
    }

}