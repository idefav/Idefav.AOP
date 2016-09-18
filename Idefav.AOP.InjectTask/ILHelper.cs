using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Idefav.AOP.InjectTask
{
    public static class ILHelper
    {
        /// <summary>
        /// 添加变量
        /// </summary>
        /// <param name="method"></param>
        /// <param name="variable"></param>
        public static void MethodAddVar(this MethodDefinition method, VariableDefinition variable)
        {
            if (method.Body.HasVariables)
            {
                method.Body.Variables.Remove(variable);
            }
            method.Body.Variables.Add(variable);
        }

        /// <summary>
        /// 添加多个变量
        /// </summary>
        /// <param name="method"></param>
        /// <param name="variables"></param>
        public static void MethodAddVars(this MethodDefinition method, VariableDefinition[] variables)
        {
            foreach (VariableDefinition variableDefinition in variables)
            {
               method.MethodAddVar(variableDefinition);
            }
        }
    }

    public class ILMethodBuilder
    {
        public List<Instruction> Instructions { get; set; }

        public TryCatchPoint TryCatchPoint { get; set; }

        public ExceptionHandler ExceptionHandler { get; set; }

        public VariableDefinition ExceptionVar { get; set; }

        public MethodDefinition Method { get; set; }

        public ILProcessor IL { get; set; }

        public Instruction Lastreturn { get; set; }

        public VariableDefinition ReturnVar { get; set; }

        public ILMethodBuilder(MethodDefinition method)
        {
            Instructions = method.Body.Instructions.ToList();
            this.Method = method;
            TryCatchPoint=new TryCatchPoint();
            IL = this.Method.Body.GetILProcessor();
            this.Lastreturn = IL.Create(OpCodes.Ret);

        }

        public ILMethodBuilder(MethodDefinition method, VariableDefinition returnVariableDefinition)
        {
            Instructions = method.Body.Instructions.ToList();
            TryCatchPoint = new TryCatchPoint();
            IL = this.Method.Body.GetILProcessor();
            this.Lastreturn = IL.Create(OpCodes.Ldloc_S,returnVariableDefinition);

        }

        public ILMethodBuilder AddVar(VariableDefinition variable)
        {
            Method.Body.Variables.Add(variable);
            return this;
        }

        public ILMethodBuilder AddVars(VariableDefinition[] variables)
        {
            foreach (var variableDefinition in variables)
            {
                Method.Body.Variables.Add(variableDefinition);
            }
            return this;
        }

        public ILMethodBuilder Add(Instruction instruction)
        {
            Instructions.Add(instruction);
            return this;
        }

        public ILMethodBuilder AddRange(Instruction[] instructions)
        {
            Instructions.AddRange(instructions);
            return this;
        }

        public ILMethodBuilder SetTryStart()
        {
            TryCatchPoint.TryStart = Instructions.Last();
            return this;
        }

        public ILMethodBuilder SetTryEnd()
        {
            TryCatchPoint.TryEnd = Instructions.Last();
            return this;
        }

        public ILMethodBuilder SetHandleStart()
        {
            TryCatchPoint.HandleStart = Instructions.Last();
            return this;
        }

        public ILMethodBuilder SetHandleEnd()
        {
            TryCatchPoint.HandleEnd = Instructions.Last();
            return this;
        }

        public ILMethodBuilder AddExceptonVar(VariableDefinition exVariableDefinition)
        {
            this.ExceptionVar = exVariableDefinition;
            Method.Body.Variables.Add(this.ExceptionVar);
            return this;
        }

        public ExceptionHandler CreateExceptionHandler_Catch(TypeReference catchtype)
        {
            return new ExceptionHandler(ExceptionHandlerType.Catch)
            {
                CatchType = Method.Module.Import(catchtype),
                TryStart = TryCatchPoint.TryStart,
                TryEnd = TryCatchPoint.TryEnd,
                HandlerStart = TryCatchPoint.HandleStart,
                HandlerEnd = TryCatchPoint.HandleEnd,

            };
        }

        public void Build()
        {
            ILProcessorExsions.Append(IL,this.Instructions.ToArray());
            if (ExceptionVar != null)
            {
                this.Method.Body.ExceptionHandlers.Add(CreateExceptionHandler_Catch(ExceptionVar.VariableType));
            }
            
        }

       
    }

    public class TryCatchPoint
    {
        public Instruction TryStart { get; set; }

        public Instruction TryEnd { get; set; }

        public Instruction HandleStart { get; set; }

        public Instruction HandleEnd { get; set; }

    }
}
