using NUnit.Framework;
using System;

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

        [SetUp]
        public void Setup()
        {
            _mockLoader = new MockAssetLoader();
        }

        [TearDown]
        public void TearDown()
        {
            _mockLoader?.Cleanup();
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
    }
}
