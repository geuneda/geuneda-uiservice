using System;
using System.Collections.Generic;
using Geuneda.Core;
using Geuneda.UiService.Views;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 입력 차단 토큰을 세고, 막힌 동안 모든 UI 위에 투명한 차단막을 켭니다.
	/// uGUI 차단막은 EventSystem 으로 입력을 받는 uGUI 와 UI Toolkit 을 모두 막습니다.
	/// EventSystem 없이 입력을 받는 UI Toolkit 을 위해, UIDocument 프레젠터가 처음 추가되면 UI Toolkit 차단 패널도 만듭니다.
	/// </summary>
	internal sealed class UiInputBlocker
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.InputBlocker");

		// 레이어 권장 범위(0~1000)보다 훨씬 위라 어떤 프레젠터보다 먼저 입력을 받습니다
		internal const int SortingOrder = 32767;

		private readonly List<int> _ids = new List<int>();
		private readonly List<string> _reasons = new List<string>();
		private readonly Action<int> _release;
		private readonly Action<bool> _blockedChanged;
		private int _lastId;
		private Transform _parent;
		private GameObject _canvasBlocker;
		private GameObject _toolkitObject;
		private VisualElement _toolkitBlocker;
		private PanelSettings _toolkitPanel;
		private ThemeStyleSheet _toolkitTheme;

		/// <param name="blockedChanged">막힘 상태가 바뀔 때(첫 토큰, 마지막 해제) 부를 함수</param>
		public UiInputBlocker(Action<bool> blockedChanged)
		{
			_release = Release;
			_blockedChanged = blockedChanged;
			Reasons = _reasons.AsReadOnly();
		}

		public bool IsBlocked => _ids.Count > 0;

		public IReadOnlyList<string> Reasons { get; }

		/// <summary>
		/// 차단막을 둘 부모를 정하고 uGUI 차단막을 만듭니다. 서비스 Init 에서 한 번 부릅니다.
		/// </summary>
		public void Attach(Transform parent)
		{
			_parent = parent;
			_canvasBlocker = new GameObject("UiInputBlocker");
			_canvasBlocker.transform.SetParent(parent, false);

			var canvas = _canvasBlocker.AddComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			canvas.sortingOrder = SortingOrder;
			_canvasBlocker.AddComponent<GraphicRaycaster>();
			// 그리지 않고 레이캐스트만 받습니다
			_canvasBlocker.AddComponent<NonDrawingView>();
			_canvasBlocker.SetActive(IsBlocked);
		}

		/// <summary>
		/// UI Toolkit 차단 패널이 없으면 만듭니다. UIDocument 프레젠터가 추가될 때 부릅니다.
		/// </summary>
		public void EnsureToolkitBlocker()
		{
			if (_toolkitBlocker != null || _parent == null)
			{
				return;
			}

			// 테마가 없는 PanelSettings 는 경고를 남기므로 빈 테마를 넣습니다. 차단막은 아무것도 그리지 않습니다
			_toolkitTheme = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			_toolkitPanel = ScriptableObject.CreateInstance<PanelSettings>();
			_toolkitPanel.name = "UiInputBlockerPanel";
			_toolkitPanel.themeStyleSheet = _toolkitTheme;
			_toolkitPanel.sortingOrder = SortingOrder;

			// 켜진 오브젝트에 붙인 뒤 PanelSettings 를 넣으면 플레이어 빌드에서 루트가 생기지 않으므로 끈 채로 설정하고 켭니다
			_toolkitObject = new GameObject("UiInputBlockerPanel");
			_toolkitObject.SetActive(false);
			_toolkitObject.transform.SetParent(_parent, false);
			var document = _toolkitObject.AddComponent<UIDocument>();
			document.panelSettings = _toolkitPanel;
			_toolkitObject.SetActive(true);

			// 게임 오브젝트를 끄면 UIDocument 가 루트를 다시 만들므로 켜 둔 채 display 로 켜고 끕니다
			_toolkitBlocker = document.rootVisualElement;
			UiStyleUtils.FillParent(_toolkitBlocker);
			_toolkitBlocker.pickingMode = PickingMode.Position;
			ApplyToolkit();
		}

		public UiInputBlock Block(string reason)
		{
			if (reason == null)
			{
				throw new ArgumentNullException(nameof(reason));
			}

			// 0 은 default 토큰과 겹치므로 건너뜁니다. 아래 콜백이 다시 막을 수 있으므로 이번 ID 를 지역 변수로 고정합니다
			_lastId = _lastId == int.MaxValue ? 1 : _lastId + 1;
			var id = _lastId;
			_ids.Add(id);
			_reasons.Add(reason);

			if (_ids.Count == 1)
			{
				Apply();
				_blockedChanged?.Invoke(true);
			}

			return new UiInputBlock(_release, id);
		}

		/// <summary>
		/// 만든 차단막과 런타임 에셋을 파괴합니다. 남은 토큰은 계속 셀 수 있지만 화면에는 반영하지 않습니다.
		/// </summary>
		public void Destroy()
		{
			if (_canvasBlocker != null)
			{
				Object.Destroy(_canvasBlocker);
			}

			if (_toolkitObject != null)
			{
				Object.Destroy(_toolkitObject);
			}

			if (_toolkitPanel != null)
			{
				Object.Destroy(_toolkitPanel);
			}

			if (_toolkitTheme != null)
			{
				Object.Destroy(_toolkitTheme);
			}

			_canvasBlocker = null;
			_toolkitObject = null;
			_toolkitBlocker = null;
			_toolkitPanel = null;
			_toolkitTheme = null;
			_parent = null;
		}

		private void Release(int id)
		{
			var index = _ids.IndexOf(id);
			if (index < 0)
			{
				Logger.Warning("이미 풀린 입력 차단 토큰을 다시 풀었습니다. 같은 토큰을 두 번 Dispose 하지 않았는지 확인하세요.");
				return;
			}

			_ids.RemoveAt(index);
			_reasons.RemoveAt(index);

			if (_ids.Count == 0)
			{
				Apply();
				_blockedChanged?.Invoke(false);
			}
		}

		private void Apply()
		{
			if (_canvasBlocker != null)
			{
				_canvasBlocker.SetActive(IsBlocked);
			}

			ApplyToolkit();
		}

		private void ApplyToolkit()
		{
			if (_toolkitBlocker != null)
			{
				_toolkitBlocker.style.display = IsBlocked ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}
	}
}
