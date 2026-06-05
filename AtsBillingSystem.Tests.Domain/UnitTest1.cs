using NUnit.Framework;
using System;
using AtsBillingSystem.Domain.Models;

namespace AtsBillingSystem.Tests.Domain
{
    [TestFixture]
    public class DomainSubscriberTests
    {
        [Test]
        public void DebitBalance_ValidAmount_DecreasesBalance()
        {
            // Arrange
            var subscriber = new DomainSubscriber { Balance = 100m };
            var amountToDebit = 40.5m;
            var expectedBalance = 59.5m;

            // Act
            subscriber.DebitBalance(amountToDebit);

            // Assert
            Assert.That(subscriber.Balance, Is.EqualTo(expectedBalance));
        }

        [Test]
        public void DebitBalance_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var subscriber = new DomainSubscriber { Balance = 100m };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => subscriber.DebitBalance(-10m));
            Assert.That(ex.Message, Does.Contain("Сумма списания не может быть отрицательной"));
        }

        [Test]
        public void CreditBalance_ValidAmount_IncreasesBalance()
        {
            // Arrange
            var subscriber = new DomainSubscriber { Balance = 100m };
            var amountToCredit = 50.25m;
            var expectedBalance = 150.25m;

            // Act
            subscriber.CreditBalance(amountToCredit);

            // Assert
            Assert.That(subscriber.Balance, Is.EqualTo(expectedBalance));
        }

        [Test]
        public void CreditBalance_NegativeAmount_ThrowsArgumentException()
        {
            // Arrange
            var subscriber = new DomainSubscriber { Balance = 100m };

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => subscriber.CreditBalance(-10m));
            Assert.That(ex.Message, Does.Contain("Сумма пополнения не может быть отрицательной"));
        }
    }
}