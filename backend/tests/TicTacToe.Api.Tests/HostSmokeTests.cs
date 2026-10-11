using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TicTacToe.Api.Tests;

public sealed class HostSmokeTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    [Trait("Req", "NFR-02")]
    public async Task Host_Starts_ServesOpenApiDocument()
    {
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
