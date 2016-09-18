﻿using System;

namespace Idefav.AOP.Interface
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class MatchedMethodInterceptBase : Attribute, IMethodInject
    {
       

        #region IMethodInject Members

        public virtual bool Executeing(MethodExecutionEventArgs args)
        {
            return true;
        }

        public virtual ExceptionStrategy Exceptioned(MethodExecutionEventArgs args)
        {
            return ExceptionStrategy.ReThrow;
        }

        public virtual void Executed(MethodExecutionEventArgs args)
        {
            
        }

       
       
        #endregion
    }
}
