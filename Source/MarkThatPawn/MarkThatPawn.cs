using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using MarkThatPawn.Harmony;
using MarkThatPawn.MarkerRules;
using RimWorld;
using UnityEngine;
using Verse;

namespace MarkThatPawn;

[StaticConstructorOnStartup]
public static class MarkThatPawn
{
    public enum PawnType
    {
        Default,
        Colonist,
        Prisoner,
        Slave,
        Enemy,
        Neutral,
        Vehicle
    }

    public const char BlobSplitter = ';';
    public const char RuleItemsSplitter = '|';
    public const char RuleAlternateItemsSplitter = '€';
    public const char RuleInternalSplitter = '$';
    public const char MarkerBlobSplitter = '£';
    public const char OverrideRuleSplitter = '§';

    public const float ButtonIconSizeFactor = 0.8f;

    private const int rotationInterval = 250;

    public static readonly List<TraitDef> AllTraits;
    public static readonly List<SkillDef> AllSkills;
    public static readonly List<ThingDef> AllAnimals;
    public static readonly List<HediffDef> AllDynamicHediffs;
    public static readonly List<HediffDef> AllStaticHediffs;
    public static readonly List<ThingDef> AllValidWeapons;
    public static readonly List<ThingDef> AllValidApparels;
    public static readonly List<ThingDef> AllExplosiveRangedWeapons;
    public static readonly List<ThingDef> AllThrownWeapons;
    public static readonly List<ThingDef> AllArmoredApparel;
    public static readonly List<ThingDef> AllPsycastApparel;
    public static readonly List<ThingDef> AllMechanatorApparel;
    public static readonly List<ThingDef> AllRoyalApparel;
    public static readonly List<ThingDef> AllEnviromentalProtectionApparel;
    public static readonly List<ThingDef> AllBasicApparel;
    public static readonly Texture2D MarkerIcon;
    public static readonly Texture2D CancelIcon;
    public static readonly Texture2D AddIcon;
    public static readonly Texture2D RemoveIcon;
    public static readonly Texture2D ExpandIcon;
    public static readonly List<Mesh> SizeMesh;
    public static readonly List<MarkerDef> MarkerDefs;
    private static readonly Dictionary<Pawn, MarkerDef> pawnMarkerCache;
    private static readonly Dictionary<Pawn, Mesh> pawnMeshCache;
    private static readonly Dictionary<Pawn, float> pawnExpandCache;
    public static readonly Dictionary<Faction, Material> FactionMaterialCache;
    public static readonly Dictionary<Ideo, Material> IdeoMaterialCache;
    public static readonly bool VehiclesLoaded;
    public static readonly bool TDFindLibLoaded;
    public static readonly Type TDFindLibRuleType;
    private static CameraZoomRange lastCameraZoomRange = CameraZoomRange.Far;
    private static readonly int standardSize;
    private static readonly Texture2D autoIcon;
    private static readonly Texture2D resetIcon;
    public static readonly List<XenotypeDef> AllValidXenotypes;
    public static readonly List<GeneDef> AllValidGenes;
    public static readonly List<GeneCategoryDef> AllValidGeneCategories;
    public static readonly Material MultiIconOverlay;
    public static readonly List<ThingDef> AllMechanoids;

