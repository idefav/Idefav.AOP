using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Idefav.AOP.Interface;

namespace Idefav.AOP.TestApp
{
    public class Program
    {

        public static void Main(string[] args)
        {
         new testclass().testm();  
            Console.ReadLine();
        }
    }


    public class testclass
    {
        [TestAOP1]
        public void testm()
        {
            throw new Exception("Exception");
            Console.WriteLine("TestM函数执行完成");
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAOP1Attribute : Attribute, IMethodInject
    {

        #region IMethodInject Members

        public bool Executeing(MethodExecutionEventArgs args)
        {
            Console.WriteLine(this.GetType() + ":" + "Executeing");
            return true;
        }



        public ExceptionStrategy Exceptioned(MethodExecutionEventArgs args)
        {
            Console.WriteLine(this.GetType() + ":" + "Exceptioned");
            return ExceptionStrategy.Handle;
        }

        public void Executed(MethodExecutionEventArgs args)
        {
            Console.WriteLine(this.GetType() + ":" + "ExecuteSuccess");
        }

        #endregion

        #region IMethodInject Members


        public bool Match(System.Reflection.MethodBase method)
        {
            return true;
        }

        #endregion
    }
}
