using NUnit.Framework;
using System;

namespace Geuneda.UiService.Tests
{
    [TestFixture]
    public class UiInstanceIdTests
    {
        [Test]
        public void Constructor_WithValidType_CreatesInstance()
        {
            // 준비 및 실행
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), "test_address");

            // 검증
            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual("test_address", instanceId.InstanceAddress);
        }

        [Test]
        public void Constructor_WithEmptyAddress_CreatesDefaultInstance()
        {
            // 준비 및 실행
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), string.Empty);

            // 검증
            Assert.AreEqual(string.Empty, instanceId.InstanceAddress);
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        public void Constructor_WithNullAddress_CreatesDefaultInstance()
        {
            // 준비 및 실행
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), null);

            // 검증
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        public void Equals_SameTypeAndAddress_ReturnsTrue()
        {
            // 준비
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address");

            // 검증
            Assert.IsTrue(id1.Equals(id2));
            Assert.IsTrue(id1 == id2);
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            // 준비
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestDataUiPresenter), "address");

            // 검증
            Assert.IsFalse(id1.Equals(id2));
            Assert.IsTrue(id1 != id2);
        }

        [Test]
        public void Equals_DifferentAddress_ReturnsFalse()
        {
            // 준비
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address1");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address2");

            // 검증
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void GetHashCode_SameTypeAndAddress_ReturnsSameHash()
        {
            // 준비
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address");

            // 검증
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            // 준비
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), "my_instance");

            // 실행
            var result = instanceId.ToString();

            // 검증
            Assert.IsTrue(result.Contains("TestUiPresenter"));
            Assert.IsTrue(result.Contains("my_instance"));
        }
    }
}

