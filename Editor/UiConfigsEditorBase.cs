using System;
using System.Collections.Generic;
using System.Linq;
using Geuneda.UiService;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// 레이어 시각화 및 세트 관리를 위한 공유 로직을 가진 <see cref="UiConfigs"/> 에디터의 기본 클래스입니다.
	/// </summary>
	/// <typeparam name="TSet"><see cref="UiSetConfig"/> id의 enum 타입</typeparam>
	public abstract class UiConfigsEditorBase<TSet> : Editor
		where TSet : Enum
	{
		protected const string VisualizerPrefsKey = "UiConfigsEditor_ShowVisualizer";

		protected UiConfigs ScriptableObjectInstance;
		protected SerializedProperty ConfigsProperty;
		protected SerializedProperty SetsProperty;
		protected bool ShowVisualizer;
		protected VisualElement VisualizerContainer;
		protected string VisualizerSearchFilter = "";

		protected virtual string ConfigExplanation =>
			"UI 프레젠터 설정\n\n" +
			"게임 내 모든 UI 프레젠터 프리팹과 정렬 레이어 값을 나열합니다. " +
			"Layer 필드는 렌더링 순서를 제어하며, 값이 클수록 카메라에 가깝게 표시됩니다. " +
			"Canvas 또는 UIDocument 컴포넌트가 있는 프레젠터의 경우, 이 값이 UI 정렬 순서에 직접 매핑됩니다.";

		protected virtual string SetExplanation =>
			"UI 세트 설정\n\n" +
			"UI 세트는 함께 표시되어야 하는 여러 프레젠터 인스턴스를 그룹화합니다. " +
			"UiService를 통해 세트가 활성화되면 모든 프레젠터가 동시에 로드되고 표시됩니다. " +
			"프레젠터는 나열된 순서대로(위에서 아래로) 로드됩니다.";

		protected virtual void OnEnable()
		{
			ScriptableObjectInstance = target as UiConfigs;
			if (ScriptableObjectInstance == null) return;

			SyncConfigs();
			
			// 세트 배열이 enum 크기와 일치하는지 확인합니다
			ScriptableObjectInstance.SetSetsSize(Enum.GetNames(typeof(TSet)).Length);
			
			serializedObject.Update();
			
			ConfigsProperty = serializedObject.FindProperty("_configs");
			SetsProperty = serializedObject.FindProperty("_sets");
			
			ShowVisualizer = EditorPrefs.GetBool(VisualizerPrefsKey, false);
		}

		/// <inheritdoc />
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			root.style.paddingTop = 5;
			root.style.paddingBottom = 5;
			root.style.paddingLeft = 3;
			root.style.paddingRight = 3;

			// 섹션 0: 레이어 시각화 도구 (접기 가능)
			var visualizerSection = CreateVisualizerSection();
			root.Add(visualizerSection);

			// 섹션 1: UI 구성 설명
			var configHelpBox = new HelpBox(ConfigExplanation, HelpBoxMessageType.Info);
			configHelpBox.style.marginBottom = 10;
			root.Add(configHelpBox);

			// 섹션 2: UI 구성 목록
			var configsListView = CreateConfigsListView();
			root.Add(configsListView);

			// 섹션 3: 구분선
			var separator = new VisualElement();
			separator.style.height = 1;
			separator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
			separator.style.marginTop = 15;
			separator.style.marginBottom = 15;
			root.Add(separator);

			// 섹션 4: UI 세트 설명
			var setHelpBox = new HelpBox(SetExplanation, HelpBoxMessageType.Info);
			setHelpBox.style.marginBottom = 10;
			root.Add(setHelpBox);

			// 섹션 5: UI 세트 목록
			var setsContainer = CreateSetsContainer();
			root.Add(setsContainer);

			return root;
		}

		protected abstract void SyncConfigs();
		protected abstract IReadOnlyList<string> GetAddressList();
		protected abstract Dictionary<string, string> GetAssetPathLookup();
		protected abstract Dictionary<string, Type> GetUiTypesByAddress();

		protected virtual ListView CreateConfigsListView()
		{
			var listView = new ListView
			{
				showBorder = true,
				showFoldoutHeader = true,
				headerTitle = "UI 프레젠터 설정",
				showAddRemoveFooter = false,
				showBoundCollectionSize = false,
				reorderable = false,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 22
			};

			listView.style.minHeight = 20;
			listView.style.marginBottom = 5;

			listView.BindProperty(ConfigsProperty);

			listView.makeItem = CreateConfigElement;
			listView.bindItem = BindConfigElement;

			return listView;
		}

		protected abstract VisualElement CreateConfigElement();
		protected abstract void BindConfigElement(VisualElement element, int index);

		protected virtual void OnLayerChanged(ChangeEvent<int> evt)
		{
			if (evt.newValue == evt.previousValue) return;

			var layerField = evt.target as IntegerField;
			if (layerField?.userData is string address)
			{
				SyncLayerToPrefab(address, evt.newValue);
			}
		}

		protected virtual void SyncLayerToPrefab(string address, int newLayer)
		{
			var pathLookup = GetAssetPathLookup();
			if (pathLookup == null || !pathLookup.TryGetValue(address, out var assetPath)) return;

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (prefab == null) return;

			bool changed = false;

			if (prefab.TryGetComponent<Canvas>(out var canvas))
			{
				canvas.sortingOrder = newLayer;
				changed = true;
			}
			else if (prefab.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document))
			{
				document.sortingOrder = newLayer;
				changed = true;
			}

			if (changed)
			{
				EditorUtility.SetDirty(prefab);
				AssetDatabase.SaveAssets();
			}
		}

		private VisualElement CreateSetsContainer()
		{
			var container = new VisualElement();
			var enumNames = Enum.GetNames(typeof(TSet));

			for (int setIndex = 0; setIndex < enumNames.Length; setIndex++)
			{
				var setElement = CreateSetElement(enumNames[setIndex], setIndex);
				container.Add(setElement);
			}

			return container;
		}

		private VisualElement CreateSetElement(string setName, int setIndex)
		{
			var setContainer = new VisualElement();
			setContainer.style.paddingLeft = 5;
			setContainer.style.paddingRight = 5;
			setContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);
			setContainer.style.borderBottomLeftRadius = 4;
			setContainer.style.borderBottomRightRadius = 4;
			setContainer.style.borderTopLeftRadius = 4;
			setContainer.style.borderTopRightRadius = 4;
			setContainer.style.marginBottom = 5;

			// Header
			var header = new Label($"{setName} Set");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.fontSize = 13;
			header.style.marginBottom = 5;
			setContainer.Add(header);

			// 이 세트의 UI 항목 프로퍼티를 가져옵니다
			var setProperty = SetsProperty.GetArrayElementAtIndex(setIndex);
			var uiEntriesProperty = setProperty.FindPropertyRelative(nameof(UiSetConfigSerializable.UiEntries));

			// 이 세트의 프레젠터 ListView
			var presenterListView = new ListView
			{
				showBorder = true,
				showAddRemoveFooter = true,
				reorderable = true,
				showBoundCollectionSize = false,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 28
			};

			presenterListView.BindProperty(uiEntriesProperty);

			presenterListView.makeItem = CreateSetPresenterElement;
			presenterListView.bindItem = (element, index) => BindSetPresenterElement(element, index, uiEntriesProperty, presenterListView);
			
			presenterListView.itemsAdded += indices => OnPresenterItemsAdded(indices, uiEntriesProperty);
			presenterListView.itemsRemoved += _ => SaveSetChanges();
			presenterListView.itemIndexChanged += (_, _) => SaveSetChanges();

			setContainer.Add(presenterListView);

			return setContainer;
		}

		private VisualElement CreateSetPresenterElement()
		{
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.alignItems = Align.Center;
			
			// 드래그 핸들
			var dragHandle = new Label("☰");
			dragHandle.style.width = 20;
			dragHandle.style.unityTextAlign = TextAnchor.MiddleCenter;
			dragHandle.style.marginLeft = 3;
			dragHandle.style.marginRight = 5;
			dragHandle.style.fontSize = 16;
			dragHandle.style.color = new Color(0.7f, 0.7f, 0.7f);
			container.Add(dragHandle);
			
			// 드롭다운
			var dropdown = new DropdownField();
			dropdown.style.flexGrow = 1;
			dropdown.style.paddingTop = 3;
			dropdown.style.paddingBottom = 3;
			dropdown.name = "ui-type-dropdown";
			container.Add(dropdown);
			
			// 삭제 버튼
			var deleteButton = new Button { text = "×" };
			deleteButton.style.width = 25;
			deleteButton.style.height = 20;
			deleteButton.style.marginLeft = 5;
			deleteButton.name = "delete-button";
			container.Add(deleteButton);
			
			return container;
		}

		private void BindSetPresenterElement(VisualElement element, int index, SerializedProperty uiEntriesProperty, ListView listView)
		{
			if (index >= uiEntriesProperty.arraySize) return;

			var dropdown = element.Q<DropdownField>("ui-type-dropdown");
			if (dropdown == null) return;

			var entryProperty = uiEntriesProperty.GetArrayElementAtIndex(index);
			var typeNameProperty = entryProperty.FindPropertyRelative(nameof(UiSetEntry.UiTypeName));
			var instanceAddressProperty = entryProperty.FindPropertyRelative(nameof(UiSetEntry.InstanceAddress));
			
			var addressList = GetAddressList();
			var uiTypesByAddress = GetUiTypesByAddress();
			dropdown.choices = new List<string>(addressList);

			// 타입에 일치하는 주소를 찾습니다
			var currentTypeName = typeNameProperty.stringValue;
			Type currentType = string.IsNullOrEmpty(currentTypeName) ? null : Type.GetType(currentTypeName);
			
			string matchingAddress = null;
			if (currentType != null && uiTypesByAddress != null)
			{
				foreach (var kvp in uiTypesByAddress)
				{
					if (kvp.Value == currentType)
					{
						matchingAddress = kvp.Key;
						break;
					}
				}
			}
			
			var selectedIndex = string.IsNullOrEmpty(matchingAddress) ? 0 : addressList.ToList().FindIndex(a => a == matchingAddress);
			if (selectedIndex < 0) selectedIndex = 0;

			if (addressList.Count > 0)
			{
				dropdown.Unbind();
				dropdown.index = selectedIndex;
				
				dropdown.RegisterValueChangedCallback(_ =>
				{
					var newIndex = dropdown.index;
					if (newIndex >= 0 && newIndex < addressList.Count)
					{
						var selectedAddress = addressList[newIndex];
						if (uiTypesByAddress.TryGetValue(selectedAddress, out var selectedType))
						{
							typeNameProperty.stringValue = selectedType.AssemblyQualifiedName;
							instanceAddressProperty.stringValue = selectedAddress;
							SaveSetChanges();
						}
					}
				});
				
				if (string.IsNullOrEmpty(typeNameProperty.stringValue) && selectedIndex < addressList.Count)
				{
					var address = addressList[selectedIndex];
					if (uiTypesByAddress.TryGetValue(address, out var type))
					{
						typeNameProperty.stringValue = type.AssemblyQualifiedName;
						instanceAddressProperty.stringValue = address;
						serializedObject.ApplyModifiedProperties();
					}
				}
			}

			var deleteButton = element.Q<Button>("delete-button");
			if (deleteButton != null)
			{
				if (deleteButton.userData is EventCallback<ClickEvent> previousCallback)
				{
					deleteButton.UnregisterCallback(previousCallback);
				}

				EventCallback<ClickEvent> clickHandler = _ =>
				{
					uiEntriesProperty.DeleteArrayElementAtIndex(index);
					SaveSetChanges();
				};

				deleteButton.userData = clickHandler;
				deleteButton.RegisterCallback(clickHandler);
			}
		}

		private void OnPresenterItemsAdded(IEnumerable<int> indices, SerializedProperty uiEntriesProperty)
		{
			var addressList = GetAddressList();
			var uiTypesByAddress = GetUiTypesByAddress();
			if (addressList.Count == 0 || uiTypesByAddress == null) return;

			var defaultAddress = addressList[0];
			Type defaultType = uiTypesByAddress.TryGetValue(defaultAddress, out var type) ? type : null;
			
			foreach (var index in indices)
			{
				if (index < uiEntriesProperty.arraySize)
				{
					var entryProperty = uiEntriesProperty.GetArrayElementAtIndex(index);
					entryProperty.FindPropertyRelative(nameof(UiSetEntry.UiTypeName)).stringValue = defaultType?.AssemblyQualifiedName ?? string.Empty;
					entryProperty.FindPropertyRelative(nameof(UiSetEntry.InstanceAddress)).stringValue = defaultAddress;
				}
			}
		
			SaveSetChanges();
		}

		protected void SaveSetChanges()
		{
			serializedObject.ApplyModifiedProperties();
			if (ScriptableObjectInstance != null)
			{
				EditorUtility.SetDirty(ScriptableObjectInstance);
				AssetDatabase.SaveAssets();
			}
		}

		protected VisualElement CreateVisualizerSection()
		{
			var section = new VisualElement();
			section.style.marginBottom = 15;
			
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			header.style.paddingTop = 5;
			header.style.paddingBottom = 5;
			header.style.paddingLeft = 5;
			header.style.paddingRight = 5;
			header.style.borderTopLeftRadius = 4;
			header.style.borderTopRightRadius = 4;
			
			var toggleButton = new Button(() => ToggleVisualizer())
			{
				text = ShowVisualizer ? "▼" : "▶"
			};
			toggleButton.style.width = 25;
			toggleButton.style.marginRight = 5;
			
			var titleLabel = new Label("UI 레이어 계층 구조 시각화");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.style.flexGrow = 1;
			
			header.Add(toggleButton);
			header.Add(titleLabel);
			section.Add(header);
			
			VisualizerContainer = new VisualElement();
			VisualizerContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
			VisualizerContainer.style.paddingTop = 10;
			VisualizerContainer.style.paddingBottom = 10;
			VisualizerContainer.style.paddingLeft = 5;
			VisualizerContainer.style.paddingRight = 5;
			VisualizerContainer.style.borderBottomLeftRadius = 4;
			VisualizerContainer.style.borderBottomRightRadius = 4;
			VisualizerContainer.style.display = ShowVisualizer ? DisplayStyle.Flex : DisplayStyle.None;
			
			var toolbar = new VisualElement();
			toolbar.style.flexDirection = FlexDirection.Row;
			toolbar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			toolbar.style.paddingTop = 3;
			toolbar.style.paddingBottom = 3;
			toolbar.style.paddingLeft = 5;
			toolbar.style.paddingRight = 5;
			toolbar.style.marginBottom = 10;
			toolbar.style.borderTopLeftRadius = 3;
			toolbar.style.borderTopRightRadius = 3;
			toolbar.style.borderBottomLeftRadius = 3;
			toolbar.style.borderBottomRightRadius = 3;
			
			var spacer = new VisualElement { style = { flexGrow = 1 } };
			toolbar.Add(spacer);
			
			var searchLabel = new Label("검색:") { style = { marginRight = 5, unityTextAlign = TextAnchor.MiddleCenter } };
			toolbar.Add(searchLabel);
			
			var searchField = new ToolbarSearchField { style = { width = 200 }, value = VisualizerSearchFilter };
			searchField.RegisterValueChangedCallback(evt =>
			{
				VisualizerSearchFilter = evt.newValue;
				RefreshVisualizerContent();
			});
			toolbar.Add(searchField);
			VisualizerContainer.Add(toolbar);
			
			var scrollView = new ScrollView { style = { maxHeight = 500, marginTop = 5 } };
			BuildVisualizerLayerHierarchy(scrollView);
			VisualizerContainer.Add(scrollView);
			
			section.Add(VisualizerContainer);
			
			return section;
		}

		protected void ToggleVisualizer()
		{
			ShowVisualizer = !ShowVisualizer;
			EditorPrefs.SetBool(VisualizerPrefsKey, ShowVisualizer);
			
			if (VisualizerContainer != null)
			{
				VisualizerContainer.style.display = ShowVisualizer ? DisplayStyle.Flex : DisplayStyle.None;
				var toggleButton = VisualizerContainer.parent.Q<Button>();
				if (toggleButton != null) toggleButton.text = ShowVisualizer ? "▼" : "▶";
				if (ShowVisualizer) RefreshVisualizerContent();
			}
		}

		protected void RefreshVisualizerContent()
		{
			if (VisualizerContainer == null) return;
			var scrollView = VisualizerContainer.Q<ScrollView>();
			if (scrollView != null)
			{
				scrollView.Clear();
				BuildVisualizerLayerHierarchy(scrollView);
			}
		}

		protected virtual void BuildVisualizerLayerHierarchy(ScrollView scrollView)
		{
			var configs = ScriptableObjectInstance?.Configs;
			if (configs == null || configs.Count == 0)
			{
				scrollView.Add(new HelpBox("UI 설정을 찾을 수 없습니다", HelpBoxMessageType.Info));
				return;
			}
			
			var filteredConfigs = string.IsNullOrEmpty(VisualizerSearchFilter) ? configs : 
				configs.Where(c => c.UiType.Name.ToLower().Contains(VisualizerSearchFilter.ToLower()) || 
				                   c.Address.ToLower().Contains(VisualizerSearchFilter.ToLower())).ToList();
			
			var layerGroups = filteredConfigs.GroupBy(c => c.Layer).OrderBy(g => g.Key).ToList();
			var layersContainer = new VisualElement { style = { marginLeft = 5, marginRight = 5 } };
			
			foreach (var layerGroup in layerGroups)
			{
				layersContainer.Add(CreateVisualizerLayerElement(layerGroup.Key, layerGroup.ToList()));
			}
			
			scrollView.Add(layersContainer);
			scrollView.Add(CreateVisualizerStatistics(filteredConfigs));
		}

		private VisualElement CreateVisualizerLayerElement(int layer, List<UiConfig> configs)
		{
			var container = new VisualElement();
			container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
			container.style.borderTopLeftRadius = 4;
			container.style.borderTopRightRadius = 4;
			container.style.borderBottomLeftRadius = 4;
			container.style.borderBottomRightRadius = 4;
			container.style.marginBottom = 5;
			container.style.paddingTop = 5;
			container.style.paddingBottom = 5;
			
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.backgroundColor = GetVisualizerLayerColor(layer);
			header.style.paddingTop = 5;
			header.style.paddingBottom = 5;
			header.style.paddingLeft = 10;
			header.style.paddingRight = 10;
			header.style.marginBottom = 5;
			header.style.alignItems = Align.Center;
			
			var textContainer = new VisualElement();
			textContainer.style.flexDirection = FlexDirection.Row;
			textContainer.style.backgroundColor = new Color(0, 0, 0, 0.5f);
			textContainer.style.paddingLeft = 8;
			textContainer.style.paddingRight = 8;
			textContainer.style.paddingTop = 3;
			textContainer.style.paddingBottom = 3;
			textContainer.style.borderTopLeftRadius = 3;
			textContainer.style.borderTopRightRadius = 3;
			textContainer.style.borderBottomLeftRadius = 3;
			textContainer.style.borderBottomRightRadius = 3;
			
			var layerLabel = new Label($"레이어 {layer}");
			layerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			layerLabel.style.fontSize = 13;
			layerLabel.style.color = Color.white;
			textContainer.Add(layerLabel);
			
			var countLabel = new Label($"({configs.Count}개 UI)");
			countLabel.style.fontSize = 11;
			countLabel.style.color = Color.white;
			countLabel.style.marginLeft = 5;
			textContainer.Add(countLabel);
			
			header.Add(textContainer);
			container.Add(header);
			
			foreach (var config in configs)
			{
				var item = new VisualElement();
				item.style.flexDirection = FlexDirection.Row;
				item.style.paddingLeft = 20;
				item.style.paddingRight = 10;
				item.style.paddingTop = 2;
				item.style.paddingBottom = 2;
				
				item.Add(new Label(config.UiType.Name) { style = { width = 200 } });
				item.Add(new Label(config.Address) { style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f), flexGrow = 1 } });
				
				if (config.LoadSynchronously)
				{
					item.Add(new Label("[SYNC]") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 50, color = new Color(1, 0.8f, 0) } });
				}
				container.Add(item);
			}
			
			return container;
		}

		private VisualElement CreateVisualizerStatistics(List<UiConfig> configs)
		{
			var container = new VisualElement { style = { marginTop = 10, marginLeft = 5, marginBottom = 10 } };
			container.Add(new Label("통계") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });

			var stats = new VisualElement { style = { marginLeft = 15 } };
			stats.Add(new Label($"전체 UI: {configs.Count}"));
			stats.Add(new Label($"사용 중인 레이어: {configs.Select(c => c.Layer).Distinct().Count()}"));
			stats.Add(new Label($"동기 로드: {configs.Count(c => c.LoadSynchronously)}"));
			stats.Add(new Label($"비동기 로드: {configs.Count(c => !c.LoadSynchronously)}"));
			
			container.Add(stats);
			return container;
		}

		private Color GetVisualizerLayerColor(int layer)
		{
			if (layer < 0) return new Color(0.8f, 0.2f, 0.2f);
			if (layer == 0) return new Color(0.3f, 0.3f, 0.3f);
			var hue = (layer * 0.1f) % 1f;
			return Color.HSVToRGB(hue, 0.5f, 0.9f);
		}
	}
}

