﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using BrightChain.EntityFrameworkCore.Infrastructure;
using BrightChain.EntityFrameworkCore.Storage.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore.Update;

namespace BrightChain.EntityFrameworkCore.TestUtilities
{
    public class BrightChainTestStore : TestStore
    {
        private readonly TestStoreContext _storeContext;
        private readonly string _dataFilePath;
        private readonly Action<BrightChainDbContextOptionsBuilder> _configureBrightChain;
        private bool _initialized;

        private static readonly Guid _runId = Guid.NewGuid();
        private static bool? _connectionAvailable;

        public static BrightChainTestStore Create(string name, Action<BrightChainDbContextOptionsBuilder> extensionConfiguration = null)
        {
            return new(name, shared: false, extensionConfiguration: extensionConfiguration);
        }

        public static BrightChainTestStore CreateInitialized(string name, Action<BrightChainDbContextOptionsBuilder> extensionConfiguration = null)
        {
            return (BrightChainTestStore)Create(name, extensionConfiguration).Initialize(null, (Func<DbContext>)null);
        }

        public static BrightChainTestStore GetOrCreate(string name)
        {
            return new(name);
        }

        public static BrightChainTestStore GetOrCreate(string name, string dataFilePath)
        {
            return new(name, dataFilePath: dataFilePath);
        }

        private BrightChainTestStore(
            string name,
            bool shared = true,
            string dataFilePath = null,
            Action<BrightChainDbContextOptionsBuilder> extensionConfiguration = null)
            : base(CreateName(name), shared)
        {
            ConnectionUri = TestEnvironment.DefaultConnection;
            AuthToken = TestEnvironment.AuthToken;
            ConnectionString = TestEnvironment.ConnectionString;
            _configureBrightChain = extensionConfiguration == null
                ? (b => b.ApplyConfiguration())
                : (b =>
                {
                    b.ApplyConfiguration();
                    extensionConfiguration(b);
                });

            _storeContext = new TestStoreContext(this);

            if (dataFilePath != null)
            {
                _dataFilePath = Path.Combine(
                    Path.GetDirectoryName(typeof(BrightChainTestStore).Assembly.Location),
                    dataFilePath);
            }
        }

        private static string CreateName(string name)
        {
            return TestEnvironment.IsEmulator || name == "Northwind"
                           ? name
                           : name + _runId;
        }

        public string ConnectionUri { get; }
        public string AuthToken { get; }
        public string ConnectionString { get; }

        protected override DbContext CreateDefaultContext()
        {
            return new TestStoreContext(this);
        }

        public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        {
            return builder.UseBrightChain(
                           ConnectionUri,
                           AuthToken,
                           Name,
                           _configureBrightChain);
        }

        public static async ValueTask<bool> IsConnectionAvailableAsync()
        {
            if (_connectionAvailable == null)
            {
                _connectionAvailable = await TryConnectAsync();
            }

            return _connectionAvailable.Value;
        }

        private static async Task<bool> TryConnectAsync()
        {
            BrightChainTestStore testStore = null;
            try
            {
                testStore = CreateInitialized("NonExistent");

                return true;
            }
            catch (AggregateException aggregate)
            {
                if (aggregate.Flatten().InnerExceptions.Any(IsNotConfigured))
                {
                    return false;
                }

                throw;
            }
            catch (Exception e)
            {
                if (IsNotConfigured(e))
                {
                    return false;
                }

                throw;
            }
            finally
            {
                if (testStore != null)
                {
                    await testStore.DisposeAsync();
                }
            }
        }

        private static bool IsNotConfigured(Exception exception)
        {
            return exception switch
            {
                HttpRequestException re => re.InnerException is SocketException // Exception in Mac/Linux
                    || (re.InnerException is IOException ioException && ioException.InnerException is SocketException), // Exception in Windows
                _ => exception.Message.Contains(
                    "The input authorization token can't serve the request. Please check that the expected payload is built as per the protocol, and check the key being used.",
                    StringComparison.Ordinal),
            };
        }

        protected override void Initialize(Func<DbContext> createContext, Action<DbContext> seed, Action<DbContext> clean)
        {
            _initialized = true;

            if (_connectionAvailable == false)
            {
                return;
            }

            if (_dataFilePath == null)
            {
                base.Initialize(createContext ?? (() => _storeContext), seed, clean);
            }
            else
            {
                using var context = createContext();
                CreateFromFile(context).GetAwaiter().GetResult();
            }
        }

