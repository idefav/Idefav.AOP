using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Idefav.AOP.Interface;

namespace Idefav.AOP.TestApp
{
    public class Program
    {

        public static void Main(string[] args)
        {
            new testclass().testm();
            Console.WriteLine(new testclass().test2());
            var testclass = new testclass();
            testclass.TestPro3 = "2";
            Console.WriteLine(testclass.TestPro3);
            Console.ReadLine();
        }
    }

    [TestAOP1]
    public class testclass
    {

        public string testm()
        {
            var methodbase = MethodBase.GetCurrentMethod();
            var ar = methodbase.GetCustomAttributes(typeof(IMethodInject), false);
            IMethodInject methodInject = ar.Length > 0 ? ar[0] as IMethodInject : null;
            object instance = null;

            if (!methodbase.IsStatic)
            {
                instance = this;
            }

            MethodExecutionEventArgs args = new MethodExecutionEventArgs(methodbase, instance, ar);
            methodInject.Executeing(args);
            //throw new Exception("Exception");
           
            try
            {
                Console.WriteLine("TestM函数执行完成");
                var value = "this is a test string";
                return value;
            }
            catch (Exception e)
            {
               var result= methodInject.Exceptioned(args);
                switch (result)
                {
                        case ExceptionStrategy.Handle:
                    {
                            Console.WriteLine(e.ToString());
                        break;

                    }
                        case ExceptionStrategy.ReThrow:
                    {
                        throw e;
                        
                    }
                        case ExceptionStrategy.ThrowNew:
                    {
                        throw  new Exception(e.ToString());
                    }
                    default:
                    {
                            Console.WriteLine(e.ToString());
                        break;
                    }
                }
                Activator.CreateInstance()

                return default(string);
            }
            //args.ReturnValue = value;
            //methodInject.Executed(args);
           
        }

        public string test2()
        {
            Console.WriteLine("Test2");
            return "Test2 完成";
        }

        [PropertyInterceptBase(Action = PropertyInterceptAction.Get)]
        public string TestPro3 { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
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
            Console.WriteLine("返回结果:" + args.ReturnValue);
        }

        #endregion

        #region IMethodInject Members

        public bool Match(System.Reflection.MethodBase method)
        {
            return true;
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class TestAopProAttribute : PropertyInterceptBase
    {
        public override bool Executeing(MethodExecutionEventArgs args)
        {
            Console.WriteLine(this.GetType() + ":" + "Executeing");
            return true;
        }

        public override ExceptionStrategy Exceptioned(MethodExecutionEventArgs args)
        {
            return ExceptionStrategy.ReThrow;
        }

        public override void Executed(MethodExecutionEventArgs args)
        {
            Console.WriteLine(this.GetType() + ":" + "ExecuteSuccess");
            Console.WriteLine("返回结果:" + args.ReturnValue);
        }
    }
}
