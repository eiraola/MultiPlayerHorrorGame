using NUnit.Framework;
using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class EventBusTests
    {
        private EventBus bus;
        private StringEventBus stringBus;

        [SetUp]
        public void SetUp()
        {
            bus = ScriptableObject.CreateInstance<EventBus>();
            stringBus = ScriptableObject.CreateInstance<StringEventBus>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(bus);
            Object.DestroyImmediate(stringBus);
        }

        [Test]
        public void Raise_NotifiesSubscribers()
        {
            int notifications = 0;
            bus.Raised += () => notifications++;

            bus.Raise();

            Assert.AreEqual(1, notifications);
        }

        [Test]
        public void Raise_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(bus.Raise);
        }

        [Test]
        public void ResetRuntimeState_RemovesSubscribers()
        {
            int notifications = 0;
            bus.Raised += () => notifications++;

            bus.ResetRuntimeState();
            bus.Raise();

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void RaiseWithPayload_DeliversPayload()
        {
            string received = null;
            stringBus.Raised += payload => received = payload;

            stringBus.Raise("Menu");

            Assert.AreEqual("Menu", received);
        }

        [Test]
        public void ResetRuntimeState_WithPayload_RemovesSubscribers()
        {
            string received = null;
            stringBus.Raised += payload => received = payload;

            stringBus.ResetRuntimeState();
            stringBus.Raise("Menu");

            Assert.IsNull(received);
        }
    }
}
