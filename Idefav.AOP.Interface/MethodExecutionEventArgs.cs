using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Idefav.AOP.Interface
{
    public class MethodExecutionEventArgs
    {
        public MethodExecutionEventArgs(MemberInfo method, object instance, object[] arguments, string returnType)
        {
            this.Method = method;
            this.Arguments = arguments;
            this.Instance = instance;
            if (returnType != null)
            {
                var type = Type.GetType(returnType);
                if (type != null)
                {
                    this.ReturnValue = DefaultForType(type);
                }
            }
        }

        public MethodExecutionEventArgs(MemberInfo method, object instance, object[] arguments)
        {
            this.Method = method;
            this.Arguments = arguments;
            this.Instance = instance;
        }

        public object Instance { get; private set; }

        public Exception Exception { get; set; }

        public MemberInfo Method { get; private set; }

        public object ReturnValue { get; set; }

        public object[] Arguments
        {
            get;
            private set;
        }

        protected static object DefaultForType(Type targetType)
        {
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }

    }

    public enum ExceptionStrategy
    {
        Handle, ReThrow, ThrowNew
    }
}
