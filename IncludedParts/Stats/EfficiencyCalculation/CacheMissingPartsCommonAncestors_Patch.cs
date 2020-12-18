using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using MSE2.HarmonyPatches;

using Verse;

namespace MSE2.HarmonyPatches
{
    [HarmonyPatch]
    internal static class CacheMissingPartsCommonAncestors_Patch
    {
        private static MethodBase TargetMethod ()
        {
            return typeof( HediffSet ).GetMethod( "CacheMissingPartsCommonAncestors", BindingFlags.NonPublic | BindingFlags.Instance );
        }

		// vanilla behaviour: ignore (dont count as missing) if parent has added parts
		// transpiler: ignore if IgnoreSubPartsUtilities.PartShouldBeIgnored 

		[HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpiler ( IEnumerable<CodeInstruction> instructions )
        {
            foreach ( CodeInstruction instruction in instructions )
            {
                // substitute call to PartOrAnyAncestorHasDirectlyAddedParts with a call to PartShouldBeIgnored
                if ( instruction.Calls( typeof( HediffSet ).GetMethod( nameof( HediffSet.PartOrAnyAncestorHasDirectlyAddedParts ) ) ) )
                {
                    yield return new CodeInstruction( OpCodes.Call, typeof( IgnoreSubPartsUtilities ).GetMethod( nameof( IgnoreSubPartsUtilities.PartShouldBeIgnored ) ) );
                }
                else yield return instruction;
            }
            yield break;
        }

        // equivalent change:

        /*
        private void CacheMissingPartsCommonAncestors()
		{
			if (this.cachedMissingPartsCommonAncestors == null)
			{
				this.cachedMissingPartsCommonAncestors = new List<Hediff_MissingPart>();
			}
			else
			{
				this.cachedMissingPartsCommonAncestors.Clear();
			}
			this.missingPartsCommonAncestorsQueue.Clear();
			this.missingPartsCommonAncestorsQueue.Enqueue(this.pawn.def.race.body.corePart);
			while (this.missingPartsCommonAncestorsQueue.Count != 0)
			{
				BodyPartRecord node = this.missingPartsCommonAncestorsQueue.Dequeue();

                if ( !this.PartShouldBeIgnored( node ) ) // ----- !this.PartOrAnyAncestorHasDirectlyAddedParts( node ) ) // <<<<<<<<<<<<< REPLACED FUNCTION CALL HERE

				{
					Hediff_MissingPart hediffMissingPart = (from x in this.GetHediffs<Hediff_MissingPart>()
					where x.Part == node
					select x).FirstOrDefault<Hediff_MissingPart>();
					if (hediffMissingPart != null)
					{
						this.cachedMissingPartsCommonAncestors.Add(hediffMissingPart);
					}
					else
					{
						for (int index = 0; index < node.parts.Count; index++)
						{
							this.missingPartsCommonAncestorsQueue.Enqueue(node.parts[index]);
						}
					}
				}
			}
		}
        */
    }
}