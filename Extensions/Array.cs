using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Extensions
{
    public static partial class Ext
    {
        /*
        return list.Cast<object>().Aggregate(new StringBuilder(),
                (builder, obj) => builder.Append(separator + elementToString(obj)),
                (builder) => 
                {
                    if (list.Count == 0)
                        return builder.ToString();
                    else
                        return builder.Remove(0, separator.Length).ToString();
                });
        */

        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, string separator, Func<T, string> elementToString)
        {
            StringBuilder builder = new();

            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i]) + separator);

            int sepLen = separator.Length;
            if (builder.Length >= sepLen)
                builder.Remove(builder.Length - sepLen, sepLen);

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, Func<T, string> elementToString)
        {
            StringBuilder builder = new();

            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i]));

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="lastSeparator">The separator to use in the list between the last two elements.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, string separator, string lastSeparator, Func<T, string> elementToString)
        {
            int countMin2 = a.Count - 2;

            StringBuilder builder = new();

            string sep = separator;
            for (int i = 0; i < countMin2; ++i)
                builder.Append(elementToString(a[i]) + sep);

            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2]) + lastSeparator);

            ++countMin2;
            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2]));

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, string separator, Func<T, int, string> elementToString)
        {
            StringBuilder builder = new();

            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i], i) + separator);

            int sepLen = separator.Length;
            if (builder.Length >= sepLen)
                builder.Remove(builder.Length - sepLen, sepLen);

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, string separator, string lastSeparator, Func<T, int, string> elementToString)
        {
            int countMin2 = a.Count - 2;

            StringBuilder builder = new();

            string sep = separator;
            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i], i) + sep);

            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2], countMin2) + lastSeparator);

            ++countMin2;
            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2], countMin2));

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, string separator)
            => ToStringList(a, separator, o => o.ToString());
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="lastSeparator">The separator to use in the list between the last two elements.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringList<T>(this IList<T> a, string separator, string lastSeparator)
            => ToStringList(a, separator, lastSeparator, o => o.ToString());

        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringListGeneric(this IList a, string separator, Func<object, string> elementToString)
        {
            StringBuilder builder = new();

            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i]) + separator);

            int sepLen = separator.Length;
            if (builder.Length >= sepLen)
                builder.Remove(builder.Length - sepLen, sepLen);

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="lastSeparator">The separator to use in the list between the last two elements.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringListGeneric(this IList a, string separator, string lastSeparator, Func<object, string> elementToString)
        {
            int countMin2 = a.Count - 2;

            StringBuilder builder = new();

            string sep = separator;
            for (int i = 0; i < countMin2; ++i)
                builder.Append(elementToString(a[i]) + sep);

            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2]) + lastSeparator);

            ++countMin2;
            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2]));

            return builder.ToString();
        }
        public delegate bool DelTryConvert(object obj, out string result);
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="lastSeparator">The separator to use in the list between the last two elements.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static bool ToStringListGenericChecked(this IList a, string separator, string lastSeparator, DelTryConvert elementToString, out string str)
        {
            int countMin2 = a.Count - 2;

            StringBuilder builder = new();

            string sep = separator;
            for (int i = 0; i < countMin2; ++i)
            {
                if (elementToString(a[i], out string val))
                    builder.Append(val + sep);
                else
                {
                    str = null;
                    return false;
                }
            }

            if (countMin2 >= 0)
            {
                if (elementToString(a[countMin2], out string val))
                    builder.Append(val + lastSeparator);
                else
                {
                    str = null;
                    return false;
                }
            }

            ++countMin2;
            if (countMin2 >= 0)
            {
                if (elementToString(a[countMin2], out string val))
                    builder.Append(val);
                else
                {
                    str = null;
                    return false;
                }
            }

            str = builder.ToString();
            return true;
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringListGeneric(this IList a, string separator, Func<object, int, string> elementToString)
        {
            StringBuilder builder = new();

            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i], i) + separator);

            int sepLen = separator.Length;
            if (builder.Length >= sepLen)
                builder.Remove(builder.Length - sepLen, sepLen);

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="elementToString">The method for converting individual array elements to strings.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringListGeneric(this IList a, string separator, string lastSeparator, Func<object, int, string> elementToString)
        {
            int countMin2 = a.Count - 2;

            StringBuilder builder = new();

            string sep = separator;
            for (int i = 0; i < a.Count; ++i)
                builder.Append(elementToString(a[i], i) + sep);

            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2], countMin2) + lastSeparator);

            ++countMin2;
            if (countMin2 >= 0)
                builder.Append(elementToString(a[countMin2], countMin2));

            return builder.ToString();
        }
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringListGeneric(this IList a, string separator)
            => ToStringListGeneric(a, separator, o => o.ToString());
        /// <summary>
        /// Converts the elements of an array into a well-formatted list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="a">The array to format into a list.</param>
        /// <param name="separator">The separator to use to separate items in the list.</param>
        /// <param name="lastSeparator">The separator to use in the list between the last two elements.</param>
        /// <returns>A list of the elements in the array as a string.</returns>
        public static string ToStringListGeneric(this IList a, string separator, string lastSeparator)
            => ToStringListGeneric(a, separator, lastSeparator, o => o.ToString());
        /// <summary>
        /// Returns true if index >= 0 && index is less than length.
        /// Use this so you don't have to write that every time.
        /// </summary>
        public static bool IndexInRangeGeneric(this IList a, int value)
            => a is null ? false : value >= 0 && value < a.Count;
        /// <summary>
        /// Returns true if index >= 0 && index is less than length.
        /// Use this so you don't have to write that every time.
        /// </summary>
        public static bool IndexInRangeGeneric(this Array a, int value)
            => a is null ? false : value >= 0 && value < a.Length;
        /// <summary>
        /// Returns true if index >= 0 && index is less than length.
        /// Use this so you don't have to write that every time.
        /// </summary>
        public static bool IndexInRange<T>(this IList<T> a, int value)
            => a is null ? false : value >= 0 && value < a.Count;
        /// <summary>
        /// Returns true if index >= 0 && index is less than length.
        /// Use this so you don't have to write that every time.
        /// </summary>
        public static bool IndexInRange<T>(this T[] a, int value)
            => a is null ? false : value >= 0 && value < a.Length;

        public static int[] FindAllOccurences<T>(this IList<T> a, T o) where T : IEquatable<T>
        {
            List<int> l = new();
            int i = 0;
            foreach (T x in a)
            {
                if (x.Equals(o))
                    l.Add(i);
                i++;
            }
            return l.ToArray();
        }

        public static int[] FindAllMatchIndices<T>(this IList<T> a, Predicate<T> predicate)
        {
            List<int> list = new(a.Count);
            for (int i = 0; i < a.Count; ++i)
                if (predicate(a[i]))
                    list.Add(i);
            return list.ToArray();
        }

        public static int IndexOf<T>(this T[] a, T value)
            => Array.IndexOf(a, value);
        
        public static bool Contains(this string[] a, string value, StringComparison comp)
        {
            for (int i = 0; i < a.Length; ++i)
                if (string.Equals(a[i], value, comp))
                    return true;
            return false;
        }
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
        public static T[] Append<T>(this T[] data, T[] appended)
        {
            T[] final = new T[data.Length + appended.Length];
            data.CopyTo(final, 0);
            appended.CopyTo(final, data.Length);
            return final;
        }
        public static T[] Resize<T>(this T[] data, int newSize)
        {
            Array.Resize(ref data, newSize);
            return data;
        }
        public static T[] FillWith<T>(this T[] array, T value)
        {
            for (int i = 0; i < array.Length; i++)
                array[i] = value;

            return array;
        }
        public static T[] FillWith<T>(this T[] array, Func<int, T> factory)
        {
            if (factory is null)
                return array;

            for (int i = 0; i < array.Length; i++)
                array[i] = factory(i);

            return array;
        }
        public static void ForEach<T>(this T[] array, Action<T> action)
        {
            try
            {
                if (array != null && action != null)
                    Array.ForEach(array, action);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public static void ForEachParallelArray<T>(this T[] array, Action<T, ParallelLoopState> action)
            => ForEachParallelArray(array, action, CancellationToken.None);
        public static void ForEachParallelArray<T>(this T[] array, Action<T, ParallelLoopState> action, CancellationToken cancellationToken)
        {
            try
            {
                if (array != null && action != null)
                {
                    OrderablePartitioner<T> rangePartitioner = Partitioner.Create(array, true);
                    ParallelOptions options = new()
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = cancellationToken,
                    };
                    Parallel.ForEach(rangePartitioner, options, action);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        public static void ForEachParallelArray<T>(this T[] array, Action<T> action)
            => ForEachParallelArray(array, action, CancellationToken.None);
        public static void ForEachParallelArray<T>(this T[] array, Action<T> action, CancellationToken cancellationToken)
        {
            try
            {
                if (array != null && action != null)
                {
                    OrderablePartitioner<T> rangePartitioner = Partitioner.Create(array, true);
                    ParallelOptions options = new()
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount,
                        CancellationToken = cancellationToken,
                    };
                    Parallel.ForEach(rangePartitioner, options, action);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
