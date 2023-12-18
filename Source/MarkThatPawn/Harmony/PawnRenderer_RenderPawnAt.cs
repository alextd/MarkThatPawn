using HarmonyLib;
using Verse;

namespace MarkThatPawn.Harmony;

[HarmonyPatch(typeof(PawnRenderer), nameof(PawnRenderer.RenderPawnAt))]
public static class PawnRenderer_RenderPawnAt
{
    public static void Postfix(Pawn ___pawn)
    {
        if (!MarkThatPawn.ValidPawn(___pawn))
        {
            return;
        }

        var tracker = ___pawn.Map.GetComponent<MarkingTracker>();

        if (tracker == null)
        {
            return;
        }

        if (!tracker.GlobalMarkingTracker.HasAnyDefinedMarking(___pawn))
        {
            return;
        }

        MarkThatPawn.RenderMarkingOverlay(___pawn, tracker);
    }
}