﻿using Microsoft.Extensions.DependencyInjection;
using NPoco;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Exceptions;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Install;
using Umbraco.Cms.Infrastructure.Migrations.Notifications;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Persistence.DatabaseAnnotations;
using Umbraco.Cms.Infrastructure.Scoping;
using Umbraco.Cms.Tests.Common.Testing;
using Umbraco.Cms.Tests.Integration.Testing;

namespace Umbraco.Cms.Tests.Integration.Umbraco.Infrastructure.Migrations;

// These tests depend on the key-value table, so we need a schema to run these tests.
[UmbracoTest(Database = UmbracoTestOptions.Database.NewSchemaPerTest)]
[TestFixture]
public class PartialMigrationsTests : UmbracoIntegrationTest
{
    public const string TableName = "testTable";
    public const string ColumnName = "testColumn";

    private IMigrationPlanExecutor MigrationPlanExecutor => GetRequiredService<IMigrationPlanExecutor>();

    private IKeyValueService KeyValueService => GetRequiredService<IKeyValueService>();

    [TearDown]
    public void ResetMigration()
    {
        ErrorMigration.ShouldExplode = true;
        UmbracoPlanExecutedTestNotificationHandler.HandleNotification = null;
    }

    protected override void ConfigureTestServices(IServiceCollection services)
        => services.AddNotificationHandler<UmbracoPlanExecutedNotification, UmbracoPlanExecutedTestNotificationHandler>();

    [Test]
    public void CanRerunPartiallyCompletedMigration()
    {
        var plan = new MigrationPlan("test")
            .From(string.Empty)
            .To<CreateTableMigration>("a")
            .To<ErrorMigration>("b")
            .To<AddColumnMigration>("c");

        var upgrader = new Upgrader(plan);

        var result = upgrader.Execute(MigrationPlanExecutor, ScopeProvider, KeyValueService);

        Assert.Multiple(() =>
        {
            Assert.IsFalse(result.Successful);
            Assert.AreEqual(string.Empty, result.InitialState);
            Assert.AreEqual("a", result.FinalState);
            Assert.AreEqual(1, result.CompletedTransitions.Count);
            Assert.IsNotNull(result.Exception);

            // Ensure that the partial success is saved in the keyvalue service so next plan execution starts correctly.
            using var scope = ScopeProvider.CreateScope(autoComplete: true);
            Assert.AreEqual("a", KeyValueService.GetValue(upgrader.StateValueKey));
            // Ensure that the changes from the first migration is persisted
            Assert.IsTrue(scope.Database.HasTable(TableName));
            // But that the final migration wasn't run
            Assert.IsFalse(ColumnExists(TableName, ColumnName, scope));
        });

        // Now let's simulate that someone came along and fixed the broken migration and we'll now try and rerun
        ErrorMigration.ShouldExplode = false;
        upgrader = new Upgrader(plan);
        result = upgrader.Execute(MigrationPlanExecutor, ScopeProvider, KeyValueService);

        Assert.Multiple(() =>
        {
            Assert.AreEqual("a", result.InitialState);
            Assert.IsTrue(result.Successful);
            Assert.IsNull(result.Exception);
            Assert.AreEqual(2, result.CompletedTransitions.Count);
            Assert.AreEqual("c", result.FinalState);

            // Ensure that everything got updated in the database.
            using var scope = ScopeProvider.CreateScope(autoComplete: true);
            Assert.AreEqual("c", KeyValueService.GetValue(upgrader.StateValueKey));
            Assert.IsTrue(scope.Database.HasTable(TableName));
            Assert.IsTrue(ColumnExists(TableName, ColumnName, scope));
        });
    }

