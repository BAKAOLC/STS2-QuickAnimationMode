using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;

namespace STS2QuickAnimationMode.Utils
{
    internal static class CardSelectionState
    {
        private static readonly Type SelectionScreenType =
            typeof(NOverlayStack).Assembly.GetType(
                "MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.ICardSelectionScreen")
            ?? typeof(NOverlayStack).Assembly.GetType("MegaCrit.Sts2.Core.Nodes.Screens.CardSelection.ICardSelector")
            ?? throw new TypeLoadException("The game's card selection screen interface could not be resolved.");

        internal static bool IsSelectionOverlayOpen =>
            SelectionScreenType.IsInstanceOfType(NOverlayStack.Instance?.Peek());
    }
}
