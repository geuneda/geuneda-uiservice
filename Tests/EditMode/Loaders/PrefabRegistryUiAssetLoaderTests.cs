using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Geuneda.UiService.Tests
{
	[TestFixture]
	public class PrefabRegistryUiAssetLoaderTests
	{
		private PrefabRegistryUiAssetLoader _loader;
		private GameObject _testPrefab;
		private Transform _parentTransform;
		private List<GameObject> _createdObjects;

		[SetUp]
		public void SetUp()
		{
			_loader = new PrefabRegistryUiAssetLoader();
			_testPrefab = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>("TestPrefab");
			
			var parentGo = new GameObject("TestParent");
			_parentTransform = parentGo.transform;
			
			_createdObjects = new List<GameObject> { _testPrefab, parentGo };
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
		public void RegisterPrefab_AddsPrefabToRegistry()
		{
			// 준비
			const string address = "test/prefab";

			// 실행
			_loader.RegisterPrefab(address, _testPrefab);

			// 검증 - 인스턴스화 성공으로 확인
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();
			
			_createdObjects.Add(instance);
			
			Assert.IsNotNull(instance);
			Assert.AreNotSame(_testPrefab, instance); // 원본이 아닌 복제본이어야 함
		}

		[Test]
		public void RegisterPrefab_OverwritesExistingEntry()
		{
			// 준비
			const string address = "test/prefab";
			var newPrefab = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>("NewPrefab");
			_createdObjects.Add(newPrefab);

			// 실행
			_loader.RegisterPrefab(address, _testPrefab);
			_loader.RegisterPrefab(address, newPrefab); // 덮어쓰기

			// 검증
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();
			
			_createdObjects.Add(instance);
			
			Assert.IsTrue(instance.name.Contains("NewPrefab"));
		}

		[Test]
		public void InstantiatePrefab_ThrowsKeyNotFoundException_WhenPrefabNotRegistered()
		{
			// 준비
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "unregistered/address");

			// 실행 및 검증
			Assert.Throws<KeyNotFoundException>(() =>
			{
				_loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			});
		}

		[Test]
		public void InstantiatePrefab_ReturnsInactiveInstance()
		{
			// 준비
			const string address = "test/prefab";
			_loader.RegisterPrefab(address, _testPrefab);
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);

			// 실행
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();

			_createdObjects.Add(instance);

			// 검증
			Assert.IsFalse(instance.activeSelf);
		}

		[Test]
		public void InstantiatePrefab_SetsCorrectParent()
		{
			// 준비
			const string address = "test/prefab";
			_loader.RegisterPrefab(address, _testPrefab);
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);

			// 실행
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();

			_createdObjects.Add(instance);

			// 검증
			Assert.AreEqual(_parentTransform, instance.transform.parent);
		}

		[Test]
		public void UnloadAsset_DestroysGameObject()
		{
			// 준비
			const string address = "test/prefab";
			_loader.RegisterPrefab(address, _testPrefab);
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();

			// 실행
			_loader.UnloadAsset(instance);

			// 검증
			Assert.IsTrue(instance == null); // Unity가 오브젝트를 파괴
		}

		[Test]
		public void UnloadAsset_HandlesNullGracefully()
		{
			// 실행 및 검증 - 예외가 발생하지 않아야 함
			Assert.DoesNotThrow(() => _loader.UnloadAsset(null));
		}
	}
}

