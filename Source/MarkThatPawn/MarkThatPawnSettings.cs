using System.Collections.Generic;
using MarkThatPawn.MarkerRules;
using Verse;

namespace MarkThatPawn;

/// <summary>
///     Definition of the settings for the mod
/// </summary>
public class MarkThatPawnSettings : ModSettings
{
    public List<string> AutoRuleBlobs = [];
    public List<MarkerRule> AutoRules = [];
    public bool ColonistDiffer;
    private string colonistMarkerSet = "WowStyle";
    private MarkerDef colonistMarkerSetDef;
    private string defaultMarkerSet = "WowStyle";
    private MarkerDef defaultMarkerSetDef;
    public bool EnemyDiffer;
    private string enemyMarkerSet = "WowStyle";
    private MarkerDef enemyMarkerSetDef;
    public bool GameIsPaused;
    public float IconScalingFactor = 1f;
    public float IconSize = 0.7f;
    public float IconSpacingFactor;
    public bool InvertOrder;
    public bool NeutralDiffer;
    private string neutralMarkerSet = "WowStyle";
    private MarkerDef neutralMarkerSetDef;
    public bool NormalShowAll = true;
    public bool PawnIsSelected;
    public bool PrisonerDiffer;
    private string prisonerMarkerSet = "WowStyle";
    private MarkerDef prisonerMarkerSetDef;
    public bool PulsatingIcons;
    public bool RefreshRules = true;
    public bool RelativeIconSize;
    public bool RelativeToZoom = true;
    public bool RotateIcons;
    public bool SeparateShowAll = true;
    public bool SeparateTemporary = true;
    public bool ShiftIsPressed;
    public bool ShowForColonist = true;

    public bool ShowForEnemy = true;

    public bool ShowForNeutral = true;
    public bool ShowForPrisoner = true;

    public bool ShowForSlave = true;

    public bool ShowForVehicles = true;
    public bool ShowOnPaused;
    public bool ShowOnShift;
    public bool ShowWhenHover;
    public bool ShowWhenSelected;
    public bool SlaveDiffer;
    private string slaveMarkerSet = "WowStyle";
    private MarkerDef slaveMarkerSetDef;

    public bool VehiclesDiffer;
    private string vehiclesMarkerSet = "WowStyle";
    private MarkerDef vehiclesMarkerSetDef;
    public float XOffset;
    public float ZOffset;

    public MarkerDef DefaultMarkerSet
    {
        get => genericSetGetter(ref defaultMarkerSetDef, ref defaultMarkerSet);
        set
        {
            defaultMarkerSetDef = value;
            defaultMarkerSet = value.defName;
        }
    }

    public MarkerDef ColonistMarkerSet
    {
        get => genericSetGetter(ref colonistMarkerSetDef, ref colonistMarkerSet);
        set
        {
            colonistMarkerSetDef = value;
            colonistMarkerSet = value.defName;
        }
    }

    public MarkerDef PrisonerMarkerSet
    {
        get => genericSetGetter(ref prisonerMarkerSetDef, ref prisonerMarkerSet);
        set
        {
            prisonerMarkerSetDef = value;
            prisonerMarkerSet = value.defName;
        }
    }

    public MarkerDef SlaveMarkerSet
    {
        get => genericSetGetter(ref slaveMarkerSetDef, ref slaveMarkerSet);
        set
        {
            slaveMarkerSetDef = value;
            slaveMarkerSet = value.defName;
        }
    }

    public MarkerDef EnemyMarkerSet
    {
        get => genericSetGetter(ref enemyMarkerSetDef, ref enemyMarkerSet);
        set
        {
            enemyMarkerSetDef = value;
            enemyMarkerSet = value.defName;
        }
    }

    public MarkerDef NeutralMarkerSet
    {
        get => genericSetGetter(ref neutralMarkerSetDef, ref neutralMarkerSet);
        set
        {
            neutralMarkerSetDef = value;
            neutralMarkerSet = value.defName;
        }
    }

    public MarkerDef VehiclesMarkerSet
    {
        get => genericSetGetter(ref vehiclesMarkerSetDef, ref vehiclesMarkerSet);
        set
        {
            vehiclesMarkerSetDef = value;
            vehiclesMarkerSet = value.defName;
        }
    }


    private MarkerDef genericSetGetter(ref MarkerDef privateMarkerSetDef, ref string privateMarkerSet)
    {
        if (privateMarkerSetDef != null)
        {
            return privateMarkerSetDef;
        }

        privateMarkerSetDef = DefDatabase<MarkerDef>.GetNamedSilentFail(privateMarkerSet);
        if (privateMarkerSetDef != null)
        {
            return privateMarkerSetDef;
        }

        privateMarkerSet = "WowStyle";
        privateMarkerSetDef = DefDatabase<MarkerDef>.GetNamedSilentFail(privateMarkerSet);

        return privateMarkerSetDef;
    }