    [Test]
    public void StateIsOnlySavedIfAMigrationSucceeds()
    {
        var plan = new MigrationPlan("test")
            .From(string.Empty)
            .To<ErrorMigration>("a")
            .To<CreateTableMigration>("b");

        var upgrader = new Upgrader(plan);
        var result = upgrader.Execute(MigrationPlanExecutor, ScopeProvider, KeyValueService);

        Assert.Multiple(() =>
        {
            Assert.IsFalse(result.Successful);
            Assert.IsNotNull(result.Exception);
            Assert.IsInstanceOf<PanicException>(result.Exception);
            Assert.IsEmpty(result.CompletedTransitions);
            Assert.AreEqual(string.Empty, result.InitialState);
            Assert.AreEqual(string.Empty, result.FinalState);

            using var scope = ScopeProvider.CreateCoreScope();
            Assert.IsNull(KeyValueService.GetValue(upgrader.StateValueKey));
        });
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void UmbracoPlanExecutedNotificationIsAlwaysPublished(bool shouldSucceed)
    {
        var notificationPublished = false;
        ErrorMigration.ShouldExplode = shouldSucceed is false;

        UmbracoPlanExecutedTestNotificationHandler.HandleNotification += notification =>
        {
            notificationPublished = true;
            Assert.Multiple(() =>
            {
                var executedPlan = notification.ExecutedPlan;

                if (shouldSucceed)
                {
                    Assert.IsTrue(executedPlan.Successful);
                    Assert.IsNull(executedPlan.Exception);
                    Assert.AreEqual("c", executedPlan.FinalState);
                    Assert.AreEqual(3, executedPlan.CompletedTransitions.Count);
                }
                else
                {
                    Assert.IsFalse(executedPlan.Successful);
                    Assert.IsNotNull(executedPlan.Exception);
                    Assert.IsInstanceOf<PanicException>(executedPlan.Exception);
                    Assert.AreEqual("a", executedPlan.FinalState);
                    Assert.AreEqual(1, executedPlan.CompletedTransitions.Count);
                }
            });
        };

        // We have to use the DatabaseBuilder otherwise the notification isn't published
        var databaseBuilder = GetRequiredService<DatabaseBuilder>();
        var plan = new TestUmbracoPlan(null!);
        databaseBuilder.UpgradeSchemaAndData(plan);

        Assert.IsTrue(notificationPublished);
    }

    private bool ColumnExists(string tableName, string columnName, IScope scope) =>
        scope.Database.SqlContext.SqlSyntax.GetColumnsInSchema(scope.Database)
            .Any(x => x.TableName.Equals(tableName) && x.ColumnName.Equals(columnName));
}


// This is just some basic migrations to test the migration plans...
public class ErrorMigration : MigrationBase
{
    // Used to determine if an exception should be thrown, used to test re-running migrations
    public static bool ShouldExplode { get; set; } = true;

    public ErrorMigration(IMigrationContext context) : base(context)
    {
    }

    protected override void Migrate()
    {
        if (ShouldExplode)
        {
            throw new PanicException();
        }
    }
}

public class CreateTableMigration : MigrationBase
{
    public CreateTableMigration(IMigrationContext context) : base(context)
    {
    }

    protected override void Migrate() => Create.Table<TestDto>().Do();
}

public class AddColumnMigration : MigrationBase
{
    public AddColumnMigration(IMigrationContext context) : base(context)
    {
    }

    protected override void Migrate() => Create
        .Column(PartialMigrationsTests.ColumnName)
        .OnTable(PartialMigrationsTests.TableName)
        .AsString()
        .Do();
}

[TableName(PartialMigrationsTests.TableName)]
[PrimaryKey("id", AutoIncrement = true)]
public class TestDto
{
    [Column("id")]
    [PrimaryKeyColumn(Name = "PK_testTable")]
    public int Id { get; set; }
}

public class UmbracoPlanExecutedTestNotificationHandler : INotificationHandler<UmbracoPlanExecutedNotification>
{
    public static Action<UmbracoPlanExecutedNotification>? HandleNotification { get; set; }

    public void Handle(UmbracoPlanExecutedNotification notification) => HandleNotification?.Invoke(notification);
}


/// <summary>
/// This is a fake UmbracoPlan used for testing of the DatabaseBuilder, this overrides everything to be of type
/// UmbracoPlan but behave like a normal migration plan.
/// </summary>
public class TestUmbracoPlan : UmbracoPlan
{
    public TestUmbracoPlan(IUmbracoVersion umbracoVersion) : base(umbracoVersion)
    {
    }

    public override string InitialState => string.Empty;

    public override bool IgnoreCurrentState => true;

    protected override void DefinePlan()
    {
        From(InitialState);
        To<CreateTableMigration>("a");
        To<ErrorMigration>("b");
        To<AddColumnMigration>("c");
    }
}