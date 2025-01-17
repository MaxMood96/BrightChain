// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BrightChain.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel;
using Microsoft.EntityFrameworkCore.TestUtilities;

// ReSharper disable InconsistentNaming
namespace BrightChain.EntityFrameworkCore
{
    public class F1BrightChainFixture<TRowVersion> : F1FixtureBase<TRowVersion>
    {
        protected override ITestStoreFactory TestStoreFactory
            => BrightChainTestStoreFactory.Instance;

        public override TestHelpers TestHelpers
            => BrightChainTestHelpers.Instance;

        protected override void BuildModelExternal(ModelBuilder modelBuilder)
        {
            base.BuildModelExternal(modelBuilder);

            modelBuilder.Entity<Microsoft.EntityFrameworkCore.TestModels.ConcurrencyModel.Engine>(
                b =>
                {
                    b.Property(e => e.EngineSupplierId).IsConcurrencyToken(false);
                    b.Property(e => e.Name).IsConcurrencyToken(false);
                    b.OwnsOne(
                        e => e.StorageLocation, lb =>
                        {
                            lb.Property(l => l.Latitude).IsConcurrencyToken(false);
                            lb.Property(l => l.Longitude).IsConcurrencyToken(false);
                        });
                });

            modelBuilder.Entity<Chassis>().Property<string>("Version").IsETagConcurrency();
            modelBuilder.Entity<Driver>().Property<string>("Version").IsETagConcurrency();
            modelBuilder.Entity<Team>().Property<string>("Version").IsETagConcurrency();

            modelBuilder.Entity<Sponsor>(
                eb =>
                {
                    eb.Property<string>("Version").IsETagConcurrency();
                    eb.Property<int?>(Sponsor.ClientTokenPropertyName).IsConcurrencyToken(false);
                });

            modelBuilder.Entity<TitleSponsor>()
                .OwnsOne(
                    s => s.Details, eb =>
                    {
                        eb.Property<string>("Version").IsETagConcurrency();
                        eb.Property<int?>(Sponsor.ClientTokenPropertyName).IsConcurrencyToken(false);
                    });
        }
    }
}
