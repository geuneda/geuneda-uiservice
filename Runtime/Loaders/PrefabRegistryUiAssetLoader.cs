using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 딕셔너리를 사용하여 주소를 프리팹에 매핑하는 <see cref="IUiAssetLoader"/> 구현입니다.
	/// 테스트 시 또는 프리팹이 ScriptableObject나 MonoBehaviour에서 직접 참조될 때 유용합니다.
	/// </summary>
	public class PrefabRegistryUiAssetLoader : IUiAssetLoader
	{
		private readonly Dictionary<string, GameObject> _prefabMap = new();

		/// <summary>
		/// 기본 생성자입니다.
		/// </summary>
		public PrefabRegistryUiAssetLoader()
		{
		}

		/// <summary>
		/// 주어진 <paramref name="configs"/>의 항목으로 로더를 초기화합니다.
		/// </summary>
		public PrefabRegistryUiAssetLoader(PrefabRegistryUiConfigs configs)
		{
			if (configs == null) return;

			foreach (var entry in configs.PrefabEntries)
			{
				RegisterPrefab(entry.Address, entry.Prefab);
			}
		}

		/// <summary>
		/// 주어진 주소로 프리팹을 등록합니다.
		/// </summary>
		public void RegisterPrefab(string address, GameObject prefab)
		{
			_prefabMap[address] = prefab;
		}

		/// <inheritdoc />
		public UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default)
		{
			if (!_prefabMap.TryGetValue(config.Address, out var prefab))
			{
				throw new KeyNotFoundException($"Prefab not registered for address: {config.Address}");
			}

			var instance = Object.Instantiate(prefab, parent);
			instance.SetActive(false);
			
			return UniTask.FromResult(instance);
		}

		/// <inheritdoc />
		public void UnloadAsset(GameObject asset)
		{
			if (asset != null)
			{
				Object.DestroyImmediate(asset);
			}
		}
	}
}

