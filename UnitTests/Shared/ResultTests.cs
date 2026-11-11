using Shared.Kernel.Results;

namespace Unit.Shared;

[TestFixture]
public class ResultTests
{
    private static readonly Error NotFound = Error.NotFound("X_NOT_FOUND", "Nie ma.");

    [Test]
    public void Ok_HasNoError()
    {
        var result = Result.Ok();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.ErrorCode, Is.Null);
    }

    [Test]
    public void AnError_ConvertsIntoAFailure()
    {
        Result result = NotFound;

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("X_NOT_FOUND"));
        Assert.That(result.ErrorMessage, Is.EqualTo("Nie ma."));
    }

    [Test]
    public void AValue_ConvertsIntoASuccess()
    {
        Result<int> result = 42;

        Assert.That(result.Success, Is.True);
        Assert.That(result.Value, Is.EqualTo(42));
    }

    [Test]
    public void AFailedResultOfT_HasNoValue()
    {
        Result<string> result = NotFound;

        Assert.That(result.Success, Is.False);
        Assert.That(result.Value, Is.Null);
        Assert.That(result.Error, Is.EqualTo(NotFound));
    }

    [Test]
    public void Error_FactoriesSetTheType()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Error.Validation("A", "a").Type, Is.EqualTo(ErrorType.Validation));
            Assert.That(Error.NotFound("A", "a").Type, Is.EqualTo(ErrorType.NotFound));
            Assert.That(Error.Conflict("A", "a").Type, Is.EqualTo(ErrorType.Conflict));
            Assert.That(Error.Forbidden("A", "a").Type, Is.EqualTo(ErrorType.Forbidden));
        });
    }
}
