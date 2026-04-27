using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using WVC_XenotypesAndGenes;

namespace BlacklightPatch.BlacklightAbilities
{
  public class CompAbilityEffect_Consume : CompAbilityEffect_ArchiverDependant
  {
  
    public Gene_Chimera ChimeraGene => this.parent?.pawn?.genes?.GetFirstGeneOfType<Gene_Chimera>();
  
    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
      base.Apply(target, dest);

      var pawn = target.Thing is Corpse corpse ? corpse.InnerPawn : target.Pawn;
      
      if (pawn == null || this.Archiver == null || this.ChimeraGene == null)
        return;
      this.DevourTarget(pawn);
    }

    private void DevourTarget(Pawn victim)
    {
      string str = "start";
      try
      {
        Pawn pawn = this.parent.pawn;

        //Archive their conciousness if they are alive, otherwise just get their genes
        if (victim.Dead)
        {
          var chimeraGeneChance = 25;
          switch (victim.Corpse.GetComp<CompRottable>()?.Stage)
          {
            case RotStage.Fresh:
              // chimeraGeneChance = 50;
              break;
            case RotStage.Rotting:
              chimeraGeneChance = 13;
              break;
            case RotStage.Dessicated:
              chimeraGeneChance = 5;
              break;
          }
          Gene_Chimera chimeraGene = this.ChimeraGene;
          str = "try copy pawn genes";
        
          //Only copy a portion of the genes off of a dead body
          var localGenes = new List<Gene>();
          var rand = new Random();
          foreach (var gene in victim.genes.GenesListForReading)
          {
            var chance = rand.Next(1, 101);
            if (chance <= chimeraGeneChance)
            {
              localGenes.Add(gene);
            }
          }
        
          chimeraGene?.TryAddGenesFromList(localGenes);
          
          //Now do the same for the chimera genes
          
          str = "try copy chimera genes";
          
          var localChimeraGenes = new List<GeneDef>();
          var victimChimeraGenes = victim.genes?.GetFirstGeneOfType<Gene_Chimera>()?.CollectedGenes;
          if (victimChimeraGenes != null && victimChimeraGenes.Count > 0)
          {
            foreach (var gene in victimChimeraGenes)
            {
              var chance = rand.Next(1, 101);
              if (chance <= chimeraGeneChance)
              {
                localChimeraGenes.Add(gene);
              }
            }

            chimeraGene?.TryAddGenesFromList(localChimeraGenes);
          }
          
          str = "reset xenotype";
          // float factor = (float) victim.genes.GenesListForReading.Count * 0.01f;
          ReimplanterUtility.SetXenotype(victim, XenotypeDefOf.Baseliner);
          
          str = "drop apparel";
          victim.apparel?.DropAll(victim.Position, dropLocked: false);
          
          str = "meat boom";
          MiscUtility.MeatSplatter(victim, FleshbeastUtility.MeatExplosionSize.Large, 7);
          
          victim.Corpse?.Kill(new DamageInfo?(new DamageInfo(DamageDefOf.ExecutionCut, 99999f, 9999f, instigator: (Thing) pawn)), (Hediff) null);
          
          str = "message";
          Messages.Message((string) "WVC_XaG_GeneManeater_VictimEated".Translate((NamedArgument) victim.NameShortColored), (LookTargets) (Thing) victim, MessageTypeDefOf.NeutralEvent, false);
        }
        else
        {
          
          str = "change goodwill";
          if (victim.HomeFaction != null && !victim.HomeFaction.IsPlayer && !victim.HostileTo(pawn.Faction) || victim.IsQuestLodger())
          {
            int goodwillChange = (victim.RaceProps.Humanlike ? -29 : -21) * (victim.guilt.IsGuilty ? 1 : 2);
            if (victim.kindDef.factionHostileOnDeath || victim.kindDef.factionHostileOnKill && !victim.guilt.IsGuilty)
              goodwillChange = pawn.Faction.GoodwillToMakeHostile(victim.HomeFaction);
            victim.HomeFaction.TryAffectGoodwillWith(pawn.Faction, goodwillChange, reason: RimWorld.HistoryEventDefOf.MemberKilled);
          }
          
          str = "try archive";
          if (!TryArchiveSelectedPawnModified(victim, this.parent.pawn, this.Archiver))
            return;
          
          str = "message";
          Messages.Message((string) "WVC_XaG_GeneArchiverIntegrator_Succes".Translate((NamedArgument) victim.NameShortColored), (LookTargets) (Thing) victim, MessageTypeDefOf.NeutralEvent, false);
          
          str = "meat boom";
          MiscUtility.MeatSplatter(victim, FleshbeastUtility.MeatExplosionSize.Large, 7);
        }
      
        
        //Add some kind of biomass shit here

        var biomassAmt = victim.BodySize * victim.health.summaryHealth.SummaryHealthPercent;
        
        str = "get food";
        if (pawn.TryGetNeedFood(out var food2))
        {
          
          food2.CurLevel += biomassAmt;
        }
        
        HealingUtility.RemoveAllRemovableBadHediffs(pawn);
      }
      catch (Exception ex)
      {
        Log.Error($"Failed archvie target: {victim.Name?.ToString()}. On phase: {str}. Reason: {ex?.ToString()}");
      }
    }

    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
      var pawn = target.Thing is Corpse corpse ? corpse.InnerPawn : target.Pawn;
      if (pawn == null)
        return false;
      if (this.parent.pawn.IsQuestLodger() || pawn.IsQuestLodger())
      {
        if (throwMessages)
          Messages.Message((string) "WVC_XaG_PawnIsQuestLodgerMessage".Translate((NamedArgument) (Thing) pawn), (LookTargets) (Thing) pawn, MessageTypeDefOf.RejectInput, false);
        return false;
      }
      if (!pawn.IsHuman())
      {
        if (throwMessages)
          Messages.Message((string) "WVC_PawnIsAndroidCheck".Translate(), (LookTargets) (Thing) this.parent.pawn, MessageTypeDefOf.RejectInput, false);
        return false;
      }
      //To be allowed to consume a target they need to either have max opinion, be downed, deathresting, have less than
      //or equal to 10% total HP, or be a fresh corpse
      if (pawn.relations.OpinionOf(this.parent.pawn) >= 100 || pawn.Downed || pawn.Deathresting || 
          pawn.health.summaryHealth.SummaryHealthPercent <= 0.1f || pawn.Dead)
        return base.Valid(target, throwMessages);
      if (throwMessages)
        Messages.Message((string) "MessageCantUseOnResistingPerson".Translate(this.parent.def.Named("ABILITY")), (LookTargets) (Thing) pawn, MessageTypeDefOf.RejectInput, false);
      return false;
    }

    public override Window ConfirmationDialog(LocalTargetInfo target, Action confirmAction)
    {
      return (Window) Dialog_MessageBox.CreateConfirmation("WVC_XaG_WarningPawnWillBeArchived".Translate(target.Pawn.Named("PAWN")), confirmAction, true);
    }
    
    public bool TryArchiveSelectedPawnModified(Pawn toHold, Pawn nextOwner, Gene_Archiver archive)
    {
      PawnContainerHolder newSet = new PawnContainerHolder();

      var genesEndogenes = new Gene[toHold.genes.Endogenes.Count];
      toHold.genes.Endogenes.CopyTo(genesEndogenes);

      //Switch all Endogenes to xenogenes so ONLY blacklight genes are germaline
      foreach (var gene in genesEndogenes)
      {
        if (gene?.def == null) continue;
        toHold.genes.RemoveGene(gene);
        toHold.genes.AddGene(gene.def, true);
      }
      
      // if (toHold.genes?.GetFirstGeneOfType<Gene_Archiver>() == null)
      //   toHold.genes.AddGene(archive.def, true);

      //Add all the blacklight genes so it basically acts like a disguise
      foreach (var gene in nextOwner.genes.Endogenes)
      {
        toHold.genes.AddGene(gene.def, false);
      }
      
      HealingUtility.RemoveAllRemovableBadHediffs(toHold);
      
      archive.SaveFormID((PawnGeneSetHolder) newSet);
      if (!newSet.TrySetContainer(nextOwner, toHold))
      {
        Log.Error("Failed set pawn container.");
        nextOwner?.Destroy(DestroyMode.Vanish);
        return false;
      }
      archive.AddSetHolder((PawnGeneSetHolder) newSet);
      return true;
    }
  }
}
