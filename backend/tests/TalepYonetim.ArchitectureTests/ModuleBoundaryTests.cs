using System.Reflection;
using Microsoft.EntityFrameworkCore;
using NetArchTest.Rules;
using Shouldly;
using TalepYonetim.Identity;
using TalepYonetim.Reporting;

namespace TalepYonetim.ArchitectureTests;

/// <summary>docs/architecture.md, "Forbidden dependencies" rules 1–4.</summary>
public sealed class ModuleBoundaryTests
{
    private sealed record Module(string Name, Assembly Assembly, string RootNamespace, string? Schema);

    private static readonly Module[] Modules =
    [
        new("Identity", typeof(IdentityModule).Assembly, "TalepYonetim.Identity", "identity"),
        new("Reporting", typeof(ReportingModule).Assembly, "TalepYonetim.Reporting", null),
    ];

    public static TheoryData<string, string> ModulePairs()
    {
        var data = new TheoryData<string, string>();
        foreach (var from in Modules)
        {
            foreach (var to in Modules.Where(m => m != from))
            {
                data.Add(from.Name, to.Name);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(ModulePairs))]
    public void Rule1_ModuleUsesOtherModuleOnlyThroughContracts(string fromName, string toName)
    {
        var from = Modules.Single(m => m.Name == fromName);
        var to = Modules.Single(m => m.Name == toName);
        var forbidden = to.Assembly.GetTypes()
            .Where(t => !t.Name.Contains('<', StringComparison.Ordinal))
            .Where(t => t.Namespace is not null
                        && !t.Namespace.StartsWith(to.RootNamespace + ".Contracts", StringComparison.Ordinal))
            .Select(t => t.FullName!)
            .ToArray();
        forbidden.ShouldNotBeEmpty();

        var result = Types.InAssembly(from.Assembly).ShouldNot().HaveDependencyOnAny(forbidden).GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Rule1_IsExercised_ReportingReadsIdentityThroughContracts()
    {
        Types.InAssembly(typeof(ReportingModule).Assembly)
            .That().HaveDependencyOn("TalepYonetim.Identity.Contracts")
            .GetTypes()
            .ShouldNotBeEmpty();
    }

    [Fact]
    public void Rule2_EveryDbContextMapsOnlyItsOwnModuleEntitiesInItsOwnSchema()
    {
        var contexts = 0;
        foreach (var module in Modules)
        {
            foreach (var contextType in module.Assembly.GetTypes().Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract))
            {
                contexts++;
                module.Schema.ShouldNotBeNull($"{module.Name} has a DbContext but no schema of its own");
                using var context = CreateContext(contextType);

                foreach (var entity in context.Model.GetEntityTypes())
                {
                    entity.GetSchema().ShouldBe(module.Schema, $"{entity.Name} in {contextType.Name}");
                    entity.ClrType.Namespace.ShouldNotBeNull();
                    entity.ClrType.Namespace.ShouldStartWith(module.RootNamespace, Case.Sensitive, $"{entity.Name} in {contextType.Name}");
                }
            }
        }

        contexts.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void Rule3_DomainDependsOnNoInfrastructurePackage()
    {
        var domain = Types.InAssemblies(Modules.Select(m => m.Assembly))
            .That().ResideInNamespaceMatching(@"^TalepYonetim\.\w+\.Domain(\..*)?$");
        domain.GetTypes().ShouldNotBeEmpty();

        var result = domain.ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Rule4_ReportingCannotWrite()
    {
        var result = Types.InAssembly(typeof(ReportingModule).Assembly)
            .ShouldNot().HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static DbContext CreateContext(Type contextType)
    {
        var builder = (DbContextOptionsBuilder)Activator.CreateInstance(typeof(DbContextOptionsBuilder<>).MakeGenericType(contextType))!;
        builder.UseNpgsql("Host=model-only"); // the model is built without connecting
        return (DbContext)Activator.CreateInstance(contextType, builder.Options)!;
    }

    private static string Describe(NetArchTest.Rules.TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
