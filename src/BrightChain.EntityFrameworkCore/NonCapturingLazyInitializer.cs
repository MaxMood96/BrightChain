// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using BrightChain.EntityFrameworkCore.Utilities;

#nullable enable

namespace BrightChain.EntityFrameworkCore.Internal
{
    internal static class NonCapturingLazyInitializer
    {
        public static TValue EnsureInitialized<TParam, TValue>(
            [NotNull] ref TValue? target,
            TParam param,
            Func<TParam, TValue> valueFactory)
            where TValue : class
        {
            var tmp = Volatile.Read(ref target);
            if (tmp != null)
            {
                Check.DebugAssert(target != null, $"target was null in {nameof(EnsureInitialized)} after check");
                return tmp;
            }

            Interlocked.CompareExchange(ref target, valueFactory(param), null);

            return target;
        }

        public static TValue EnsureInitialized<TParam1, TParam2, TValue>(
            [NotNull] ref TValue? target,
            TParam1 param1,
            TParam2 param2,
            Func<TParam1, TParam2, TValue> valueFactory)
            where TValue : class
        {
            var tmp = Volatile.Read(ref target);
            if (tmp != null)
            {
                Check.DebugAssert(target != null, $"target was null in {nameof(EnsureInitialized)} after check");
                return tmp;
            }

            Interlocked.CompareExchange(ref target, valueFactory(param1, param2), null);

            return target;
        }

        public static TValue EnsureInitialized<TParam1, TParam2, TParam3, TValue>(
            [NotNull] ref TValue? target,
            TParam1 param1,
            TParam2 param2,
            TParam3 param3,
            Func<TParam1, TParam2, TParam3, TValue> valueFactory)
            where TValue : class
        {
            var tmp = Volatile.Read(ref target);
            if (tmp != null)
            {
                Check.DebugAssert(target != null, $"target was null in {nameof(EnsureInitialized)} after check");
                return tmp;
            }

            Interlocked.CompareExchange(ref target, valueFactory(param1, param2, param3), null);

            return target;
        }

        public static TValue EnsureInitialized<TParam, TValue>(
            ref TValue target,
            ref bool initialized,
            TParam param,
            Func<TParam, TValue> valueFactory)
            where TValue : class?
        {
            var alreadyInitialized = Volatile.Read(ref initialized);
            if (alreadyInitialized)
            {
                var value = Volatile.Read(ref target);
                Check.DebugAssert(target != null, $"target was null in {nameof(EnsureInitialized)} after check");
                Check.DebugAssert(value != null, $"value was null in {nameof(EnsureInitialized)} after check");
                return value;
            }

            Volatile.Write(ref target, valueFactory(param));
            Volatile.Write(ref initialized, true);

            return target;
        }

        public static TValue EnsureInitialized<TValue>(
            [NotNull] ref TValue? target,
            TValue value)
            where TValue : class
        {
            var tmp = Volatile.Read(ref target);
            if (tmp != null)
            {
                Check.DebugAssert(target != null, $"target was null in {nameof(EnsureInitialized)} after check");
                return tmp;
            }

            Interlocked.CompareExchange(ref target, value, null);

            return target;
        }

        public static TValue EnsureInitialized<TParam, TValue>(
            [NotNull] ref TValue? target,
            TParam param,
            Action<TParam> valueFactory)
            where TValue : class
        {
            var tmp = Volatile.Read(ref target);
            if (tmp != null)
            {
                Check.DebugAssert(target != null, $"target was null in {nameof(EnsureInitialized)} after check");
                return tmp;
            }

            valueFactory(param);

            var tmp2 = Volatile.Read(ref target);
            Check.DebugAssert(target != null && tmp2 != null,
                $"{nameof(valueFactory)} did not initialize {nameof(target)} in {nameof(EnsureInitialized)}");
            return tmp2;
        }
    }
}
