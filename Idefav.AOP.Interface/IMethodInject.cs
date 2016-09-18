using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Idefav.AOP.Interface
{
    public interface IMethodInject
    {
        bool Executeing(MethodExecutionEventArgs args);
        ExceptionStrategy Exceptioned(MethodExecutionEventArgs args);
        void Executed(MethodExecutionEventArgs args);
    }
}
