namespace Idefav.AOP.InjectTask
{

    class Program
    {
        static void Main(string[] args)
        {
            //Assembly.GetExecutingAssembly().

            IMethodILInjectTask task = new MethodILInjectTask(args[0]);
            task.Run();           
        }

    }
}