using System;
using System.ComponentModel;
using System.Globalization;

namespace OrleanPG.Grains.Interfaces.Infrastructure
{
    public abstract class TypeToStringConverter<TType> : TypeConverter where TType : class
    {
        protected abstract TType CreateFromString(string value);
        protected abstract string ConvertToString(TType value);

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str)
            {
                return CreateFromString(str);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is TType val)
            {
                return ConvertToString(val);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }
    }

}
