using NUnit.Framework;
using ProjectScare.Core;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class EventBusTests
    {
        [Test]
        public void Raise_NotifiesSubscribers()
        {
            EventBus bus = ScriptableObject.CreateInstance<EventBus>();
            int notifications = 0;
            bus.Raised += () => notifications++;

            bus.Raise();

            Assert.AreEqual(1, notifications);
            Object.DestroyImmediate(bus);
        }

        [Test]
        public void Raise_WithoutSubscribers_DoesNotThrow()
        {
            EventBus bus = ScriptableObject.CreateInstance<EventBus>();

            Assert.DoesNotThrow(bus.Raise);

            Object.DestroyImmediate(bus);
        }

        [Test]
        public void ResetRuntimeState_RemovesSubscribers()
        {
            EventBus bus = ScriptableObject.CreateInstance<EventBus>();
            int notifications = 0;
            bus.Raised += () => notifications++;

            bus.ResetRuntimeState();
            bus.Raise();

            Assert.AreEqual(0, notifications);
            Object.DestroyImmediate(bus);
        }

        [Test]
        public void RaiseWithPayload_DeliversPayload()
        {
            StringEventBus bus = ScriptableObject.CreateInstance<StringEventBus>();
            string received = null;
            bus.Raised += payload => received = payload;

            bus.Raise("Menu");

            Assert.AreEqual("Menu", received);
            Object.DestroyImmediate(bus);
        }

        [Test]
        public void ResetRuntimeState_WithPayload_RemovesSubscribers()
        {
            StringEventBus bus = ScriptableObject.CreateInstance<StringEventBus>();
            string received = null;
            bus.Raised += payload => received = payload;

            bus.ResetRuntimeState();
            bus.Raise("Menu");

            Assert.IsNull(received);
            Object.DestroyImmediate(bus);
        }
    }
}
