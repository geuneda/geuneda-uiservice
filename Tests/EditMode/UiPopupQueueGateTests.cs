using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests
{
	/// <summary>
	/// 여러 팝업 큐가 팩토리를 하나씩 돌리게 하는 UiPopupQueueGate 테스트입니다.
	/// </summary>
	[TestFixture]
	public class UiPopupQueueGateTests
	{
		private UiPopupQueueGate _gate;
		private List<string> _calls;

		[SetUp]
		public void Setup()
		{
			_gate = new UiPopupQueueGate();
			_calls = new List<string>();
		}

		[Test]
		public void TryEnter_WhileAnotherQueueLoads_IsRefused()
		{
			Action first = () => { };
			Action second = () => { };

			Assert.That(_gate.TryEnter(first), Is.True);
			Assert.That(_gate.TryEnter(second), Is.False);
		}

		[Test]
		public void Exit_ResumesWaitersInWaitOrder_UntilOneEnters()
		{
			Action owner = () => { };
			Action third = null;
			Action second = null;
			// 먼저 기다린 third 가 들어가면 second 는 계속 기다립니다
			third = () =>
			{
				_calls.Add("third");
				_gate.TryEnter(third);
			};
			second = () => _calls.Add("second");
			_gate.TryEnter(owner);
			_gate.TryEnter(third);
			_gate.TryEnter(second);

			_gate.Exit(owner);
			Assert.That(_calls, Is.EqualTo(new[] { "third" }));

			_gate.Exit(third);
			Assert.That(_calls, Is.EqualTo(new[] { "third", "second" }));
		}

		[Test]
		public void Exit_SkipsWaiterThatDoesNotEnter()
		{
			Action owner = () => { };
			Action held = () => _calls.Add("held");
			Action next = null;
			next = () =>
			{
				_calls.Add("next");
				_gate.TryEnter(next);
			};
			_gate.TryEnter(owner);
			_gate.TryEnter(held);
			_gate.TryEnter(next);

			_gate.Exit(owner);

			Assert.That(_calls, Is.EqualTo(new[] { "held", "next" }));
			Assert.That(_gate.TryEnter(held), Is.False);
		}

		[Test]
		public void TryEnter_AgainWhileWaiting_KeepsPosition()
		{
			Action owner = () => { };
			Action first = () => _calls.Add("first");
			Action second = () => _calls.Add("second");
			_gate.TryEnter(owner);
			_gate.TryEnter(first);
			_gate.TryEnter(second);
			_gate.TryEnter(first);

			_gate.Exit(owner);

			Assert.That(_calls, Is.EqualTo(new[] { "first", "second" }));
		}

		[Test]
		public void Exit_ByQueueThatIsNotLoading_OnlyStopsItsWait()
		{
			Action owner = () => { };
			Action waiter = () => _calls.Add("waiter");
			Action disposed = () => _calls.Add("disposed");
			_gate.TryEnter(owner);
			_gate.TryEnter(disposed);
			_gate.TryEnter(waiter);

			// 기다리던 큐를 Dispose 해도 로드 중인 큐는 그대로입니다
			_gate.Exit(disposed);
			Assert.That(_calls, Is.Empty);

			_gate.Exit(owner);
			Assert.That(_calls, Is.EqualTo(new[] { "waiter" }));
		}

		[Test]
		public void Exit_WaiterThrows_IsLoggedAndNextWaiterResumes()
		{
			Action owner = () => { };
			Action broken = () => throw new InvalidOperationException("drained boom");
			Action next = () => _calls.Add("next");
			_gate.TryEnter(owner);
			_gate.TryEnter(broken);
			_gate.TryEnter(next);
			LogAssert.Expect(LogType.Error, new Regex(@"\[Geuneda\.UiService\.PopupQueue\] A waiting popup queue threw while resuming"));
			LogAssert.Expect(LogType.Exception, new Regex("drained boom"));

			Assert.DoesNotThrow(() => _gate.Exit(owner));

			Assert.That(_calls, Is.EqualTo(new[] { "next" }));
		}
	}
}
