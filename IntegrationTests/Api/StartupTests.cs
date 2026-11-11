using System.Net;

namespace IntegrationTests.Api;

[TestFixture]
public class StartupTests
{
    [Test]
    public async Task Host_StartsWithValidDependencyGraph()
    {
        await using var factory = new ApiFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/bookings");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), await response.Content.ReadAsStringAsync());
    }
}
