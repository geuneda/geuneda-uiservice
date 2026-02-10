using System;
using System.Collections.Generic;
using UnityEngine;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// 테스트 오브젝트 생성을 위한 헬퍼 메서드
	/// </summary>
	public static class TestHelpers
	{
		public static UiConfig CreateTestConfig(Type type, string address, int layer = 0, bool loadSync = false)
		{
			return new UiConfig
			{
				Address = address,
				Layer = layer,
				UiType = type,
				LoadSynchronously = loadSync
			};
		}
		
		public static UiConfigs CreateTestConfigs(params UiConfig[] configs)
		{
			var scriptableObject = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
			scriptableObject.Configs = new List<UiConfig>(configs);
			return scriptableObject;
		}
		
		public static UiConfigs CreateTestConfigsWithSets(UiConfig[] configs, UiSetConfig[] sets)
		{
			var scriptableObject = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
			scriptableObject.Configs = new List<UiConfig>(configs);

			// 직렬화 가능 버전을 사용하여 데이터를 설정
			var serializableSets = new List<UiSetConfigSerializable>();
			foreach (var set in sets)
			{
				serializableSets.Add(UiSetConfigSerializable.FromUiSetConfig(set));
			}

			// 읽기 전용 프로퍼티를 우회하는 유일한 방법이므로 리플렉션을 사용하여 private 필드 _sets를 설정
			var field = typeof(UiConfigs).GetField("_sets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			field.SetValue(scriptableObject, serializableSets);

			return scriptableObject;
		}

		public static GameObject CreateTestPresenterPrefab<T>(string name = null) where T : UiPresenter
		{
			var go = new GameObject(name ?? typeof(T).Name);
			go.AddComponent<Canvas>(); // 대부분의 프레젠터는 캔버스가 필요
			go.AddComponent<T>(); // 캔버스를 찾을 수 있도록 캔버스 이후에 프레젠터 추가
			go.SetActive(false);
			return go;
		}
		
		public static UiSetConfig CreateTestUiSet(int setId, params UiInstanceId[] instanceIds)
		{
			return new UiSetConfig
			{
				SetId = setId,
				UiInstanceIds = instanceIds
			};
		}

		public static UiSetEntry CreateSetEntry(Type type, string address = "")
		{
			return new UiSetEntry
			{
				UiTypeName = type.AssemblyQualifiedName,
				InstanceAddress = address
			};
		}
	}
}

