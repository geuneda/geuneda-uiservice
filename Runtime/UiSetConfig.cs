using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// <seealso cref="UiService"/>에서 함께 관리할 수 있는 UI 구성 세트를 나타냅니다.
	/// 항상 함께 표시되는 UI 조합 세트에 유용합니다 (예: 재화 및 설정이 있는 플레이어 HUD)
	/// UiInstanceId를 통해 동일한 UI 타입의 다중 인스턴스를 지원합니다
	/// </summary>
	[Serializable]
	public struct UiSetConfig
	{
		public int SetId;
		public UiInstanceId[] UiInstanceIds;
	}

	/// <summary>
	/// 세트 내 UI 인스턴스를 위한 직렬화 가능한 항목입니다.
	/// Type을 문자열로 저장하고 선택적 인스턴스 주소를 포함합니다.
	/// 변경될 수 있는 어드레서블 주소를 저장하는 것보다 더 견고합니다.
	/// </summary>
	[Serializable]
	public struct UiSetEntry
	{
		/// <summary>
		/// UI 프레젠터 타입의 AssemblyQualifiedName입니다
		/// </summary>
		public string UiTypeName;
		
		/// <summary>
		/// 다중 인스턴스 지원을 위한 선택적 인스턴스 주소입니다. 빈 문자열은 기본 인스턴스를 의미합니다.
		/// </summary>
		public string InstanceAddress;
		
		public UiInstanceId ToUiInstanceId()
		{
			var type = Type.GetType(UiTypeName);
			if (type == null)
			{
				Debug.LogWarning($"Could not find type: {UiTypeName}");
				return default;
			}
			return new UiInstanceId(type, string.IsNullOrEmpty(InstanceAddress) ? null : InstanceAddress);
		}
		
		public static UiSetEntry FromUiInstanceId(UiInstanceId instanceId)
		{
			return new UiSetEntry
			{
				UiTypeName = instanceId.PresenterType.AssemblyQualifiedName,
				InstanceAddress = instanceId.InstanceAddress ?? string.Empty
			};
		}
	}

	/// <summary>
	/// ScriptableObject에서 데이터를 직렬화하기 위해 필요합니다.
	/// 견고성을 위해 어드레서블 주소 대신 Type 이름을 저장합니다.
	/// </summary>
	[Serializable]
	public struct UiSetConfigSerializable
	{
		public int SetId;
		public List<UiSetEntry> UiEntries;

		public static UiSetConfig ToUiSetConfig(UiSetConfigSerializable serializable)
		{
			var instanceIds = new List<UiInstanceId>();
			
			if (serializable.UiEntries != null)
			{
				foreach (var entry in serializable.UiEntries)
				{
					var instanceId = entry.ToUiInstanceId();
					if (instanceId.PresenterType != null)
					{
						instanceIds.Add(instanceId);
					}
				}
			}
			
			return new UiSetConfig 
			{ 
				SetId = serializable.SetId, 
				UiInstanceIds = instanceIds.ToArray()
			};
		}

		public static UiSetConfigSerializable FromUiSetConfig(UiSetConfig config)
		{
			var entries = new List<UiSetEntry>();
			
			if (config.UiInstanceIds != null)
			{
				foreach (var instanceId in config.UiInstanceIds)
				{
					if (instanceId.PresenterType != null)
					{
						entries.Add(UiSetEntry.FromUiInstanceId(instanceId));
					}
				}
			}
			
			return new UiSetConfigSerializable 
			{ 
				SetId = config.SetId, 
				UiEntries = entries 
			};
		}
	}
}