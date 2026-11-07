using VehicleRental.Rentals.Domain;

namespace VehicleTests.Tests.Unit.Rentals.Domain;

public class MoneyTests
{
    [Fact]
    public void Constructor_ShouldThrowException_WhenAmountIsNegative()
    {
        // Arrange
        const int negativeAmount = -1;
        const Currency currency = Currency.USD;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Money(negativeAmount, currency));
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties_WhenValidParameters()
    {
        // Arrange
        const int amount = 100;
        const Currency currency = Currency.USD;

        // Act
        var money = new Money(amount, currency);

        // Assert
        Assert.Equal(amount, money.Amount);
        Assert.Equal(currency, money.Currency);
    }

    [Fact]
    public void OperatorPlus_ShouldReturnCorrectSum_WhenSameCurrency()
    {
        // Arrange
        var money1 = new Money(100, Currency.USD);
        var money2 = new Money(50, Currency.USD);

        // Act
        var result = money1 + money2;

        // Assert
        Assert.Equal(150, result.Amount);
        Assert.Equal(Currency.USD, result.Currency);
    }

    [Fact]
    public void OperatorPlus_ShouldThrowException_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, Currency.USD);
        var money2 = new Money(50, Currency.EUR);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1 + money2);
    }

    [Fact]
    public void OperatorMinus_ShouldReturnCorrectDifference_WhenSameCurrency()
    {
        // Arrange
        var money1 = new Money(100, Currency.USD);
        var money2 = new Money(50, Currency.USD);

        // Act
        var result = money1 - money2;

        // Assert
        Assert.Equal(50, result.Amount);
        Assert.Equal(Currency.USD, result.Currency);
    }

    [Fact]
    public void OperatorMinus_ShouldThrowException_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, Currency.USD);
        var money2 = new Money(50, Currency.EUR);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1 - money2);
    }

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(100, 100, true)]
    [InlineData(99, 100, false)]
    public void OperatorGreaterThanOrEqual_ShouldCompareAmounts_WhenSameCurrency(int left, int right, bool expected)
    {
        // Arrange
        var money1 = new Money(left, Currency.USD);
        var money2 = new Money(right, Currency.USD);

        // Act & Assert
        Assert.Equal(expected, money1 >= money2);
        Assert.Equal(!expected, money1 < money2);
    }

    [Fact]
    public void Comparison_ShouldThrowException_WhenDifferentCurrencies()
    {
        // Arrange
        var money1 = new Money(100, Currency.USD);
        var money2 = new Money(50, Currency.EUR);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => money1 >= money2);
        Assert.Throws<InvalidOperationException>(() => money1 < money2);
        Assert.Throws<InvalidOperationException>(() => money1 > money2);
        Assert.Throws<InvalidOperationException>(() => money1 <= money2);
    }
}