using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// Addressables 없이 테스트하기 위한 모의 에셋 로더.
	/// 실제 동작(GameObject를 실제로 인스턴스화)을 가지고 있으므로 Mock이 아닌 Fake입니다.
	/// 실제 Unity 오브젝트 생명주기 관리가 필요하므로 NSubstitute를 사용할 수 없습니다.
	/// </summary>
	public class MockAssetLoader : IUiAssetLoader
	{
		private readonly Dictionary<string, GameObject> _prefabs = new();
		private readonly List<GameObject> _instantiatedObjects = new();
		
		public int InstantiateCallCount { get; private set; }
		public int UnloadCallCount { get; private set; }
		public bool ShouldFail { get; set; }
		public int SimulatedDelayMs { get; set; }

		public void RegisterPrefab(string address, GameObject prefab)
		{
			_prefabs[address] = prefab;
		}

		public void RegisterPrefab<T>(string address) where T : UiPresenter
		{
			_prefabs[address] = TestHelpers.CreateTestPresenterPrefab<T>();
		}

		public async UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken ct = default)
		{
			InstantiateCallCount++;

			if (ShouldFail)
			{
				throw new Exception($"Simulated load failure for {config.Address}");
			}

			if (SimulatedDelayMs > 0)
			{
				await UniTask.Delay(SimulatedDelayMs, cancellationToken: ct);
			}

			if (!_prefabs.TryGetValue(config.Address, out var prefab))
			{
				throw new Exception($"Prefab not registered: {config.Address}");
			}

			var instance = UnityEngine.Object.Instantiate(prefab, parent);
			
			// 모든 컴포넌트에서 Awake()를 트리거하기 위해 잠시 활성화한 후 비활성화
			// Addressables가 활성 프리팹으로 인스턴스화하는 방식을 모방
			var wasActive = instance.activeSelf;
			if (!wasActive)
			{
				instance.SetActive(true);
				instance.SetActive(false);
			}
			
			_instantiatedObjects.Add(instance);
			return instance;
		}

		public void UnloadAsset(GameObject gameObject)
		{
			UnloadCallCount++;
			if (gameObject != null)
			{
				_instantiatedObjects.Remove(gameObject);
				UnityEngine.Object.DestroyImmediate(gameObject);
			}
		}

		public void Cleanup()
		{
			foreach (var obj in _instantiatedObjects)
			{
				if (obj != null)
				{
					UnityEngine.Object.DestroyImmediate(obj);
				}
			}
			_instantiatedObjects.Clear();
			
			foreach (var prefab in _prefabs.Values)
			{
				if (prefab != null)
				{
					UnityEngine.Object.DestroyImmediate(prefab);
				}
			}
			_prefabs.Clear();
		}
	}
}

