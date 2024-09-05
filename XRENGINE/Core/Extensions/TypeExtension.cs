using System.Collections;

namespace Extensions
{
    public static class TypeExtension
    {
        public static Type? DetermineElementType(this IList list)
        {
            Type listType = list.GetType();
            Type? elementType = listType.GetElementType();
            return elementType ?? (listType.IsGenericType && listType.GenericTypeArguments.Length == 1 ? listType.GenericTypeArguments[0] : null);
        }
        public static Type? DetermineKeyType(this IDictionary dic)
        {
            Type listType = dic.GetType();
            return listType.IsGenericType ? listType.GenericTypeArguments[0] : null;
        }
        public static Type? DetermineValueType(this IDictionary dic)
        {
            Type listType = dic.GetType();
            return listType.IsGenericType ? listType.GenericTypeArguments[1] : null;
        }
    }   
}
