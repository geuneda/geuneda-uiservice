using System.Collections;
using NUnit.Framework;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Geuneda.UiService.Tests.PlayMode
{
	/// <summary>
	/// ReadsBackInput 이 Input System 의 Escape 키로 HandleBack 을 부르는지 확인합니다.
	/// </summary>
	[TestFixture]
	public class UiBackInputReaderTests : InputTestFixture
	{
		private UiService _service;
		private Keyboard _keyboard;
		private int _backCount;

		[SetUp]
		public override void Setup()
		{
			base.Setup();
			_keyboard = InputSystem.AddDevice<Keyboard>();
			_service = new UiService(new PrefabRegistryUiAssetLoader());
			_service.Init(UnityEngine.ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>());
			_backCount = 0;
			_service.BackUnhandled += () => _backCount++;
		}

		[TearDown]
		public override void TearDown()
		{
			_service?.Dispose();
			base.TearDown();
		}

		[UnityTest]
		public IEnumerator ReadsBackInput_EscapePress_CallsHandleBack()
		{
			_service.ReadsBackInput = true;

			Press(_keyboard.escapeKey, queueEventOnly: true);
			yield return null;
			yield return null;

			Assert.That(_backCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator EscapeTappedWithinOneFrameOnAnotherKeyboard_CallsHandleBack()
		{
			// Android 뒤로가기는 Keyboard.current 가 아닌 키보드 장치로 눌렀다 떼기가 한 프레임에 들어올 수 있습니다
			var other = InputSystem.AddDevice<Keyboard>();
			_keyboard.MakeCurrent();
			_service.ReadsBackInput = true;
			yield return null;

			Press(other.escapeKey, queueEventOnly: true);
			Release(other.escapeKey, queueEventOnly: true);
			yield return null;
			yield return null;

			Assert.That(_backCount, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator ReadsBackInputOff_EscapePress_IsIgnored()
		{
			_service.ReadsBackInput = true;
			_service.ReadsBackInput = false;

			Press(_keyboard.escapeKey, queueEventOnly: true);
			yield return null;
			yield return null;

			Assert.That(_backCount, Is.EqualTo(0));
		}
	}
}
