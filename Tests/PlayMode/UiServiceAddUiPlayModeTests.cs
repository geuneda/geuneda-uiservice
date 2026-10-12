using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
    /// <summary>
    /// 설정 없이 <see cref="UiService.AddUi{T}(T, int, bool)"/> 로 넣은 프레젠터의 언로드와 레이어 닫기 테스트.
    /// 프레젠터 컴포넌트를 GameObject 에 붙여야 해서 EditMode(에디터 전용 어셈블리)가 아니라 PlayMode 에 둡니다.
    /// </summary>
    [TestFixture]
    public class UiServiceAddUiPlayModeTests
    {
        private MockAssetLoader _mockLoader;
        private UiService _service;
        private List<GameObject> _createdObjects;

        [SetUp]
        public void Setup()
        {
            _mockLoader = new MockAssetLoader();
            _createdObjects = new List<GameObject>();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _mockLoader?.Cleanup();

            foreach (var go in _createdObjects)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [UnityTest]
        public IEnumerator UnloadUi_AddedWithoutConfig_UnloadsPresenter()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var presenter = CreatePresenter();
            _service.AddUi(presenter, 0);

            // 실행
            _service.UnloadUi<TestUiPresenter>();

            // 검증
            Assert.AreEqual(1, _mockLoader.UnloadCallCount);
            Assert.IsEmpty(_service.GetLoadedPresenters());
            yield return null;
        }

        [UnityTest]
        public IEnumerator CloseAllUi_Layer_AddedWithoutConfig_ClosesPresenterOnLayer()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var presenter = CreatePresenter();
            _service.AddUi(presenter, 3, true);

            Assert.IsTrue(_service.IsVisible<TestUiPresenter>());

            // 실행
            _service.CloseAllUi(3);

            // 검증
            Assert.IsFalse(_service.IsVisible<TestUiPresenter>());
            Assert.IsTrue(presenter.WasClosed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CloseAllUi_Layer_AddedWithoutConfig_KeepsPresenterOnOtherLayer()
        {
            // 준비
            _service = new UiService(_mockLoader);
            var presenter = CreatePresenter();
            _service.AddUi(presenter, 3, true);

            // 실행
            _service.CloseAllUi(5);

            // 검증
            Assert.IsTrue(_service.IsVisible<TestUiPresenter>());
            Assert.IsFalse(presenter.WasClosed);
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnloadUi_AddressablesLoader_AddedWithoutAddressables_DestroysGameObject()
        {
            // 준비 - 기본 로더에 Addressables 로 만들지 않은 인스턴스를 넣는다
            _service = new UiService(new AddressablesUiAssetLoader());
            var presenter = CreatePresenter();
            var go = presenter.gameObject;
            _service.AddUi(presenter, 0);

            // 실행
            _service.UnloadUi<TestUiPresenter>();
            yield return null;

            // 검증 - 서비스 목록에서 빠지고 GameObject 도 실제로 파괴되어야 한다
            Assert.IsEmpty(_service.GetLoadedPresenters());
            Assert.IsTrue(go == null);
        }

        private TestUiPresenter CreatePresenter()
        {
            var go = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>();
            _createdObjects.Add(go);
            return go.GetComponent<TestUiPresenter>();
        }
    }
}
