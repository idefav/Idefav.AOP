using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Idefav.AOP.Interface;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Idefav.AOP.InjectTask
{
    public class MethodILInjectTask : IMethodILInjectTask
    {
        private string _binPath = string.Empty;

        private AssemblyDefinition assembly = null;
        public MethodILInjectTask(string binpath)
        {
            if (string.IsNullOrEmpty(binpath))
            {
                throw new Exception("The binary source file is shoulded!");
            }
            this._binPath = binpath;

            assembly = AssemblyDefinition.ReadAssembly(binpath);
        }

        public void Run()
        {
            CheckModules(assembly);
            //assembly.Write(System.IO.Path.GetFileName(_binPath));
            assembly.Write(_binPath);
        }

        protected virtual void CheckModules(AssemblyDefinition assembly)
        {
            foreach (var modeul in assembly.Modules)
            {
                CheckTypes(modeul);
            }
        }

        public static bool MatchedMethodInterceptBaseMatch(string Rule, string method)
        {
            var re = Rule.Replace("*", @"\w*").Replace("-", @"\w");
            return System.Text.RegularExpressions.Regex.IsMatch(method, string.Format("^{0}$", re), System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        protected virtual void CheckTypes(ModuleDefinition modeul)
        {
            var mtype = modeul.Types.Where(t => !t.IsSpecialName).Where(t => !t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName)).ToList();

            mtype.Where(t => t.CustomAttributes.Any(k => IsSubclassOf(k.AttributeType.Resolve(), t.Module.Import(typeof(IMethodInject)).Resolve(), true)))
                .Select(t =>
                    new
                    {
                        Type = t,
                        CustomAttributes = t.CustomAttributes.Where(attr => IsSubclassOf(attr.AttributeType.Resolve(), t.Module.Import(typeof(IMethodInject)).Resolve(), true)).ToList(),
                    })
                .ToList().ForEach(
                t =>
                {
                    t.CustomAttributes.ForEach(attr =>
                    {

                        t.Type.Methods.Where(m => !m.IsSpecialName && !m.IsSetter && !m.IsGetter &&!m.CustomAttributes.Any(c=>c.AttributeType.FullName==typeof(NonInject).FullName)
                       && !t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName))
                       .ToList().ForEach(
                       k =>
                       {
                           k.CustomAttributes.Add(attr);
                           DealMethodInject(attr, t.Type, k, InjectAttributeUsage.Class);
                           //k.CustomAttributes.Remove(attr);
                       });
                    });

                });

            mtype.Where(t =>!(t.CustomAttributes.Any(k => IsSubclassOf(k.AttributeType.Resolve(), t.Module.Import(typeof(IMethodInject)).Resolve(), true)))).ToList().ForEach(t =>
            {
                CheckMethods(t);
               
            });
            mtype.ForEach(t =>
            {
                CheckPropertys(t);
            });
        }

        protected virtual void CheckPropertys(TypeDefinition mtype)
        {
            var propertys = mtype.Properties.Where(t => !t.IsSpecialName).Where(t => t.HasCustomAttributes
                    && !t.CustomAttributes.Any(k => k.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName)).ToList();
            propertys.ForEach(
                p =>
                {
                    var customerAttributes = p.CustomAttributes.Where(k => IsSubclassOf(k.AttributeType.Resolve(), p.Module.Import(typeof(PropertyInterceptBase)).Resolve(), false))
                      .Select(t =>
                          new
                          {
                              Property = p,
                              Attribute = t,
                              Order = t.Properties.Any(tp => tp.Name == "Order") ? (int)t.Properties.SingleOrDefault(tp => tp.Name == "Order").Argument.Value : int.MaxValue,
                              Action = t.Properties.Any(tp => tp.Name == "Action") ? (PropertyInterceptAction)t.Properties.SingleOrDefault(tp => tp.Name == "Action").Argument.Value : PropertyInterceptAction.None,
                          }).OrderBy(t => t.Order).ToList();

                    customerAttributes.ForEach(t =>
                        {
                            if (t.Action == PropertyInterceptAction.Get && t.Property.GetMethod != null)
                            {
                                DealMethodInject(t.Attribute, mtype, t.Property.GetMethod, InjectAttributeUsage.Property, t.Property);
                            }
                            else if (t.Action == PropertyInterceptAction.Set && t.Property.SetMethod != null)
                            {
                                DealMethodInject(t.Attribute, mtype, t.Property.SetMethod, InjectAttributeUsage.Property, t.Property);
                            }
                        });
                });
        }

        protected virtual void CheckMethods(TypeDefinition mtype)
        {
            var methods = mtype.Methods.Where(t => !t.IsSpecialName && !t.IsSetter && !t.IsGetter).ToList();
            for (var i = methods.Count - 1; i >= 0; i--)
            {
                var method = methods[i];
                if (method.HasCustomAttributes && !method.CustomAttributes
                    .Any(t => t.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName))
                {
                    var customerAttributes = method.CustomAttributes
                        .Where(t => IsSubclassOf(t.AttributeType.Resolve(), mtype.Module.Import(typeof(IMethodInject)).Resolve(), true))
                        .Select(t =>
                            new
                            {
                                Attribute = t,
                                Order = t.Properties.Any(p => p.Name == "Order") ? (int)t.Properties.SingleOrDefault(p => p.Name == "Order").Argument.Value : int.MaxValue,
                            }).OrderBy(t => t.Order).Select(t => t.Attribute).ToList();

                    customerAttributes.ForEach(
                            t => DealMethodInject(t, mtype, method, InjectAttributeUsage.Method)
                        );

                }
            }
        }

        public static bool IsSubclassOf(TypeDefinition type, TypeDefinition baseType, bool isInterface)
        {
            if (type == null || baseType == null)
                return false;
            if (type.FullName == typeof(object).FullName)
            {
                return false;
            }
            if (isInterface)
            {
                if (type.Interfaces.Any(t => t.FullName == baseType.FullName))
                {
                    return true;
                }
            }
            else
            {
                if (type.FullName == baseType.FullName)
                {
                    return true;
                }
            }
            return IsSubclassOf(type.BaseType.Resolve(), baseType, isInterface);
        }

        protected virtual void DealMethodInject(CustomAttribute methodInject, TypeDefinition mtype, MethodDefinition method, InjectAttributeUsage usage)
        {
            DealMethodInject(methodInject, mtype, method, usage, null);
        }
        protected virtual void DealMethodInject(CustomAttribute methodInject, TypeDefinition mtype, MethodDefinition method, InjectAttributeUsage usage, PropertyDefinition property)
        {
            var il = method.Body.GetILProcessor();
            var module = method.Module;

            var newmethod = CompilerGeneratedNewMethod(method, module);

            if (!newmethod.CustomAttributes.Any(t => t.AttributeType.FullName == typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).FullName))
            {
                newmethod.CustomAttributes.Add(new CustomAttribute(module.Import(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute).GetConstructor(new Type[0]))));
            }

            mtype.Methods.Add(newmethod);

            method.Body.Instructions.Clear();
            method.Body.ExceptionHandlers.Clear();
            method.Body.Variables.Clear();
            method.Body.Instructions.Add(il.Create(OpCodes.Nop));
            method.Body.InitLocals = false;
            ILMethodBuilder methodBuilder = new ILMethodBuilder(method);
            methodBuilder.Method = method;
            methodBuilder.SetTryStart();
            //var varexception = new VariableDefinition(module.Import(typeof(System.Exception)));
            //method.Body.Variables.Add(varexception);
            methodBuilder.AddExceptonVar(new VariableDefinition(module.Import(typeof(System.Exception))));

            var imethodInject = new VariableDefinition(module.Import(typeof(IMethodInject)));
            methodBuilder.AddVar(imethodInject);

            var varmethodexcetionEventargs = new VariableDefinition(module.Import(typeof(MethodExecutionEventArgs)));
            methodBuilder.AddVar(varmethodexcetionEventargs);

            var varmethodbase = new VariableDefinition(module.Import(typeof(MethodBase)));
            methodBuilder.AddVar(varmethodbase);
            var varinstance = new VariableDefinition(module.Import(typeof(object)));
            methodBuilder.AddVar(varinstance);
            var varparams = new VariableDefinition(module.Import(typeof(object[])));
            methodBuilder.AddVar(varparams);

            var varExceptionStrategy = new VariableDefinition(module.Import(typeof(ExceptionStrategy)));
            methodBuilder.AddVar(varExceptionStrategy);

            // MethodBase
            if (usage == InjectAttributeUsage.Property)
            {
                methodBuilder.AddRange(new[]
                {
                     il.Create(OpCodes.Ldtoken,mtype),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                     il.Create(OpCodes.Ldstr,property.Name),
                     il.Create(OpCodes.Ldtoken,method.IsGetter?method.ReturnType:method.Parameters[0].ParameterType),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
                     il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetProperty",new Type[]{typeof(string),typeof(Type)}))),
                     il.Create(OpCodes.Stloc_S,varmethodbase),
                });
            }
            else
            {
                methodBuilder.AddRange(new[]
            {
                il.Create(OpCodes.Nop),
                il.Create(OpCodes.Call,module.Import(typeof(MethodBase).GetMethod("GetCurrentMethod"))),
                il.Create(OpCodes.Stloc_S,varmethodbase),
            });
            }
            

            // 函数参数
            methodBuilder.AddRange(new[]
            {
                il.Create(OpCodes.Nop),
                il.Create(OpCodes.Ldloc_S, varmethodbase),
                il.Create(OpCodes.Ldtoken, module.Import(typeof (IMethodInject))),
                il.Create(OpCodes.Call,
                    module.Import(typeof (Type).GetMethod("GetTypeFromHandle", new[] {typeof (RuntimeTypeHandle)}))),
                il.Create(OpCodes.Ldc_I4_0),
                il.Create(OpCodes.Callvirt,
                    module.Import(typeof (MethodBase).GetMethod("GetCustomAttributes",
                        new[] {typeof (Type), typeof (bool)}))),
                il.Create(OpCodes.Stloc_S, varparams),
                il.Create(OpCodes.Ldloc_S, varparams),
                il.Create(OpCodes.Ldlen)
            })
                .Ifelsetrue(() => new List<Instruction>
                {
                    il.Create(OpCodes.Ldloc_S, varparams),
                    il.Create(OpCodes.Ldc_I4_0),
                    il.Create(OpCodes.Ldelem_Ref),
                    il.Create(OpCodes.Isinst, module.Import(typeof (IMethodInject))),
                    il.Create(OpCodes.Stloc_S, imethodInject),
                }, () => new List<Instruction>
                {
                    il.Create(OpCodes.Ldnull),
                    il.Create(OpCodes.Stloc_S, imethodInject),
                })
                .AddRange(new[]
                {
                    il.Create(OpCodes.Ldnull),
                    il.Create(OpCodes.Stloc_S, varinstance),
                    il.Create(OpCodes.Ldloc_S, varmethodbase),
                    il.Create(OpCodes.Callvirt, module.Import(typeof (MethodBase).GetMethod("get_IsStatic"))),
                    il.Create(OpCodes.Ldc_I4_0),
                    il.Create(OpCodes.Ceq)

                })
            .Iffalse(() => new List<Instruction>
            {
                il.Create(OpCodes.Nop),
                
            }, () => new List<Instruction>
            {
                il.Create(OpCodes.Ldarg_0)

            });

            methodBuilder.AddRange(new[]
            {
                il.Create(OpCodes.Ldloc_S,varmethodbase),
                il.Create(OpCodes.Ldloc_S,varinstance),
                il.Create(OpCodes.Ldloc_S,varparams),
            });
            if (method.ReturnType.FullName != "System.Void")
            {
                methodBuilder.Add(il.Create(OpCodes.Ldstr, method.ReturnType.FullName));
            }
            else
            {
                methodBuilder.Add(il.Create(OpCodes.Ldnull));
            }
            methodBuilder.AddRange(new[]
            {
                
                il.Create(OpCodes.Newobj,module.Import(typeof(MethodExecutionEventArgs).GetConstructor(new [] {typeof(MethodBase),typeof(object),typeof(object[]),typeof(string)}))),
                il.Create(OpCodes.Stloc_S,varmethodexcetionEventargs)
            });


            // 初始化ImethodInject
            //methodBuilder.AddRange(new[]
            //{
            //    il.Create(OpCodes.Nop),
            //    il.Create(OpCodes.Ldtoken,methodInject.AttributeType ),
            //    il.Create(OpCodes.Call,module.Import(typeof(System.Type).GetMethod("GetTypeFromHandle",new Type[]{typeof(System.RuntimeTypeHandle)}))),
            //    il.Create(OpCodes.Ldc_I4_0),
            //    il.Create(OpCodes.Callvirt,module.Import(typeof(System.Reflection.MemberInfo).GetMethod("GetCustomAttributes",new Type[]{typeof(System.Type),typeof(bool)}))),
            //    il.Create(OpCodes.Ldc_I4_0),
            //    il.Create(OpCodes.Ldelem_Ref),
            //    il.Create(OpCodes.Isinst,methodInject.AttributeType),
            //    il.Create(OpCodes.Stloc_S,imethodInject)
            //});
            //执行开始函数
            methodBuilder.AddRange(new[]
            {
                il.Create(OpCodes.Ldloc_S,imethodInject),
                il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInject).GetMethod("Executeing",new Type[] {typeof(MethodExecutionEventArgs)}))),
                il.Create(OpCodes.Pop)
            });


            // 调用原来的函数
            // 判断是不是静态函数
            if (!method.IsStatic)
            {
                methodBuilder.Add(il.Create(OpCodes.Ldarg_0));
            }
            method.Parameters.ToList().ForEach(t =>
            {
                methodBuilder.Add(il.Create(OpCodes.Ldarg_S, t));
            });
            methodBuilder.AddRange(new[]
            {
                il.Create(OpCodes.Call,newmethod),

            });

            

            if (method.ReturnType.FullName != "System.Void")
            {
                var varreturnValue = new VariableDefinition(method.ReturnType);
                method.MethodAddVar(varreturnValue);
                methodBuilder.Lastreturn = il.Create(OpCodes.Ldloc_S, varreturnValue);

                methodBuilder.AddRange(new[]
                {
                    il.Create(OpCodes.Stloc_S, varreturnValue),
                    
                });
                // 执行完成函数
                methodBuilder.AddRange(new[]
                {
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Ldloc_S,varreturnValue),
                    il.Create(OpCodes.Box,module.Import(method.ReturnType)),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("set_ReturnValue",new []{typeof(object)}))),
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,imethodInject),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInject).GetMethod("Executed",new Type[] {typeof(MethodExecutionEventArgs)}))),
                    il.Create(OpCodes.Nop),
                });
                methodBuilder.AddRange(new[]
                {
                    il.Create(OpCodes.Leave_S, methodBuilder.Lastreturn),
                    il.Create(OpCodes.Nop)
                });

                methodBuilder.SetTryEnd()
                .SetHandleStart()
                .AddRange(new[]
                {
                    il.Create(OpCodes.Stloc_S, methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Ldloc_S, methodBuilder.ExceptionVar),
                    //il.Create(OpCodes.Box,module.Import(methodBuilder.ExceptionVar.GetType())),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("set_Exception",new []{typeof(Exception)}))),
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,imethodInject),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInject).GetMethod("Exceptioned",new Type[] {typeof(MethodExecutionEventArgs)}))),
                    il.Create(OpCodes.Stloc_S,varExceptionStrategy),
                    
                    //il.Create(OpCodes.Ldloc_S,methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Ldloc_S,varExceptionStrategy),
                    
                });

                //switch

               methodBuilder.Switch(() => new List<Instruction>
               {
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("get_ReturnValue",new Type[]{}))),
                    il.Create(OpCodes.Unbox_Any,method.ReturnType),
                    il.Create(OpCodes.Stloc_S,varreturnValue),
                    il.Create(OpCodes.Leave_S,methodBuilder.Lastreturn)
               }, // handle
               () => new List<Instruction>
               {
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("get_ReturnValue",new Type[]{}))),
                    il.Create(OpCodes.Unbox_Any,method.ReturnType),
                    il.Create(OpCodes.Stloc_S,varreturnValue),
                    il.Create(OpCodes.Leave_S,methodBuilder.Lastreturn)
               },
               // throw
               () => new List<Instruction>
               {
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Throw)
               },
               () => new List<Instruction>
               {
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Throw)
               });


                methodBuilder.SetHandleEnd()
                .AddRange(new[]
                {
                    methodBuilder.Lastreturn,
                    il.Create(OpCodes.Ret)
                });

            }
            else
            {
                // 执行完成函数
                methodBuilder.AddRange(new[]
                {
                    il.Create(OpCodes.Ldloc_S,imethodInject),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInject).GetMethod("Executed",new Type[] {typeof(MethodExecutionEventArgs)}))),
                    il.Create(OpCodes.Stloc_S,varExceptionStrategy),
                    il.Create(OpCodes.Nop)
                });
                methodBuilder.AddRange(new[]
                {
                    il.Create(OpCodes.Leave_S,methodBuilder.Lastreturn),
                    il.Create(OpCodes.Nop)
                })
                .SetTryEnd()
                .SetHandleStart()
                .AddRange(new[]
                {
                    il.Create(OpCodes.Stloc_S, methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Ldloc_S, methodBuilder.ExceptionVar),
                    //il.Create(OpCodes.Box,methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(MethodExecutionEventArgs).GetMethod("set_Exception",new []{typeof(Exception)}))),
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,imethodInject),
                    il.Create(OpCodes.Ldloc_S,varmethodexcetionEventargs),
                    il.Create(OpCodes.Callvirt,module.Import(typeof(IMethodInject).GetMethod("Exceptioned",new Type[] {typeof(MethodExecutionEventArgs)}))),
                    il.Create(OpCodes.Stloc_S,varExceptionStrategy),
                    il.Create(OpCodes.Ldloc_S,varExceptionStrategy),
                    //il.Create(OpCodes.Ldloc_S, methodBuilder.ExceptionVar),
                    //il.Create(OpCodes.Callvirt,method.Module.Import(typeof(System.Object).GetMethod("ToString"))),
                    //il.Create(OpCodes.Call,method.Module.Import(typeof(System.Console).GetMethod("WriteLine", new Type[] { typeof(string) }))),
                    //il.Create(OpCodes.Ldloc_S, methodBuilder.ExceptionVar),
                    //il.Create(OpCodes.Throw),
                    
                });

                // switch
                methodBuilder.Switch(() => new List<Instruction>
                {
                    il.Create(OpCodes.Nop),
                    
                }, // handle
                () => new List<Instruction>
                {
                    il.Create(OpCodes.Nop),
                    
                },
                // throw
                () => new List<Instruction>
                {
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Throw)
                },
                () => new List<Instruction>
                {
                    il.Create(OpCodes.Nop),
                    il.Create(OpCodes.Ldloc_S,methodBuilder.ExceptionVar),
                    il.Create(OpCodes.Throw)
                });

                methodBuilder.AddRange(new[]
                {
                    il.Create(OpCodes.Leave_S,methodBuilder.Lastreturn),
                    il.Create(OpCodes.Nop)
                });
                methodBuilder.SetHandleEnd()
                .Add(methodBuilder.Lastreturn);
            }

            methodBuilder.Build();


            //var business = il.Create(OpCodes.Call, newmethod);
            //var exception = il.Create(OpCodes.Stloc_S, varexception);
            //var tryEnd = il.Create(OpCodes.Nop);
            //var handerEnd = il.Create(OpCodes.Nop);
            //var rethrow = il.Create(OpCodes.Throw);




            //ILProcessorExsions.InsertAfter(il, method.Body.Instructions.Last(), new[] { business });

            //if (method.ReturnType.FullName != "System.Void")
            //{
            //    var varreturnValue = new VariableDefinition(method.ReturnType);
            //    method.Body.Variables.Add(varreturnValue);
            //    var lastreturn = il.Create(OpCodes.Ldloc_S, varreturnValue);
            //    ILProcessorExsions.Append(il, new[]
            //    {

            //        //il.Create(OpCodes.Unbox_Any, method.ReturnType),
            //        il.Create(OpCodes.Stloc_S, varreturnValue),
            //        //il.Create(OpCodes.Br_S, lastreturn),
            //        il.Create(OpCodes.Leave_S,lastreturn) ,
            //        tryEnd,
            //        exception,
            //        il.Create(OpCodes.Ldloc_0),
            //        rethrow,
            //        handerEnd,
            //        lastreturn

            //    });
            //}
            //else
            //{
            //    ILProcessorExsions.InsertAfter(il, method.Body.Instructions.Last(), new[]
            //    {
            //        tryEnd,
            //        exception,
            //        il.Create(OpCodes.Ldloc_0),
            //        rethrow,
            //        handerEnd,
            //    });
            //}

            ////method.Body.Instructions.Add(ret);

            //// try-catch
            //ILProcessorExsions.InsertAfter(il, method.Body.Instructions.Last(), new[] { ret });

            //var handler = new ExceptionHandler(ExceptionHandlerType.Catch)
            //{
            //    TryStart = method.Body.Instructions.First(),
            //    TryEnd = tryEnd,
            //    HandlerStart = tryEnd,
            //    HandlerEnd = handerEnd,
            //    CatchType = module.Import(typeof(Exception))
            //};
            //method.Body.ExceptionHandlers.Add(handler);
        }

        private MethodDefinition CompilerGeneratedNewMethod(MethodDefinition method, ModuleDefinition module)
        {
            var newmethod = new MethodDefinition(method.Name + (Guid.NewGuid().ToString().Replace("-", "_")), method.Attributes, method.ReturnType)
            {
                IsPrivate = true,
                IsStatic = method.IsStatic,
            };
            method.CustomAttributes.ToList().ForEach(t => { newmethod.CustomAttributes.Add(t); });
            method.Body.Instructions.ToList().ForEach(t => { newmethod.Body.Instructions.Add(t); });
            method.Body.Variables.ToList().ForEach(t => { newmethod.Body.Variables.Add(t); });
            method.Body.ExceptionHandlers.ToList().ForEach(t => { newmethod.Body.ExceptionHandlers.Add(t); });
            method.Parameters.ToList().ForEach(t => { newmethod.Parameters.Add(t); });
            method.GenericParameters.ToList().ForEach(t => { newmethod.GenericParameters.Add(t); });

            newmethod.Body.LocalVarToken = method.Body.LocalVarToken;
            newmethod.Body.InitLocals = method.Body.InitLocals;
            return newmethod;
        }

    }

    public class ILProcessorExsions
    {
        public static void InsertBefore(ILProcessor iLProcessor, Instruction target, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => iLProcessor.InsertBefore(target, t));
            }
        }

        public static void InsertAfter(ILProcessor iLProcessor, Instruction target, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => { iLProcessor.InsertAfter(target, t); target = t; });
            }
        }

        public static void Append(ILProcessor iLProcessor, Instruction[] ins)
        {
            if (ins != null && ins.Length > 0)
            {
                Array.ForEach(ins, t => { iLProcessor.Append(t); });
            }
        }
    }

    public enum InjectAttributeUsage
    {
        Method,
        Class,
        Property,
        Field
    }
}
