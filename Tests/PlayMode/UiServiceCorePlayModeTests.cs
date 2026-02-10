using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
    /// <summary>
    /// Init() 호출이 필요한 UiService의 핵심 PlayMode 테스트.
    /// UiService.Init()이 런타임에서만 동작하는 DontDestroyOnLoad를 호출하므로
    /// 이 테스트들은 PlayMode가 필요합니다.
    /// </summary>
    [TestFixture]
    public class UiServiceCorePlayModeTests
    {
        private MockAssetLoader _mockLoader;
        private UiService _service;

        [SetUp]
        public void Setup()
        {
            _mockLoader = new MockAssetLoader();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _mockLoader?.Cleanup();
        }

        [UnityTest]
        public IEnumerator Init_WithValidConfigs_InitializesCorrectly()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );

            // 실행
            _service.Init(configs);

            // 검증 - 서비스가 로드 요청을 수락할 수 있는지 확인
            Assert.IsNotNull(_service.UiSets);
            Assert.IsNotNull(_service.VisiblePresenters);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddUiConfig_NewConfig_AddsSuccessfully()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());
            var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "new_address", 0);

            // 실행 및 검증 - 예외가 발생하지 않아야 함
            Assert.DoesNotThrow(() => _service.AddUiConfig(config));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddUiSet_NewSet_AddsSuccessfully()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());
            var set = TestHelpers.CreateTestUiSet(1);

            // 실행 & Assert
            Assert.DoesNotThrow(() => _service.AddUiSet(set));
            Assert.IsTrue(_service.UiSets.ContainsKey(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisiblePresenters_InitiallyEmpty()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // 검증
            Assert.AreEqual(0, _service.VisiblePresenters.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLoadedPresenters_InitiallyEmpty()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // 실행
            var loaded = _service.GetLoadedPresenters();

            // 검증
            Assert.AreEqual(0, loaded.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetUi_NotLoaded_ThrowsKeyNotFoundException()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );
            _service.Init(configs);

            // 실행 & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.GetUi<TestUiPresenter>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnloadUi_NotLoaded_ThrowsKeyNotFoundException()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );
            _service.Init(configs);

            // 실행 & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.UnloadUi(typeof(TestUiPresenter)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveUiSet_InvalidSetId_ThrowsKeyNotFoundException()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // 실행 & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.RemoveUiSet(999));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Dispose_CalledTwice_DoesNotThrow()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // 실행 & Assert
            Assert.DoesNotThrow(() => _service.Dispose());
            Assert.DoesNotThrow(() => _service.Dispose());
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveUi_NotLoaded_ReturnsFalse()
        {
            // 준비
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // 실행
            var result = _service.RemoveUi(typeof(TestUiPresenter));

            // 검증
            Assert.IsFalse(result);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IsVisible_NotLoaded_ReturnsFalse()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );
            _service.Init(configs);

            // 실행
            var result = _service.IsVisible<TestUiPresenter>();

            // 검증
            Assert.IsFalse(result);
            yield return null;
        }
    }
}

