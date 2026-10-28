using Geuneda.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Geuneda.UiService.Rendering
{
	/// <summary>
	/// 이 피처를 넣은 Universal Renderer 에셋을 쓰는 모든 카메라에, <see cref="UiBackdropBlurPresenterFeature"/> 프레젠터가
	/// 하나라도 열려 있는 동안 흐린 배경을 그립니다.
	/// </summary>
	/// <remarks>
	/// 흐림의 모양(다운샘플, 반복 횟수, 퍼짐, 틴트)은 프로젝트 전체의 아트 결정 하나라 프레젠터가 아니라 여기서 정합니다.
	/// 셰이더는 <c>Resources.Load</c> 나 <c>Shader.Find</c> 가 아니라 직렬화 참조로 두어 Graphics Settings 를 거쳐
	/// 실제 빌드 의존성으로 들어가고 변형이 제대로 컴파일됩니다(그렇지 않으면 always-included 가 아닌 한 플레이어에서 분홍색).
	/// </remarks>
	public class UiBackdropBlurRendererFeature : ScriptableRendererFeature
	{
		/// <summary>
		/// <see cref="IterationsOverride"/> 의 하한. 인스펙터 필드의 범위와 같습니다
		/// </summary>
		public const int MinIterations = 1;

		/// <summary>
		/// <see cref="IterationsOverride"/> 의 상한. 인스펙터 필드의 범위와 같습니다
		/// </summary>
		public const int MaxIterations = 8;

		private const string ShaderPath = "Packages/com.geuneda.uiservice/Runtime/Rendering/Shaders/UiBackdropBlur.shader";

		private static readonly ModuleLogger Logger = new ModuleLogger("Geuneda.UiService.Rendering");

		// 이 에셋은 Renderer 가 소유해 게임 코드에서 닿을 참조 경로가 없으므로 설치 여부와 런타임 재정의를 정적으로 둔다
		private static bool _isInstalled;
		private static int _iterationsOverride;
		private static int _authoredIterations = 2;

		[SerializeField] private Shader _blurShader;
		[SerializeField, Range(0, 3)] private int _downsample = 1;
		[SerializeField, Range(1, 8)] private int _iterations = 2;
		[SerializeField, Min(0f)] private float _spread = 1f;
		[SerializeField] private Color _tint = Color.white;

		private Material _blurMaterial;
		private UiBackdropBlurPass _pass;

		/// <summary>
		/// 로드된 Renderer 에셋에 이 피처가 만들어진 뒤 true. 프레젠터가 피처 없음을 경고하는 데 씁니다
		/// </summary>
		internal static bool IsInstalled => _isInstalled;

		/// <summary>
		/// 인스펙터에 정한 반복 횟수를 런타임에 덮어씁니다(품질 단계, 접근성 설정, 튜닝 UI).
		/// 0 이하는 덮어쓰기를 지웁니다. [<see cref="MinIterations"/>, <see cref="MaxIterations"/>] 로 자릅니다
		/// </summary>
		/// <remarks>
		/// 직렬화 필드에 쓰지 않습니다. 이 피처는 ScriptableObject 에셋이라 런타임에 필드를 바꾸면 에디터에서는 디스크에 저장돼
		/// 프로젝트 전체의 모양이 바뀝니다. 에셋 인스턴스는 Renderer 가 소유해 게임 코드에서 닿을 참조 경로가 없으므로 정적입니다.
		/// </remarks>
		public static int IterationsOverride
		{
			get => _iterationsOverride;
			set => _iterationsOverride = value <= 0 ? 0 : Mathf.Clamp(value, MinIterations, MaxIterations);
		}

		/// <summary>
		/// 에셋에 정한 반복 횟수. 튜닝 UI 가 여기서부터 조절할 수 있게 읽기만 됩니다
		/// </summary>
		public static int AuthoredIterations => _authoredIterations;

		/// <summary>
		/// 지금 적용되는 반복 횟수(덮어쓰기 포함)
		/// </summary>
		public static int EffectiveIterations => _iterationsOverride > 0 ? _iterationsOverride : _authoredIterations;

		/// <summary>
		/// 작업 텍스처의 다운샘플 단계(2 의 거듭제곱). 높을수록 싸고 더 흐립니다
		/// </summary>
		internal int Downsample => _downsample;

		/// <summary>
		/// 이번 프레임에 실제로 쓰는 흐림 패스 수. 런타임 덮어쓰기가 우선합니다
		/// </summary>
		internal int Iterations => _iterationsOverride > 0 ? _iterationsOverride : _iterations;

		/// <summary>
		/// 흐림 패스마다 샘플을 벌리는 간격(픽셀)
		/// </summary>
		internal float Spread => _spread;

		/// <summary>
		/// 흐린 결과에 곱하는 색
		/// </summary>
		internal Color Tint => _tint;

		/// <summary>
		/// 테스트가 만들어진 재질을 확인하는 접근자. 제품 코드에서는 쓰지 않습니다
		/// </summary>
		internal Material BlurMaterialInternal => _blurMaterial;

		/// <inheritdoc />
		public override void Create()
		{
			TryLoadShaderInEditor();

			if (_blurShader == null)
			{
				Logger.Error($"{nameof(UiBackdropBlurRendererFeature)} '{name}': no blur shader assigned. " +
				             "Assign Shaders/UiBackdropBlur.shader in the Inspector.", this);
				return;
			}

			CoreUtils.Destroy(_blurMaterial);
			_blurMaterial = CoreUtils.CreateEngineMaterial(_blurShader);
			_pass = new UiBackdropBlurPass(_blurMaterial, this)
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing
			};
			_isInstalled = true;
			_authoredIterations = _iterations;
		}

		/// <inheritdoc />
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (_pass == null || _blurMaterial == null)
			{
				return;
			}

			renderer.EnqueuePass(_pass);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			// 피처 에셋은 플레이 모드보다 오래 살아 재질을 지우지 않으면 샌다
			CoreUtils.Destroy(_blurMaterial);
			_blurMaterial = null;
		}

		// 도메인 리로드를 끄면 정적 상태가 플레이 세션 사이에 남는다
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_iterationsOverride = 0;
		}

		private void Reset()
		{
			TryLoadShaderInEditor();
		}

		private void TryLoadShaderInEditor()
		{
#if UNITY_EDITOR
			if (_blurShader == null)
			{
				_blurShader = UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
			}
#endif
		}
	}
}
