using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// Escape 키(Android 뒤로가기 포함)를 읽어 <see cref="IUiService.HandleBack"/> 를 부릅니다.
	/// <see cref="UiService.ReadsBackInput"/> 가 켜져 있으면 서비스가 UI 루트에 붙입니다.
	/// Input System 패키지가 있고 활성화돼 있으면 그것을, 아니면 레거시 Input Manager 를 씁니다.
	/// </summary>
	internal class UiBackInputReader : MonoBehaviour
	{
#if GEUNEDA_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
		// Keyboard.current 의 키를 읽으면, Android 뒤로가기가 다른 키보드 장치로 한 프레임 안에 눌렀다 떼어질 때 놓칩니다.
		// 액션은 모든 키보드의 이벤트를 하나씩 처리하므로 이 경우도 잡습니다
		private UnityEngine.InputSystem.InputAction _backAction;
#endif

		public IUiService Service { get; set; }

#if GEUNEDA_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
		private void OnEnable()
		{
			_backAction = new UnityEngine.InputSystem.InputAction("UiBack", UnityEngine.InputSystem.InputActionType.Button, "<Keyboard>/escape");
			_backAction.Enable();
		}

		private void OnDisable()
		{
			_backAction.Dispose();
			_backAction = null;
		}
#endif

		private void Update()
		{
			if (Service != null && WasBackPressedThisFrame())
			{
				Service.HandleBack();
			}
		}

		private bool WasBackPressedThisFrame()
		{
#if GEUNEDA_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
			return _backAction.WasPressedThisFrame();
#elif ENABLE_LEGACY_INPUT_MANAGER
			return Input.GetKeyDown(KeyCode.Escape);
#else
			return false;
#endif
		}
	}
}
