using UnityEngine;

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="UiPresenter"/>에 부착하여 기능을 확장할 수 있는 프레젠터 기능의 기본 클래스입니다.
	/// 프레젠터 생명주기에 연결되는 커스텀 기능을 만들려면 이 클래스를 상속하세요.
	/// 열기/닫기 전환 딜레이를 제공하는 기능의 경우 <see cref="ITransitionFeature"/>를 구현하세요.
	/// </summary>
	public abstract class PresenterFeatureBase : MonoBehaviour
	{
		/// <summary>
		/// 이 기능을 소유하는 프레젠터입니다
		/// </summary>
		protected UiPresenter Presenter { get; private set; }

		/// <summary>
		/// 프레젠터가 초기화될 때 호출됩니다. 프레젠터가 생성될 때 한 번만 호출됩니다.
		/// </summary>
		/// <param name="presenter">이 기능을 소유하는 프레젠터</param>
		public virtual void OnPresenterInitialized(UiPresenter presenter)
		{
			Presenter = presenter;
		}

		/// <summary>
		/// 프레젠터가 열리고 있을 때 호출됩니다. 프레젠터가 표시되기 전에 호출됩니다.
		/// </summary>
		public virtual void OnPresenterOpening() { }

		/// <summary>
		/// 프레젠터가 열린 후 현재 표시되고 있을 때 호출됩니다.
		/// </summary>
		public virtual void OnPresenterOpened() { }

		/// <summary>
		/// 프레젠터가 닫히고 있을 때 호출됩니다. 프레젠터가 숨겨지기 전에 호출됩니다.
		/// </summary>
		public virtual void OnPresenterClosing() { }

		/// <summary>
		/// 프레젠터가 닫힌 후 더 이상 표시되지 않을 때 호출됩니다.
		/// </summary>
		public virtual void OnPresenterClosed() { }
	}
}

