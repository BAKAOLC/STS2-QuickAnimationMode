using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace STS2QuickAnimationMode.Utils
{
    internal static class NativeTweenCompat
    {
        private static readonly Func<Tween, Node, Task> AwaitFinished = ResolveAwaitFinished();

        internal static Task AwaitFinishedAsync(Tween tween, Node owner)
        {
            return AwaitFinished(tween, owner);
        }

        private static Func<Tween, Node, Task> ResolveAwaitFinished()
        {
            var type = typeof(TweenHelper);
            var current = type.GetMethod(nameof(TweenHelper.AwaitFinished),
                [typeof(Tween), typeof(Node), typeof(CancellationToken)]);
            if (current != null)
            {
                var awaitFinished = current.CreateDelegate<Func<Tween, Node, CancellationToken, Task>>();
                return (tween, owner) => awaitFinished(tween, owner, CancellationToken.None);
            }

            var legacy = type.GetMethod(nameof(TweenHelper.AwaitFinished), [typeof(Tween), typeof(Node)])
                         ?? throw new MissingMethodException(type.FullName, nameof(TweenHelper.AwaitFinished));
            return legacy.CreateDelegate<Func<Tween, Node, Task>>();
        }
    }
}
