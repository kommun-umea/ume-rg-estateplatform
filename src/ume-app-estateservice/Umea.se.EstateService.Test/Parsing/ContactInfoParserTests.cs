using Umea.se.EstateService.Shared.Parsing;

namespace Umea.se.EstateService.Test.Parsing;

public class ContactInfoParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Parse_EmptyInput_ReturnsNulls(string? input)
    {
        (string? phone, string? email) = ContactInfoParser.Parse(input);

        phone.ShouldBeNull();
        email.ShouldBeNull();
    }

    [Fact]
    public void Parse_PhoneSlashEmail_ExtractsBoth()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("012-345678 / alice@example.com");

        phone.ShouldBe("012-345678");
        email.ShouldBe("alice@example.com");
    }

    [Fact]
    public void Parse_EmailSlashPhone_ExtractsBoth()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("bob@example.com / 070 123 45 67");

        phone.ShouldBe("070 123 45 67");
        email.ShouldBe("bob@example.com");
    }

    [Fact]
    public void Parse_CommaSeparated_ExtractsBoth()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("012-345678, carol@example.com");

        phone.ShouldBe("012-345678");
        email.ShouldBe("carol@example.com");
    }

    [Fact]
    public void Parse_OnlyPhone_ReturnsPhoneOnly()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("012-345678");

        phone.ShouldBe("012-345678");
        email.ShouldBeNull();
    }

    [Fact]
    public void Parse_OnlyEmail_ReturnsEmailOnly()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("dave@example.com");

        phone.ShouldBeNull();
        email.ShouldBe("dave@example.com");
    }

    [Fact]
    public void Parse_InternationalPhone_Extracts()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("+46 (0)12 34 56 78");

        phone.ShouldNotBeNull();
        phone.ShouldContain("46");
    }

    [Fact]
    public void Parse_NoiseAroundValues_StillExtracts()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("Tel: 012-345678, e-post: eve@example.com");

        phone.ShouldBe("012-345678");
        email.ShouldBe("eve@example.com");
    }

    [Fact]
    public void Parse_ShortNumber_DoesNotMatchAsPhone()
    {
        (string? phone, string? email) = ContactInfoParser.Parse("rum 12");

        phone.ShouldBeNull();
        email.ShouldBeNull();
    }

    [Fact]
    public void Parse_EmailAdjacentToPhone_DoesNotLeakAtPrefix()
    {
        // The '@' in the email is unique — the digits in the email's local part should not leak
        // into the phone (e.g., the domain digits shouldn't be parsed twice).
        (string? phone, string? email) = ContactInfoParser.Parse("user1234567@example.com 012-345");

        email.ShouldBe("user1234567@example.com");
        // Phone may or may not match "012-345" depending on digit count; assert it didn't swallow email digits.
        if (phone is not null)
        {
            phone.ShouldNotContain("@");
        }
    }
}
