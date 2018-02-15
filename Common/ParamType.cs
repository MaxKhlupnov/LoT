
namespace HomeOS.Hub.Common
{
    using System;

    public sealed class ParamType : MarshalByRefObject, HomeOS.Hub.Platform.Views.VParamType
    {
        /// <summary>
        /// The list of simple types supported by HomeOS Port operations. They are further
        /// subtyped in BaseType.
        /// </summary>
        public enum SimpleType { error = -2, unsupported = -1, opaque = 0, integer, numdouble, binary, range, jpegimage, list, text };

        SimpleType maintype;
        Object value;
        string name;

        public ParamType(SimpleType maint, string name, Object value)
        {
            this.maintype = maint;
            this.value = value;
            this.name = name;
        }

        public ParamType(SimpleType maint, Object value) : this(maint, "", value) { }

        public ParamType(int value) : this (SimpleType.integer, value)  { }

        public ParamType(bool value) : this(SimpleType.binary, value) { }

        public ParamType(double value) : this(SimpleType.numdouble, value) { }

        public ParamType(string value) : this(SimpleType.text, value) { }

        public int Maintype()
        {
            return (int)maintype;
        }

        public Object Value()
        {
            return value;
        }

        public string Name()
        {
            return name;
        }

        public void SetValue(Object v)
        {
            value = v;
        }
    }
}