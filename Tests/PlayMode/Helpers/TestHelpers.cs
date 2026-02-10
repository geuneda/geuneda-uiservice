using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace Geuneda.UiService.Tests.PlayMode
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

		/// <summary>
		/// UIDocument의 패널이 연결될 때까지 대기합니다.
		/// UI Toolkit은 활성화 후 패널 연결에 몇 프레임이 필요합니다.
		/// </summary>
		/// <param name="document">대기할 UIDocument.</param>
		/// <param name="maxFrames">실패하기 전 최대 대기 프레임 수.</param>
		/// <returns>코루틴 열거자.</returns>
		public static IEnumerator WaitForPanelAttachment(UIDocument document, int maxFrames = 10)
		{
			for (int i = 0; i < maxFrames; i++)
			{
				if (document != null && document.rootVisualElement?.panel != null)
				{
					yield break;
				}
				yield return null;
			}

			Assert.Fail($"UIDocument panel not attached after {maxFrames} frames");
		}

		/// <summary>
		/// 프레젠터의 UIDocument 패널이 연결될 때까지 대기합니다.
		/// </summary>
		/// <param name="presenter">UIDocument를 포함하는 프레젠터.</param>
		/// <param name="maxFrames">실패하기 전 최대 대기 프레임 수.</param>
		/// <returns>코루틴 열거자.</returns>
		public static IEnumerator WaitForPanelAttachment(UiPresenter presenter, int maxFrames = 10)
		{
			var document = presenter?.GetComponent<UIDocument>();
			if (document == null)
			{
				Assert.Fail("Presenter does not have a UIDocument component");
				yield break;
			}

			yield return WaitForPanelAttachment(document, maxFrames);
		}
	}
}

