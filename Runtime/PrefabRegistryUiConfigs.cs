using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// PrefabRegistry 기반 에셋 로딩을 위한 UiConfigs 구현입니다.
	/// 직접 프리팹 참조를 통해 UI 프레젠터를 로드할 때 사용하세요.
	/// </summary>
	[CreateAssetMenu(fileName = "PrefabRegistryUiConfigs", menuName = "ScriptableObjects/Configs/UiConfigs/PrefabRegistry")]
	public class PrefabRegistryUiConfigs : UiConfigs
	{
		/// <summary>
		/// 주소를 프리팹에 매핑하는 항목을 나타냅니다.
		/// </summary>
		[Serializable]
		public struct PrefabEntry
		{
			public string Address;
			public GameObject Prefab;
		}

		[SerializeField] private List<PrefabEntry> _prefabEntries = new();

		/// <summary>
		/// <see cref="PrefabRegistryUiAssetLoader"/>와 함께 사용할 프리팹 항목 목록을 가져옵니다.
		/// </summary>
		public IReadOnlyList<PrefabEntry> PrefabEntries => _prefabEntries;
	}
}

