using Cysharp.Threading.Tasks;

namespace Geuneda.UiService
{
	/// <summary>
	/// 열기/닫기 전환 딜레이를 제공하는 프레젠터 기능을 위한 인터페이스입니다.
	/// 이 인터페이스를 구현하는 기능은 프레젠터가 전환을 조율하기 위해 대기할 수 있습니다.
	/// </summary>
	public interface ITransitionFeature
	{
		/// <summary>
		/// 열기 전환이 완료되면 완료되는 태스크입니다.
		/// 활성 열기 전환이 없으면 <see cref="UniTask.CompletedTask"/>를 반환합니다.
		/// </summary>
		UniTask OpenTransitionTask { get; }

		/// <summary>
		/// 닫기 전환이 완료되면 완료되는 태스크입니다.
		/// 활성 닫기 전환이 없으면 <see cref="UniTask.CompletedTask"/>를 반환합니다.
		/// </summary>
		UniTask CloseTransitionTask { get; }
	}
}

