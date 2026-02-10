using System;
using System.Collections.Generic;
using System.Linq;
using Geuneda.UiService;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

// ReSharper disable once CheckNamespace

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// <see cref="PrefabRegistryUiConfigs"/>와 동기화하는 <see cref="UiConfigsEditorBase{TSet}"/>의 구현입니다.
	/// </summary>
	public abstract class PrefabRegistryUiConfigsEditor<TSet> : UiConfigsEditorBase<TSet>
		where TSet : Enum
	{
		private Dictionary<string, string> _assetPathLookup;
		private List<string> _uiConfigsAddress;
		private Dictionary<string, Type> _uiTypesByAddress;
		private SerializedProperty _prefabEntriesProperty;
		private ListView _prefabListView;

		protected override void OnEnable()
		{
			// SyncConfigs()가 base.OnEnable()에서 호출되고 이 프로퍼티가 필요하므로
			// base.OnEnable() 전에 _prefabEntriesProperty를 초기화해야 합니다
			_prefabEntriesProperty = serializedObject.FindProperty("_prefabEntries");
			base.OnEnable();
		}

		protected override void SyncConfigs()
		{
			var configs = new List<UiConfig>();
			_uiConfigsAddress = new List<string>();
			_assetPathLookup = new Dictionary<string, string>();
			var existingConfigs = ScriptableObjectInstance.Configs;
			var prefabConfigs = ScriptableObjectInstance as PrefabRegistryUiConfigs;

			if (prefabConfigs == null) return;

			// 첫 번째 패스: 프리팹 이름으로부터 주소를 업데이트합니다
			UpdateAddressesFromPrefabs();

			foreach (var entry in prefabConfigs.PrefabEntries)
			{
				if (entry.Prefab == null) continue;

				var uiPresenter = entry.Prefab.GetComponent<UiPresenter>();
				if (uiPresenter == null) continue;

				var sortingOrder = GetSortingOrder(uiPresenter);
				var existingConfigIndex = existingConfigs.FindIndex(c => c.Address == entry.Address);
				var presenterType = uiPresenter.GetType();

				var config = new UiConfig
				{
					Address = entry.Address,
					Layer = existingConfigIndex >= 0 && sortingOrder < 0 ? existingConfigs[existingConfigIndex].Layer : 
					        sortingOrder < 0 ? 0 : sortingOrder,
					UiType = presenterType,
					LoadSynchronously = Attribute.IsDefined(presenterType, typeof(LoadSynchronouslyAttribute))
				};

				configs.Add(config);
				_uiConfigsAddress.Add(entry.Address);
				_assetPathLookup[entry.Address] = AssetDatabase.GetAssetPath(entry.Prefab);
			}

			ScriptableObjectInstance.Configs = configs;
			
			_uiTypesByAddress = new Dictionary<string, Type>();
			foreach (var config in configs)
			{
				if (!string.IsNullOrEmpty(config.Address) && config.UiType != null)
				{
					_uiTypesByAddress[config.Address] = config.UiType;
				}
			}

			EditorUtility.SetDirty(ScriptableObjectInstance);
		}

		/// <summary>
		/// 프리팹 항목의 주소를 프리팹 이름과 일치하도록 업데이트합니다.
		/// 주소가 항상 프리팹 이름에서 파생되도록 SyncConfigs 중에 호출됩니다.
		/// </summary>
		private void UpdateAddressesFromPrefabs()
		{
			if (_prefabEntriesProperty == null) return;
			
			serializedObject.Update();
			var modified = false;

			for (int i = 0; i < _prefabEntriesProperty.arraySize; i++)
			{
				var itemProperty = _prefabEntriesProperty.GetArrayElementAtIndex(i);
				var addressProperty = itemProperty.FindPropertyRelative("Address");
				var prefabProperty = itemProperty.FindPropertyRelative("Prefab");

				var prefab = prefabProperty.objectReferenceValue as GameObject;
				if (prefab != null && addressProperty.stringValue != prefab.name)
				{
					addressProperty.stringValue = prefab.name;
					modified = true;
				}
			}

			if (modified)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		/// <summary>
		/// 여러 프리팹을 한 번에 레지스트리에 추가합니다.
		/// 각 프리팹에 UiPresenter 컴포넌트가 있는지 검증하고 중복을 방지합니다.
		/// </summary>
		/// <param name="prefabs">추가할 프리팹들</param>
		private void AddPrefabs(IEnumerable<GameObject> prefabs)
		{
			serializedObject.Update();
			var existingPrefabs = new HashSet<GameObject>();

			// 중복 방지를 위해 기존 프리팹을 수집합니다
			for (int i = 0; i < _prefabEntriesProperty.arraySize; i++)
			{
				var itemProperty = _prefabEntriesProperty.GetArrayElementAtIndex(i);
				var prefabProperty = itemProperty.FindPropertyRelative("Prefab");
				if (prefabProperty.objectReferenceValue is GameObject existingPrefab)
				{
					existingPrefabs.Add(existingPrefab);
				}
			}

			var addedCount = 0;
			var skippedNoPresenter = 0;
			var skippedDuplicate = 0;

			foreach (var prefab in prefabs)
			{
				if (prefab == null) continue;

				// 중복 건너뛰기
				if (existingPrefabs.Contains(prefab))
				{
					skippedDuplicate++;
					continue;
				}

				// 프리팹에 UiPresenter 컴포넌트가 있는지 검증합니다
				if (prefab.GetComponent<UiPresenter>() == null)
				{
					skippedNoPresenter++;
					continue;
				}

				// 새 항목 추가
				var newIndex = _prefabEntriesProperty.arraySize;
				_prefabEntriesProperty.InsertArrayElementAtIndex(newIndex);
				var newItem = _prefabEntriesProperty.GetArrayElementAtIndex(newIndex);
				newItem.FindPropertyRelative("Address").stringValue = prefab.name;
				newItem.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
				
				existingPrefabs.Add(prefab);
				addedCount++;
			}

			serializedObject.ApplyModifiedProperties();
			SyncConfigs();
			_prefabListView?.RefreshItems();

			// 건너뛴 항목이 있으면 피드백을 표시합니다
			if (skippedNoPresenter > 0 || skippedDuplicate > 0)
			{
				var message = $"Added {addedCount} prefab(s).";
				if (skippedDuplicate > 0) message += $" Skipped {skippedDuplicate} duplicate(s).";
				if (skippedNoPresenter > 0) message += $" Skipped {skippedNoPresenter} without UiPresenter.";
				Debug.Log($"[PrefabRegistryUiConfigs] {message}");
			}
		}

		public override VisualElement CreateInspectorGUI()
		{
			var root = base.CreateInspectorGUI();

			// 섹션 2 상단에 프리팹 항목 섹션을 추가합니다 (구성 목록 앞)
			var prefabSection = new VisualElement { style = { marginBottom = 10 } };
			var helpBox = new HelpBox(
				"Drag and drop UI Prefabs below. Addresses are automatically derived from prefab names. " +
				"Only prefabs with a UiPresenter component are accepted.", 
				HelpBoxMessageType.Info);
			helpBox.style.marginBottom = 5;
			prefabSection.Add(helpBox);

			// 일괄 프리팹 드롭을 위한 드롭 영역을 생성합니다
			var dropZone = CreateDropZone();
			prefabSection.Add(dropZone);

			_prefabListView = new ListView
			{
				showBorder = true,
				showAddRemoveFooter = true,
				reorderable = true,
				headerTitle = "Embedded Prefab Registry",
				showFoldoutHeader = true,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 24
			};

			_prefabListView.BindProperty(_prefabEntriesProperty);
			_prefabListView.makeItem = CreateEntryElement;
			_prefabListView.bindItem = (element, index) => BindEntryElement(element, index, _prefabEntriesProperty);
			
			_prefabListView.itemsAdded += _ => SyncConfigs();
			_prefabListView.itemsRemoved += _ => SyncConfigs();
			_prefabListView.itemIndexChanged += (_, _) => SyncConfigs();

			// 시각화 섹션 뒤에 삽입합니다 (root에서 인덱스 1)
			root.Insert(1, prefabSection);
			root.Insert(2, _prefabListView);

			return root;
		}

		/// <summary>
		/// 일괄 프리팹 드롭을 위한 드롭 영역을 생성합니다.
		/// </summary>
		private VisualElement CreateDropZone()
		{
			var dropZone = new VisualElement();
			dropZone.style.height = 50;
			dropZone.style.marginBottom = 10;
			dropZone.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
			dropZone.style.borderTopLeftRadius = 4;
			dropZone.style.borderTopRightRadius = 4;
			dropZone.style.borderBottomLeftRadius = 4;
			dropZone.style.borderBottomRightRadius = 4;
			dropZone.style.borderTopWidth = 2;
			dropZone.style.borderBottomWidth = 2;
			dropZone.style.borderLeftWidth = 2;
			dropZone.style.borderRightWidth = 2;
			dropZone.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.justifyContent = Justify.Center;
			dropZone.style.alignItems = Align.Center;

			var dropLabel = new Label("Drop Prefabs Here (supports multiple)");
			dropLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
			dropLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
			dropZone.Add(dropLabel);

			// 드래그 앤 드롭 콜백을 등록합니다
			dropZone.RegisterCallback<DragEnterEvent>(evt =>
			{
				if (HasValidPrefabsInDrag())
				{
					dropZone.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f, 0.5f);
					dropZone.style.borderTopColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropZone.style.borderBottomColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropZone.style.borderLeftColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropZone.style.borderRightColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropLabel.text = "Release to add prefabs";
					dropLabel.style.color = new Color(0.8f, 1f, 0.8f);
				}
			});

			dropZone.RegisterCallback<DragLeaveEvent>(evt =>
			{
				ResetDropZoneStyle(dropZone, dropLabel);
			});

			dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
			{
				if (HasValidPrefabsInDrag())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				}
			});

			dropZone.RegisterCallback<DragPerformEvent>(evt =>
			{
				var prefabs = GetPrefabsFromDrag();
				if (prefabs.Count > 0)
				{
					AddPrefabs(prefabs);
				}
				ResetDropZoneStyle(dropZone, dropLabel);
			});

			return dropZone;
		}

		private void ResetDropZoneStyle(VisualElement dropZone, Label dropLabel)
		{
			dropZone.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
			dropZone.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropLabel.text = "Drop Prefabs Here (supports multiple)";
			dropLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
		}

		private bool HasValidPrefabsInDrag()
		{
			return DragAndDrop.objectReferences.Any(obj => obj is GameObject);
		}

		private List<GameObject> GetPrefabsFromDrag()
		{
			var prefabs = new List<GameObject>();
			foreach (var obj in DragAndDrop.objectReferences)
			{
				if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
				{
					prefabs.Add(go);
				}
			}
			return prefabs;
		}

		private VisualElement CreateEntryElement()
		{
			var container = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 2, paddingTop = 2 } };
			
			// 주소를 표시하는 읽기 전용 레이블 (프리팹 이름에서 파생됨)
			var addressLabel = new Label 
			{ 
				name = "address-label", 
				style = 
				{ 
					flexGrow = 1, 
					marginRight = 5,
					unityTextAlign = TextAnchor.MiddleLeft,
					color = new Color(0.6f, 0.6f, 0.6f),
					fontSize = 11,
					paddingLeft = 3
				} 
			};
			container.Add(addressLabel);
			
			var prefabField = new ObjectField { name = "prefab-field", objectType = typeof(GameObject), style = { flexGrow = 2 } };
			container.Add(prefabField);
			
			return container;
		}

		private void BindEntryElement(VisualElement element, int index, SerializedProperty entriesProperty)
		{
			if (index >= entriesProperty.arraySize) return;

			var itemProperty = entriesProperty.GetArrayElementAtIndex(index);
			var addressProperty = itemProperty.FindPropertyRelative("Address");
			var prefabProperty = itemProperty.FindPropertyRelative("Prefab");

			var addressLabel = element.Q<Label>("address-label");
			var prefabField = element.Q<ObjectField>("prefab-field");

			// 주소를 읽기 전용 레이블로 표시합니다
			var prefab = prefabProperty.objectReferenceValue as GameObject;
			addressLabel.text = prefab != null ? $"[{prefab.name}]" : "[No Prefab]";

			prefabField.Unbind();
			prefabField.BindProperty(prefabProperty);

			prefabField.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue is GameObject newPrefab)
				{
					// 프리팹 이름에서 자동으로 주소를 업데이트합니다
					addressProperty.stringValue = newPrefab.name;
					addressProperty.serializedObject.ApplyModifiedProperties();
					addressLabel.text = $"[{newPrefab.name}]";
				}
				else
				{
					addressLabel.text = "[No Prefab]";
				}
				SyncConfigs();
			});
		}

		protected override IReadOnlyList<string> GetAddressList() => _uiConfigsAddress ?? new List<string>();

		protected override Dictionary<string, string> GetAssetPathLookup() => _assetPathLookup;

		protected override Dictionary<string, Type> GetUiTypesByAddress() => _uiTypesByAddress;

		protected override VisualElement CreateConfigElement()
		{
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.alignItems = Align.Center;

			container.Add(new Label { style = { flexGrow = 1, paddingLeft = 5, unityTextAlign = TextAnchor.MiddleLeft } });
			container.Add(new IntegerField { style = { width = 80, marginRight = 5 } });

			return container;
		}

		protected override void BindConfigElement(VisualElement element, int index)
		{
			if (index >= ConfigsProperty.arraySize) return;

			var itemProperty = ConfigsProperty.GetArrayElementAtIndex(index);
			var addressProperty = itemProperty.FindPropertyRelative(nameof(UiConfigs.UiConfigSerializable.Address));
			var layerProperty = itemProperty.FindPropertyRelative(nameof(UiConfigs.UiConfigSerializable.Layer));

			var label = element.Q<Label>();
			var layerField = element.Q<IntegerField>();

			label.text = addressProperty.stringValue;
			layerField.Unbind();
			layerField.BindProperty(layerProperty);
			layerField.userData = addressProperty.stringValue;
			layerField.RegisterValueChangedCallback(OnLayerChanged);
		}

		private int GetSortingOrder(UiPresenter presenter)
		{
			if (presenter.TryGetComponent<Canvas>(out var canvas)) return canvas.sortingOrder;
			if (presenter.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document)) return (int)document.sortingOrder;
			return -1;
		}
	}
}

