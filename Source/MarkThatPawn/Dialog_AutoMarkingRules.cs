using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MarkThatPawn;

public class Dialog_AutoMarkingRules : Window
{
    private static readonly Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    private static readonly float rowHeight = 62f;

    private MarkerRule originalRule;
    private MarkerRule ruleWorkingCopy;

    public Vector2 ScrollPosition;

    public Dialog_AutoMarkingRules()
    {
        forcePause = true;
        doCloseX = true;
        doCloseButton = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
        ruleWorkingCopy = null;
    }

    public override Vector2 InitialSize => new Vector2(900f, 800f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        var labelRect = new Rect(0f, 0f, inRect.width - 150f - 17f, 35f);
        Widgets.Label(labelRect, "MTP.AutomaticRulesTitle".Translate());
        Text.Font = GameFont.Small;

        if (Widgets.ButtonText(labelRect.RightPart(0.35f).LeftHalf().ContractedBy(1f), "MTP.NewRuleButton".Translate()))
        {
            showNewRuleMenu();
        }

        if (MarkThatPawnMod.instance.Settings.AutoRules.Any() &&
            Widgets.ButtonText(labelRect.RightPart(0.35f).RightHalf().ContractedBy(1f), "MTP.ResetAll".Translate()))
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "MTP.ResetAllConfirm".Translate(),
                MarkThatPawnMod.instance.Settings.AutoRules.Clear));
        }

        var rulesOuterRect = new Rect(0f, 40f, inRect.width, inRect.height - 40f - CloseButSize.y);
        var rulesInnerRect = new Rect(0f, 0f, rulesOuterRect.width - 20f,
            rowHeight * (MarkThatPawnMod.instance.Settings.AutoRules.Count + 1));

        var rulesListing = new Listing_Standard();
        Widgets.BeginScrollView(rulesOuterRect, ref ScrollPosition, rulesInnerRect);
        rulesListing.Begin(rulesInnerRect);
        foreach (var autoRule in MarkThatPawnMod.instance.Settings.AutoRules.OrderBy(rule => rule.RuleOrder))
        {
            rulesListing.GapLine();
            var editing = originalRule == autoRule;
            var ruleRow = rulesListing.GetRect(50f);
            var rowRightArea = ruleRow.RightPart(0.3f);
            var prioButtonsArea = rowRightArea.RightPartPixels(25f);
            if (ruleWorkingCopy == null)
            {
                var increaseButtonArea = prioButtonsArea.TopPartPixels(24f);
                var decreaseButtonArea = prioButtonsArea.BottomPartPixels(24f);
                if (autoRule.RuleOrder != MarkThatPawnMod.instance.Settings.AutoRules.Min(rule => rule.RuleOrder))
                {
                    TooltipHandler.TipRegion(increaseButtonArea, "MTP.IncreasePrio".Translate());
                    if (Widgets.ButtonImage(increaseButtonArea, TexButton.ReorderUp))
                    {
                        autoRule.IncreasePrio();
                    }
                }

                if (autoRule.RuleOrder != MarkThatPawnMod.instance.Settings.AutoRules.Max(rule => rule.RuleOrder))
                {
                    TooltipHandler.TipRegion(decreaseButtonArea, "MTP.IncreasePrio".Translate());
                    if (Widgets.ButtonImage(decreaseButtonArea, TexButton.ReorderDown))
                    {
                        autoRule.DecreasePrio();
                    }
                }
            }

            var rowLeftArea = ruleRow.LeftPart(0.68f);
            var imageRect = rowRightArea.RightPartPixels(rowRightArea.height);
            imageRect.x -= prioButtonsArea.width;
            var positiveButtonRect = rowLeftArea.LeftPart(0.04f).TopHalf().ContractedBy(1f);
            var negativeButtonRect = rowLeftArea.LeftPart(0.04f).BottomHalf().ContractedBy(1f);

            var enabledIconRect = rowLeftArea.LeftPartPixels(ruleRow.height).ContractedBy(5f);
            enabledIconRect.x += positiveButtonRect.width;

            var workingRect = rowLeftArea.RightPart(0.88f);

            Rect infoRect;
            string infoLabel;
            if (!editing)
            {
                if (!autoRule.Enabled)
                {
                    Widgets.DrawBoxSolid(ruleRow.ContractedBy(1f), inactiveColor);
                }

                GUI.DrawTexture(imageRect, autoRule.GetIconTexture());
                TooltipHandler.TipRegion(enabledIconRect,
                    autoRule.Enabled ? "MTP.Enabled".Translate() : "MTP.Disabled".Translate());
                GUI.DrawTexture(enabledIconRect,
                    autoRule.Enabled ? TexButton.SpeedButtonTextures[1] : TexButton.SpeedButtonTextures[0]);

                if (ruleWorkingCopy == null)
                {
                    TooltipHandler.TipRegion(positiveButtonRect, "MTP.EditAutomaticType".Translate());
                    if (Widgets.ButtonImageWithBG(positiveButtonRect, TexButton.Info,
                            positiveButtonRect.size * MarkThatPawn.ButtonIconSizeFactor))
                    {
                        ruleWorkingCopy = autoRule.GetCopy();
                        originalRule = autoRule;
                        continue;
                    }

                    TooltipHandler.TipRegion(negativeButtonRect, "MTP.DeleteAutomaticType".Translate());
                    if (Widgets.ButtonImageWithBG(negativeButtonRect, TexButton.DeleteX,
                            negativeButtonRect.size * MarkThatPawn.ButtonIconSizeFactor))
                    {
                        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                            "MTP.DeleteAutomaticTypeConfirm".Translate(),
                            delegate { MarkThatPawnMod.instance.Settings.AutoRules.Remove(autoRule); }));
                        continue;
                    }
                }

                infoRect = workingRect.LeftPart(0.45f).TopHalf().CenteredOnYIn(workingRect);
                infoLabel = autoRule.GetTranslatedType();
                if (!autoRule.Enabled)
                {
                    infoLabel = $"{infoLabel} ({"MTP.Disabled".Translate()})";
                }

                Widgets.Label(infoRect, infoLabel);

                autoRule.ShowTypeParametersRect(workingRect.RightPart(0.53f), false);

                Widgets.Label(rowRightArea.LeftHalf().TopHalf(), autoRule.MarkerDef.LabelCap);
                Widgets.Label(rowRightArea.LeftHalf().BottomHalf(), autoRule.GetTranslatedMarkerIndex());
                continue;
            }

            if (ruleWorkingCopy == null)
            {
                continue;
            }

            if (!ruleWorkingCopy.Enabled)
            {
                Widgets.DrawBoxSolid(ruleRow.ContractedBy(1f), inactiveColor);
            }

            GUI.DrawTexture(imageRect, ruleWorkingCopy.GetIconTexture());
            TooltipHandler.TipRegion(enabledIconRect,
                ruleWorkingCopy.Enabled ? "MTP.EnabledChange".Translate() : "MTP.DisabledChange".Translate());
            if (Widgets.ButtonImageWithBG(enabledIconRect,
                    ruleWorkingCopy.Enabled ? TexButton.SpeedButtonTextures[1] : TexButton.SpeedButtonTextures[0],
                    enabledIconRect.size * MarkThatPawn.ButtonIconSizeFactor))
            {
                ruleWorkingCopy.SetEnabled(!ruleWorkingCopy.Enabled);
            }

            infoRect = workingRect.LeftPart(0.45f).TopHalf().CenteredOnYIn(workingRect);
            infoLabel = ruleWorkingCopy.GetTranslatedType();
            if (!ruleWorkingCopy.Enabled)
            {
                infoLabel = $"{infoLabel} ({"MTP.Disabled".Translate()})";
            }

            Widgets.Label(infoRect, infoLabel);

            ruleWorkingCopy.ShowTypeParametersRect(workingRect.RightPart(0.53f), true);

            var changeMarkerDefRect = rowRightArea.LeftHalf().TopHalf().ContractedBy(1f);
            TooltipHandler.TipRegion(changeMarkerDefRect, ruleWorkingCopy.MarkerDef.description);
            if (Widgets.ButtonText(changeMarkerDefRect, ruleWorkingCopy.MarkerDef.LabelCap))
            {
                ruleWorkingCopy.ShowChangeMarkerDefMenu();
            }

            if (Widgets.ButtonText(rowRightArea.LeftHalf().BottomHalf().ContractedBy(1f),
                    ruleWorkingCopy.GetTranslatedMarkerIndex()))
            {
                ruleWorkingCopy.ShowChangeMarkerMenu();
            }

            TooltipHandler.TipRegion(positiveButtonRect, "MTP.SaveAutomaticType".Translate());
            if (Widgets.ButtonImageWithBG(positiveButtonRect, TexButton.Save,
                    positiveButtonRect.size * MarkThatPawn.ButtonIconSizeFactor))
            {
                autoRule.SaveFromCopy(ruleWorkingCopy);
                ruleWorkingCopy = null;
                originalRule = null;
            }

            TooltipHandler.TipRegion(negativeButtonRect, "MTP.CancelAutomaticType".Translate());
            if (Widgets.ButtonImageWithBG(negativeButtonRect, MarkThatPawn.CancelIcon,
                    negativeButtonRect.size * MarkThatPawn.ButtonIconSizeFactor))
            {
                ruleWorkingCopy = null;
                originalRule = null;
            }
        }

        rulesListing.End();
        Widgets.EndScrollView();
    }

    public override bool OnCloseRequest()
    {
        if (!base.OnCloseRequest())
        {
            return false;
        }

        if (ruleWorkingCopy == null)
        {
            return true;
        }

        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
            "MTP.UnsavedRule".Translate(),
            delegate
            {
                MarkThatPawnMod.instance.Settings
                    .AutoRules[MarkThatPawnMod.instance.Settings.AutoRules.IndexOf(originalRule)]
                    .SaveFromCopy(ruleWorkingCopy);
                ruleWorkingCopy = null;
                originalRule = null;
                Close();
            }));

        return false;
    }


    private void showNewRuleMenu()
    {
        var ruleTypeList = new List<FloatMenuOption>();

        foreach (var ruleType in (MarkerRule.AutoRuleType[])Enum.GetValues(typeof(MarkerRule.AutoRuleType)))
        {
            switch (ruleType)
            {
                case MarkerRule.AutoRuleType.Weapon:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new WeaponMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.WeaponType:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new WeaponTypeMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Trait:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new TraitMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Skill:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new SkillMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Relative:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new RelativeMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.PawnType:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new PawnTypeMarkerRule())));
                    break;
                default:
                    continue;
            }
        }

        Find.WindowStack.Add(new FloatMenu(ruleTypeList));
    }
}