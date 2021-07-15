﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BrightChain.EntityFrameworkCore.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.TestUtilities;
using System.Threading.Tasks;
using Xunit;

namespace BrightChain.EntityFrameworkCore
{
    public class ConcurrencyDetectorDisabledBrightChainTest : ConcurrencyDetectorDisabledTestBase<
        ConcurrencyDetectorDisabledBrightChainTest.ConcurrencyDetectorBrightChainFixture>
    {
        public ConcurrencyDetectorDisabledBrightChainTest(ConcurrencyDetectorBrightChainFixture fixture)
            : base(fixture)
        {
        }

        [ConditionalTheory(Skip = "Issue #17246")]
        public override Task Any(bool async)
        {
            return base.Any(async);
        }

        public class ConcurrencyDetectorBrightChainFixture : ConcurrencyDetectorFixtureBase
        {
            protected override ITestStoreFactory TestStoreFactory
                => BrightChainTestStoreFactory.Instance;

            public TestSqlLoggerFactory TestSqlLoggerFactory
                => (TestSqlLoggerFactory)ListLoggerFactory;

            public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            {
                return builder.EnableThreadSafetyChecks(enableChecks: false);
            }
        }
    }
}
