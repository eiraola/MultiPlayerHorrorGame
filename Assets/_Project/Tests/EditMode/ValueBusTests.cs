using NUnit.Framework;
using ProjectScare.Core;
using UnityEditor;
using UnityEngine;

namespace ProjectScare.Tests.EditMode
{
    public class ValueBusTests
    {
        private BoolValueBus bus;

        [SetUp]
        public void SetUp() => bus = ScriptableObject.CreateInstance<BoolValueBus>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(bus);

        [Test]
        public void Set_NewValue_UpdatesValueAndNotifies()
        {
            bool received = false;
            bus.Changed += value => received = value;

            bus.Set(true);

            Assert.IsTrue(bus.Value);
            Assert.IsTrue(received);
        }

        [Test]
        public void Set_SameValue_DoesNotNotify()
        {
            int notifications = 0;
            bus.Changed += _ => notifications++;

            bus.Set(false);

            Assert.AreEqual(0, notifications);
        }

        [Test]
        public void ResetRuntimeState_RestoresInitialValue()
        {
            SetInitialValue(true);
            bus.Set(false);

            bus.ResetRuntimeState();

            Assert.IsTrue(bus.Value);
        }

        [Test]
        public void ResetRuntimeState_RemovesSubscribers()
        {
            int notifications = 0;
            bus.Changed += _ => notifications++;

            bus.ResetRuntimeState();
            bus.Set(true);

            Assert.AreEqual(0, notifications);
        }

        private void SetInitialValue(bool value)
        {
            using var serializedBus = new SerializedObject(bus);
            serializedBus.FindProperty("initialValue").boolValue = value;
            serializedBus.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