    /// <summary>
    ///     Saving and loading the values
    /// </summary>
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref defaultMarkerSet, "defaultMarkerSet", "WowStyle");
        Scribe_Values.Look(ref colonistMarkerSet, "colonistMarkerSet", "WowStyle");
        Scribe_Values.Look(ref prisonerMarkerSet, "prisonerMarkerSet", "WowStyle");
        Scribe_Values.Look(ref slaveMarkerSet, "slaveMarkerSet", "WowStyle");
        Scribe_Values.Look(ref enemyMarkerSet, "enemyMarkerSet", "WowStyle");
        Scribe_Values.Look(ref neutralMarkerSet, "neutralMarkerSet", "WowStyle");
        Scribe_Values.Look(ref vehiclesMarkerSet, "vehiclesMarkerSet", "WowStyle");
        Scribe_Values.Look(ref PulsatingIcons, "PulsatingIcons");
        Scribe_Values.Look(ref RelativeToZoom, "RelativeToZoom", true);
        Scribe_Values.Look(ref IconScalingFactor, "IconScalingFactor", 1f);
        Scribe_Values.Look(ref ShowForColonist, "ShowForColonist", true);
        Scribe_Values.Look(ref ShowForPrisoner, "ShowForPrisoner", true);
        Scribe_Values.Look(ref ShowForSlave, "ShowForSlave", true);
        Scribe_Values.Look(ref SeparateShowAll, "SeparateShowAll", true);
        Scribe_Values.Look(ref NormalShowAll, "NormalShowAll", true);
        Scribe_Values.Look(ref ShowForEnemy, "ShowForEnemy", true);
        Scribe_Values.Look(ref ShowForNeutral, "ShowForNeutral", true);
        Scribe_Values.Look(ref SeparateTemporary, "SeparateTemporary", true);
        Scribe_Values.Look(ref ShowForVehicles, "ShowForVehicles", true);
        Scribe_Values.Look(ref RefreshRules, "RefreshRules", true);
        Scribe_Values.Look(ref ColonistDiffer, "ColonistDiffer");
        Scribe_Values.Look(ref RotateIcons, "RotateIcons");
        Scribe_Values.Look(ref InvertOrder, "InvertOrder");
        Scribe_Values.Look(ref ShowWhenSelected, "ShowWhenSelected");
        Scribe_Values.Look(ref ShowWhenHover, "ShowWhenHover");
        Scribe_Values.Look(ref ShowOnShift, "ShowOnShift");
        Scribe_Values.Look(ref ShowOnPaused, "ShowOnPaused");
        Scribe_Values.Look(ref ShiftIsPressed, "ShiftIsPressed");
        Scribe_Values.Look(ref PawnIsSelected, "PawnIsSelected");
        Scribe_Values.Look(ref GameIsPaused, "GameIsPaused");
        Scribe_Values.Look(ref PrisonerDiffer, "PrisonerDiffer");
        Scribe_Values.Look(ref RelativeIconSize, "RelativeIconSize");
        Scribe_Values.Look(ref SlaveDiffer, "SlaveDiffer");
        Scribe_Values.Look(ref EnemyDiffer, "EnemyDiffer");
        Scribe_Values.Look(ref NeutralDiffer, "NeutralDiffer");

        // Backward compat rules loading:
        if(Scribe.mode == LoadSaveMode.LoadingVars)
            Scribe_Collections.Look(ref AutoRuleBlobs, "AutoRuleBlobs");
        // From now on, expose Rules directly
        Scribe_Collections.Look(ref AutoRules, "AutoRules");
        if (AutoRules == null)
            AutoRules = [];

        Scribe_Values.Look(ref VehiclesDiffer, "VehiclesDiffer");
        Scribe_Values.Look(ref IconSize, "IconSize", 0.7f);
        Scribe_Values.Look(ref IconSpacingFactor, "IconSpacingFactor");
        Scribe_Values.Look(ref XOffset, "XOffset");
        Scribe_Values.Look(ref ZOffset, "ZOffset");
    }

    public void Reset()
    {
        DefaultMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        ColonistMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        PrisonerMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        SlaveMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        EnemyMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        NeutralMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        VehiclesMarkerSet = DefDatabase<MarkerDef>.GetNamedSilentFail("WowStyle");
        RelativeIconSize = false;
        RotateIcons = false;
        RelativeToZoom = true;
        IconScalingFactor = 1f;
        IconSpacingFactor = 0f;
        RefreshRules = true;
        ShowForColonist = true;
        ShowForPrisoner = true;
        SeparateTemporary = true;
        ShowForSlave = true;
        SeparateShowAll = true;
        NormalShowAll = true;
        ShowForEnemy = true;
        ShowForNeutral = true;
        ShowForVehicles = true;
        PulsatingIcons = false;
        ColonistDiffer = false;
        PrisonerDiffer = false;
        SlaveDiffer = false;
        ShowWhenSelected = false;
        ShowWhenHover = false;
        ShowOnShift = false;
        ShowOnPaused = false;
        GameIsPaused = false;
        ShiftIsPressed = false;
        PawnIsSelected = false;
        EnemyDiffer = false;
        NeutralDiffer = false;
        VehiclesDiffer = false;
        InvertOrder = false;
        IconSize = 0.7f;
        XOffset = 0f;
        ZOffset = 0f;
    }

    public void ClearRules()
    {
        foreach (var markerRule in AutoRules)
        {
            markerRule.OnDelete();
        }

        AutoRules.Clear();
    }
}