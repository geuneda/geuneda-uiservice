using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <inheritdoc />
	public class AddressablesUiAssetLoader : IUiAssetLoader
	{
		/// <inheritdoc />
		public async UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default)
		{
			var operation = Addressables.InstantiateAsync(config.Address, new InstantiationParameters(parent, false));

			if(config.LoadSynchronously)
			{
				operation.WaitForCompletion();
			}
			else
			{
				await operation.ToUniTask(cancellationToken: cancellationToken);
			}

			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				throw operation.OperationException;
			}

			return operation.Result;
		}

		/// <inheritdoc />
		public void UnloadAsset(GameObject asset)
		{
			if (asset == null)
			{
				return;
			}

			// Addressables 로 만들지 않은 인스턴스(AddUi 로 직접 넣은 프레젠터 등)는 ReleaseInstance 가
			// 파괴하지 않고 false 를 돌려주므로 직접 파괴합니다
			if (!Addressables.ReleaseInstance(asset))
			{
				Object.Destroy(asset);
			}
		}
	}
}