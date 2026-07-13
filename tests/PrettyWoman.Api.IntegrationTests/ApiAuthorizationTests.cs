using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.Clients;

namespace PrettyWoman.Api.IntegrationTests;

public class ApiAuthorizationTests(PrettyWomanApiFactory factory) : IClassFixture<PrettyWomanApiFactory>
{
    private readonly PrettyWomanApiFactory _factory = factory;

    [Fact]
    public async Task ProtectedEndpoint_WithoutJwt_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/clients");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LivenessHealthCheck_IsAnonymousAndReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReadinessHealthCheck_VerifiesPostgreSqlAndReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Cors_PreflightFromAdminFrontend_ReturnsAllowedOrigin()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/categories");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("http://localhost:5173", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task Employee_CanReadCatalogsButCannotManageThem()
    {
        using var client = await CreateEmployeeClientAsync();

        var readResponse = await client.GetAsync("/api/v1/categories");
        var createResponse = await client.PostAsJsonAsync("/api/v1/categories", new { name = "No autorizado" });

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, createResponse.StatusCode);
    }

    [Fact]
    public async Task Employee_CanCreateClientButCannotAccessFinances()
    {
        using var client = await CreateEmployeeClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/clients", new CreateClientDTO
        {
            Name = "Cliente de integración"
        });
        var financeResponse = await client.GetAsync("/api/v1/finances/current-balance");

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, financeResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsMiddlewareErrorResponse()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = PrettyWomanApiFactory.AdminEmail,
            Password = "invalid-password"
        });
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal(401, error.Status);
        Assert.Equal("No autorizado", error.Title);
        Assert.Equal("Credenciales invalidas.", error.Detail);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AuthenticatedClient_RequestingMissingClient_ReturnsNotFoundMiddlewareResponse()
    {
        using var client = await CreateEmployeeClientAsync();

        var response = await client.GetAsync("/api/v1/clients/999999");
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(error);
        Assert.Equal(404, error.Status);
        Assert.Equal("Recurso no encontrado", error.Title);
        Assert.NotNull(error.Detail);
    }

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        await _factory.EnsureEmployeeAsync();

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Email = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private sealed class ApiErrorResponse
    {
        public int Status { get; init; }
        public string? Title { get; init; }
        public string? Detail { get; init; }
        public string? Instance { get; init; }
    }
}