        private async Task CreateFromFile(DbContext context)
        {
            if (await context.Database.EnsureCreatedAsync())
            {
                var brightChainClient = context.GetService<IBrightChainClientWrapper>();
                using var fs = new FileStream(_dataFilePath, FileMode.Open, FileAccess.Read);
                using var sr = new StreamReader(fs);
                throw new NotImplementedException();
                //using var reader = new JsonTextReader(sr);
                //while (reader.Read())
                //{
                //    if (reader.TokenType == JsonToken.StartArray)
                //    {
                //        NextEntityType:
                //        while (reader.Read())
                //        {
                //            if (reader.TokenType == JsonToken.StartObject)
                //            {
                //                string entityName = null;
                //                while (reader.Read())
                //                {
                //                    if (reader.TokenType == JsonToken.PropertyName)
                //                    {
                //                        switch (reader.Value)
                //                        {
                //                            case "Name":
                //                                reader.Read();
                //                                entityName = (string)reader.Value;
                //                                break;
                //                            case "Data":
                //                                while (reader.Read())
                //                                {
                //                                    if (reader.TokenType == JsonToken.StartObject)
                //                                    {
                //                                        var document = JsonSerializer.Deserialize<JsonNode>(reader);

                //                                        document["id"] = $"{entityName}|{document["id"]}";
                //                                        document["Discriminator"] = entityName;

                //                                        await brightChainClient.CreateItemAsync(
                //                                            "NorthwindContext", document, new FakeUpdateEntry());
                //                                    }
                //                                    else if (reader.TokenType == JsonToken.EndObject)
                //                                    {
                //                                        goto NextEntityType;
                //                                    }
                //                                }

                //                                break;
                //                        }
                //                    }
                //                }
                //            }
                //        }
                //    }
                //}
            }
        }

        public override void Clean(DbContext context)
        {
            CleanAsync(context).GetAwaiter().GetResult();
        }

        public override async Task CleanAsync(DbContext context)
        {
            var brightChainClientWrapper = context.GetService<IBrightChainClientWrapper>();
            var created = await brightChainClientWrapper.CreateDatabaseIfNotExistsAsync();
            try
            {
                if (!created)
                {
                    var brightChainClient = context.Database.GetBrightChainClient();
                    var database = brightChainClient.GetDatabase(Name);
                    throw new NotImplementedException();
                    //var containerIterator = database.GetContainerQueryIterator<ContainerProperties>();
                    //while (containerIterator.HasMoreResults)
                    //{
                    //    foreach (var containerProperties in await containerIterator.ReadNextAsync())
                    //    {
                    //        var container = database.GetContainer(containerProperties.Id);
                    //        var partitionKey = containerProperties.PartitionKeyPath[1..];
                    //        var itemIterator = container.GetItemQueryIterator<JObject>(
                    //            new QueryDefinition("SELECT * FROM c"));

                    //        var items = new List<(string Id, string PartitionKey)>();
                    //        while (itemIterator.HasMoreResults)
                    //        {
                    //            foreach (var item in await itemIterator.ReadNextAsync())
                    //            {
                    //                items.Add((item["id"].ToString(), item[partitionKey]?.ToString()));
                    //            }
                    //        }

                    //        foreach (var item in items)
                    //        {
                    //            await container.DeleteItemAsync<object>(
                    //                item.Id,
                    //                item.PartitionKey == null ? PartitionKey.None : new PartitionKey(item.PartitionKey));
                    //        }
                    //    }
                    //}

                    created = await context.Database.EnsureCreatedAsync();
                    if (!created)
                    {
                        var creator = (BrightChainDatabaseCreator)context.GetService<IDatabaseCreator>();
                        await creator.SeedAsync();
                    }
                }
                else
                {
                    await context.Database.EnsureCreatedAsync();
                }
            }
            catch (Exception)
            {
                try
                {
                    await context.Database.EnsureDeletedAsync();
                }
                catch (Exception)
                {
                }

                throw;
            }
        }

        public override void Dispose()
        {
            throw new InvalidOperationException("Calling Dispose can cause deadlocks. Use DisposeAsync instead.");
        }

        public override async Task DisposeAsync()
        {
            if (_initialized
                && _dataFilePath == null)
            {
                if (_connectionAvailable == false)
                {
                    return;
                }

                if (Shared)
                {
                    GetTestStoreIndex(ServiceProvider).RemoveShared(GetType().Name + Name);
                }

                await _storeContext.Database.EnsureDeletedAsync();
            }

            _storeContext.Dispose();
        }

        private class TestStoreContext : DbContext
        {
            private readonly BrightChainTestStore _testStore;

            public TestStoreContext(BrightChainTestStore testStore)
            {
                _testStore = testStore;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseBrightChain(_testStore.ConnectionUri, _testStore.AuthToken, _testStore.Name, _testStore._configureBrightChain);
            }
        }

