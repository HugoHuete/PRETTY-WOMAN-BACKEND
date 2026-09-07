using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using PrettyWoman.Api.IntegrationTests.Infrastructure;
using PrettyWoman.Application.DTOs.Auth;
using PrettyWoman.Application.DTOs.Clients;

namespace PrettyWoman.Api.IntegrationTests;

[Collection(ApiIntegrationCollection.Name)]
public class ApiAuthorizationTests(PrettyWomanApiFactory factory)
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
    public async Task Request_WithCorrelationId_ReturnsItInResponseAndProblemDetails()
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new LoginRequestDTO
            {
                Username = PrettyWomanApiFactory.AdminUsername,
                Password = "invalid-password"
            })
        };
        request.Headers.Add("X-Correlation-ID", "integration-correlation-id");

        var response = await client.SendAsync(request);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal("integration-correlation-id", response.Headers.GetValues("X-Correlation-ID").Single());
        Assert.Equal("integration-correlation-id", document.RootElement.GetProperty("traceId").GetString());
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
    public async Task RailwayHealthCheck_VerifiesPostgreSqlAndReturnsOk()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

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
            Username = PrettyWomanApiFactory.AdminUsername,
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
    public async Task Login_WithUsername_ReturnsToken()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            username = PrettyWomanApiFactory.AdminUsername,
            password = PrettyWomanApiFactory.AdminPassword
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_IssuesRefreshTokenCookie_AndRefreshRotatesIt()
    {
        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.AdminUsername,
            Password = PrettyWomanApiFactory.AdminPassword
        });
        var initialAuth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        var cookies = loginResponse.Headers.GetValues("Set-Cookie").ToArray();
        var refreshCookie = GetCookie(cookies, "refresh_token");
        var csrfCookie = GetCookie(cookies, "csrf_token");

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(initialAuth);
        Assert.Equal(csrfCookie, initialAuth.CsrfToken);
        Assert.Contains(cookies, value => value.Contains("refresh_token=") && value.Contains("httponly", StringComparison.OrdinalIgnoreCase)
            && value.Contains("samesite=none", StringComparison.OrdinalIgnoreCase)
            && value.Contains("secure", StringComparison.OrdinalIgnoreCase));

        using var refreshClient = _factory.CreateClient();
        refreshClient.DefaultRequestHeaders.Add("Cookie", $"refresh_token={refreshCookie}; csrf_token={csrfCookie}");
        refreshClient.DefaultRequestHeaders.Add("X-CSRF-Token", initialAuth.CsrfToken);
        var refreshResponse = await refreshClient.PostAsync("/api/v1/auth/refresh", null);

        Assert.True(refreshResponse.StatusCode == HttpStatusCode.OK, await refreshResponse.Content.ReadAsStringAsync());
        var refreshedAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();
        Assert.NotNull(refreshedAuth);
        Assert.NotEqual(initialAuth.AccessToken, refreshedAuth.AccessToken);

        using var reusedClient = _factory.CreateClient();
        reusedClient.DefaultRequestHeaders.Add("Cookie", $"refresh_token={refreshCookie}; csrf_token={csrfCookie}");
        reusedClient.DefaultRequestHeaders.Add("X-CSRF-Token", initialAuth.CsrfToken);
        var reusedResponse = await reusedClient.PostAsync("/api/v1/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, reusedResponse.StatusCode);
    }

    [Fact]
    public async Task Login_InDevelopmentOverHttp_IssuesCookiesWithoutSecureAttribute()
    {
        using var developmentFactory = _factory.WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = developmentFactory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.AdminUsername,
            Password = PrettyWomanApiFactory.AdminPassword
        });

        var cookies = response.Headers.GetValues("Set-Cookie").ToArray();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(cookies, value => value.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase)
            && value.Contains("secure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cookies, value => value.StartsWith("csrf_token=", StringComparison.OrdinalIgnoreCase)
            && value.Contains("secure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookies, value => value.StartsWith("refresh_token=", StringComparison.OrdinalIgnoreCase)
            && value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(cookies, value => value.StartsWith("csrf_token=", StringComparison.OrdinalIgnoreCase)
            && value.Contains("samesite=lax", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DisabledUser_CannotLoginOrUsePreviouslyIssuedToken_AndCanLoginAfterBeingEnabled()
    {
        await _factory.EnsureEmployeeAsync();
        using var employeeClient = _factory.CreateClient();
        var initialLogin = await employeeClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });
        var employeeAuth = await initialLogin.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, initialLogin.StatusCode);
        Assert.NotNull(employeeAuth);

        employeeClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", employeeAuth.AccessToken);

        using var adminClient = await CreateAdminClientAsync();
        var disableResponse = await adminClient.PostAsync($"/api/v1/auth/users/{employeeAuth.User.Id}/disable", null);
        var existingSessionResponse = await employeeClient.GetAsync("/api/v1/clients");

        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, existingSessionResponse.StatusCode);

        var disabledLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });

        Assert.Equal(HttpStatusCode.Unauthorized, disabledLogin.StatusCode);

        var enableResponse = await adminClient.PostAsync($"/api/v1/auth/users/{employeeAuth.User.Id}/enable", null);

        Assert.Equal(HttpStatusCode.OK, enableResponse.StatusCode);

        var enabledLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });

        Assert.Equal(HttpStatusCode.OK, enabledLogin.StatusCode);
    }

    [Fact]
    public async Task Admin_CanListUsers()
    {
        await _factory.EnsureEmployeeAsync();
        using var adminClient = await CreateAdminClientAsync();

        var response = await adminClient.GetAsync("/api/v1/auth/users");

        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        var users = await response.Content.ReadFromJsonAsync<UserDTO[]>();
        Assert.NotNull(users);
        Assert.Contains(users, user => user.Email == PrettyWomanApiFactory.EmployeeEmail && user.Enabled);
    }

    [Fact]
    public async Task Admin_CanFilterUsersBySearchRoleAndEnabledStatus()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        using var adminClient = await CreateAdminClientAsync();

        var enabledEmployee = await CreateUserAsync(
            adminClient,
            $"maria.enabled.{suffix}",
            $"maria.enabled.{suffix}@prettywoman.test",
            "María",
            "Habilitada",
            "Employee");
        var disabledEmployee = await CreateUserAsync(
            adminClient,
            $"maria.disabled.{suffix}",
            $"maria.disabled.{suffix}@prettywoman.test",
            "María",
            "Deshabilitada",
            "Employee");
        var enabledAdmin = await CreateUserAsync(
            adminClient,
            $"maria.admin.{suffix}",
            $"maria.admin.{suffix}@prettywoman.test",
            "María",
            "Administradora",
            "Admin");

        var disableResponse = await adminClient.PostAsync($"/api/v1/auth/users/{disabledEmployee.Id}/disable", null);
        Assert.Equal(HttpStatusCode.OK, disableResponse.StatusCode);

        var searchResponse = await adminClient.GetAsync($"/api/v1/auth/users?user=maria.disabled.{suffix}");
        var roleResponse = await adminClient.GetAsync("/api/v1/auth/users?role=Employee");
        var statusResponse = await adminClient.GetAsync("/api/v1/auth/users?enabled=false");
        var combinedResponse = await adminClient.GetAsync($"/api/v1/auth/users?user=maría&role=Employee&enabled=true");

        var searchedUsers = await searchResponse.Content.ReadFromJsonAsync<UserDTO[]>();
        var employeeUsers = await roleResponse.Content.ReadFromJsonAsync<UserDTO[]>();
        var disabledUsers = await statusResponse.Content.ReadFromJsonAsync<UserDTO[]>();
        var combinedUsers = await combinedResponse.Content.ReadFromJsonAsync<UserDTO[]>();

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, roleResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, combinedResponse.StatusCode);
        Assert.NotNull(searchedUsers);
        Assert.NotNull(employeeUsers);
        Assert.NotNull(disabledUsers);
        Assert.NotNull(combinedUsers);
        Assert.Single(searchedUsers, user => user.Id == disabledEmployee.Id);
        Assert.Contains(employeeUsers, user => user.Id == enabledEmployee.Id);
        Assert.Contains(employeeUsers, user => user.Id == disabledEmployee.Id);
        Assert.DoesNotContain(employeeUsers, user => user.Id == enabledAdmin.Id);
        Assert.Contains(disabledUsers, user => user.Id == disabledEmployee.Id);
        Assert.DoesNotContain(disabledUsers, user => user.Id == enabledEmployee.Id);
        Assert.Single(combinedUsers, user => user.Id == enabledEmployee.Id);
    }

    [Fact]
    public async Task Admin_CanUpdateUserProfileAndPassword_WhichInvalidatesPreviousToken()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"user.update.{suffix}";
        var password = "ClaveInicial123!";
        using var adminClient = await CreateAdminClientAsync();
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/auth/users", new CreateUserDTO
        {
            Username = username,
            Email = $"user.update.{suffix}@prettywoman.test",
            Password = password,
            Name = "Usuario",
            Lastname = "Actualizable",
            Role = "Employee"
        });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDTO>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdUser);

        using var userClient = _factory.CreateClient();
        var initialLogin = await userClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = username,
            Password = password
        });
        var userAuth = await initialLogin.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, initialLogin.StatusCode);
        Assert.NotNull(userAuth);

        userClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", userAuth.AccessToken);

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/v1/auth/users/{createdUser.Id}", new
        {
            name = "Empleado actualizado",
            lastname = "Integracion actualizado",
            email = "empleado.actualizado@prettywoman.test",
            password = "NuevaClave123!"
        });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedUser = await updateResponse.Content.ReadFromJsonAsync<UserDTO>();
        var previousTokenResponse = await userClient.GetAsync("/api/v1/clients");
        Assert.NotNull(updatedUser);
        Assert.Equal("Empleado actualizado", updatedUser.Name);
        Assert.Equal("Integracion actualizado", updatedUser.Lastname);
        Assert.Equal("empleado.actualizado@prettywoman.test", updatedUser.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, previousTokenResponse.StatusCode);

        var newLogin = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = username,
            Password = "NuevaClave123!"
        });

        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }

    [Fact]
    public async Task Admin_UpdateWithInvalidPassword_DoesNotPersistProfileChanges()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"user.atomic.{suffix}";
        var originalEmail = $"user.atomic.{suffix}@prettywoman.test";
        var password = "ClaveInicial123!";
        using var adminClient = await CreateAdminClientAsync();
        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/auth/users", new CreateUserDTO
        {
            Username = username,
            Email = originalEmail,
            Password = password,
            Name = "Nombre original",
            Lastname = "Apellido original",
            Role = "Employee"
        });
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserDTO>();

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdUser);

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/v1/auth/users/{createdUser.Id}", new
        {
            name = "Nombre rechazado",
            lastname = "Apellido rechazado",
            email = "rechazado@prettywoman.test",
            password = "corta"
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);

        var usersResponse = await adminClient.GetAsync("/api/v1/auth/users");
        var users = await usersResponse.Content.ReadFromJsonAsync<UserDTO[]>();

        Assert.Equal(HttpStatusCode.OK, usersResponse.StatusCode);
        Assert.NotNull(users);
        var userAfterFailedUpdate = Assert.Single(users, user => user.Id == createdUser.Id);
        Assert.Equal("Nombre original", userAfterFailedUpdate.Name);
        Assert.Equal("Apellido original", userAfterFailedUpdate.Lastname);
        Assert.Equal(originalEmail, userAfterFailedUpdate.Email);

        var loginResponse = await _factory.CreateClient().PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = username,
            Password = password
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_WhenGlobalLimitIsExceeded_ReturnsTooManyRequests()
    {
        var (rateLimitFactory, client) = CreateRateLimitClient("RateLimiting__LoginPermitLimit", "5");
        using (rateLimitFactory)
        using (client)
        {
            for (var attempt = 0; attempt < 5; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
                {
                    Content = JsonContent.Create(new LoginRequestDTO
                    {
                        Username = PrettyWomanApiFactory.AdminUsername,
                        Password = PrettyWomanApiFactory.AdminPassword
                    })
                };
                request.Headers.Add("X-Forwarded-For", $"198.51.100.{attempt + 1}");
                var response = await client.SendAsync(request);

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            using var limitedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login/")
            {
                Content = JsonContent.Create(new LoginRequestDTO
                {
                    Username = PrettyWomanApiFactory.AdminUsername,
                    Password = PrettyWomanApiFactory.AdminPassword
                })
            };
            limitedRequest.Headers.Add("X-Forwarded-For", "198.51.100.6");
            var limitedResponse = await client.SendAsync(limitedRequest);

            Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        }
    }

    [Fact]
    public async Task ProtectedRead_WhenGlobalLimitIsExceededWithoutJwt_ReturnsTooManyRequests()
    {
        var (rateLimitFactory, client) = CreateRateLimitClient("RateLimiting__ReadPermitLimit", "2");
        using (rateLimitFactory)
        using (client)
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/clients");
                request.Headers.Add("X-Forwarded-For", $"198.51.100.{attempt + 1}");
                var response = await client.SendAsync(request);
                Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            }

            using var limitedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/clients");
            limitedRequest.Headers.Add("X-Forwarded-For", "198.51.100.3");
            var limitedResponse = await client.SendAsync(limitedRequest);

            Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        }
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

    private static async Task<UserDTO> CreateUserAsync(
        HttpClient adminClient,
        string username,
        string email,
        string name,
        string lastname,
        string role)
    {
        var response = await adminClient.PostAsJsonAsync("/api/v1/auth/users", new CreateUserDTO
        {
            Username = username,
            Email = email,
            Password = "ClavePrueba123!",
            Name = name,
            Lastname = lastname,
            Role = role
        });
        var user = await response.Content.ReadFromJsonAsync<UserDTO>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<UserDTO>(user);
    }

    private async Task<HttpClient> CreateEmployeeClientAsync()
    {
        await _factory.EnsureEmployeeAsync();

        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.EmployeeEmail,
            Password = PrettyWomanApiFactory.EmployeePassword
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private static string GetCookie(IEnumerable<string> cookies, string name)
    {
        var cookie = Assert.Single(cookies, value => value.StartsWith($"{name}=", StringComparison.OrdinalIgnoreCase));
        return cookie.Split(';', 2)[0][(name.Length + 1)..];
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDTO
        {
            Username = PrettyWomanApiFactory.AdminUsername,
            Password = PrettyWomanApiFactory.AdminPassword
        });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDTO>();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.NotNull(auth);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private (WebApplicationFactory<Program> Factory, HttpClient Client) CreateRateLimitClient(string variableName, string value)
    {
        var originalValue = Environment.GetEnvironmentVariable(variableName);
        Environment.SetEnvironmentVariable(variableName, value);

        try
        {
            var factory = _factory.WithWebHostBuilder(_ => { });
            return (factory, factory.CreateClient());
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, originalValue);
        }
    }

    private sealed class ApiErrorResponse
    {
        public int Status { get; init; }
        public string? Title { get; init; }
        public string? Detail { get; init; }
        public string? Instance { get; init; }
    }
}