    static MarkThatPawn()
    {
        var harmony = new HarmonyLib.Harmony("Mlie.MarkThatPawn");
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        AllTraits = DefDatabase<TraitDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
        AllSkills = DefDatabase<SkillDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
        if (ModLister.BiotechInstalled)
        {
            AllValidXenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
            AllValidGenes = DefDatabase<GeneDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
            AllValidGeneCategories =
                DefDatabase<GeneCategoryDef>.AllDefsListForReading.OrderBy(def => def.label).ToList();
        }
        else
        {
            AllValidXenotypes = [];
            AllValidGenes = [];
            AllValidGeneCategories = [];
        }

        AllAnimals = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.race?.Animal == true)
            .OrderBy(def => def.label).ToList();
        AllMechanoids = DefDatabase<ThingDef>.AllDefsListForReading.Where(def => def.race?.IsMechanoid == true)
            .OrderBy(def => def.label).ToList();
        AllDynamicHediffs = DefDatabase<HediffDef>.AllDefsListForReading
            .Where(def => def.stages != null && def.stages.Any() && def.spawnThingOnRemoved == null ||
                          def.injuryProps != null)
            .OrderBy(def => def.label).ToList();
        AllStaticHediffs = DefDatabase<HediffDef>.AllDefsListForReading
            .Where(def => !AllDynamicHediffs.Contains(def))
            .OrderBy(def => def.label).ToList();
        MarkerDefs = DefDatabase<MarkerDef>.AllDefsListForReading.Where(def => def.Enabled).OrderBy(def => def.label)
            .ToList();
        pawnMarkerCache = new Dictionary<Pawn, MarkerDef>();
        pawnMeshCache = new Dictionary<Pawn, Mesh>();
        pawnExpandCache = new Dictionary<Pawn, float>();
        FactionMaterialCache = new Dictionary<Faction, Material>();
        IdeoMaterialCache = new Dictionary<Ideo, Material>();
        standardSize = ThingDefOf.Human.size.z;
        MarkerIcon = ContentFinder<Texture2D>.Get("UI/Marker_Icon");
        CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
        autoIcon = ContentFinder<Texture2D>.Get("UI/Icons/DrugPolicy/Scheduled");
        resetIcon = ContentFinder<Texture2D>.Get("UI/Widgets/RotLeft");
        AddIcon = ContentFinder<Texture2D>.Get("ScaledIcons/Add");
        ExpandIcon = ContentFinder<Texture2D>.Get("ScaledIcons/ArrowRight");
        RemoveIcon = ContentFinder<Texture2D>.Get("ScaledIcons/Empty");
        SizeMesh = [];
        for (var i = 1; i <= 50; i++)
        {
            SizeMesh.Add(MeshMakerPlanes.NewPlaneMesh(i / 10f));
        }