        private class FakeUpdateEntry : IUpdateEntry
        {
            public IEntityType EntityType
                => new FakeEntityType();

            public EntityState EntityState { get => EntityState.Added; set => throw new NotImplementedException(); }

            public IUpdateEntry SharedIdentityEntry
                => throw new NotImplementedException();

            public object GetCurrentValue(IPropertyBase propertyBase)
            {
                throw new NotImplementedException();
            }

            public TProperty GetCurrentValue<TProperty>(IPropertyBase propertyBase)
            {
                throw new NotImplementedException();
            }

            public object GetOriginalValue(IPropertyBase propertyBase)
            {
                throw new NotImplementedException();
            }

            public TProperty GetOriginalValue<TProperty>(IProperty property)
            {
                throw new NotImplementedException();
            }

            public bool HasTemporaryValue(IProperty property)
            {
                throw new NotImplementedException();
            }

            public bool IsModified(IProperty property)
            {
                throw new NotImplementedException();
            }

            public bool IsStoreGenerated(IProperty property)
            {
                throw new NotImplementedException();
            }

            public void SetOriginalValue(IProperty property, object value)
            {
                throw new NotImplementedException();
            }

            public void SetPropertyModified(IProperty property)
            {
                throw new NotImplementedException();
            }

            public void SetStoreGeneratedValue(IProperty property, object value)
            {
                throw new NotImplementedException();
            }

            public EntityEntry ToEntityEntry()
            {
                throw new NotImplementedException();
            }

            public object GetRelationshipSnapshotValue(IPropertyBase propertyBase)
            {
                throw new NotImplementedException();
            }

            public object GetPreStoreGeneratedCurrentValue(IPropertyBase propertyBase)
            {
                throw new NotImplementedException();
            }

            public bool IsConceptualNull(IProperty property)
            {
                throw new NotImplementedException();
            }
        }

        public class FakeEntityType : Annotatable, IEntityType
        {
            public IEntityType BaseType
                => throw new NotImplementedException();

            public string DefiningNavigationName
                => throw new NotImplementedException();

            public IEntityType DefiningEntityType
                => throw new NotImplementedException();

            public IModel Model
                => throw new NotImplementedException();

            public string Name
                => throw new NotImplementedException();

            public Type ClrType
                => throw new NotImplementedException();

            public bool HasSharedClrType
                => throw new NotImplementedException();

            public bool IsPropertyBag
                => throw new NotImplementedException();

            public InstantiationBinding ConstructorBinding
                => throw new NotImplementedException();

            IReadOnlyEntityType IReadOnlyEntityType.BaseType
                => throw new NotImplementedException();

            IReadOnlyModel IReadOnlyTypeBase.Model
                => throw new NotImplementedException();

