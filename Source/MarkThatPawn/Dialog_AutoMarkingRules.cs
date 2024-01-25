using System;
using System.Collections.Generic;
using System.Linq;
using MarkThatPawn.MarkerRules;
using UnityEngine;
using Verse;

namespace MarkThatPawn;

public class Dialog_AutoMarkingRules : Window
{
    private static readonly Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    private static readonly Color overrideColor = new Color(0.1f, 0.3f, 0.3f, 0.6f);
    private static readonly float rowHeight = 62f;
    private readonly Texture2D copyIcon;

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
        copyIcon = ContentFinder<Texture2D>.Get("UI/Commands/CopySettings");
    }

    public override Vector2 InitialSize => new Vector2(900f, 800f);

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        var labelRect = new Rect(0f, 0f, inRect.width - 150f - 17f, 35f);
        Widgets.Label(labelRect, "MTP.AutomaticRulesTitle".Translate());
        Text.Font = GameFont.Small;
        if (ruleWorkingCopy == null)
        {
            if (Widgets.ButtonText(labelRect.RightPart(0.35f).LeftHalf().ContractedBy(1f),
                    "MTP.NewRuleButton".Translate()))
            {
                showNewRuleMenu();
            }

            if (MarkThatPawnMod.instance.Settings.AutoRules.Any() &&
                Widgets.ButtonText(labelRect.RightPart(0.35f).RightHalf().ContractedBy(1f), "MTP.ResetAll".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "MTP.ResetAllConfirm".Translate(),
                    MarkThatPawnMod.instance.Settings.ClearRules));
            }
        }

        var rulesOuterRect = new Rect(0f, 40f, inRect.width, inRect.height - 40f - CloseButSize.y);
        var rulesInnerRect = new Rect(0f, 0f, rulesOuterRect.width - 20f,
            rowHeight * (MarkThatPawnMod.instance.Settings.AutoRules.Count + 1));

        var rulesListing = new Listing_Standard();
        Widgets.BeginScrollView(rulesOuterRect, ref ScrollPosition, rulesInnerRect);
        rulesListing.Begin(rulesInnerRect);
        foreach (var autoRule in MarkThatPawnMod.instance.Settings.AutoRules.OrderByDescending(rule => rule.IsOverride)
                     .ThenBy(rule => rule.RuleOrder))
        {
            rulesListing.GapLine();
            var editing = originalRule == autoRule;
            var ruleRow = rulesListing.GetRect(50f);
            if (autoRule.IsOverride)
            {
                TooltipHandler.TipRegion(ruleRow, "MTP.OverrideRule".Translate());
            }

            var rowRightArea = ruleRow.RightPart(0.3f);
            var prioButtonsArea = rowRightArea.RightPartPixels(25f);
            if (ruleWorkingCopy == null)
            {
                var increaseButtonArea = prioButtonsArea.TopPartPixels(24f);
                var decreaseButtonArea = prioButtonsArea.BottomPartPixels(24f);
                if (autoRule.RuleOrder != MarkThatPawnMod.instance.Settings.AutoRules
                        .Where(rule => rule.IsOverride == autoRule.IsOverride).Min(rule => rule.RuleOrder))
                {
                    TooltipHandler.TipRegion(increaseButtonArea, "MTP.IncreasePrio".Translate());
                    if (Widgets.ButtonImage(increaseButtonArea, TexButton.ReorderUp,
                            autoRule.IsOverride ? Color.magenta : Color.white))
                    {
                        autoRule.IncreasePrio();
                    }
                }

                if (autoRule.RuleOrder != MarkThatPawnMod.instance.Settings.AutoRules
                        .Where(rule => rule.IsOverride == autoRule.IsOverride).Max(rule => rule.RuleOrder))
                {
                    TooltipHandler.TipRegion(decreaseButtonArea, "MTP.DecreasePrio".Translate());
                    if (Widgets.ButtonImage(decreaseButtonArea, TexButton.ReorderDown,
                            autoRule.IsOverride ? Color.magenta : Color.white))
                    {
                        autoRule.DecreasePrio();
                    }
                }
            }

            var rowLeftArea = ruleRow.LeftPart(0.68f);
            var imageRect = rowRightArea.RightPartPixels(rowRightArea.height);
            imageRect.x -= prioButtonsArea.width;
            var fullButtonArea = rowLeftArea.LeftPart(0.04f).TopHalf().CenteredOnYIn(rowLeftArea);
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
                else if (autoRule.IsOverride)
                {
                    Widgets.DrawBoxSolid(ruleRow.ContractedBy(1f), overrideColor);
                }

                if (!autoRule.UsesDynamicIcons)
                {
                    GUI.DrawTexture(imageRect, autoRule.GetIconTexture());
                }

                TooltipHandler.TipRegion(enabledIconRect,
                    autoRule.Enabled ? "MTP.Enabled".Translate() : "MTP.Disabled".Translate());
                GUI.DrawTexture(enabledIconRect,
                    autoRule.Enabled ? TexButton.SpeedButtonTextures[1] : TexButton.SpeedButtonTextures[0]);

                if (ruleWorkingCopy == null)
                {
                    if (Widgets.ButtonImageWithBG(fullButtonArea, TexButton.Info,
                            fullButtonArea.size * MarkThatPawn.ButtonIconSizeFactor))
                    {
                        var editFloatMenu = new List<FloatMenuOption>();

                        editFloatMenu.Add(new FloatMenuOption("MTP.EditAutomaticType".Translate(),
                            () =>
                            {
                                ruleWorkingCopy = autoRule.GetCopy();
                                originalRule = autoRule;
                            }, TexButton.OpenStatsReport, Color.white));
                        editFloatMenu.Add(new FloatMenuOption("MTP.DuplicateRule".Translate(),
                            () =>
                            {
                                var ruleCopy = autoRule.GetCopy();
                                ruleCopy.RuleOrder = MarkThatPawnMod.instance.Settings.AutoRules
                                    .OrderByDescending(rule => rule.RuleOrder).First().RuleOrder + 1;
                                MarkThatPawnMod.instance.Settings.AutoRules.Add(ruleCopy);
                            }, copyIcon, Color.white));

                        editFloatMenu.Add(new FloatMenuOption("MTP.DeleteAutomaticType".Translate(),
                            () =>
                            {
                                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                                    "MTP.DeleteAutomaticTypeConfirm".Translate(),
                                    delegate
                                    {
                                        autoRule.OnDelete();
                                        MarkThatPawnMod.instance.Settings.AutoRules.Remove(autoRule);
                                    }));
                            }, TexButton.DeleteX, Color.white));

                        Find.WindowStack.Add(new FloatMenu(editFloatMenu));
                    }
                }

                infoLabel = autoRule.GetTranslatedType();
                if (!autoRule.Enabled)
                {
                    infoLabel = $"{infoLabel} ({"MTP.Disabled".Translate()})";
                }

                infoRect = workingRect.LeftPart(0.45f).TopHalf().CenteredOnYIn(workingRect);
                if (autoRule.PawnLimitation != MarkThatPawn.PawnType.Default)
                {
                    infoRect = workingRect.LeftPart(0.45f).TopPart(0.75f).CenteredOnYIn(workingRect);
                    infoLabel +=
                        $"{Environment.NewLine}{"MTP.PawnLimitation".Translate($"MTP.PawnType.{autoRule.PawnLimitation}".Translate())}";
                }

                Widgets.Label(infoRect, infoLabel);

                autoRule.ShowTypeParametersRect(workingRect.RightPart(0.53f), false);

                if (autoRule.UsesDynamicIcons)
                {
                    continue;
                }

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
            else if (ruleWorkingCopy.IsOverride)
            {
                Widgets.DrawBoxSolid(ruleRow.ContractedBy(1f), overrideColor);
            }

            if (!ruleWorkingCopy.UsesDynamicIcons)
            {
                GUI.DrawTexture(imageRect, ruleWorkingCopy.GetIconTexture());
            }

            TooltipHandler.TipRegion(enabledIconRect,
                ruleWorkingCopy.Enabled ? "MTP.EnabledChange".Translate() : "MTP.DisabledChange".Translate());
            if (Widgets.ButtonImageWithBG(enabledIconRect,
                    ruleWorkingCopy.Enabled ? TexButton.SpeedButtonTextures[1] : TexButton.SpeedButtonTextures[0],
                    enabledIconRect.size * MarkThatPawn.ButtonIconSizeFactor))
            {
                ruleWorkingCopy.SetEnabled(!ruleWorkingCopy.Enabled);
            }

            infoRect = workingRect.LeftPart(0.45f).TopHalf().ContractedBy(1f);
            if (!ruleWorkingCopy.GetType().Name.EndsWith("TDFindLibRule") &&
                ruleWorkingCopy.ApplicablePawnTypes.Count > 1)
            {
                var limitationRect = workingRect.LeftPart(0.45f).BottomHalf().ContractedBy(1f);
                Widgets.Label(limitationRect.LeftHalf(), "MTP.PawnLimitation".Translate());
                if (Widgets.ButtonText(limitationRect.RightHalf(),
                        $"MTP.PawnType.{ruleWorkingCopy.PawnLimitation}".Translate()))
                {
                    ruleWorkingCopy.ShowPawnLimitationSelectorMenu();
                }
            }
            else
            {
                infoRect = infoRect.CenteredOnYIn(workingRect);
            }

            infoLabel = ruleWorkingCopy.GetTranslatedType();
            if (!ruleWorkingCopy.Enabled)
            {
                infoLabel = $"{infoLabel} ({"MTP.Disabled".Translate()})";
            }

            Widgets.Label(infoRect, infoLabel);


            ruleWorkingCopy.ShowTypeParametersRect(workingRect.RightPart(0.53f), true);

            if (!autoRule.UsesDynamicIcons)
            {
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
            // ReSharper disable once InvertIf
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

        //TODO: try to generate this based on AutoRuleTypes
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
                case MarkerRule.AutoRuleType.Drafted:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new DraftedMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.MentalState:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new MentalStateMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.HediffDynamic:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new DynamicHediffMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.HediffStatic:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new StaticHediffMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Animal:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new AnimalMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Mechanoid:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new MechanoidMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Gender:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new GenderMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Age:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new AgeMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Xenotype when ModLister.BiotechInstalled:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new XenotypeMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Gene when ModLister.BiotechInstalled:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new GeneMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.Apparel:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new ApparelMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.ApparelType:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new ApparelTypeMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.AnyHediffStatic:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new AnyStaticHediffMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.FactionIcon:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new FactionIconMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.FactionLeader:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new FactionLeaderMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.IdeologyIcon when ModLister.IdeologyInstalled:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(new IdeologyIconMarkerRule())));
                    break;
                case MarkerRule.AutoRuleType.TDFindLib when MarkThatPawn.TDFindLibLoaded:
                    ruleTypeList.Add(new FloatMenuOption($"MTP.AutomaticType.{ruleType}".Translate(),
                        () => MarkThatPawnMod.instance.Settings.AutoRules.Add(
                            (MarkerRule)Activator.CreateInstance(MarkThatPawn.TDFindLibRuleType))));
                    break;
                default:
                    continue;
            }
        }

        Find.WindowStack.Add(new FloatMenu(ruleTypeList));
    }
}