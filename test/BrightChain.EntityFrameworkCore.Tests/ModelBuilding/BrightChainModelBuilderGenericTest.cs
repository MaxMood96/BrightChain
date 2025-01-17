// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using BrightChain.EntityFrameworkCore.Metadata.Conventions;
using BrightChain.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.ModelBuilding;
using Xunit;

// ReSharper disable InconsistentNaming
namespace BrightChain.EntityFrameworkCore.ModelBuilding
{
    public class BrightChainModelBuilderGenericTest : ModelBuilderGenericTest
    {
        public class BrightChainGenericNonRelationship : GenericNonRelationship
        {
            public override void Properties_can_set_row_version()
            {
                // Fails due to ETags
            }

            public override void Properties_can_be_made_concurrency_tokens()
            {
                // Fails due to ETags
            }

            public override void Properties_specified_by_string_are_shadow_properties_unless_already_known_to_be_CLR_properties()
            {
                // Fails due to extra shadow properties
            }

            [ConditionalFact]
            public override void Properties_can_have_provider_type_set_for_type()
            {
                var modelBuilder = CreateModelBuilder(c => c.Properties<string>().HaveConversion<byte[]>());

                modelBuilder.Entity<Quarks>(
                    b =>
                    {
                        b.Property(e => e.Up);
                        b.Property(e => e.Down);
                        b.Property<int>("Charm");
                        b.Property<string>("Strange");
                        b.Property<string>("__id").HasConversion((Type)null);
                    });

                var model = modelBuilder.FinalizeModel();
                var entityType = (IReadOnlyEntityType)model.FindEntityType(typeof(Quarks));

                Assert.Null(entityType.FindProperty("Up").GetProviderClrType());
                Assert.Same(typeof(byte[]), entityType.FindProperty("Down").GetProviderClrType());
                Assert.Null(entityType.FindProperty("Charm").GetProviderClrType());
                Assert.Same(typeof(byte[]), entityType.FindProperty("Strange").GetProviderClrType());
            }

            [ConditionalFact]
            public virtual void Partition_key_is_added_to_the_keys()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>()
                    .Ignore(b => b.Details)
                    .Ignore(b => b.Orders)
                    .HasPartitionKey(b => b.AlternateKey)
                    .Property(b => b.AlternateKey).HasConversion<string>();

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    new[] { nameof(Customer.Id), nameof(Customer.AlternateKey) },
                    entity.FindPrimaryKey().Properties.Select(p => p.Name));
                Assert.Equal(
                    new[] { StoreKeyConvention.DefaultIdPropertyName, nameof(Customer.AlternateKey) },
                    entity.GetKeys().First(k => k != entity.FindPrimaryKey()).Properties.Select(p => p.Name));

                var idProperty = entity.FindProperty(StoreKeyConvention.DefaultIdPropertyName);
                Assert.Single(idProperty.GetContainingKeys());
                Assert.NotNull(idProperty.GetValueGeneratorFactory());
            }

