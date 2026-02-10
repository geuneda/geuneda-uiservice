using Geuneda.UiService;
using UnityEditor;

namespace GeunedaEditor.UiService
{
	/// <summary>
	/// 기본 제공 사용을 위한 기본 UI 세트 식별자입니다.
	/// 사용자는 자체 enum과 커스텀 에디터를 만들어 이 기본값을 재정의할 수 있습니다.
	/// </summary>
	public enum DefaultUiSetId
	{
		InitialLoading = 0,
		MainMenu = 1,
		Gameplay = 2,
		Settings = 3,
		Overlays = 4,
		Popups = 5
	}

	/// <summary>
	/// Addressables 기반 로딩을 위한 UiConfigs 에디터의 기본 구현입니다.
	/// 사용자 구현 없이 라이브러리가 바로 작동하도록 합니다.
	/// 사용자는 <see cref="AddressablesUiConfigs"/>에 대한 자체 CustomEditor 구현을 만들어 재정의할 수 있습니다.
	/// </summary>
	[CustomEditor(typeof(AddressablesUiConfigs))]
	public class DefaultAddressablesUiConfigsEditor : AddressablesUiConfigsEditor<DefaultUiSetId>
	{
		// 추가 구현 불필요 - 기본적으로 Addressables 로더 기능을 사용합니다
	}

	/// <summary>
	/// Resources 폴더 기반 로딩을 위한 UiConfigs 에디터의 기본 구현입니다.
	/// 사용자 구현 없이 라이브러리가 바로 작동하도록 합니다.
	/// 사용자는 <see cref="ResourcesUiConfigs"/>에 대한 자체 CustomEditor 구현을 만들어 재정의할 수 있습니다.
	/// </summary>
	[CustomEditor(typeof(ResourcesUiConfigs))]
	public class DefaultResourcesUiConfigsEditor : ResourcesUiConfigsEditor<DefaultUiSetId>
	{
		// 추가 구현 불필요 - 기본적으로 Resources 로더 기능을 사용합니다
	}

	/// <summary>
	/// PrefabRegistry 기반 로딩을 위한 UiConfigs 에디터의 기본 구현입니다.
	/// 사용자 구현 없이 라이브러리가 바로 작동하도록 합니다.
	/// 사용자는 <see cref="PrefabRegistryUiConfigs"/>에 대한 자체 CustomEditor 구현을 만들어 재정의할 수 있습니다.
	/// </summary>
	[CustomEditor(typeof(PrefabRegistryUiConfigs))]
	public class DefaultPrefabRegistryUiConfigsEditor : PrefabRegistryUiConfigsEditor<DefaultUiSetId>
	{
		// 추가 구현 불필요 - 기본적으로 PrefabRegistry 로더 기능을 사용합니다
	}
}
