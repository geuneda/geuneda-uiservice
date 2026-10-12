using NUnit.Framework;
using UnityEngine;

namespace Geuneda.UiService.Tests
{
    [TestFixture]
    public class UiConfigTests
    {
        [Test]
        public void UiConfig_Creation_PreservesAllProperties()
        {
            // 준비 및 실행
            var config = new UiConfig
            {
                Address = "test_address",
                Layer = 5,
                UiType = typeof(TestUiPresenter),
                LoadSynchronously = true
            };

            // 검증
            Assert.AreEqual("test_address", config.Address);
            Assert.AreEqual(5, config.Layer);
            Assert.AreEqual(typeof(TestUiPresenter), config.UiType);
            Assert.IsTrue(config.LoadSynchronously);
        }

        [Test]
        public void UiConfig_DefaultValues_AreCorrect()
        {
            // 준비 및 실행
            var config = new UiConfig();

            // 검증
            Assert.IsNull(config.Address);
            Assert.AreEqual(0, config.Layer);
            Assert.IsNull(config.UiType);
            Assert.IsFalse(config.LoadSynchronously);
        }

        [Test]
        public void UiConfigs_Configs_PreservesLoadSynchronously()
        {
            // 준비
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "sync_address", 0, true),
                TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "async_address", 0, false));

            try
            {
                // 실행
                var result = configs.Configs;

                // 검증
                Assert.AreEqual(2, result.Count);
                Assert.IsTrue(result[0].LoadSynchronously);
                Assert.IsFalse(result[1].LoadSynchronously);
            }
            finally
            {
                Object.DestroyImmediate(configs);
            }
        }

        [Test]
        public void UiConfigSerializable_JsonRoundTrip_PreservesLoadSynchronously()
        {
            // 준비
            UiConfigs.UiConfigSerializable serializable = new UiConfig
            {
                Address = "test_address",
                Layer = 3,
                UiType = typeof(TestUiPresenter),
                LoadSynchronously = true
            };

            // 실행
            var json = JsonUtility.ToJson(serializable);
            UiConfig config = JsonUtility.FromJson<UiConfigs.UiConfigSerializable>(json);

            // 검증
            Assert.IsTrue(config.LoadSynchronously);
            Assert.AreEqual("test_address", config.Address);
            Assert.AreEqual(3, config.Layer);
            Assert.AreEqual(typeof(TestUiPresenter), config.UiType);
        }

        [Test]
        public void UiConfigSerializable_DataWithoutLoadSynchronously_ReadsAsFalse()
        {
            // 준비 - LoadSynchronously 필드가 추가되기 전에 저장된 데이터
            var json = "{\"Address\":\"test_address\",\"Layer\":1,\"UiType\":\"" + typeof(TestUiPresenter).AssemblyQualifiedName + "\"}";

            // 실행
            UiConfig config = JsonUtility.FromJson<UiConfigs.UiConfigSerializable>(json);

            // 검증
            Assert.IsFalse(config.LoadSynchronously);
            Assert.AreEqual("test_address", config.Address);
            Assert.AreEqual(1, config.Layer);
            Assert.AreEqual(typeof(TestUiPresenter), config.UiType);
        }
    }
}

