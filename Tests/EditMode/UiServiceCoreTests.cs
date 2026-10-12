using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Geuneda.UiService.Tests
{
    /// <summary>
    /// UiService 생성자 및 기본 초기화를 위한 EditMode 테스트.
    /// Init()(DontDestroyOnLoad를 호출하는)이 필요한 테스트는 PlayMode 테스트에 있습니다.
    /// </summary>
    [TestFixture]
    public class UiServiceCoreTests
    {
        private MockAssetLoader _mockLoader;
        private UiService _service;
        private List<Object> _createdObjects;

        [SetUp]
        public void Setup()
        {
            _mockLoader = new MockAssetLoader();
            _createdObjects = new List<Object>();
        }

        [TearDown]
        public void TearDown()
        {
            _mockLoader?.Cleanup();

            foreach (var obj in _createdObjects)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj);
                }
            }
            _createdObjects.Clear();
        }

        [Test]
        public void Constructor_WithDefaultParams_CreatesService()
        {
            // 실행
            _service = new UiService();

            // 검증
            Assert.IsNotNull(_service);
        }

        [Test]
        public void Constructor_WithCustomAssetLoader_UsesCustomLoader()
        {
            // 실행
            _service = new UiService(_mockLoader);

            // 검증
            Assert.IsNotNull(_service);
        }

        [Test]
        public void Init_WithNullConfigs_ThrowsArgumentNullException()
        {
            // 준비
            _service = new UiService(_mockLoader);

            // 실행 및 검증
            Assert.Throws<ArgumentNullException>(() => _service.Init(null));
        }

        [Test]
        public void Init_WithEmptyAddressAndNullType_ThrowsArgumentException()
        {
            // 준비 - 주소가 비어 있고 타입 이름을 찾을 수 없는 에셋 데이터 (예: 클래스 이름 변경 후 남은 항목)
            var configs = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
            _createdObjects.Add(configs);

            var field = typeof(UiConfigs).GetField("_configs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(configs, new List<UiConfigs.UiConfigSerializable>
            {
                new UiConfigs.UiConfigSerializable
                {
                    Address = string.Empty,
                    Layer = 0,
                    UiType = "Geuneda.UiService.Tests.MissingPresenter"
                }
            });

            _service = new UiService(_mockLoader);

            // 실행 및 검증
            Assert.Throws<ArgumentException>(() => _service.Init(configs));
        }

        [Test]
        public void Init_ConfigWithNullTypeSavedThroughConfigs_ThrowsArgumentException()
        {
            // 준비 - 타입이 없는 설정을 Configs 로 저장한 뒤 다시 읽는 왕복 경로
            var configs = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
            _createdObjects.Add(configs);
            configs.Configs = new List<UiConfig> { new UiConfig { Address = "address", Layer = 0, UiType = null } };

            // 검증 - 다시 읽어도 예외 없이 null 타입으로 돌아온다
            Assert.IsNull(configs.Configs[0].UiType);
            Assert.AreEqual("address", configs.Configs[0].Address);

            _service = new UiService(_mockLoader);

            // 실행 및 검증 - Init 의 설정 검증이 주소를 담은 ArgumentException 으로 알린다
            var exception = Assert.Throws<ArgumentException>(() => _service.Init(configs));
            StringAssert.Contains("address", exception.Message);
        }
    }
}
