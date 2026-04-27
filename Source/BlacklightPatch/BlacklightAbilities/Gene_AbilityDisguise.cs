using System;
using System.Collections.Generic;
using Verse;
using WVC_XenotypesAndGenes;

namespace BlacklightPatch.BlacklightAbilities
{
    
    public class Gene_AbilityDisguise : Gene_AbilityMorph
    {
        public override void MorpherTrigger(PawnGeneSetHolder geneSet)
        {
            try
            {
                //This goofy ass shit should keep the chimera genes consistent between forms OR IT SHOULD
                
                // var chimeraGene = pawn.genes?.GetFirstGeneOfType<Gene_Chimera>();
                // if (chimeraGene != null)
                // {
                //     Log.Message("Chimera Gene Found");
                // }
                // var chimeraGenes = chimeraGene?.CollectedGenes;
                // if (chimeraGenes != null && chimeraGenes.Count > 0)
                // {
                //     Log.Message("Chimera Gene list found and greater than 0");
                // }

                this.Morpher?.TryMorph(geneSet, true, this.Morpher.IsOneTime);

                // var postMorphChimeraGenes = Morpher?.pawn.genes?.GetFirstGeneOfType<Gene_Chimera>();
                //
                // if (postMorphChimeraGenes != null && chimeraGenes != null)
                // {
                //     Log.Message("Found Post Chimera gene");
                //     postMorphChimeraGenes.TryAddGenesFromList(chimeraGenes);
                // }
                
            }
            catch (Exception ex)
            {
                Log.Error("Failed create form and morph. Reason: " + ex?.ToString());
            }
        }
    }

}