using System;

namespace Idefav.AOP.Interface
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class PropertyInterceptBase : Attribute, IMethodInject
    {
        public PropertyInterceptAction Action
        {
            get;
            set;
        }

       
        #region IMethodInject Members

        public virtual bool Executeing(MethodExecutionEventArgs args)
        {
            return true;
        }

      

        public ExceptionStrategy Exceptioned(MethodExecutionEventArgs args)
        {
            return ExceptionStrategy.ReThrow;
        }

        public virtual void Executed(MethodExecutionEventArgs args)
        {
           
        }

        #endregion
    }

    public enum PropertyInterceptAction
    {
       None, Get, Set
    }
}