        //Log.Message("[MarkThatPawn]: Caching all valid weapons");
        AllValidWeapons = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => !string.IsNullOrEmpty(def.label) && def.IsWeapon)
            .OrderBy(def => def.label).ToList();

        //Log.Message("[MarkThatPawn]: Caching all valid explosive weapons");
        AllExplosiveRangedWeapons = AllValidWeapons
            .Where(def => def.IsRangedWeapon && def.Verbs != null && def.Verbs.Any(properties =>
                properties.CausesExplosion && properties.defaultProjectile?.projectile.arcHeightFactor == 0)).ToList();

        //Log.Message("[MarkThatPawn]: Caching all valid thrown weapons");
        AllThrownWeapons = AllValidWeapons.Where(def =>
            def.Verbs.Any(properties => properties.defaultProjectile?.projectile?.arcHeightFactor > 0)).ToList();

        //Log.Message(
        //    $"[MarkThatPawn]: Found {AllValidWeapons.Count} loaded weapons, {AllExplosiveRangedWeapons.Count} explosive weapons and {AllThrownWeapons.Count} thrown projectiles");

        AllValidApparels = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def => !string.IsNullOrEmpty(def.label) && def.IsApparel)
            .OrderBy(def => def.label).ToList();

        AllArmoredApparel = AllValidApparels.Where(def =>
            def.StatBaseDefined(StatDefOf.ArmorRating_Blunt) ||
            def.StatBaseDefined(StatDefOf.ArmorRating_Sharp) ||
            def.StatBaseDefined(StatDefOf.ArmorRating_Heat)).ToList();

        AllRoyalApparel = ModLister.RoyaltyInstalled
            ? AllValidApparels.Where(def => def.apparel.tags?.Contains("Royal") == true).ToList()
            : [];

        AllPsycastApparel = ModLister.RoyaltyInstalled
            ? AllValidApparels.Where(def =>
                def.equippedStatOffsets?.Any(modifier =>
                    modifier.stat == StatDefOf.PsychicEntropyRecoveryRate && modifier.value > 0) == true).ToList()
            : [];

        AllEnviromentalProtectionApparel = AllValidApparels.Where(def =>
            def.equippedStatOffsets?.Any(modifier =>
                (modifier.stat == StatDefOf.ToxicEnvironmentResistance || modifier.stat == StatDefOf.ToxicResistance) &&
                modifier.value > 0) == true).ToList();

        AllMechanatorApparel = ModLister.BiotechInstalled
            ? AllValidApparels.Where(def => def.apparel.mechanitorApparel).ToList()
            : [];

        AllBasicApparel = AllValidApparels.Except(AllRoyalApparel)
            .Except(AllMechanatorApparel)
            .Except(AllArmoredApparel)
            .Except(AllEnviromentalProtectionApparel)
            .Except(AllPsycastApparel).ToList();

        //Log.Message(
        //    $"[MarkThatPawn]: Found {AllValidApparels.Count} loaded apparel, {AllArmoredApparel.Count} armored apparel, {AllRoyalApparel.Count} royal apparel, " +
        //    $"{AllPsycastApparel.Count} psycast apparel, {AllEnviromentalProtectionApparel.Count} enviromental protection apparel, " +
        //    $"{AllMechanatorApparel.Count} mechanator apparel and {AllBasicApparel.Count} basic apparel ");

        TDFindLibLoaded = ModLister.GetActiveModWithIdentifier("Uuugggg.TDFindLib", true) != null;
        if (TDFindLibLoaded)
        {
            TDFindLibRuleType = AccessTools.TypeByName("TDFindLibRule");
            if (TDFindLibRuleType == null)
            {
                Log.Message(
                    "[MarkThatPawn]: Failed to fetch the TDFindLibRule type, will not add TD FInd Lib support.");
                TDFindLibLoaded = false;
            }
        }

        // Backward compat: load rules from blob strings instead of ExposeData
        foreach (var ruleBlob in MarkThatPawnMod.instance.Settings.AutoRuleBlobs ?? [])
        {
            MarkerRule rule;
            if (!MarkerRule.TryGetRuleTypeFromBlob(ruleBlob, out var type))
            {
                continue;
            }

            //TODO: try to generate this based on AutoRuleTypes
            switch (type)
            {
                case MarkerRule.AutoRuleType.Weapon:
                    rule = new WeaponMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.WeaponType:
                    rule = new WeaponTypeMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Trait:
                    rule = new TraitMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Skill:
                    rule = new SkillMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Relative:
                    rule = new RelativeMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.PawnType:
                    rule = new PawnTypeMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Drafted:
                    rule = new DraftedMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.MentalState:
                    rule = new MentalStateMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.HediffDynamic:
                    rule = new DynamicHediffMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.HediffStatic:
                    rule = new StaticHediffMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Animal:
                    rule = new AnimalMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Mechanoid:
                    rule = new MechanoidMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Gender:
                    rule = new GenderMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Age:
                    rule = new AgeMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Xenotype when ModLister.BiotechInstalled:
                    rule = new XenotypeMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Gene when ModLister.BiotechInstalled:
                    rule = new GeneMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.Apparel:
                    rule = new ApparelMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.ApparelType:
                    rule = new ApparelTypeMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.AnyHediffStatic:
                    rule = new AnyStaticHediffMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.FactionIcon:
                    rule = new FactionIconMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.FactionLeader:
                    rule = new FactionLeaderMarkerRule(ruleBlob);
                    break;
                case MarkerRule.AutoRuleType.IdeologyIcon when ModLister.IdeologyInstalled:
                    rule = new IdeologyIconMarkerRule(ruleBlob);
                    break;
                // No TDFindLib rule will be here for backward compat
                default:
                    continue;
            }

            if (rule.ConfigError)
            {
                Log.Warning(
                    $"Failed to load a marker-rule from blob: \n{ruleBlob}\n{rule.ErrorMessage}\nDisabling the rule.");
                rule.SetEnabled(false);
            }

            MarkThatPawnMod.instance.Settings.AutoRules.Add(rule);
        }

        MultiIconOverlay = MaterialPool.MatFrom("UI/MultipleImageOverlay", ShaderDatabase.MetaOverlay);

        //Log.Message(
        //    $"[MarkThatPawn]: Found {MarkThatPawnMod.instance.Settings.AutoRules.Count} automatic rules defined");

        VehiclesLoaded = ModLister.GetActiveModWithIdentifier("SmashPhil.VehicleFramework") != null;
        if (VehiclesLoaded)
        {
            Log.Message("[MarkThatPawn]: Vehicle Framework detected, adding compatility patch");
            var original = AccessTools.Method("Vehicles.VehicleRenderer:RenderPawnAt");
            var postfix = typeof(VehicleRenderer_RenderPawnAt).GetMethod(nameof(VehicleRenderer_RenderPawnAt.Postfix));
            harmony.Patch(original, postfix: new HarmonyMethod(postfix));
        }
    }

    public static void RenderMarkingOverlay(Pawn pawn, MarkingTracker tracker)
    {
        if (!pawn.Spawned)
        {
            if (pawnExpandCache.ContainsKey(pawn))
            {
                pawnExpandCache.Remove(pawn);
            }

            return;
        }

        pawnExpandCache.TryAdd(pawn, 1f);

        var passedTest = !(MarkThatPawnMod.instance.Settings.ShiftIsPressed ||
                           MarkThatPawnMod.instance.Settings.GameIsPaused ||
                           MarkThatPawnMod.instance.Settings.PawnIsSelected);

        if (!passedTest && MarkThatPawnMod.instance.Settings.ShiftIsPressed)
        {
            passedTest = Event.current.shift;
        }

        if (!passedTest && MarkThatPawnMod.instance.Settings.GameIsPaused)
        {
            passedTest = Find.TickManager.Paused;
        }

        if (!passedTest && MarkThatPawnMod.instance.Settings.PawnIsSelected)
        {
            passedTest = Find.Selector.SelectedPawns.Contains(pawn);
        }

        if (!passedTest)
        {
            return;
        }

        var baseMaterials = new List<Material>();
        var overrideMaterials = new List<Material>();

        var marker = tracker.GlobalMarkingTracker.GetPawnMarking(pawn);
        switch (marker)
        {
            case > 0:
                var markerSet = GetMarkerDefForPawn(pawn);
                if (markerSet == null)
                {
                    tracker.GlobalMarkingTracker.MarkedPawns[pawn] = 0;
                    break;
                }

                if (markerSet.MarkerMaterials.Count < marker)
                {
                    tracker.GlobalMarkingTracker.MarkedPawns[pawn] = 0;
                    break;
                }

                baseMaterials.Add(markerSet.MarkerMaterials[marker - 1]);
                break;
            case -1:
                if (!tracker.GlobalMarkingTracker.AutomaticPawns.TryGetValue(pawn, out var autoString))
                {
                    tracker.GlobalMarkingTracker.MarkedPawns[pawn] = 0;
                    break;
                }

                foreach (var autoRuleString in autoString.Split(MarkerBlobSplitter))
                {
                    if (!TryToConvertStringToMaterial(autoRuleString, out var autoMaterial, pawn))
                    {
                        continue;
                    }

                    baseMaterials.Add(autoMaterial);

                    if (!MarkThatPawnMod.instance.Settings.NormalShowAll)
                    {
                        break;
                    }
                }

                break;
            case -2:
                if (!tracker.GlobalMarkingTracker.CustomPawns.TryGetValue(pawn, out var customString))
                {
                    tracker.GlobalMarkingTracker.MarkedPawns[pawn] = 0;
                    break;
                }

                if (!TryToConvertStringToMaterial(customString, out var customMaterial))
                {
                    tracker.GlobalMarkingTracker.MarkedPawns[pawn] = 0;
                    break;
                }

                baseMaterials.Add(customMaterial);
                break;
        }

        if (tracker.GlobalMarkingTracker.OverridePawns.TryGetValue(pawn, out var overrideString))
        {
            foreach (var overrideRuleString in overrideString.Split(MarkerBlobSplitter))
            {
                var overrideStringTypeless = overrideRuleString.Split(OverrideRuleSplitter)[0];
                if (!TryToConvertStringToMaterial(overrideStringTypeless, out var material))
                {
                    continue;
                }

                overrideMaterials.Add(material);
                if (!MarkThatPawnMod.instance.Settings.SeparateShowAll)
                {
                    break;
                }
            }
        }

        if (!baseMaterials.Any() && !overrideMaterials.Any())
        {
            return;
        }

        var pawnHeight = pawn.def.size.z;
        if (VehiclesLoaded && pawn.def.thingClass.Name.EndsWith("VehiclePawn"))
        {
            pawnHeight = standardSize + (int)Math.Floor((pawn.def.size.z - standardSize) / 2f);
        }

        var drawPos = pawn.DrawPos;
        drawPos.x += MarkThatPawnMod.instance.Settings.XOffset;
        drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor() + 0.28125f;
        drawPos.z += pawnHeight + (MarkThatPawnMod.instance.Settings.IconSize / 3);
        drawPos.z += MarkThatPawnMod.instance.Settings.ZOffset;
        var mesh = getRightSizeMesh(pawn);

        var icons = overrideMaterials.Count + baseMaterials.Count;
        var iconSpaces = icons - 1;

        if (icons > 1 && tracker.GlobalMarkingTracker.ShouldShowMultiMarking(pawn))
        {
            var iconWidth = mesh.vertices[2].x / 0.5f;
            var shouldExpand = MarkThatPawnMod.instance.Settings.ShowWhenSelected &&
                               Find.Selector.SelectedPawns.Contains(pawn);

            if (!shouldExpand && MarkThatPawnMod.instance.Settings.ShowOnShift &&
                Event.current.shift)
            {
                shouldExpand = true;
            }

            if (!shouldExpand && MarkThatPawnMod.instance.Settings.ShowOnPaused &&
                Find.TickManager.Paused)
            {
                shouldExpand = true;
            }

            if (!shouldExpand && MarkThatPawnMod.instance.Settings.ShowWhenHover)
            {
                var tempWidth = (2f + (pawnExpandCache[pawn] * MarkThatPawnMod.instance.Settings.IconSpacingFactor)) *
                                iconWidth * iconSpaces;

                var mouseOverRect = new Rect(drawPos.x - (tempWidth / 2), drawPos.z - mesh.vertices[2].z, tempWidth,
                    mesh.vertices[2].z / 0.5f);
                var mousePositionVector = new Vector2(UI.MouseMapPosition().x, UI.MouseMapPosition().z);
                shouldExpand = mouseOverRect.Contains(mousePositionVector);
            }

            overrideMaterials.AddRange(baseMaterials);

            if (!MarkThatPawnMod.instance.Settings.InvertOrder)
            {
                overrideMaterials.Reverse();
            }

            if (!shouldExpand && MarkThatPawnMod.instance.Settings.RotateIcons && pawnExpandCache[pawn] == 1f)
            {
                var tickInterval = rotationInterval;
                if (!Find.TickManager.Paused)
                {
                    tickInterval *= (int)Find.TickManager.TickRateMultiplier;
                }

                var amountOfTickGroups = GenTicks.TicksGame / tickInterval;
                var material = overrideMaterials[amountOfTickGroups % overrideMaterials.Count];
                renderMarker(pawn, material, drawPos, mesh);
                if (overrideMaterials.Count <= 1)
                {
                    return;
                }

                drawPos.y += 0.00001f;
                renderMarker(pawn, MultiIconOverlay, drawPos, mesh);

                return;
            }

            if (!shouldExpand)
            {
                if (pawnExpandCache[pawn] < 1f)
                {
                    pawnExpandCache[pawn] += 0.1f;
                }
            }
            else
            {
                if (pawnExpandCache[pawn] > 0)
                {
                    pawnExpandCache[pawn] -= 0.1f;
                }
            }

            iconWidth *= 1f + (pawnExpandCache[pawn] * MarkThatPawnMod.instance.Settings.IconSpacingFactor);

            var totalWidth = iconWidth * iconSpaces;
            drawPos.x -= totalWidth / 2;
            foreach (var material in overrideMaterials)
            {
                renderMarker(pawn, material, drawPos, mesh);
                drawPos.x += iconWidth;
                drawPos.y += 0.00001f;
            }

            return;
        }

        if (overrideMaterials.Any())
        {
            renderMarker(pawn, overrideMaterials[0], drawPos, mesh);
            return;
        }

        renderMarker(pawn, baseMaterials[0], drawPos, mesh);
    }

    private static Mesh getRightSizeMesh(Pawn pawn)
    {
        if (MarkThatPawnMod.instance.Settings.RelativeToZoom && lastCameraZoomRange != Find.CameraDriver.CurrentZoom)
        {
            pawnMeshCache.Clear();
            lastCameraZoomRange = Find.CameraDriver.CurrentZoom;
        }

        if (!pawn.IsHashIntervalTick(GenTicks.TickLongInterval) &&
            pawnMeshCache.TryGetValue(pawn, out var meshForPawn))
        {
            return meshForPawn;
        }

        var iconInt = (int)Math.Round(MarkThatPawnMod.instance.Settings.IconSize * 10);

        if (MarkThatPawnMod.instance.Settings.RelativeIconSize)
        {
            var relativeSize = ((pawn.def.size.z - standardSize) / 2) + 1;
            iconInt = Math.Min((int)Math.Round(relativeSize * (float)iconInt), SizeMesh.Count);
        }

        if (MarkThatPawnMod.instance.Settings.RelativeToZoom)
        {
            iconInt = Math.Min(
                iconInt + (int)Math.Round((int)Find.CameraDriver.CurrentZoom *
                                          MarkThatPawnMod.instance.Settings.IconScalingFactor), SizeMesh.Count);
        }

        pawnMeshCache[pawn] = SizeMesh.Count < iconInt ? MeshPool.plane10 : SizeMesh[iconInt - 1];
        return pawnMeshCache[pawn];
    }

    private static void renderMarker(Pawn pawn, Material material, Vector3 drawPos, Mesh mesh)
    {
        if (!MarkThatPawnMod.instance.Settings.PulsatingIcons)
        {
            Graphics.DrawMesh(mesh, drawPos, Quaternion.identity, material, 0);
            return;
        }

        var iterator = (Time.realtimeSinceStartup + (397f * (pawn.thingIDNumber % 571))) * 4f;
        var pulsatingCyclePlace = ((float)Math.Sin(iterator) + 1f) * 0.5f;
        pulsatingCyclePlace = 0.3f + (pulsatingCyclePlace * 0.7f);

        var fadedMaterial = FadedMaterialPool.FadedVersionOf(material, pulsatingCyclePlace);
        Graphics.DrawMesh(mesh, drawPos, Quaternion.identity, fadedMaterial, 0);
    }

    public static bool TryGetAutoMarkerForPawn(Pawn pawn, out string result)
    {
        result = null;
        if (MarkThatPawnMod.instance.Settings.AutoRules == null || !MarkThatPawnMod.instance.Settings.AutoRules.Any())
        {
            return false;
        }

        if (!ValidPawn(pawn))
        {
            return false;
        }

        var validRules = new List<string>();

        foreach (var markerRule in MarkThatPawnMod.instance.Settings.AutoRules
                     .Where(rule => rule.Enabled && !rule.IsOverride && rule.AppliesToPawn(pawn))
                     .OrderBy(rule => rule.RuleOrder))
        {
            validRules.Add(markerRule.GetMarkerBlob());
            if (!MarkThatPawnMod.instance.Settings.NormalShowAll)
            {
                break;
            }
        }

        if (!validRules.Any())
        {
            return false;
        }

        result = string.Join(MarkerBlobSplitter.ToString(), validRules);
        return true;
    }

    public static bool TryToConvertStringToMaterial(string markerString, out Material result, Pawn pawn = null)
    {
        result = null;
        if (markerString == null || !markerString.Contains(";"))
        {
            return false;
        }

        if (markerString.Split(BlobSplitter)[0] == "__custom__")
        {
            switch (markerString.Split(BlobSplitter)[1])
            {
                case "FactionIcon":

                    if (pawn?.Faction?.def?.FactionIcon == null)
                    {
                        return false;
                    }

                    if (FactionMaterialCache.TryGetValue(pawn.Faction, out result))
                    {
                        return true;
                    }

                    result = MaterialPool.MatFrom(pawn.Faction.def.factionIconPath, ShaderDatabase.MetaOverlay,
                        pawn.Faction.Color);
                    FactionMaterialCache[pawn.Faction] = result;
                    return true;
                case "IdeologyIcon":
                    if (pawn?.Ideo?.Icon == null)
                    {
                        return false;
                    }

                    if (IdeoMaterialCache.TryGetValue(pawn.Ideo, out result))
                    {
                        return true;
                    }

                    result = MaterialPool.MatFrom(pawn.Ideo.iconDef.iconPath, ShaderDatabase.MetaOverlay,
                        pawn.Ideo.Color);
                    IdeoMaterialCache[pawn.Ideo] = result;
                    return true;
            }

            return false;
        }

        var markerSet = MarkerDefs.FirstOrDefault(def => def.defName == markerString.Split(BlobSplitter)[0]);
        if (markerSet == null)
        {
            return false;
        }

        if (!int.TryParse(markerString.Split(BlobSplitter)[1], out var number))
        {
            return false;
        }

        if (markerSet.MarkerMaterials.Count < number)
        {
            return false;
        }

        result = markerSet.MarkerMaterials[number - 1];
        return true;
    }

    public static bool TryToConvertStringToTexture2D(string markerString, out Texture2D result, Pawn pawn = null)
    {
        result = null;
        if (markerString == null || !markerString.Contains(";"))
        {
            return false;
        }

        if (markerString.Split(BlobSplitter)[0] == "__custom__")
        {
            switch (markerString.Split(BlobSplitter)[1])
            {
                case "FactionIcon":

                    if (pawn?.Faction?.def?.FactionIcon == null)
                    {
                        return false;
                    }

                    result = pawn.Faction.def.FactionIcon;
                    return false;
                case "IdeologyIcon":
                    if (pawn?.Ideo?.Icon == null)
                    {
                        return false;
                    }

                    result = pawn.Ideo.Icon;
                    return false;
            }

            return false;
        }

        var markerSet = MarkerDefs.FirstOrDefault(def => def.defName == markerString.Split(BlobSplitter)[0]);
        if (markerSet == null)
        {
            return false;
        }

        if (!int.TryParse(markerString.Split(BlobSplitter)[1], out var number))
        {
            return false;
        }

        if (markerSet.MarkerTextures.Count < number)
        {
            return false;
        }

        result = markerSet.MarkerTextures[number - 1];
        return true;
    }

    public static MarkerDef GetMarkerDefForPawn(Pawn pawn)
    {
        if (!pawn.IsHashIntervalTick(GenTicks.TickLongInterval) &&
            pawnMarkerCache.TryGetValue(pawn, out var markerDefForPawn))
        {
            return markerDefForPawn;
        }

        switch (pawn.GetPawnType())
        {
            case PawnType.Colonist when MarkThatPawnMod.instance.Settings.ColonistDiffer:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.ColonistMarkerSet;
                break;
            case PawnType.Prisoner when MarkThatPawnMod.instance.Settings.PrisonerDiffer:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.PrisonerMarkerSet;
                break;
            case PawnType.Slave when MarkThatPawnMod.instance.Settings.SlaveDiffer:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.SlaveMarkerSet;
                break;
            case PawnType.Enemy when MarkThatPawnMod.instance.Settings.EnemyDiffer:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.EnemyMarkerSet;
                break;
            case PawnType.Neutral when MarkThatPawnMod.instance.Settings.NeutralDiffer:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.NeutralMarkerSet;
                break;
            case PawnType.Vehicle when MarkThatPawnMod.instance.Settings.VehiclesDiffer:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.VehiclesMarkerSet;
                break;
            default:
                pawnMarkerCache[pawn] = MarkThatPawnMod.instance.Settings.DefaultMarkerSet;
                break;
        }

        return pawnMarkerCache[pawn];
    }

    public static void ResetCache(Pawn pawn = null)
    {
        if (pawn == null)
        {
            pawnMeshCache.Clear();
            pawnMarkerCache.Clear();
            return;
        }

        if (pawnMeshCache.ContainsKey(pawn))
        {
            pawnMeshCache.Remove(pawn);
        }

        if (pawnMarkerCache.ContainsKey(pawn))
        {
            pawnMarkerCache.Remove(pawn);
        }
    }

    public static PawnType GetPawnType(this Pawn pawn)
    {
        if (VehiclesLoaded && pawn.def.thingClass.Name.EndsWith("VehiclePawn"))
        {
            return PawnType.Vehicle;
        }

        if (ModLister.RoyaltyInstalled && pawn.IsSlaveOfColony)
        {
            return PawnType.Slave;
        }

        if (pawn.IsPrisonerOfColony)
        {
            return PawnType.Prisoner;
        }

        if (pawn.IsColonist || pawn.Faction == Faction.OfPlayer)
        {
            return PawnType.Colonist;
        }

        return pawn.HostileTo(Faction.OfPlayer) ? PawnType.Enemy : PawnType.Neutral;
    }

    public static bool ValidPawn(Pawn pawn)
    {
        if (pawn == null)
        {
            return false;
        }

        if (!pawn.Spawned)
        {
            return false;
        }

        if (pawn.Map == null)
        {
            return false;
        }

        switch (pawn.GetPawnType())
        {
            case PawnType.Colonist:
                return MarkThatPawnMod.instance.Settings.ShowForColonist;
            case PawnType.Prisoner:
                return MarkThatPawnMod.instance.Settings.ShowForPrisoner;
            case PawnType.Slave:
                return MarkThatPawnMod.instance.Settings.ShowForSlave;
            case PawnType.Enemy:
                return MarkThatPawnMod.instance.Settings.ShowForEnemy;
            case PawnType.Neutral:
                return MarkThatPawnMod.instance.Settings.ShowForNeutral;
            case PawnType.Vehicle:
                return MarkThatPawnMod.instance.Settings.ShowForVehicles;
            default:
                return true;
        }
    }

    public static TaggedString GetDistinctHediffName(HediffDef original, List<HediffDef> hediffList)
    {
        if (hediffList.Count(def => def.label == original.label) == 1)
        {
            return original.LabelCap;
        }

        return (TaggedString)$"{original.LabelCap} ({original.defName})";
    }

    public static List<FloatMenuOption> GetMarkingOptions(int currentMarking, MarkingTracker tracker,
        MarkerDef markerSet, Pawn pawn)
    {
        var returnList = new List<FloatMenuOption>();
        if (tracker.GlobalMarkingTracker.AutomaticPawns == null)
        {
            tracker.GlobalMarkingTracker.AutomaticPawns = new Dictionary<Pawn, string>();
        }

        if (tracker.GlobalMarkingTracker.CustomPawns == null)
        {
            tracker.GlobalMarkingTracker.CustomPawns = new Dictionary<Pawn, string>();
        }

        returnList.Add(new FloatMenuOption("MTP.CustomIcon".Translate(), CustomAction, TexButton.NewItem,
            Color.white));

        if (tracker.GlobalMarkingTracker.AutomaticPawns.TryGetValue(pawn, out _))
        {
            void AutoAction()
            {
                tracker.GlobalMarkingTracker.SetPawnMarking(pawn, -1, currentMarking);
            }

            void ResetAction()
            {
                tracker.GlobalMarkingTracker.AutomaticPawns.Remove(pawn);
                if (tracker.GlobalMarkingTracker.MarkedPawns.TryGetValue(pawn, out var marking) && marking == -1)
                {
                    tracker.GlobalMarkingTracker.MarkedPawns.Remove(pawn);
                }

                var mapTracker = pawn.Map.GetComponent<MarkingTracker>();
                if (mapTracker?.PawnsToEvaluate.Contains(pawn) == true)
                {
                    return;
                }

                mapTracker?.PawnsToEvaluate.Add(pawn);
            }

            returnList.Add(new FloatMenuOption("MTP.UseAutoIcon".Translate(), AutoAction, autoIcon, Color.white));
            returnList.Add(new FloatMenuOption("MTP.ResetAutoIcon".Translate(), ResetAction, resetIcon, Color.white));
        }

        for (var i = 0; i <= markerSet.MarkerTextures.Count; i++)
        {
            var mark = i;

            Texture2D icon;
            TaggedString title;
            if (i == 0)
            {
                title = "MTP.None".Translate(i);
                icon = CancelIcon;
            }
            else
            {
                title = "MTP.MarkerNumber".Translate(i);
                icon = markerSet.MarkerTextures[i - 1];
            }

            returnList.Add(new FloatMenuOption(title, Action, icon, Color.white));
            continue;

            void Action()
            {
                tracker.GlobalMarkingTracker.SetPawnMarking(pawn, mark, currentMarking);
            }
        }

        return returnList;

        void CustomAction()
        {
            var markerMenu = new List<FloatMenuOption>();
            foreach (var markerDef in MarkerDefs)
            {
                markerMenu.Add(new FloatMenuOption(markerDef.LabelCap, () =>
                {
                    var markerNumber = new List<FloatMenuOption>();
                    for (var i = 0; i < markerDef.MarkerTextures.Count; i++)
                    {
                        var mark = i + 1;

                        markerNumber.Add(new FloatMenuOption("MTP.MarkerNumber".Translate(mark), Action,
                            markerDef.MarkerTextures[i], Color.white));
                        continue;

                        void Action()
                        {
                            tracker.GlobalMarkingTracker.SetPawnMarking(pawn, -2, currentMarking,
                                customMarkerString: $"{markerDef.defName};{mark}");
                        }
                    }

                    Find.WindowStack.Add(new FloatMenu(markerNumber));
                }, markerDef.Icon, Color.white));
            }

            Find.WindowStack.Add(new FloatMenu(markerMenu));
        }
    }

    public static List<FloatMenuOption> GetMarkingSetOptions(PawnType type)
    {
        var returnList = new List<FloatMenuOption>();
        foreach (var markingSet in MarkerDefs)
        {
            var action = () => { MarkThatPawnMod.instance.Settings.DefaultMarkerSet = markingSet; };

            switch (type)
            {
                case PawnType.Colonist:
                    action = () => { MarkThatPawnMod.instance.Settings.ColonistMarkerSet = markingSet; };
                    break;
                case PawnType.Prisoner:
                    action = () => { MarkThatPawnMod.instance.Settings.PrisonerMarkerSet = markingSet; };
                    break;
                case PawnType.Slave:
                    action = () => { MarkThatPawnMod.instance.Settings.SlaveMarkerSet = markingSet; };
                    break;
                case PawnType.Enemy:
                    action = () => { MarkThatPawnMod.instance.Settings.EnemyMarkerSet = markingSet; };
                    break;
                case PawnType.Neutral:
                    action = () => { MarkThatPawnMod.instance.Settings.NeutralMarkerSet = markingSet; };
                    break;
                case PawnType.Vehicle:
                    action = () => { MarkThatPawnMod.instance.Settings.VehiclesMarkerSet = markingSet; };
                    break;
            }


            returnList.Add(new FloatMenuOption(markingSet.LabelCap, action, markingSet.Icon, Color.white));
        }

        return returnList;
    }
}