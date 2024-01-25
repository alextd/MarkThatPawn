using TD_Find_Lib;
using UnityEngine;
using Verse;

namespace MarkThatPawn.MarkerRules;

public class TDFindLibRule : MarkerRule
{
    private QueryHolder tdFilter;

    public TDFindLibRule()
    {
        RuleType = AutoRuleType.TDFindLib;
        SetDefaultValues();
        tdFilter = new();
    }

    public TDFindLibRule(string blob)
    {
        RuleType = AutoRuleType.TDFindLib;
        SetBlob(blob);
    }

    public override void ShowTypeParametersRect(Rect rect, bool edit)
    {
        var mentalStateTypeArea = rect.LeftPart(0.75f);

        if (edit)
        {
            Widgets.Label(mentalStateTypeArea.TopHalf(), "Advanced");
            if (Widgets.ButtonText(mentalStateTypeArea.BottomHalf(), "MTP.EditTdRule".Translate()))
            {
                Find.WindowStack.Add(new PawnToMarkEditor(tdFilter));
            }
        }
        else
        {
            Widgets.Label(mentalStateTypeArea.TopHalf().CenteredOnYIn(rect), "Advanced");
        }
    }

    public override MarkerRule GetCopy()
    {
        var returnRule = new TDFindLibRule(GetBlob());

        returnRule.tdFilter = tdFilter.CloneAsHolder();

        return returnRule;
    }
    public override void SaveFromCopy(MarkerRule copy)
    {
        base.SaveFromCopy(copy);

        TDFindLibRule tdCopy = copy as TDFindLibRule;
        tdFilter = tdCopy.tdFilter.CloneAsHolder();
    }

    public override bool AppliesToPawn(Pawn pawn)
    {
        if (!base.AppliesToPawn(pawn))
        {
            return false;
        }

        if (pawn == null || pawn.Destroyed || !pawn.Spawned || pawn.Map == null)
        {
            return false;
        }

        return tdFilter.AppliesTo(pawn, pawn.Map);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        Scribe_Deep.Look(ref tdFilter, "tdFilter");
    }

    public class PawnToMarkEditor : HolderEditorWindow
    {
        // ISearchReceiver stuff
        public static string TransferTag = "TD.MTP";

        public PawnToMarkEditor(QueryHolder search) : base(search, TransferTag)
        {
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            onlyOneOfTypeAllowed = true;
            draggable = false;
            resizeable = false;
        }

        public override Vector2 InitialSize => new Vector2(750, 320);

        public override void SetInitialSizeAndPosition()
        {
            base.SetInitialSizeAndPosition();
            windowRect.height = UI.screenHeight / (float)2;
            windowRect.width = UI.screenWidth / (float)2;
            windowRect.x = (UI.screenWidth - windowRect.width) / 2;
            windowRect.y = (UI.screenHeight - windowRect.height) / 2;
        }
    }
}