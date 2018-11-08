using System;
using System.Runtime.Serialization;
using Wist.Client.Common.Properties;

namespace Wist.Client.Common.Exceptions
{
    [Serializable]
    public class WrongBindableInheritanceException : Exception
    {
        public WrongBindableInheritanceException() { }
        public WrongBindableInheritanceException(string className) : base(string.Format(Resources.ERR_WRONG_BINDABLE_INHERITANCE, className)) { }
        public WrongBindableInheritanceException(string className, Exception inner) : base(string.Format(Resources.ERR_WRONG_BINDABLE_INHERITANCE, className), inner) { }
        protected WrongBindableInheritanceException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
