using NUnit.Framework;
using System.Collections.Generic;

namespace Geuneda.UiService.Tests
{
    [TestFixture]
    public class UiSetConfigTests
    {
        [Test]
        public void UiSetConfig_Creation_StoresSetId()
        {
            // 준비 및 실행
            var setConfig = new UiSetConfig
            {
                SetId = 42,
                UiInstanceIds = new UiInstanceId[0]
            };

            // 검증
            Assert.AreEqual(42, setConfig.SetId);
        }

        [Test]
        public void UiSetEntry_ToUiInstanceId_ConvertsCorrectly()
        {
            // 준비
            var entry = new UiSetEntry
            {
                UiTypeName = typeof(TestUiPresenter).AssemblyQualifiedName,
                InstanceAddress = "test_instance"
            };

            // 실행 - 변환을 수행하는 UiInstanceId 프로퍼티에 접근
            var instanceId = entry.ToUiInstanceId();

            // 검증
            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual("test_instance", instanceId.InstanceAddress);
        }

        [Test]
        public void UiSetEntry_EmptyAddress_IsDefault()
        {
            // 준비
            var entry = new UiSetEntry
            {
                UiTypeName = typeof(TestUiPresenter).AssemblyQualifiedName,
                InstanceAddress = ""
            };

            // 실행
            var instanceId = entry.ToUiInstanceId();

            // 검증
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        public void Serialization_RoundTrip_PreservesData()
        {
            // 준비
            var originalConfig = new UiSetConfig
            {
                SetId = 1,
                UiInstanceIds = new[]
                {
                    new UiInstanceId(typeof(TestUiPresenter), "inst1"),
                    new UiInstanceId(typeof(TestDataUiPresenter))
                }
            };

            // 실행
            var serializable = UiSetConfigSerializable.FromUiSetConfig(originalConfig);
            var deserialized = UiSetConfigSerializable.ToUiSetConfig(serializable);

            // 검증
            Assert.AreEqual(originalConfig.SetId, deserialized.SetId);
            Assert.AreEqual(originalConfig.UiInstanceIds.Length, deserialized.UiInstanceIds.Length);
            Assert.AreEqual(originalConfig.UiInstanceIds[0], deserialized.UiInstanceIds[0]);
            Assert.AreEqual(originalConfig.UiInstanceIds[1], deserialized.UiInstanceIds[1]);
        }
    }
}

