using System.Linq;
using System.Reflection;
using LedgerCore.Api.Controllers;
using LedgerCore.Application.Authentication;
using LedgerCore.Domain.Entities;
using LedgerCore.Infrastructure.Database;
using NetArchTest.Rules;
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
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .And()
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .And()
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result));
    }

    [Fact]
    public void Application_Should_Not_HaveDependenciesOnInfrastructureOrApi()
    {
        var result = Types
            .InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .And()
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result));
    }

    [Fact]
    public void Infrastructure_Should_Not_HaveDependenciesOnApi()
    {
        var result = Types
            .InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        Assert.True(result.IsSuccessful, BuildFailureMessage(result));
    }

    private static string BuildFailureMessage(ArchTestResult result)
    {
        if (result.FailingTypeNames is { Count: > 0 })
        {
            return $"Dependency rule violation - failing types: {string.Join(", ", result.FailingTypeNames)}";
        }

        return "Dependency rule violation detected (no type details available).";
    }
}
