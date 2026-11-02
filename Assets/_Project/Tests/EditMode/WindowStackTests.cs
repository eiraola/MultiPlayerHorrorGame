using NUnit.Framework;
using ProjectScare.UI;

namespace ProjectScare.Tests.EditMode
{
    public class WindowStackTests
    {
        private const string Root = "root";
        private const string Child = "child";

        private WindowStack<string> stack;

        [SetUp]
        public void SetUp() => stack = new WindowStack<string>();

        [Test]
        public void TryBeginPush_EmptyStack_HasNoPrevious()
        {
            bool accepted = stack.TryBeginPush(Root, out string previous);

            Assert.IsTrue(accepted);
            Assert.IsNull(previous);
            Assert.AreEqual(Root, stack.Current);
            Assert.IsTrue(stack.IsTransitioning);
        }

        [Test]
        public void TryBeginPush_ReturnsCurrentAsPrevious()
        {
            PushCompleted(Root);

            stack.TryBeginPush(Child, out string previous);

            Assert.AreEqual(Root, previous);
            Assert.AreEqual(Child, stack.Current);
        }

        [Test]
        public void TryBeginPush_NullWindow_IsRejected()
        {
            Assert.IsFalse(stack.TryBeginPush(null, out _));
            Assert.AreEqual(0, stack.Count);
            Assert.IsFalse(stack.IsTransitioning);
        }

        [Test]
        public void TryBeginPop_ReturnsClosingAndNext()
        {
            PushCompleted(Root);
            PushCompleted(Child);

            bool accepted = stack.TryBeginPop(out string closing, out string next);

            Assert.IsTrue(accepted);
            Assert.AreEqual(Child, closing);
            Assert.AreEqual(Root, next);
            Assert.AreEqual(Root, stack.Current);
        }

        [Test]
        public void TryBeginPop_AtRoot_IsRejected()
        {
            PushCompleted(Root);

            Assert.IsFalse(stack.TryBeginPop(out _, out _));
            Assert.AreEqual(1, stack.Count);
            Assert.IsFalse(stack.IsTransitioning);
        }

        [Test]
        public void TryBeginPop_EmptyStack_IsRejected()
        {
            Assert.IsFalse(stack.TryBeginPop(out _, out _));
        }

        [Test]
        public void Operations_DuringTransition_AreIgnored()
        {
            PushCompleted(Root);
            stack.TryBeginPush(Child, out _);

            Assert.IsFalse(stack.TryBeginPush("other", out _));
            Assert.IsFalse(stack.TryBeginPop(out _, out _));
            Assert.AreEqual(Child, stack.Current);
            Assert.AreEqual(2, stack.Count);
        }

        [Test]
        public void RepeatedPush_OnlyFirstIsAccepted_UntilTransitionEnds()
        {
            PushCompleted(Root);

            Assert.IsTrue(stack.TryBeginPush(Child, out _));
            Assert.IsFalse(stack.TryBeginPush(Child, out _));
            Assert.IsFalse(stack.TryBeginPush(Child, out _));
            Assert.AreEqual(2, stack.Count);

            stack.EndTransition();

            Assert.IsFalse(stack.IsTransitioning);
        }

        [Test]
        public void RepeatedPop_OnlyFirstIsAccepted()
        {
            PushCompleted(Root);
            PushCompleted(Child);

            Assert.IsTrue(stack.TryBeginPop(out _, out _));
            Assert.IsFalse(stack.TryBeginPop(out _, out _));
            Assert.AreEqual(1, stack.Count);
        }

        [Test]
        public void EndTransition_WithoutTransition_KeepsState()
        {
            PushCompleted(Root);

            Assert.DoesNotThrow(stack.EndTransition);
            Assert.IsFalse(stack.IsTransitioning);
            Assert.AreEqual(Root, stack.Current);
        }

        private void PushCompleted(string window)
        {
            stack.TryBeginPush(window, out _);
            stack.EndTransition();
        }
    }
}
