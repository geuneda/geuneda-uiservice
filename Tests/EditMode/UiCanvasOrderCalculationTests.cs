using NUnit.Framework;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// uGUI 루트 Canvas 의 정렬값('레이어 + 연 순위', 다음 설정 레이어와 Canvas 범위 안) 계산 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiCanvasOrderCalculationTests
	{
		[Test]
		public void GetCanvasOrderLimit_IsJustBelowNextConfiguredLayer_OrBelowInputBlocker()
		{
			var service = CreateService(0, 10, 20);

			Assert.That(service.GetCanvasOrderLimit(0), Is.EqualTo(9));
			Assert.That(service.GetCanvasOrderLimit(5), Is.EqualTo(9));
			Assert.That(service.GetCanvasOrderLimit(10), Is.EqualTo(19));
			Assert.That(service.GetCanvasOrderLimit(-5), Is.EqualTo(-1));
			Assert.That(service.GetCanvasOrderLimit(20), Is.EqualTo(32766));
			Assert.That(service.GetCanvasOrderLimit(int.MaxValue), Is.EqualTo(32766));
		}

		[Test]
		public void GetCanvasOrderLimit_DoesNotGoBelowCanvasRange()
		{
			var service = CreateService(-40000);

			Assert.That(service.GetCanvasOrderLimit(int.MinValue), Is.EqualTo(-32768));
		}

		[Test]
		public void GetCanvasOrder_WithEnoughGap_AddsOpenRank()
		{
			Assert.That(Orders(10, 19, 3), Is.EqualTo(new[] { 10, 11, 12 }));
			Assert.That(Orders(10, 19, 10), Is.EqualTo(new[] { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19 }));
		}

		[Test]
		public void GetCanvasOrder_WithoutEnoughGap_EarlierCanvasesShareStart()
		{
			Assert.That(Orders(20, 21, 4), Is.EqualTo(new[] { 20, 20, 20, 21 }));
			Assert.That(Orders(10, 10, 3), Is.EqualTo(new[] { 10, 10, 10 }));
		}

		[Test]
		public void GetCanvasOrder_NearInputBlocker_NeverReachesIt()
		{
			Assert.That(Orders(32765, 32766, 3), Is.EqualTo(new[] { 32765, 32765, 32766 }));
			Assert.That(Orders(32766, 32766, 2), Is.EqualTo(new[] { 32766, 32766 }));
			Assert.That(Orders(32767, 32766, 1), Is.EqualTo(new[] { 32766 }));
			Assert.That(Orders(int.MaxValue, 32766, 2), Is.EqualTo(new[] { 32766, 32766 }));
		}

		[Test]
		public void GetCanvasOrder_BelowCanvasRange_StartsAtMinimum()
		{
			Assert.That(Orders(-32768, -32767, 3), Is.EqualTo(new[] { -32768, -32768, -32767 }));
			Assert.That(Orders(-40000, 32766, 2), Is.EqualTo(new[] { -32768, -32767 }));
			Assert.That(Orders(int.MinValue, -1, 2), Is.EqualTo(new[] { -32768, -32767 }));
		}

		[Test]
		public void GetCanvasOrderStart_ClampsToCanvasRangeAndLimit()
		{
			Assert.That(UiService.GetCanvasOrderStart(10, 19), Is.EqualTo(10));
			Assert.That(UiService.GetCanvasOrderStart(32767, 32766), Is.EqualTo(32766));
			Assert.That(UiService.GetCanvasOrderStart(-40000, 32766), Is.EqualTo(-32768));
		}

		private static int[] Orders(int layer, int limit, int count)
		{
			var orders = new int[count];
			for (var i = 0; i < count; i++)
			{
				orders[i] = UiService.GetCanvasOrder(layer, limit, count, i);
			}

			return orders;
		}

		// Init 없이 설정만 넣은 서비스. 레이어 값만 상한 계산에 쓰입니다
		private static UiService CreateService(params int[] layers)
		{
			var service = new UiService(new MockAssetLoader());
			var types = new[] { typeof(TestUiPresenter), typeof(TestDataUiPresenter), typeof(UiPresenter) };
			for (var i = 0; i < layers.Length; i++)
			{
				service.AddUiConfig(new UiConfig { Address = "layer" + i, Layer = layers[i], UiType = types[i] });
			}

			return service;
		}
	}
}
