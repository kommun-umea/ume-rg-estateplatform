using Umea.se.EstateService.Shared.Exceptions;

namespace Umea.se.EstateService.Test.Exceptions;

public class ValidationErrorBuilderTests
{
    [Fact]
    public void HasErrors_WhenEmpty_ReturnsFalse()
    {
        ValidationErrorBuilder builder = new();

        builder.HasErrors.ShouldBeFalse();
    }

    [Fact]
    public void HasErrors_AfterAddError_ReturnsTrue()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("field", "required");

        builder.HasErrors.ShouldBeTrue();
    }

    [Fact]
    public void Build_SingleFieldError_CreatesExceptionWithCorrectErrorCode()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("description", "required");

        BusinessValidationException exception = builder.Build();

        exception.Errors.ShouldContainKey("description");
        exception.Errors["description"].ShouldBe(["required"]);
        exception.Message.ShouldBe("One or more validation errors occurred.");
    }

    [Fact]
    public void Build_MultipleErrorCodesOnSameField_AggregatesCodes()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("description", "required");
        builder.AddError("description", "max_length");

        BusinessValidationException exception = builder.Build();

        exception.Errors["description"].Length.ShouldBe(2);
        exception.Errors["description"].ShouldContain("required");
        exception.Errors["description"].ShouldContain("max_length");
    }

    [Fact]
    public void Build_MultipleFields_CreatesAllEntries()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("description", "required");
        builder.AddError("location", "invalid_value");

        BusinessValidationException exception = builder.Build();

        exception.Errors.Count.ShouldBe(2);
        exception.Errors.ShouldContainKey("description");
        exception.Errors.ShouldContainKey("location");
    }

    [Fact]
    public void Build_CustomSummary_SetsExceptionMessage()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("field", "required");

        BusinessValidationException exception = builder.Build("Custom summary.");

        exception.Message.ShouldBe("Custom summary.");
    }

    [Fact]
    public void AddError_IndexedKeys_WorksForFileValidation()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("files[0]", "file_too_large");
        builder.AddError("files[2]", "invalid_content_type");

        BusinessValidationException exception = builder.Build();

        exception.Errors.ShouldContainKey("files[0]");
        exception.Errors["files[0]"].ShouldBe(["file_too_large"]);
        exception.Errors.ShouldContainKey("files[2]");
        exception.Errors["files[2]"].ShouldBe(["invalid_content_type"]);
    }

    [Fact]
    public void AddError_SameKey_MultipleErrors_Aggregates()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("files[1]", "file_too_large");
        builder.AddError("files[1]", "invalid_content_type");

        BusinessValidationException exception = builder.Build();

        exception.Errors["files[1]"].Length.ShouldBe(2);
    }

    [Fact]
    public void ThrowIfErrors_WhenEmpty_DoesNotThrow()
    {
        ValidationErrorBuilder builder = new();

        Should.NotThrow(() => builder.ThrowIfErrors());
    }

    [Fact]
    public void ThrowIfErrors_WithErrors_ThrowsBusinessValidationException()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("field", "required");

        BusinessValidationException exception = Should.Throw<BusinessValidationException>(
            () => builder.ThrowIfErrors());

        exception.Errors.ShouldContainKey("field");
    }

    [Fact]
    public void ThrowIfErrors_CustomSummary_UsesCustomMessage()
    {
        ValidationErrorBuilder builder = new();
        builder.AddError("field", "required");

        BusinessValidationException exception = Should.Throw<BusinessValidationException>(
            () => builder.ThrowIfErrors("Custom."));

        exception.Message.ShouldBe("Custom.");
    }

    [Fact]
    public void AddError_ReturnsSelf_ForFluentChaining()
    {
        ValidationErrorBuilder builder = new();

        ValidationErrorBuilder result = builder
            .AddError("a", "required")
            .AddError("b", "invalid_value");

        result.ShouldBeSameAs(builder);
        result.HasErrors.ShouldBeTrue();
    }
}
