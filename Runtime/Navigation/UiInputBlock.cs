using System;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// <see cref="IUiService.BlockInput"/> 가 돌려주는 입력 차단 토큰입니다. Dispose 하면 이 차단이 풀립니다.
	/// </summary>
	/// <remarks>
	/// 할당 없는 struct 입니다. 같은 토큰을 두 번 Dispose 하면(복사본 포함) 두 번째는 차단을 더 풀지 않고 경고만 남깁니다.
	/// <c>default</c> 값은 아무 차단도 아니며 Dispose 해도 아무것도 하지 않습니다.
	/// </remarks>
	public readonly struct UiInputBlock : IDisposable
	{
		private readonly Action<int> _release;
		private readonly int _id;

		/// <summary>
		/// 토큰을 만듭니다. <see cref="IUiService"/> 를 직접 구현할 때 <see cref="IUiService.BlockInput"/> 에서 씁니다.
		/// </summary>
		/// <param name="release">
		/// 토큰을 풀 때 토큰 ID 를 받아 호출할 함수. 토큰마다 할당하지 않도록 서비스가 한 번 만들어 둔 델리게이트를 넘깁니다.
		/// 같은 토큰의 복사본이 여러 번 Dispose 될 수 있으므로, 이미 풀린 ID 를 받으면 차단 수를 바꾸지 않아야 합니다.
		/// </param>
		/// <param name="id">서비스 안에서 이 토큰을 구분하는 ID</param>
		public UiInputBlock(Action<int> release, int id)
		{
			_release = release;
			_id = id;
		}

		/// <summary>
		/// 이 차단을 풉니다. 마지막 토큰이면 입력이 다시 열립니다.
		/// </summary>
		public void Dispose()
		{
			_release?.Invoke(_id);
		}
	}
}
