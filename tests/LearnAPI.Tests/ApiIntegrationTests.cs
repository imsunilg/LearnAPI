using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LearnAPI.Tests;

/// <summary>
/// Day 55: real HTTP pipeline (routing, middleware, filters, JWT, permissions, validation, error handling) against the real
/// PostgreSQL database - only the network socket is replaced by an in-memory server. Requires LearnDB/setup.ps1 to have been run.
/// Read-mostly: the only write is one rejected request, which proves the audit trail.
/// </summary>
public class ApiIntegrationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));

    private async Task<HttpClient> LoginAsync(string user, string password)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new { username = user, password });
        res.EnsureSuccessStatusCode();
        var token = (await res.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Protected_endpoint_without_a_token_returns_401_problem_details()
    {
        var res = await _factory.CreateClient().GetAsync("/api/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Every_response_has_security_headers_and_a_correlation_id()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "integration-test-0001");
        var res = await client.GetAsync("/api/customers");

        Assert.Equal("integration-test-0001", res.Headers.GetValues("X-Correlation-Id").Single());
        Assert.Equal("nosniff", res.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", res.Headers.GetValues("X-Frame-Options").Single());
    }

    [Fact]
    public async Task Admin_can_read_the_dashboard_report_with_real_data()
    {
        var client = await LoginAsync("admin", "Admin@123");
        var res = await client.GetAsync("/api/reports/dashboard");
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("kpis").GetProperty("activePolicies").GetInt32() > 0);
        Assert.Equal(12, json.GetProperty("premiumByMonth").GetArrayLength());
    }

    [Fact]
    public async Task Viewer_role_is_forbidden_from_changing_data()
    {
        var client = await LoginAsync("viewer", "Pass@123");
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/customers")).StatusCode);   // may read...
        var res = await client.PostAsJsonAsync("/api/customers", new { });                        // ...but not create
        Assert.True(res.StatusCode == HttpStatusCode.Forbidden, $"expected 403 but got {(int)res.StatusCode}: {await res.Content.ReadAsStringAsync()}");
    }

    [Fact]
    public async Task Invalid_payment_request_is_rejected_with_a_problem_details_body_and_audited()
    {
        var client = await LoginAsync("admin", "Admin@123");
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "audit-probe-" + Guid.NewGuid().ToString("N")[..12]);
        var correlation = client.DefaultRequestHeaders.GetValues("X-Correlation-Id").Single();

        // no Idempotency-Key header => business rule violation (400), nothing is charged
        var res = await client.PostAsJsonAsync("/api/payments/pay", new { policyId = 1, mode = "UPI", reference = "a@b" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        var problem = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(correlation, problem.GetProperty("correlationId").GetString());

        // the failed attempt is in the audit trail, found by its correlation id
        var audit = await client.GetFromJsonAsync<JsonElement>($"/api/audit-logs?correlationId={correlation}");
        var row = Assert.Single(audit.GetProperty("items").EnumerateArray());
        Assert.Equal("POST", row.GetProperty("method").GetString());
        Assert.Equal(400, row.GetProperty("statusCode").GetInt32());
        Assert.Equal("admin", row.GetProperty("userName").GetString());
    }

    [Fact]
    public async Task Uploading_a_disguised_file_is_rejected_before_anything_is_stored()
    {
        var client = await LoginAsync("admin", "Admin@123");
        using var form = new MultipartFormDataContent
        {
            { new StringContent("policy"), "entityType" }, { new StringContent("21"), "entityId" }, { new StringContent("POLICY_COPY"), "documentType" },
            { new ByteArrayContent("not really a png"u8.ToArray()), "file", "fake.png" },
        };
        var res = await client.PostAsync("/api/documents", form);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        Assert.Contains("does not match", await res.Content.ReadAsStringAsync());
    }
}
