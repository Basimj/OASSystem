using NUnit.Framework;
using OAS.Contracts.Common.Errors;
using OAS.Contracts.Common.Pagination;
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

    [OneTimeSetUp]
    public async Task Setup()
    {
        await TestDatabase.EnsureCreatedAndMigratedAsync();

        _factory = new EmployeeApiFactory();

        _client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });

        _client.DefaultRequestHeaders.Add(
            "X-Test-Admin",
            "true");
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
        using var client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

        var response =
            await client.GetAsync("/api/employees");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Unauthorized));
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
    public async Task PostValid_Returns201()
    {
       

        var request = new CreateEmployeeRequest(
            $"API-{Guid.NewGuid():N}"[..10],
            "Api",
            "Employee",
            "777000001",
            "Sales",
            null,
            null,
            true,
            false,
            true,
            true,
            null);

        var response =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        var employee =
            await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.That(employee, Is.Not.Null);
        Assert.That(employee!.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public async Task PostDuplicateCode_Returns409()
    {
        

        var code =
            $"DUP-{Guid.NewGuid():N}"[..10];

        var request = new CreateEmployeeRequest(
            code,
            "First",
            "Employee",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            true,
            null);

        var first =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.That(
            first.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        var second =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.That(
            second.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task GetUnknown_Returns404()
    {
       

        var response =
            await _client.GetAsync(
                $"/api/employees/{Guid.NewGuid()}");

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task PutValid_Returns200()
    {
        

        var created =
            await CreateEmployeeAsync();

        var request = new UpdateEmployeeRequest(
            created.EmployeeCode,
            "Updated",
            "Employee",
            created.Phone,
            created.JobTitle,
            created.HireDate,
            created.Notes,
            created.IsSalesperson,
            created.IsTechnician,
            created.IsCommissionEligible,
            null,
            created.RowVersion);

        var response =
            await _client.PutAsJsonAsync(
                $"/api/employees/{created.Id}",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var employee =
            await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.That(employee, Is.Not.Null);
        Assert.That(
            employee!.FirstName,
            Is.EqualTo("Updated"));
    }

    [Test]
    public async Task PutStaleRowVersion_Returns409()
    {
        

        var created =
            await CreateEmployeeAsync();

        var request = new UpdateEmployeeRequest(
            created.EmployeeCode,
            "Updated",
            "Employee",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            null,
            Convert.ToBase64String([1, 2, 3]));

        var response =
            await _client.PutAsJsonAsync(
                $"/api/employees/{created.Id}",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Conflict));
    }

    [Test]
    public async Task PostStatus_Returns200()
    {
      

        var created =
            await CreateEmployeeAsync();

        var request = new SetEmployeeStatusRequest(
            false,
            created.RowVersion);

        var response =
            await _client.PostAsJsonAsync(
                $"/api/employees/{created.Id}/status",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK));

        var employee =
            await response.Content.ReadFromJsonAsync<EmployeeDto>();

        Assert.That(employee, Is.Not.Null);
        Assert.That(employee!.IsActive, Is.False);
    }

    [Test]
    public async Task DeleteEndpoint_DoesNotExist()
    {
      

        var response =
            await _client.DeleteAsync(
                $"/api/employees/{Guid.NewGuid()}");

        Assert.That(
       response.StatusCode,
       Is.EqualTo(HttpStatusCode.MethodNotAllowed));
    }

    

    private async Task<EmployeeDto> CreateEmployeeAsync()
    {
        var request = new CreateEmployeeRequest(
            $"API-{Guid.NewGuid():N}"[..10],
            "Test",
            "Employee",
            "777000002",
            "Sales",
            null,
            null,
            true,
            false,
            true,
            true,
            null);

        var response =
            await _client.PostAsJsonAsync(
                "/api/employees",
                request);

        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.Created));

        return (await response.Content
            .ReadFromJsonAsync<EmployeeDto>())!;
    }
}