// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

// ReSharper disable once CheckNamespace
namespace BrightChain.EntityFrameworkCore.Utilities
{
    [DebuggerStepThrough]
    internal static class EnumerableExtensions
    {
        public static IOrderedEnumerable<TSource> OrderByOrdinal<TSource>(
            this IEnumerable<TSource> source,
            Func<TSource, string> keySelector)
        {
            return source.OrderBy(keySelector, StringComparer.Ordinal);
        }

        public static IEnumerable<T> Distinct<T>(
            this IEnumerable<T> source,
            Func<T?, T?, bool> comparer)
            where T : class
        {
            return source.Distinct(new DynamicEqualityComparer<T>(comparer));
        }

        private sealed class DynamicEqualityComparer<T> : IEqualityComparer<T>
            where T : class
        {
            private readonly Func<T?, T?, bool> _func;

            public DynamicEqualityComparer(Func<T?, T?, bool> func)
            {
                _func = func;
            }

            public bool Equals(T? x, T? y)
            {
                return _func(x, y);
            }

            public int GetHashCode(T obj)
            {
                return 0;
            }
        }

        public static string Join(
            this IEnumerable<object> source,
            string separator = ", ")
        {
            return string.Join(separator, source);
        }

        public static bool StructuralSequenceEqual<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            using var firstEnumerator = first.GetEnumerator();
            using var secondEnumerator = second.GetEnumerator();
            while (firstEnumerator.MoveNext())
            {
                if (!secondEnumerator.MoveNext()
                    || !StructuralComparisons.StructuralEqualityComparer
                        .Equals(firstEnumerator.Current, secondEnumerator.Current))
                {
                    return false;
                }
            }

            return !secondEnumerator.MoveNext();
        }

        public static bool StartsWith<TSource>(
            this IEnumerable<TSource> first,
            IEnumerable<TSource> second)
        {
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            using (var firstEnumerator = first.GetEnumerator())
            {
                using var secondEnumerator = second.GetEnumerator();
                while (secondEnumerator.MoveNext())
                {
                    if (!firstEnumerator.MoveNext()
                        || !Equals(firstEnumerator.Current, secondEnumerator.Current))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static int IndexOf<T>(this IEnumerable<T> source, T item)
        {
            return IndexOf(source, item, EqualityComparer<T>.Default);
        }

        public static int IndexOf<T>(
            this IEnumerable<T> source,
            T item,
            IEqualityComparer<T> comparer)
        {
            return source.Select(
                               (x, index) =>
                                   comparer.Equals(item, x) ? index : -1)
                           .FirstOr(x => x != -1, -1);
        }

        public static T FirstOr<T>(this IEnumerable<T> source, T alternate)
        {
            return source.DefaultIfEmpty(alternate).First();
        }

        public static T FirstOr<T>(this IEnumerable<T> source, Func<T, bool> predicate, T alternate)
        {
            return source.Where(predicate).FirstOr(alternate);
        }

        public static bool Any(this IEnumerable source)
        {
            foreach (var _ in source)
            {
                return true;
            }

            return false;
        }

        public static async Task<List<TSource>> ToListAsync<TSource>(
            this IAsyncEnumerable<TSource> source,
            CancellationToken cancellationToken = default)
        {
            var list = new List<TSource>();
            await foreach (var element in source.WithCancellation(cancellationToken))
            {
                list.Add(element);
            }

            return list;
        }

        public static List<TSource> ToList<TSource>(this IEnumerable source)
        {
            return source.OfType<TSource>().ToList();
        }

        public static string Format(this IEnumerable<string> strings)
        {
            return "{"
                           + string.Join(
                               ", ",
                               strings.Select(s => "'" + s + "'"))
                           + "}";
        }
    }
}
