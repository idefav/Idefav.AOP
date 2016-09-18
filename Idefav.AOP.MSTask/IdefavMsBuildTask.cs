using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Idefav.AOP.InjectTask;

namespace Idefav.AOP.MSTask
{
    public class IdefavMsBuildTask : Microsoft.Build.Utilities.Task
    {
        [Microsoft.Build.Framework.Required]
        public string OutputFile
        {
            get;
            set;
        }

        [Microsoft.Build.Framework.Required]
        public string TaskFile
        {
            get;
            set;
        }

        public override bool Execute()
        {
            IMethodILInjectTask task = new MethodILInjectTask(OutputFile);
            task.Run();
            return true;
        }
    }
}
