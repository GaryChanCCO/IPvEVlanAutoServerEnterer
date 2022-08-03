namespace System.ComponentModel
{
    public static class TypeConverterExtensions
    {
        public static T ConvertTo<T>(this TypeConverter converter, object value)
        {
            return (T)converter.ConvertTo(value, typeof(T));
        }
    }
}
