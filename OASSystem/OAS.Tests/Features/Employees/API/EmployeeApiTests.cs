
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using OAS.Contracts.Features.Employees;
using OAS.Tests.Features.Employees.Integration;
using System.Net;
using System.Net.Http.Json;

namespace OAS.Tests.Features.Employees.API;

[TestFixture]
public sealed class EmployeeApiTests
{
    private EmployeeApiFactory _factory = null!;
    private HttpClient _client = null!;
    private Guid _jobTitleId;

    [OneTimeSetUp]
    public async Task Setup()
    {
        // Ensure the test database exists and all migrations are applied.
        await TestDatabase.EnsureCreatedAndMigratedAsync();

        _factory = new EmployeeApiFactory();

        _client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });

        // The TestAuthenticationHandler creates the Administrator
        // identity when this header is present.
        _client.DefaultRequestHeaders.Add(
            "X-Test-Admin",
            "true");

        // Do not call /api/job-titles here.
        //
        // JobTitlesController itself is protected by:
        // [Authorize(Roles = "Administrator")]
        //
        // Setup should not depend on another protected API.
        // Read an active JobTitle directly from the test database instead.
        _jobTitleId = await GetActiveJobTitleIdAsync();

        Assert.That(
            _jobTitleId,
            Is.Not.EqualTo(Guid.Empty),
            "Test database must contain at least one active job title.");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    // =========================================================
    // Authentication
    // =========================================================

    [Test]
    public async Task GetList_WithoutAuthentication_Returns401()
    {
        using var client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var response = await client.GetAsync(
            "/api/employees");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    // =========================================================
    // GET
    // =========================================================

    [Test]
    public async Task Admin_GetList_Returns200()
    {
        var response = await _client.GetAsync(
            "/api/employees?pageNumber=1&pageSize=20");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetUnknown_Returns404()
    {
        var response = await _client.GetAsync(
            $"/api/employees/{Guid.NewGuid()}");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    // =========================================================
    // Employee Code Reservation
    // =========================================================

    [Test]
    public async Task ReserveNumber_Twice_ReturnsDistinctFormattedEmployeeCodes()
    {
        var firstResponse = await _client.PostAsync(
            "/api/employees/number/reserve",
            content: null);

        var secondResponse = await _client.PostAsync(
            "/api/employees/number/reserve",
            content: null);

        Assert.Multiple(() =>
        {
            Assert.That(
                firstResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK));

            Assert.That(
                secondResponse.StatusCode,
                Is.EqualTo(HttpStatusCode.OK));
        });

        var first =
            await firstResponse.Content
                .ReadFromJsonAsync<EmployeeNumberReservationDto>();

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<EmployeeNumberReservationDto>();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);

            Assert.That(
                first!.EmployeeCode,
                Does.Match(@"^EMP-\d{5,}$"));

            Assert.That(
                second!.EmployeeCode,
                Does.Match(@"^EMP-\d{5,}$"));

            Assert.That(
                second.EmployeeCode,
                Is.Not.EqualTo(first.EmployeeCode));
        });
    }

    // =========================================================
    // POST
    // =========================================================

    [Test]
    public async Task PostValid_Returns201WithFormattedEmployeeCodeAndContactData()
    {
        var employee = await CreateEmployeeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                employee.Id,
                Is.Not.EqualTo(Guid.Empty));

            Assert.That(
                employee.EmployeeCode,
                Is.Not.Null.And.Not.Empty);

            Assert.That(
                employee.EmployeeCode,
                Does.Match(@"^EMP-\d{5,}$"));

            Assert.That(
                employee.JobTitleId,
                Is.EqualTo(_jobTitleId));

            Assert.That(
                employee.Email,
                Is.EqualTo("employee@example.com"));

            Assert.That(
                employee.Country,
                Is.EqualTo("Yemen"));
        });
    }

    // =========================================================
    // PUT
    // =========================================================

    [Test]
    public async Task PutValid_Returns200()
    {
        var created = await CreateEmployeeAsync();

        var request = UpdateRequest(
            created,
            firstName: "Updated",
            city: "Aden");

        var response = await _client.PutAsJsonAsync(
            $"/api/employees/{created.Id}",
            request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var employee =
            await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.That(
            employee,
            Is.Not.Null);

        Assert.Multiple(() =>
        {
            Assert.That(
                employee!.FirstName,
                Is.EqualTo("Updated"));

            Assert.That(
                employee.City,
                Is.EqualTo("Aden"));

            // EmployeeCode must remain unchanged during update.
            Assert.That(
                employee.EmployeeCode,
                Is.EqualTo(created.EmployeeCode));
        });
    }

    [Test]
    public async Task PutStaleRowVersion_Returns409()
    {
        var created = await CreateEmployeeAsync();

        var request =
            UpdateRequest(created) with
            {
                RowVersion =
                    Convert.ToBase64String([1, 2, 3])
            };

        var response = await _client.PutAsJsonAsync(
            $"/api/employees/{created.Id}",
            request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }

    // =========================================================
    // STATUS / DEACTIVATION
    // =========================================================

    [Test]
    public async Task PostStatus_Returns200()
    {
        var created = await CreateEmployeeAsync();

        var response = await _client.PostAsJsonAsync(
            $"/api/employees/{created.Id}/status",
            new SetEmployeeStatusRequest(
                false,
                created.RowVersion));

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var employee =
            await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.That(
            employee,
            Is.Not.Null);

        Assert.That(
            employee!.IsActive,
            Is.False);
    }

    // =========================================================
    // DELETE MUST NOT EXIST
    // =========================================================

    [Test]
    public async Task DeleteEndpoint_DoesNotExist()
    {
        var response = await _client.DeleteAsync(
            $"/api/employees/{Guid.NewGuid()}");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.MethodNotAllowed));
    }

    // =========================================================
    // Helpers
    // =========================================================

    private async Task<Guid> GetActiveJobTitleIdAsync()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                SELECT TOP (1) [Id]
                FROM [hr].[JobTitles]
                WHERE [IsActive] = 1
                ORDER BY [Name], [Id];
                """,
                connection);

        var result =
            await command.ExecuteScalarAsync();

        if (result is Guid id)
        {
            return id;
        }

        return Guid.Empty;
    }

    private async Task<EmployeeDto> CreateEmployeeAsync()
    {
        var request = new CreateEmployeeRequest(
            FirstName: "Test",
            LastName: "Employee",
            Phone: "777000002",
            Email: "employee@example.com",
            Country: "Yemen",
            Governorate: "Sana'a",
            City: "Sana'a",
            PostalCode: "10001",
            ResidentialAddress: "Main Street",
            JobTitleId: _jobTitleId,
            HireDate: null,
            IsCommissionEligible: true,
            IsActive: true,
            UserAccountId: null,

            // Leave EmployeeCode null.
            //
            // The application must reserve the sequence number
            // and generate a formatted value such as:
            // EMP-00001
            EmployeeCode: null);

        var response =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created),
            await ReadErrorMessageAsync(response));

        var employee =
            await response.Content
                .ReadFromJsonAsync<EmployeeDto>();

        Assert.That(
            employee,
            Is.Not.Null,
            "Created employee response must contain EmployeeDto.");

        return employee!;
    }

    private static UpdateEmployeeRequest UpdateRequest(
        EmployeeDto employee,
        string? firstName = null,
        string? city = null)
    {
        return new UpdateEmployeeRequest(
            FirstName:
                firstName ?? employee.FirstName,

            LastName:
                employee.LastName,

            Phone:
                employee.Phone,

            Email:
                employee.Email,

            Country:
                employee.Country,

            Governorate:
                employee.Governorate,

            City:
                city ?? employee.City,

            PostalCode:
                employee.PostalCode,

            ResidentialAddress:
                employee.ResidentialAddress,

            JobTitleId:
                employee.JobTitleId,

            HireDate:
                employee.HireDate,

            IsCommissionEligible:
                employee.IsCommissionEligible,

            IsActive:
                employee.IsActive,

            UserAccountId:
                employee.UserAccountId,

            RowVersion:
                employee.RowVersion);
    }

    private static async Task<string> ReadErrorMessageAsync(
        HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        return string.IsNullOrWhiteSpace(body)
            ? $"Unexpected status code: {response.StatusCode}"
            : $"Unexpected status code: {response.StatusCode}. Response: {body}";
    }
}