            public IEnumerable<IForeignKey> FindDeclaredForeignKeys(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            public INavigation FindDeclaredNavigation(string name)
            {
                throw new NotImplementedException();
            }

            public IProperty FindDeclaredProperty(string name)
            {
                throw new NotImplementedException();
            }

            public IForeignKey FindForeignKey(IReadOnlyList<IProperty> properties, IKey principalKey, IEntityType principalEntityType)
            {
                throw new NotImplementedException();
            }

            public IForeignKey FindForeignKey(
                IReadOnlyList<IReadOnlyProperty> properties, IReadOnlyKey principalKey, IReadOnlyEntityType principalEntityType)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IForeignKey> FindForeignKeys(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            public IIndex FindIndex(IReadOnlyList<IProperty> properties)
            {
                throw new NotImplementedException();
            }

            public IIndex FindIndex(string name)
            {
                throw new NotImplementedException();
            }

            public IIndex FindIndex(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            public PropertyInfo FindIndexerPropertyInfo()
            {
                throw new NotImplementedException();
            }

            public IKey FindKey(IReadOnlyList<IProperty> properties)
            {
                throw new NotImplementedException();
            }

            public IKey FindKey(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            public IKey FindPrimaryKey()
            {
                throw new NotImplementedException();
            }

            public IReadOnlyList<IReadOnlyProperty> FindProperties(IReadOnlyList<string> propertyNames)
            {
                throw new NotImplementedException();
            }

            public IProperty FindProperty(string name)
            {
                return null;
            }

            public IServiceProperty FindServiceProperty(string name)
            {
                throw new NotImplementedException();
            }

            public ISkipNavigation FindSkipNavigation(string name)
            {
                throw new NotImplementedException();
            }

            public ChangeTrackingStrategy GetChangeTrackingStrategy()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IForeignKey> GetDeclaredForeignKeys()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IIndex> GetDeclaredIndexes()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IKey> GetDeclaredKeys()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<INavigation> GetDeclaredNavigations()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IProperty> GetDeclaredProperties()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IForeignKey> GetDeclaredReferencingForeignKeys()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IServiceProperty> GetDeclaredServiceProperties()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlySkipNavigation> GetDeclaredSkipNavigations()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IForeignKey> GetDerivedForeignKeys()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IIndex> GetDerivedIndexes()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlyNavigation> GetDerivedNavigations()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlyProperty> GetDerivedProperties()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlyServiceProperty> GetDerivedServiceProperties()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlySkipNavigation> GetDerivedSkipNavigations()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IReadOnlyEntityType> GetDerivedTypes()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IEntityType> GetDirectlyDerivedTypes()
            {
                throw new NotImplementedException();
            }

            public string GetDiscriminatorPropertyName()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IProperty> GetForeignKeyProperties()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IForeignKey> GetForeignKeys()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IIndex> GetIndexes()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IKey> GetKeys()
            {
                throw new NotImplementedException();
            }

            public PropertyAccessMode GetNavigationAccessMode()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<INavigation> GetNavigations()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IProperty> GetProperties()
            {
                throw new NotImplementedException();
            }

            public PropertyAccessMode GetPropertyAccessMode()
            {
                throw new NotImplementedException();
            }

            public LambdaExpression GetQueryFilter()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IForeignKey> GetReferencingForeignKeys()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IDictionary<string, object>> GetSeedData(bool providerValues = false)
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IServiceProperty> GetServiceProperties()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<ISkipNavigation> GetSkipNavigations()
            {
                throw new NotImplementedException();
            }

            public IEnumerable<IProperty> GetValueGeneratingProperties()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.FindDeclaredForeignKeys(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            IReadOnlyNavigation IReadOnlyEntityType.FindDeclaredNavigation(string name)
            {
                throw new NotImplementedException();
            }

            IReadOnlyProperty IReadOnlyEntityType.FindDeclaredProperty(string name)
            {
                throw new NotImplementedException();
            }

            IReadOnlyForeignKey IReadOnlyEntityType.FindForeignKey(
                IReadOnlyList<IReadOnlyProperty> properties, IReadOnlyKey principalKey, IReadOnlyEntityType principalEntityType)
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.FindForeignKeys(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            IReadOnlyIndex IReadOnlyEntityType.FindIndex(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            IReadOnlyIndex IReadOnlyEntityType.FindIndex(string name)
            {
                throw new NotImplementedException();
            }

            IReadOnlyKey IReadOnlyEntityType.FindKey(IReadOnlyList<IReadOnlyProperty> properties)
            {
                throw new NotImplementedException();
            }

            IReadOnlyKey IReadOnlyEntityType.FindPrimaryKey()
            {
                throw new NotImplementedException();
            }

            IReadOnlyProperty IReadOnlyEntityType.FindProperty(string name)
            {
                throw new NotImplementedException();
            }

            IReadOnlyServiceProperty IReadOnlyEntityType.FindServiceProperty(string name)
            {
                throw new NotImplementedException();
            }

            IReadOnlySkipNavigation IReadOnlyEntityType.FindSkipNavigation(string name)
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.GetDeclaredForeignKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyIndex> IReadOnlyEntityType.GetDeclaredIndexes()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyKey> IReadOnlyEntityType.GetDeclaredKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyNavigation> IReadOnlyEntityType.GetDeclaredNavigations()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyProperty> IReadOnlyEntityType.GetDeclaredProperties()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.GetDeclaredReferencingForeignKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyServiceProperty> IReadOnlyEntityType.GetDeclaredServiceProperties()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.GetDerivedForeignKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyIndex> IReadOnlyEntityType.GetDerivedIndexes()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyEntityType> IReadOnlyEntityType.GetDirectlyDerivedTypes()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.GetForeignKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyIndex> IReadOnlyEntityType.GetIndexes()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyKey> IReadOnlyEntityType.GetKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyNavigation> IReadOnlyEntityType.GetNavigations()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyProperty> IReadOnlyEntityType.GetProperties()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyForeignKey> IReadOnlyEntityType.GetReferencingForeignKeys()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlyServiceProperty> IReadOnlyEntityType.GetServiceProperties()
            {
                throw new NotImplementedException();
            }

            IEnumerable<IReadOnlySkipNavigation> IReadOnlyEntityType.GetSkipNavigations()
            {
                throw new NotImplementedException();
            }
        }
    }
}
