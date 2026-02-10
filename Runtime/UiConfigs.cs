using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// <seealso cref="UiPresenter"/>의 모든 중요 데이터를 포함하는 구성을 나타냅니다.
	/// Id는 UiIdsGenerator 코드 생성기가 생성한 UI의 정수 표현입니다
	/// </summary>
	[Serializable]
	public struct UiConfig
	{
		[FormerlySerializedAs("AddressableAddress")]
		public string Address;
		public int Layer;
		public Type UiType;
		public bool LoadSynchronously;
	}

	/// <summary>
	/// <see cref="IUiService"/>에서 사용할 <seealso cref="UiConfig"/> 및 <seealso cref="UiSetConfig"/>를 가져오기 위한 ScriptableObject 도구입니다.
	/// UiConfigs 에셋을 생성하려면 다음 파생 타입 중 하나를 사용하세요:
	/// <list type="bullet">
	/// <item><see cref="AddressablesUiConfigs"/> - Addressables 기반 로딩용</item>
	/// <item><see cref="ResourcesUiConfigs"/> - Resources 폴더 로딩용</item>
	/// <item><see cref="PrefabRegistryUiConfigs"/> - 직접 프리팹 참조용</item>
	/// </list>
	/// </summary>
	public abstract class UiConfigs : ScriptableObject//, IConfigsContainer<UiConfig>
	{
		[SerializeField]
		private List<UiConfigSerializable> _configs = new List<UiConfigSerializable>();
		[SerializeField]
		private List<UiSetConfigSerializable> _sets = new List<UiSetConfigSerializable>();

		/// <summary>
		/// UI 구성 목록을 가져오거나 설정합니다
		/// </summary>
		public List<UiConfig> Configs
		{
			get { return _configs.ConvertAll(element => (UiConfig)element); }
			set { _configs = value.ConvertAll(element => (UiConfigSerializable)element); }
		}

		/// <summary>
		/// UI 세트 구성 목록을 가져옵니다
		/// </summary>
		public List<UiSetConfig> Sets => _sets.ConvertAll(element => UiSetConfigSerializable.ToUiSetConfig(element));

		/// <summary>
		/// 이 ScriptableObject의 <seealso cref="UiSetConfig"/> 목록의 새로운 크기를 설정합니다.
		/// UiConfigSet은 목록의 인덱스와 동일한 id 값을 가집니다.
		/// 사용 가능한 UI 구성에 대해 항목을 검증합니다.
		/// </summary>
		/// <param name="size">목록의 새로운 크기</param>
		public void SetSetsSize(int size)
		{
			var validTypeNames = new HashSet<string>(_configs.Select(c => c.UiType));
			
			if (size < _sets.Count)
			{
				_sets.RemoveRange(size, _sets.Count - size);
			}

			for (int i = 0; i < size; i++)
			{
				if (i < _sets.Count)
				{
					var set = _sets[i];
					
					// UiEntries가 null이면 초기화합니다
					if (set.UiEntries == null)
					{
						set.UiEntries = new List<UiSetEntry>();
					}
					
					// 존재하지 않는 UI 타입을 참조하는 항목을 제거합니다
					set.UiEntries.RemoveAll(entry => !validTypeNames.Contains(entry.UiTypeName));
					_sets[i] = set;
					continue;
				}

				_sets.Add(new UiSetConfigSerializable { SetId = i, UiEntries = new List<UiSetEntry>() });
			}
		}

		/// <summary>
		/// ScriptableObject에서 데이터를 직렬화하기 위해 필요합니다
		/// </summary>
		[Serializable]
		public struct UiConfigSerializable
		{
			[FormerlySerializedAs("AddressableAddress")]
			public string Address;
			public int Layer;
			public string UiType;

			public static implicit operator UiConfig(UiConfigSerializable serializable)
			{
				return new UiConfig
				{
					Address = serializable.Address,
					Layer = serializable.Layer,
					UiType = Type.GetType(serializable.UiType),
					LoadSynchronously = false
				};
			}

			public static implicit operator UiConfigSerializable(UiConfig config)
			{
				return new UiConfigSerializable
				{
					Address = config.Address,
					Layer = config.Layer,
					UiType = config.UiType.AssemblyQualifiedName
				};
			}
		}

	}
}