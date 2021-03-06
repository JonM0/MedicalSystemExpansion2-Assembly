using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using HarmonyLib;

using Verse;


namespace MSE2.BackCompatibility
{
    [HarmonyPatch( typeof( Verse.BackCompatibility ), nameof( Verse.BackCompatibility.BackCompatibleDefName ) )]
    internal static class ConvertDefNames
    {

        [HarmonyPostfix]
        internal static void Convert ( ref string __result, Type defType )
        {
            try
            {
                if ( defType == typeof( ThingDef ) || defType == typeof( HediffDef ) )
                {
                    switch ( __result )
                    {
                    // all
                    case "PowerArm":
                    case "AdvancedPowerArm":
                    case "AdvancedBionicArm":
                        __result = "BionicArm";
                        break;

                    case "AdvancedBionicHand":
                        __result = "BionicHand";
                        break;

                    case "AdvancedBionicFinger":
                        __result = "BionicFinger";
                        break;

                    case "AdvancedBionicLeg":
                        __result = "BionicLeg";
                        break;

                    case "AdvancedBionicFoot":
                        __result = "BionicFoot";
                        break;

                    case "AdvancedBionicToe":
                        __result = "BionicToe";
                        break;

                    case "AdvancedPowerClaw":
                        __result = "PowerClaw";
                        break;

                    // epoef royalty
                    case "EPOE_ScytherHandTalon":
                    case "EPOE_AdvancedHandTalon":
                        __result = "HandTalonModule";
                        break;
                    case "EPOE_ScytherElbowBlade":
                    case "EPOE_AdvancedElbowBlade":
                        __result = "ElbowBladeModule";
                        break;
                    case "EPOE_ScytherKneeSpike":
                    case "EPOE_AdvancedKneeSpike":
                        __result = "KneeSpikeModule";
                        break;

                    case "EPOE_AdvancedDrillArm":
                        __result = "DrillArm";
                        break;
                    case "EPOE_AdvancedFieldHand":
                        __result = "FieldHand";
                        break;

                    // rbse
                    case "ArtificialHumerus":
                        __result = "SimpleProstheticHumerus";
                        break;
                    case "ArtificialRadius":
                        __result = "SimpleProstheticRadius";
                        break;
                    case "ArtificialTibia":
                        __result = "SimpleProstheticTibia";
                        break;
                    case "ArtificialFemur":
                        __result = "SimpleProstheticFemur";
                        break;
                    case "ArtificialClavicle":
                        __result = "SimpleProstheticClavicle";
                        break;
                    case "ArtificialSternum":
                        __result = "SimpleProstheticSternum";
                        break;

                    // CONN
                    case "Trunken_hediff_PowerArms":
                    case "ANN_PowerArms":
                        __result = "BionicArm";
                        break;

                    default:
                        break;
                    }
                }
            }
            catch ( Exception ex )
            {
                Log.Error( "[MSE2] Error trying to convert def names: " + ex );
                throw;
            }
        }

    }
}