            [ConditionalFact]
            public virtual void Partition_key_is_added_to_the_alternate_key_if_primary_key_contains_id()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>().HasKey(StoreKeyConvention.DefaultIdPropertyName);
                modelBuilder.Entity<Customer>()
                    .Ignore(b => b.Details)
                    .Ignore(b => b.Orders)
                    .HasPartitionKey(b => b.AlternateKey)
                    .Property(b => b.AlternateKey).HasConversion<string>();

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    new[] { StoreKeyConvention.DefaultIdPropertyName },
                    entity.FindPrimaryKey().Properties.Select(p => p.Name));
                Assert.Equal(
                    new[] { StoreKeyConvention.DefaultIdPropertyName, nameof(Customer.AlternateKey) },
                    entity.GetKeys().First(k => k != entity.FindPrimaryKey()).Properties.Select(p => p.Name));
            }

            [ConditionalFact]
            public virtual void No_id_property_created_if_another_property_mapped_to_id()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>()
                    .Property(c => c.Name)
                    .ToJsonProperty(StoreKeyConvention.IdPropertyJsonName);
                modelBuilder.Entity<Customer>()
                    .Ignore(b => b.Details)
                    .Ignore(b => b.Orders);

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Null(entity.FindProperty(StoreKeyConvention.DefaultIdPropertyName));
                Assert.Single(entity.GetKeys().Where(k => k != entity.FindPrimaryKey()));

                var idProperty = entity.GetDeclaredProperties()
                    .Single(p => p.GetJsonPropertyName() == StoreKeyConvention.IdPropertyJsonName);
                Assert.Single(idProperty.GetContainingKeys());
                Assert.NotNull(idProperty.GetValueGeneratorFactory());
            }

            [ConditionalFact]
            public virtual void No_id_property_created_if_another_property_mapped_to_id_in_pk()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>()
                    .Property(c => c.Name)
                    .ToJsonProperty(StoreKeyConvention.IdPropertyJsonName);
                modelBuilder.Entity<Customer>()
                    .Ignore(c => c.Details)
                    .Ignore(c => c.Orders)
                    .HasKey(c => c.Name);

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Null(entity.FindProperty(StoreKeyConvention.DefaultIdPropertyName));
                Assert.Empty(entity.GetKeys().Where(k => k != entity.FindPrimaryKey()));

                var idProperty = entity.GetDeclaredProperties()
                    .Single(p => p.GetJsonPropertyName() == StoreKeyConvention.IdPropertyJsonName);
                Assert.Single(idProperty.GetContainingKeys());
                Assert.Null(idProperty.GetValueGeneratorFactory());
            }

            [ConditionalFact]
            public virtual void No_alternate_key_is_created_if_primary_key_contains_id()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>().HasKey(StoreKeyConvention.DefaultIdPropertyName);
                modelBuilder.Entity<Customer>()
                    .Ignore(b => b.Details)
                    .Ignore(b => b.Orders);

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    new[] { StoreKeyConvention.DefaultIdPropertyName },
                    entity.FindPrimaryKey().Properties.Select(p => p.Name));
                Assert.Empty(entity.GetKeys().Where(k => k != entity.FindPrimaryKey()));

                var idProperty = entity.FindProperty(StoreKeyConvention.DefaultIdPropertyName);
                Assert.Single(idProperty.GetContainingKeys());
                Assert.Null(idProperty.GetValueGeneratorFactory());
            }

            [ConditionalFact]
            public virtual void No_alternate_key_is_created_if_primary_key_contains_id_and_partition_key()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>().HasKey(nameof(Customer.AlternateKey), StoreKeyConvention.DefaultIdPropertyName);
                modelBuilder.Entity<Customer>()
                    .Ignore(b => b.Details)
                    .Ignore(b => b.Orders)
                    .HasPartitionKey(b => b.AlternateKey)
                    .Property(b => b.AlternateKey).HasConversion<string>();

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    new[] { nameof(Customer.AlternateKey), StoreKeyConvention.DefaultIdPropertyName },
                    entity.FindPrimaryKey().Properties.Select(p => p.Name));
                Assert.Empty(entity.GetKeys().Where(k => k != entity.FindPrimaryKey()));
            }

            [ConditionalFact]
            public virtual void No_alternate_key_is_created_if_id_is_partition_key()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Entity<Customer>().HasKey(nameof(Customer.AlternateKey));
                modelBuilder.Entity<Customer>()
                    .Ignore(b => b.Details)
                    .Ignore(b => b.Orders)
                    .HasPartitionKey(b => b.AlternateKey)
                    .Property(b => b.AlternateKey).HasConversion<string>().ToJsonProperty("id");

                var model = modelBuilder.FinalizeModel();

                var entity = model.FindEntityType(typeof(Customer));

                Assert.Equal(
                    new[] { nameof(Customer.AlternateKey) },
                    entity.FindPrimaryKey().Properties.Select(p => p.Name));
                Assert.Empty(entity.GetKeys().Where(k => k != entity.FindPrimaryKey()));
            }

            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }

        public class BrightChainGenericInheritance : GenericInheritance
        {
            public override void Can_set_and_remove_base_type()
            {
                // Fails due to presence of __jObject
            }

            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }

        public class BrightChainGenericOneToMany : GenericOneToMany
        {
            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }

        public class BrightChainGenericManyToOne : GenericManyToOne
        {
            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }

        public class BrightChainGenericOneToOne : GenericOneToOne
        {
            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }

        public class BrightChainGenericManyToMany : GenericManyToMany
        {
            [ConditionalFact]
            public virtual void Can_use_shared_type_as_join_entity_with_partition_keys()
            {
                var modelBuilder = CreateModelBuilder();

                modelBuilder.Ignore<OneToManyNavPrincipal>();
                modelBuilder.Ignore<OneToOneNavPrincipal>();

                modelBuilder.Entity<ManyToManyNavPrincipal>(mb =>
                {
                    mb.Property<string>("PartitionId");
                    mb.HasPartitionKey("PartitionId");
                });

                modelBuilder.Entity<NavDependent>(mb =>
                {
                    mb.Property<string>("PartitionId");
                    mb.HasPartitionKey("PartitionId");
                });

                modelBuilder.Entity<ManyToManyNavPrincipal>()
                    .HasMany(e => e.Dependents)
                    .WithMany(e => e.ManyToManyPrincipals)
                    .UsingEntity<Dictionary<string, object>>(
                        "JoinType",
                        e => e.HasOne<NavDependent>().WithMany().HasForeignKey("DependentId", "PartitionId"),
                        e => e.HasOne<ManyToManyNavPrincipal>().WithMany().HasForeignKey("PrincipalId", "PartitionId"),
                        e =>
                        {
                            e.HasPartitionKey("PartitionId");
                        });

                var model = modelBuilder.FinalizeModel();

                var joinType = model.FindEntityType("JoinType");
                Assert.NotNull(joinType);
                Assert.Equal(2, joinType.GetForeignKeys().Count());
                Assert.Equal(3, joinType.FindPrimaryKey().Properties.Count);
                Assert.Equal(6, joinType.GetProperties().Count());
                Assert.Equal("PartitionId", joinType.GetPartitionKeyPropertyName());
                Assert.Equal("PartitionId", joinType.FindPrimaryKey().Properties.Last().Name);
            }

            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }

        public class BrightChainGenericOwnedTypes : GenericOwnedTypes
        {
            protected override TestModelBuilder CreateModelBuilder(Action<ModelConfigurationBuilder> configure = null)
            {
                return CreateTestModelBuilder(BrightChainTestHelpers.Instance, configure);
            }
        }
    }
}
