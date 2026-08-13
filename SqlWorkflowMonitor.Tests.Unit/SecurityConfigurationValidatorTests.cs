using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using SqlWorkflowMonitor.Security;

namespace SqlWorkflowMonitor.Tests.Unit;

[TestClass]
public sealed class SecurityConfigurationValidatorTests
{
    [TestMethod]
    public void Validate_WithValidConfiguration_DoesNotThrow()
    {
        IConfiguration configuration = CreateConfiguration(
            apiKey: new string('a', 32),
            username: "administrator",
            passwordHash: Convert.ToBase64String(new byte[32]),
            passwordSalt: Convert.ToBase64String(new byte[32]));

        SecurityConfigurationValidator.Validate(configuration);
    }

    [TestMethod]
    public void Validate_WithShortApiKey_ThrowsClearError()
    {
        IConfiguration configuration = CreateConfiguration(
            apiKey: "short",
            username: "administrator",
            passwordHash: Convert.ToBase64String(new byte[32]),
            passwordSalt: Convert.ToBase64String(new byte[32]));

        InvalidOperationException exception =
            Assert.ThrowsExactly<InvalidOperationException>(
                () => SecurityConfigurationValidator.Validate(configuration));

        StringAssert.Contains(
            exception.Message,
            "Security:ApiKey");
    }

    [TestMethod]
    public void Validate_WithInvalidPasswordHash_ThrowsClearError()
    {
        IConfiguration configuration = CreateConfiguration(
            apiKey: new string('a', 32),
            username: "administrator",
            passwordHash: "not-base64",
            passwordSalt: Convert.ToBase64String(new byte[32]));

        InvalidOperationException exception =
            Assert.ThrowsExactly<InvalidOperationException>(
                () => SecurityConfigurationValidator.Validate(configuration));

        StringAssert.Contains(
            exception.Message,
            "Security:Admin:PasswordHash");
    }

    private static IConfiguration CreateConfiguration(
        string apiKey,
        string username,
        string passwordHash,
        string passwordSalt)
    {
        var values = new Dictionary<string, string?>
        {
            ["Security:ApiKey"] = apiKey,
            ["Security:Admin:Username"] = username,
            ["Security:Admin:PasswordHash"] = passwordHash,
            ["Security:Admin:PasswordSalt"] = passwordSalt
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

}
