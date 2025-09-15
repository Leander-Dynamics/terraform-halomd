using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace MPArbitration.Utility
{
    public static class QueryExtensions
    {
        public static IQueryable<T> SelectAllExcept<T>(this IQueryable<T> source, string excludedProperty)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var bindings = typeof(T).GetProperties()
                                    .Where(p => p.Name != excludedProperty)
                                    .Select(p => Expression.Bind(p, Expression.Property(parameter, p)))
                                    .ToArray();

            var body = Expression.MemberInit(Expression.New(typeof(T)), bindings);
            var selector = Expression.Lambda<Func<T, T>>(body, parameter);

            return source.Select(selector);
        }
    }
}
