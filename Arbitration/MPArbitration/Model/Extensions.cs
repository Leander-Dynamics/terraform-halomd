using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Options;

namespace MPArbitration.Model
{
    /// <summary>
    /// 
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// .
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string GetNumbers(this string text)
        {
            text = text ?? string.Empty;
            return new string(text.Where(p => char.IsDigit(p)).ToArray());
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="array"></param>
        /// <param name="mutator"></param>
        public static void ApplyToEach<T>(this T[] array, Func<T, T> mutator)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = mutator(array[i]);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        /// <param name="separator"></param>
        /// <returns></returns>
        public static string StringJoin(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="JSONString"></param>
        /// <returns></returns>
        public static bool IsValidJSONString(this string JSONString)
        {
            JSONString = JSONString.Trim();
            if (string.IsNullOrEmpty(JSONString)) return false;


            // this JSON Token (object) check
            if (!JSONString.StartsWith("{") && !JSONString.EndsWith("}"))
                return false;


            // this JSON array check
            if (!JSONString.StartsWith("[") && !JSONString.EndsWith("]"))
                return false;

            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNumeric(this Type type)
        {
            if (type == null) { return false; }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = type.GetGenericArguments()[0];
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <param name="workingDays"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static DateTime AddWorkDays(this DateTime date, int workingDays)
        {
            if (holidays.Count() == 0)
                throw new Exception("Holiday list not loaded!");

            int direction = workingDays < 0 ? -1 : 1;
            DateTime newDate = date;
            while (workingDays != 0)
            {
                newDate = newDate.AddDays(direction);
                if (newDate.DayOfWeek != DayOfWeek.Saturday &&
                    newDate.DayOfWeek != DayOfWeek.Sunday &&
                    !newDate.IsHoliday())
                {
                    workingDays -= direction;
                }
            }
            return newDate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static async Task EnsureHolidays(ArbitrationDbContext context)
        {
            if (holidays.Count() > 0)
                return;

            holidays = await context.Holidays.AsNoTracking().Select(d => d.StartDate).ToArrayAsync();
        }

        /// <summary>
        /// 
        /// </summary>
        public static DateTime?[] holidays = new DateTime?[] { }; // new DateTime(2022, 12, 26), new DateTime(2023, 01, 02) 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static bool IsHoliday(this DateTime date)
        {
            return holidays != null && holidays.FirstOrDefault(d => d.HasValue && d.Value.Date == date.Date) != null;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        // EF Extensions
        // https://stackoverflow.com/questions/55778452/how-to-execute-raw-sql-query-with-ef-core
        private class ContextForQuery<T> : DbContext where T : class
        {
            private readonly string connectionString;

            public ContextForQuery(string connectionString)
            {
                this.connectionString = connectionString;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                //optionsBuilder.UseSqlServer(connectionString, options => options.EnableRetryOnFailure());
                optionsBuilder.UseSqlServer(connectionString, sqlServerOptionsAction: sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(60),
                        errorNumbersToAdd: null);
                });
                base.OnConfiguring(optionsBuilder);
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<T>().HasNoKey();
                base.OnModelCreating(modelBuilder);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="query"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static IList<T> Query<T>(string connectionString, string query, params object[] parameters) where T : class
        {
            using (var contextGeneric = new ContextForQuery<T>(connectionString))
            {
                return contextGeneric.Set<T>().FromSqlRaw(query, parameters).ToList();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="context"></param>
        /// <param name="entityObject"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static T TruncateStringsBasedOnMaxLength<T>(this DbContext context, T entityObject, ILogger? logger = null)
        {
            if (entityObject == null)
                return entityObject;

            var clone = entityObject.Clone();

            var entityTypes = context.Model.GetEntityTypes();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            var properties = entityTypes.First(e => e.Name == clone.GetType().FullName).GetProperties().ToDictionary(p => p.Name, p => p.GetMaxLength());

            foreach (var propertyInfo in clone.GetType().GetProperties().Where(p => p.PropertyType == typeof(string)))
            {
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                var value = (string?)propertyInfo.GetValue(clone);

                if (value == null)
                    continue;

                // If Property Contains 'Phone'. Assume its a phone number and remove all non-digits
                if (propertyInfo.Name.ToLower().Contains("phone"))
                {
                    value = value.SanitizeToDigitsOnly();
                }

                var maxLength = properties[propertyInfo.Name];

                if (maxLength.HasValue)
                {
                    propertyInfo.SetValue(clone, value.Truncate(maxLength.Value, logger));
                }
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return clone;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryable"></param>
        /// <param name="predicate"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        // LINQ extensions
        public static IQueryable<T> AddCondition<T>(this IQueryable<T> queryable, Func<bool> predicate, Expression<Func<T, bool>> filter)
        {
            if (predicate())
                return queryable.Where(filter);
            else
                return queryable;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        // String extensions
        public static string SanitizeToDigitsOnly(this string value)
        {
            return (value == null) ? "" : Regex.Replace(value, @"\D", "");
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="length"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static string Truncate(this string s, int length, ILogger? logger = null)
        {
            string result;

            if (string.IsNullOrEmpty(s) || s.Length <= length)
            {
                result = s;
            }
            else
            {
                if (length <= 0)
                {
                    result = string.Empty;
                }
                else
                {
                    result = s.Substring(0, length);

                    logger?.LogWarning("Truncated string:{stringToTruncate}", s);
                }
            }

            return result;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        // System extensions
        public static T? Clone<T>(this T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceProvider"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static T BuildObject<T>(this IServiceProvider serviceProvider, params object[] parameters)
            => ActivatorUtilities.CreateInstance<T>(serviceProvider, parameters);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="days"></param>
        /// <returns></returns>
        public static DateTime AddBusinessDays(this DateTime current, int days)
        {
            var sign = Math.Sign(days);
            var unsignedDays = Math.Abs(days);
            for (var i = 0; i < unsignedDays; i++)
            {
                do
                {
                    current = current.AddDays(sign);
                } while (current.DayOfWeek == DayOfWeek.Saturday ||
                         current.DayOfWeek == DayOfWeek.Sunday);
            }
            return current;
        }
    }
}

