using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Idefav.AOP.InjectTask
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
    public class NonInject : Attribute
    {

    }
}
