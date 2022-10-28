using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Verse;
using RimWorld;

using HarmonyLib;
using System.Reflection.Emit;
using System.Reflection;

namespace MSE2
{
    [HarmonyPatch(typeof(HealthCardUtility))]
    [HarmonyPatch("CreateSurgeryBill")]
    static class MakeSurgeryBillLimbAware
    {
        // Equivalent change:
        /*
            private static void CreateSurgeryBill(Pawn medPawn, RecipeDef recipe, BodyPartRecord part)
		    {
		+       if ( RecipeShouldBeLimbAware(recipe) ) 
        +           Bill_Medical bill_Medical = new Bill_MedicalLimbAware(recipe);
        +       else
                    Bill_Medical bill_Medical = new Bill_Medical(recipe);			    
        
                medPawn.BillStack.AddBill(bill_Medical);
			    bill_Medical.Part = part;
        */


        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionList = instructions.ToList();

            var constructorBillMedical = typeof(Bill_Medical).GetConstructor(new Type[] { typeof(RecipeDef), typeof(List<Thing>) });
            var constructorBillMedicalLimbAware = typeof(Bill_MedicalLimbAware).GetConstructor(new Type[] { typeof(RecipeDef), typeof(List<Thing>) });

            // find index of Bill_Medical constructor call
            int constrIndex = instructionList.FindIndex(i => i.opcode == OpCodes.Newobj && (ConstructorInfo)i.operand == constructorBillMedical);

            Label oldConstructor = iLGenerator.DefineLabel();
            instructionList[constrIndex].labels.Add(oldConstructor);

            Label afterOldConstructor = iLGenerator.DefineLabel();
            instructionList[constrIndex + 1].labels.Add(afterOldConstructor);

            // insert if/else before constructor call
            instructionList.InsertRange(constrIndex, new CodeInstruction[]{
                new CodeInstruction(instructionList[constrIndex - 3]),
                new CodeInstruction(instructionList[constrIndex - 2]), // duplicate the RecipeDef on top of the stack
                new CodeInstruction(OpCodes.Call, typeof(MakeSurgeryBillLimbAware) // replace topmost RecipeDef with bool that determines which type should be used
                .GetMethod(nameof(RecipeShouldBeLimbAware), BindingFlags.NonPublic | BindingFlags.Static)),
                new CodeInstruction(OpCodes.Brfalse, oldConstructor), // if recipe should not be limb aware, go to else brach that instantiates Bill_Medical
                new CodeInstruction(OpCodes.Newobj, constructorBillMedicalLimbAware), // then branch: instantiate Bill_MedicalLimbAware
                new CodeInstruction(OpCodes.Br, afterOldConstructor), // skip else branch
            });

            return instructionList;
        }

        private static bool RecipeShouldBeLimbAware(RecipeDef recipe)
        {
            return recipe.Worker is Recipe_InstallArtificialBodyPartWithChildren
                && recipe.ingredients.Exists(i =>
                     i.IsFixedIngredient
                     && i.FixedIngredient.GetCompProperties<CompProperties_IncludedChildParts>() != null);
        }
    }
}
