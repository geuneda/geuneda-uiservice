using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// 에셋 로딩 방식을 UI 메모리로 래핑할 수 있게 하는 인터페이스입니다
	/// </summary>
	public interface IUiAssetLoader
	{
		/// <summary>
		/// 주어진 <paramref name="config"/>와 <paramref name="parent"/>로 프리팹을 비동기적으로 인스턴스화합니다.
		/// </summary>
		/// <param name="config">인스턴스화할 UI 구성.</param>
		/// <param name="parent">프리팹을 인스턴스화할 부모 트랜스폼.</param>
		/// <param name="cancellationToken">작업을 취소하기 위한 취소 토큰.</param>
		/// <returns>인스턴스화된 프리팹 게임 오브젝트로 완료되는 태스크.</returns>
		UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default);

		/// <summary>
		/// 주어진 <paramref name="asset"/>을 게임 메모리에서 언로드합니다
		/// </summary>
		void UnloadAsset(GameObject asset);
	}
}

