using System;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// 뒤로가기와 모달 동작을 바꿔 가며 시험하는 팝업 프레젠터.
	/// 기본값은 뒤로가기 대상이자 모달입니다.
	/// </summary>
	public class TestPopupPresenter : UiPresenter
	{
		public bool BackEnabled { get; set; } = true;
		public bool Modal { get; set; } = true;
		public bool ConsumeBackWithoutClosing { get; set; }
		public bool ThrowOnIsModal { get; set; }
		public int BackPressedCount { get; private set; }
		public int DimClickedCount { get; private set; }
		public Action OpenedCallback { get; set; }
		public Action ClosedCallback { get; set; }

		protected override bool HandlesBack => BackEnabled;

		protected override bool IsModal => ThrowOnIsModal ? throw new System.InvalidOperationException("modal boom") : Modal;

		protected override void OnBackPressed()
		{
			BackPressedCount++;

			if (!ConsumeBackWithoutClosing)
			{
				base.OnBackPressed();
			}
		}

		protected override void OnDimClicked()
		{
			DimClickedCount++;
		}

		protected override void OnOpened()
		{
			OpenedCallback?.Invoke();
		}

		protected override void OnClosed()
		{
			ClosedCallback?.Invoke();
		}
	}
}
