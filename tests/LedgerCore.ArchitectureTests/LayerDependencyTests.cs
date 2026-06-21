using NetArchTest.Rules;
using System.Linq;
using System.Reflection;
using LedgerCore.Api.Controllers;
using LedgerCore.Application.Authentication;
using LedgerCore.Domain.Entities;
using LedgerCore.Infrastructure.Database;
using Xunit;

namespace LedgerCore.ArchitectureTests;

public class LayerDependencyTests
{
    private const string DomainNamespace = "LedgerCore.Domain";
    private const string ApplicationNamespace = "LedgerCore.Application";
    private const string InfrastructureNamespace = "LedgerCore.Infrastructure";
    private const string ApiNamespace = "LedgerCore.Api";

    private static Assembly DomainAssembly => typeof(Account).Assembly;
    private static Assembly ApplicationAssembly => typeof(IJwtProvider).Assembly;
    private static Assembly InfrastructureAssembly => typeof(LedgerDbContext).Assembly;
    private static Assembly ApiAssembly => typeof(AccountsController).Assembly;

    [Fact]
    public void Domain_Should_Not_HaveDependenciesOnOtherProjects()
    {
        var result = Types
            .InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOn(ApplicationNamespace)
            .And()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .And()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_Should_Not_HaveDependenciesOnInfrastructureOrApi()
    {
        var result = Types
            .InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOn(InfrastructureNamespace)
            .And()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_Should_Not_HaveDependenciesOnApi()
    {
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .Should()
            .NotHaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

}
