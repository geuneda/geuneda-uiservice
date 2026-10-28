using System.Collections.Generic;
using Geuneda.UiService.Rendering;
using NUnit.Framework;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// 스택 삽입 위치 계산만 확인하는 순수 로직 테스트. URP 나 엔진 타입을 만들지 않는다.
	/// </summary>
	[TestFixture]
	public class UiCameraStackInsertIndexTests
	{
		[Test]
		public void InsertIndex_EmptyList_ReturnsZero()
		{
			Assert.AreEqual(0, UiCameraStackFeature.InsertIndex(new List<int>(), 5));
		}

		[Test]
		public void InsertIndex_LowerPriority_InsertsAtStart()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(0, UiCameraStackFeature.InsertIndex(existing, 5));
		}

		[Test]
		public void InsertIndex_HigherPriority_InsertsAtEnd()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(3, UiCameraStackFeature.InsertIndex(existing, 40));
		}

		[Test]
		public void InsertIndex_MiddlePriority_InsertsBetween()
		{
			var existing = new List<int> { 10, 20, 30 };
			Assert.AreEqual(2, UiCameraStackFeature.InsertIndex(existing, 25));
		}

		[Test]
		public void InsertIndex_EqualPriority_InsertsAfterExisting()
		{
			// 같은 값은 기존 항목 뒤에 둬야 다시 열 때마다 그리는 순서가 뒤집히지 않는다
			var existing = new List<int> { 10, 20, 20, 30 };
			Assert.AreEqual(3, UiCameraStackFeature.InsertIndex(existing, 20));
		}

		[Test]
		public void InsertIndex_SequentialInserts_ProduceSortedOrder()
		{
			var priorities = new List<int>();

			void InsertAndTrack(int priority)
			{
				var index = UiCameraStackFeature.InsertIndex(priorities, priority);
				priorities.Insert(index, priority);
			}

			InsertAndTrack(20);
			InsertAndTrack(5);
			InsertAndTrack(30);
			InsertAndTrack(15);

			CollectionAssert.AreEqual(new[] { 5, 15, 20, 30 }, priorities);
		}
	}
}
