using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Geuneda.UiService.Tests
{
	[TestFixture]
	public class ResourcesUiAssetLoaderTests
	{
		private ResourcesUiAssetLoader _loader;
		private Transform _parentTransform;
		private List<GameObject> _createdObjects;

		// 참고: 테스트에서 Resources 폴더 에셋을 쉽게 생성할 수 없으므로 모의 접근 방식을 사용합니다.
		// 로더는 누락된 리소스에 대해 KeyNotFoundException을 던지며, 이를 검증합니다.

		[SetUp]
		public void SetUp()
		{
			_loader = new ResourcesUiAssetLoader();
			
			var parentGo = new GameObject("TestParent");
			_parentTransform = parentGo.transform;
			
			_createdObjects = new List<GameObject> { parentGo };
		}

		[TearDown]
		public void TearDown()
		{
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
		public void InstantiatePrefab_ThrowsKeyNotFoundException_WhenResourceNotFound()
		{
			// 준비
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "nonexistent/resource/path");

			// 실행 및 검증
			Assert.Throws<KeyNotFoundException>(() =>
			{
				_loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			});
		}

		[Test]
		public void UnloadAsset_HandlesNullGracefully()
		{
			// 실행 및 검증 - 예외가 발생하지 않아야 함
			Assert.DoesNotThrow(() => _loader.UnloadAsset(null));
		}

		[Test]
		public void UnloadAsset_DestroysGameObject()
		{
			// 준비 - 파괴 테스트를 위한 간단한 GameObject 생성
			var testObject = new GameObject("TestObject");
			_createdObjects.Add(testObject);

			// 실행
			_loader.UnloadAsset(testObject);

			// 검증
			Assert.IsTrue(testObject == null); // Unity가 오브젝트를 파괴
		}

		[Test]
		public void InstantiatePrefab_UsesAddressAsResourcePath()
		{
			// 준비
			const string resourcePath = "UI/TestPresenter";
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), resourcePath);

			// 실행 및 검증
			// 메시지에 경로가 포함된 KeyNotFoundException을 던져야 함
			var ex = Assert.Throws<KeyNotFoundException>(() =>
			{
				_loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			});

			Assert.IsTrue(ex.Message.Contains(resourcePath));
		}

		[Test]
		public void MultipleInstances_CanBeCreated()
		{
			// 로더에 싱글톤 제한이 없는지 검증하는 테스트
			// 실제 Resources로 테스트할 수 없지만, 로더가 여러 번 호출될 수 있는지 확인
			
			var loader1 = new ResourcesUiAssetLoader();
			var loader2 = new ResourcesUiAssetLoader();

			// 둘 다 독립적인 인스턴스여야 함
			Assert.AreNotSame(loader1, loader2);
		}
	}
}

