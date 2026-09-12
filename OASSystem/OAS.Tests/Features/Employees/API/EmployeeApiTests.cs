using System.Net;
using System.Net.Http.Json;
using NUnit.Framework;
using OAS.Contracts.Features.Employees;
using OAS.Contracts.Features.Employees.JobTitles;
using OAS.Tests.Features.Employees.Integration;

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
        await TestDatabase
            .EnsureCreatedAndMigratedAsync();

        _factory =
            new EmployeeApiFactory();

        _client =
            _factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing
                    .WebApplicationFactoryClientOptions
                {
                    HandleCookies = true,
                    AllowAutoRedirect = false
                });

        _client.DefaultRequestHeaders.Add(
            "X-Test-Admin",
            "true");

        var titles =
            await _client.GetFromJsonAsync<
                JobTitleDto[]>(
                "/api/job-titles");

        _jobTitleId =
            titles?
                .FirstOrDefault(
                    x => x.IsActive)?
                .Id
            ?? Guid.Empty;

        Assert.That(
            _jobTitleId,
            Is.Not.EqualTo(Guid.Empty),
            "Migration must seed at least one active job title.");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task GetList_WithoutAuthentication_Returns401()
    {
        using var client =
            _factory.CreateClient(
                new Microsoft.AspNetCore.Mvc.Testing
                    .WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false
                });

        var response =
            await client.GetAsync(
                "/api/employees");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(
                HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task Admin_GetList_Returns200()
    {
        var response =
            await _client.GetAsync(
                "/api/employees?pageNumber=1&pageSize=20");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ReserveNumber_Twice_ReturnsDistinctFormattedEmployeeCodes()
    {
        var firstResponse =
            await _client.PostAsync(
                "/api/employees/number/reserve",
                null);

        var secondResponse =
            await _client.PostAsync(
                "/api/employees/number/reserve",
                null);

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
                .ReadFromJsonAsync<
                    EmployeeNumberReservationDto>();

        var second =
            await secondResponse.Content
                .ReadFromJsonAsync<
                    EmployeeNumberReservationDto>();

        Assert.Multiple(() =>
        {
            Assert.That(
                first,
                Is.Not.Null);

            Assert.That(
                second,
                Is.Not.Null);

            Assert.That(
                first!.EmployeeCode,
                Does.Match(
                    "^EMP-\\d{5}$"));

            Assert.That(
                second!.EmployeeCode,
                Does.Match(
                    "^EMP-\\d{5}$"));

            Assert.That(
                second.EmployeeCode,
                Is.Not.EqualTo(
                    first.EmployeeCode));
        });
    }

    [Test]
    public async Task PostValid_Returns201WithFormattedEmployeeCodeAndContactData()
    {
        var employee =
            await CreateEmployeeAsync();

        Assert.Multiple(() =>
        {
            Assert.That(
                employee.Id,
                Is.Not.EqualTo(Guid.Empty));

            Assert.That(
                employee.EmployeeCode,
                Does.Match(
                    "^EMP-\\d{5}$"));

            Assert.That(
                employee.JobTitleId,
                Is.EqualTo(_jobTitleId));

            Assert.That(
                employee.Email,
                Is.EqualTo(
                    "employee@example.com"));

            Assert.That(
                employee.Country,
                Is.EqualTo("Yemen"));
        });
    }

    [Test]
    public async Task GetUnknown_Returns404()
    {
        var response =
            await _client.GetAsync(
                $"/api/employees/{Guid.NewGuid()}");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(
                HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PutValid_Returns200()
    {
        var created =
            await CreateEmployeeAsync();

        var request =
            UpdateRequest(
                created,
                firstName: "Updated",
                city: "Aden");

        var response =
            await _client.PutAsJsonAsync(
                $"/api/employees/{created.Id}",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var employee =
            await response.Content
                .ReadFromJsonAsync<EmployeeDto>();

        Assert.Multiple(() =>
        {
            Assert.That(
                employee,
                Is.Not.Null);

            Assert.That(
                employee!.FirstName,
                Is.EqualTo("Updated"));

            Assert.That(
                employee.City,
                Is.EqualTo("Aden"));

            Assert.That(
                employee.EmployeeCode,
                Is.EqualTo(
                    created.EmployeeCode));
        });
    }

    [Test]
    public async Task PutStaleRowVersion_Returns409()
    {
        var created =
            await CreateEmployeeAsync();

        var request =
            UpdateRequest(created) with
            {
                RowVersion =
                    Convert.ToBase64String(
                        [1, 2, 3])
            };

        var response =
            await _client.PutAsJsonAsync(
                $"/api/employees/{created.Id}",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(
                HttpStatusCode.Conflict));
    }

    [Test]
    public async Task PostStatus_Returns200()
    {
        var created =
            await CreateEmployeeAsync();

        var response =
            await _client.PostAsJsonAsync(
                $"/api/employees/{created.Id}/status",
                new SetEmployeeStatusRequest(
                    false,
                    created.RowVersion));

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var employee =
            await response.Content
                .ReadFromJsonAsync<EmployeeDto>();

        Assert.That(
            employee,
            Is.Not.Null);

        Assert.That(
            employee!.IsActive,
            Is.False);

        Assert.That(
            employee.EmployeeCode,
            Is.EqualTo(
                created.EmployeeCode));
    }

    [Test]
    public async Task DeleteEndpoint_DoesNotExist()
    {
        var response =
            await _client.DeleteAsync(
                $"/api/employees/{Guid.NewGuid()}");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(
                HttpStatusCode.MethodNotAllowed));
    }

    private async Task<EmployeeDto> CreateEmployeeAsync()
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/employees",
                new CreateEmployeeRequest(
                    "Test",
                    "Employee",
                    "777000002",
                    "employee@example.com",
                    "Yemen",
                    "Sana'a",
                    "Sana'a",
                    "10001",
                    "Main Street",
                    _jobTitleId,
                    null,
                    true,
                    true));

        Assert.That(
            response.StatusCode,
            Is.EqualTo(
                HttpStatusCode.Created));

        var employee =
            await response.Content
                .ReadFromJsonAsync<EmployeeDto>();

        Assert.That(
            employee,
            Is.Not.Null);

        return employee!;
    }

    private static UpdateEmployeeRequest UpdateRequest(
        EmployeeDto employee,
        string? firstName = null,
        string? city = null) =>
        new(
            firstName ?? employee.FirstName,
            employee.LastName,
            employee.Phone,
            employee.Email,
            employee.Country,
            employee.Governorate,
            city ?? employee.City,
            employee.PostalCode,
            employee.ResidentialAddress,
            employee.JobTitleId,
            employee.HireDate,
            employee.IsCommissionEligible,
            employee.IsActive,
            employee.UserAccountId,
            employee.RowVersion);
}