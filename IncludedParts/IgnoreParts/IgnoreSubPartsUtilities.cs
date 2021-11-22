using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using RimWorld;

using Verse;

namespace MSE2
{
    internal static class IgnoreSubPartsUtilities
    {
        public static bool PartShouldBeIgnored(this HediffSet set, BodyPartRecord bodyPart)
        {
            if (bodyPart != null && bodyPart.parent != null)
            {
                IgnoreSubParts modExt = set.hediffs
                    .Find(h => h != null && h.Part == bodyPart.parent && (h.def?.HasModExtension<IgnoreSubParts>() ?? false))// added part on parent bodypartrecord
                    ?.def.GetModExtension<IgnoreSubParts>();

                return
                    (modExt != null && modExt.ignoredSubParts.Contains(bodyPart.def))
                    || set.PartShouldBeIgnored(bodyPart.parent);
            }
            return false;
        }

        public static IEnumerable<BodyPartRecord> AllChildParts(this BodyPartRecord bodyPart)
        {
            foreach (BodyPartRecord p in bodyPart.parts)
            {
                yield return p;
                foreach (BodyPartRecord p2 in p.AllChildParts())
                    yield return p2;
            }
            yield break;
        }

        private static List<BodyPartDef> AllChildPartDefs(this BodyPartDef bodyPartDef, IEnumerable<BodyDef> bodies = null, bool recursive = true)
        {
            List<BodyPartDef> list = new();

            foreach (BodyDef bodyDef in bodies ?? DefDatabase<BodyDef>.AllDefs)
            {
                foreach (BodyPartRecord partRecord in bodyDef.AllParts)
                {
                    if (partRecord.def == bodyPartDef)
                    {
                        list.AddRange(
                            (recursive ? partRecord.AllChildParts() : partRecord.parts)
                            .Select(r => r.def)
                            .Where(d => !list.Contains(d)));
                    }
                }
            }

            list.RemoveDuplicates();

            return list;
        }

        public static bool NullOrEmpty<T>(this ICollection<T> c)
        {
            return c == null || c.Count == 0;
        }

        /// <summary>
        /// Makes limb prostheses without a CompProperties_IncludedChildParts behave like they would in vanilla
        /// </summary>
        public static void IgnoreAllNonCompedSubparts()
        {
            try
            {
                List<string> brokenMods = new();

                StringBuilder unpatchedDefs = new();

                foreach ((RecipeDef recipeDef, IgnoreSubParts oldME) in
                    from r in DefDatabase<RecipeDef>.AllDefs
                    where r != null
                    where r.IsSurgery
                    where
                        // has ingredients
                        r.fixedIngredientFilter?.AllowedThingDefs != null
                        // no ingredient has a CompProperties_IncludedChildParts
                        && !r.fixedIngredientFilter.AllowedThingDefs
                            .Select(t => t.GetCompProperties<CompProperties_IncludedChildParts>())
                            .Any(c => c != null)
                    where
                        // adds a hediff
                        r.addsHediff?.hediffClass != null
                        // of type addedpart
                        && typeof(Hediff_AddedPart).IsAssignableFrom(r.addsHediff.hediffClass)
                    let me = r.addsHediff.GetModExtension<IgnoreSubParts>()
                    where
                        // that does not already ignore parts
                        me == null || me.ignoreAll
                    select (r, me))
                {
                    IgnoreSubParts modExt = oldME ?? new IgnoreSubParts();

                    // add all the subparts this prosthesis could have
                    if (recipeDef.appliedOnFixedBodyParts != null)
                        foreach (BodyPartDef partDef in recipeDef.appliedOnFixedBodyParts)
                        {
                            modExt.ignoredSubParts.AddRange(partDef.AllChildPartDefs(recipeDef.AllRecipeUsers.Select(ru => ru.race.body)));
                        }

                    // found any and it wasnt already present
                    if (!modExt.ignoredSubParts.NullOrEmpty() && oldME == null)
                    {
                        // add the modextension
                        if (recipeDef.addsHediff.modExtensions == null)
                            recipeDef.addsHediff.modExtensions = new List<DefModExtension>();

                        recipeDef.addsHediff.modExtensions.Add(modExt);

                        // log only for humanlike
                        //if ( recipeDef.AllRecipeUsers.Any( td => td.race.Humanlike ) )
                        {
                            unpatchedDefs.AppendLine(
                                string.Format("<{0}> from \"{1}\": {2}",
                                    recipeDef.addsHediff.defName,
                                    recipeDef.modContentPack?.Name ?? "???",
                                    string.Join(", ", modExt.ignoredSubParts.Select(p => p.label))
                                    )
                                );

                            if (!brokenMods.Contains(recipeDef.modContentPack?.Name))
                                brokenMods.Add(recipeDef.modContentPack?.Name);
                        }
                    }
                }

                if (brokenMods.Count > 0)
                {
                    Log.Message(string.Format("[MSE2] Some prostheses that have not been patched were detected in mods: {0}. They will default to vanilla behaviour.\n\n{1}", string.Join(", ", brokenMods), unpatchedDefs));
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MSE2] Exception applying IgnoreAllNonCompedSubparts: " + ex);
            }
        }

        /// <summary>
        /// Adds IgnoreSubParts to ignore subparts that dont have an installable standard subpart on prostheses with CompProperties_IncludedChildParts
        /// </summary>
        public static void IgnoreUnsupportedSubparts()
        {
            try
            {
                StringBuilder logMessage = new();
                int protCount = 0;

                logMessage.AppendLine("[MSE2] Ignoring unsupported sub-parts:");

                foreach (var (comp, hediff) in from t in DefDatabase<ThingDef>.AllDefs
                                               let c = t.GetCompProperties<CompProperties_IncludedChildParts>()
                                               where c != null
                                               from h in DefDatabase<HediffDef>.AllDefs
                                               where h.spawnThingOnRemoved == t
                                               select (c, h))
                {
                    var standardPartDests = comp.standardChildren
                        .SelectMany(IncludedPartsUtilities.InstallationDestinations)
                        .Select(l => l.PartDef)
                        .ToHashSet();

                    var hediffParts = IncludedPartsUtilities.SurgeryToInstall(hediff)
                        .SelectMany(s => s.appliedOnFixedBodyParts)
                        .SelectMany(p => p.AllChildPartDefs(comp.CompatibleBodyDefs, recursive: false))
                        .ToList();

                    hediffParts.RemoveAll(standardPartDests.Contains);

                    if (hediffParts.Count > 0)
                    {
                        if (hediff.GetModExtension<IgnoreSubParts>() == null)
                        {
                            // add modextensions
                            hediff.modExtensions ??= new List<DefModExtension>();
                            // add IgnoreSubParts
                            hediff.modExtensions.Add(new IgnoreSubParts());
                        }

                        // add the ignored parts
                        var me = hediff.GetModExtension<IgnoreSubParts>();
                        me.ignoredSubParts.AddRange(hediffParts);

                        // log
                        logMessage.AppendFormat("{0}: {1}", hediff.defName, string.Join(", ", hediffParts)).AppendLine();
                        protCount++;
                    }

                }

                if (protCount > 0)
                {
                    Log.Message(logMessage.ToString());
                }

                FinishedIgnoring = true;
            }
            catch (Exception ex)
            {
                Log.Error("[MSE2] Exception applying IgnoreUnsupportedSubparts: " + ex);
            }
        }

        public static bool FinishedIgnoring { get; private set; } = false;
    }
}