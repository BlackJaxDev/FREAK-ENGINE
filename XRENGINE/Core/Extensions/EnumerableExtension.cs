namespace Extensions
{
    public static class EnumerableExtension
    {
        public static ThreadSafeList<T> AsThreadSafeList<T>(this IEnumerable<T> enumerable)
        {
            return new ThreadSafeList<T>(enumerable);
        }
    }   
}
