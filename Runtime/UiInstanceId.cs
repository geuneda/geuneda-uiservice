using System;

// ReSharper disable CheckNamespace

namespace Geuneda.UiService
{
	/// <summary>
	/// UI 프레젠터 인스턴스의 고유 식별자를 나타냅니다.
	/// 프레젠터 타입과 선택적 인스턴스 주소를 결합하여 동일 타입의 다중 인스턴스를 지원합니다.
	/// </summary>
	public readonly struct UiInstanceId : IEquatable<UiInstanceId>
	{
		/// <summary>
		/// UI 프레젠터의 타입입니다
		/// </summary>
		public readonly Type PresenterType;

		/// <summary>
		/// 선택적 인스턴스 주소입니다. null 또는 빈 문자열이면 기본/싱글턴 인스턴스를 나타냅니다.
		/// </summary>
		public readonly string InstanceAddress;

		/// <summary>
		/// 특정 프레젠터 타입과 선택적 인스턴스 주소에 대한 UI 인스턴스 식별자를 생성합니다
		/// </summary>
		/// <param name="presenterType">UI 프레젠터의 타입</param>
		/// <param name="instanceAddress">선택적 인스턴스 주소입니다. 싱글턴 인스턴스의 경우 null 또는 빈 문자열을 사용하세요.</param>
		public UiInstanceId(Type presenterType, string instanceAddress = null)
		{
			PresenterType = presenterType ?? throw new ArgumentNullException(nameof(presenterType));
			InstanceAddress = string.IsNullOrEmpty(instanceAddress) ? string.Empty : instanceAddress;
		}

		/// <summary>
		/// 프레젠터 타입의 기본 인스턴스 ID를 생성합니다 (싱글턴)
		/// </summary>
		public static UiInstanceId Default(Type presenterType) => new UiInstanceId(presenterType);

		/// <summary>
		/// 프레젠터 타입의 이름이 지정된 인스턴스 ID를 생성합니다
		/// </summary>
		public static UiInstanceId Named(Type presenterType, string instanceAddress) => new UiInstanceId(presenterType, instanceAddress);

		/// <summary>
		/// 기본/싱글턴 인스턴스인지 확인합니다 (특정 인스턴스 주소 없음)
		/// </summary>
		public bool IsDefault => string.IsNullOrEmpty(InstanceAddress);

		public bool Equals(UiInstanceId other)
		{
			return PresenterType == other.PresenterType && InstanceAddress == other.InstanceAddress;
		}

		public override bool Equals(object obj)
		{
			return obj is UiInstanceId other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((PresenterType != null ? PresenterType.GetHashCode() : 0) * 397) ^ (InstanceAddress != null ? InstanceAddress.GetHashCode() : 0);
			}
		}

		public static bool operator ==(UiInstanceId left, UiInstanceId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(UiInstanceId left, UiInstanceId right)
		{
			return !left.Equals(right);
		}

		public override string ToString()
		{
			return IsDefault ? $"{PresenterType.Name}" : $"{PresenterType.Name}:{InstanceAddress}";
		}
	}
}

