using System.Net;

namespace TicTacToe.Api.Tests;

public sealed class HealthCheckTests : ApiTestBase
{
    [Fact]
    public async Task Health_ReturnsOk()
    {
        using var response = await Client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
