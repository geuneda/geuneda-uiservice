using Geuneda.Core;
using UnityEngine;

namespace Geuneda.UiService.Rendering
{
	/// <summary>
	/// 이 프레젠터가 보이는 동안 뒤 배경을 흐리게 유지합니다. 흐림의 모양은 프레젠터가 아니라
	/// URP Renderer 에셋의 <see cref="UiBackdropBlurRendererFeature"/> 에서 정합니다.
	/// </summary>
	public class UiBackdropBlurPresenterFeature : PresenterFeatureBase
	{
		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.Rendering");

		// 렌더러 피처는 Renderer 에셋이 소유해 프레젠터에서 닿을 경로가 없으므로, 열린 프레젠터 수를 정적으로 센다
		private static int _openCount;

		private bool _counted;

		/// <summary>
		/// 흐린 배경을 열어 둔 프레젠터가 하나라도 있으면 true
		/// </summary>
		internal static bool AnyOpen => _openCount > 0;

		private void OnDisable()
		{
			// OnPresenterClosed 가 아니다. 그것은 닫기 전환이 끝나고 SetActive(false) 가 되기 전에 불려서,
			// 프레젠터가 아직 보이는데 흐림이 먼저 꺼진다
			if (!_counted)
			{
				return;
			}

			_openCount--;
			_counted = false;
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();

			if (_counted)
			{
				return;
			}

			if (!UiBackdropBlurRendererFeature.IsInstalled)
			{
				Logger.Error($"{nameof(UiBackdropBlurPresenterFeature)} on '{name}': no {nameof(UiBackdropBlurRendererFeature)} was found " +
				             "on the active URP Renderer asset, so nothing will render the blur. Select your Universal Renderer asset " +
				             "(not the Pipeline asset) and use Add Renderer Feature.", this);
			}

			_openCount++;
			_counted = true;
		}

		// 도메인 리로드를 끄면 정적 상태가 플레이 세션 사이에 남는다
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_openCount = 0;
		}
	}
}
