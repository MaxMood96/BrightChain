// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

#nullable enable

namespace System.Reflection
{
    internal static class EntityFrameworkMemberInfoExtensions

    {
        public static Type GetMemberType(this MemberInfo memberInfo)
        {
            return (memberInfo as PropertyInfo)?.PropertyType ?? ((FieldInfo)memberInfo).FieldType;
        }

        public static bool IsSameAs(this MemberInfo? propertyInfo, MemberInfo? otherPropertyInfo)
        {
            return propertyInfo == null
                           ? otherPropertyInfo == null
                           : (otherPropertyInfo != null
                               && (Equals(propertyInfo, otherPropertyInfo)
                                   || (propertyInfo.Name == otherPropertyInfo.Name
                                       && propertyInfo.DeclaringType != null
                                       && otherPropertyInfo.DeclaringType != null
                                       && (propertyInfo.DeclaringType == otherPropertyInfo.DeclaringType
                                           || propertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(otherPropertyInfo.DeclaringType)
                                           || otherPropertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(propertyInfo.DeclaringType)
                                           || propertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces.Contains(otherPropertyInfo.DeclaringType)
                                           || otherPropertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces
                                               .Contains(propertyInfo.DeclaringType)))));
        }

        public static bool IsOverridenBy(this MemberInfo? propertyInfo, MemberInfo? otherPropertyInfo)
        {
            return propertyInfo == null
                           ? otherPropertyInfo == null
                           : (otherPropertyInfo != null
                               && (Equals(propertyInfo, otherPropertyInfo)
                                   || (propertyInfo.Name == otherPropertyInfo.Name
                                       && propertyInfo.DeclaringType != null
                                       && otherPropertyInfo.DeclaringType != null
                                       && (propertyInfo.DeclaringType == otherPropertyInfo.DeclaringType
                                           || otherPropertyInfo.DeclaringType.GetTypeInfo().IsSubclassOf(propertyInfo.DeclaringType)
                                           || otherPropertyInfo.DeclaringType.GetTypeInfo().ImplementedInterfaces
                                               .Contains(propertyInfo.DeclaringType)))));
        }

        public static string GetSimpleMemberName(this MemberInfo member)
        {
            var name = member.Name;
            var index = name.LastIndexOf('.');
            return index >= 0 ? name[(index + 1)..] : name;
        }

        public static bool IsReallyVirtual(this MethodInfo method)
        {
            return method.IsVirtual && !method.IsFinal;
        }
    }
}
