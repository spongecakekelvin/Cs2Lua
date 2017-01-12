﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.Semantics;

namespace RoslynTool.CsToLua
{
    internal class ArgDefaultValueInfo
    {
        internal object Value;
        internal object OperOrSym;
    }
    internal class InvocationInfo
    {
        internal string ClassKey = string.Empty;
        internal string GenericClassKey = string.Empty;
        internal List<ExpressionSyntax> Args = new List<ExpressionSyntax>();
        internal List<ArgDefaultValueInfo> DefaultValueArgs = new List<ArgDefaultValueInfo>();
        internal List<ExpressionSyntax> ReturnArgs = new List<ExpressionSyntax>();
        internal List<ITypeSymbol> GenericTypeArgs = new List<ITypeSymbol>();
        internal bool ArrayToParams = false;
        internal bool IsComponentGetOrAdd = false;
        internal bool IsBasicValueMethod = false;
        internal bool IsArrayStaticMethod = false;
        internal ExpressionSyntax FirstRefArray = null;
        internal ExpressionSyntax SecondRefArray = null;

        internal IMethodSymbol MethodSymbol = null;
        internal IAssemblySymbol AssemblySymbol = null;

        internal void Init(IMethodSymbol sym, ArgumentListSyntax argList, SemanticModel model)
        {
            IAssemblySymbol assemblySym = SymbolTable.Instance.AssemblySymbol;
            Init(sym);

            if (null != argList) {
                var args = argList.Arguments;

                int ct = args.Count;
                for (int i = 0; i < ct; ++i) {
                    var arg = args[i];                    
                    TryAddExternEnum(ClassKey, arg.Expression, model);
                    if (i < sym.Parameters.Length) {
                        var param = sym.Parameters[i];
                        if (!param.IsParams && param.Type.TypeKind == TypeKind.Array) {
                            RecordRefArray(arg.Expression);
                        }
                        if (param.RefKind == RefKind.Ref) {
                            Args.Add(arg.Expression);
                            ReturnArgs.Add(arg.Expression);
                        } else if (param.RefKind == RefKind.Out) {
                            //方法的out参数，为与Slua的机制一致，在调用时传入__cs2lua_out，这里用null标记一下，在实际输出参数时再变为__cs2lua_out
                            Args.Add(null);
                            ReturnArgs.Add(arg.Expression);
                        } else if (param.IsParams) {
                            var argOper = model.GetOperation(arg.Expression);
                            if (null != argOper && null != argOper.Type && argOper.Type.TypeKind == TypeKind.Array && i == ct - 1) {
                                ArrayToParams = true;
                            }
                            Args.Add(arg.Expression);
                        } else {
                            Args.Add(arg.Expression);
                        }
                    } else {
                        Args.Add(arg.Expression);
                    }
                }
                for (int i = ct; i < sym.Parameters.Length; ++i) {
                    var param = sym.Parameters[i];
                    if (param.HasExplicitDefaultValue) {
                        var decl = param.DeclaringSyntaxReferences;
                        if (decl.Length == 1) {
                            var node = param.DeclaringSyntaxReferences[0].GetSyntax() as ParameterSyntax;
                            if (null != node) {
                                var exp = node.Default.Value;
                                var tree = node.SyntaxTree;
                                var newModel = SymbolTable.Instance.Compilation.GetSemanticModel(tree, true);
                                if (null != newModel) {
                                    var oper = newModel.GetOperation(exp);
                                    //var dsym = newModel.GetSymbolInfo(exp).Symbol;
                                    DefaultValueArgs.Add(new ArgDefaultValueInfo { Value = param.ExplicitDefaultValue, OperOrSym = oper });
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void Init(IMethodSymbol sym, BracketedArgumentListSyntax argList, SemanticModel model)
        {
            IAssemblySymbol assemblySym = SymbolTable.Instance.AssemblySymbol; 
            Init(sym);

            if (null != argList) {
                var args = argList.Arguments;
                int ct = args.Count;
                for (int i = 0; i < ct; ++i) {
                    var arg = args[i];
                    TryAddExternEnum(ClassKey, arg.Expression, model);
                    if (i < sym.Parameters.Length) {
                        var param = sym.Parameters[i];
                        if (!param.IsParams && param.Type.TypeKind == TypeKind.Array) {
                            RecordRefArray(arg.Expression);
                        }
                        if (param.RefKind == RefKind.Ref) {
                            Args.Add(arg.Expression);
                            ReturnArgs.Add(arg.Expression);
                        } else if (param.RefKind == RefKind.Out) {
                            //方法的out参数，为与Slua的机制一致，在调用时传入__cs2lua_out，这里用null标记一下，在实际输出参数时再变为__cs2lua_out
                            Args.Add(null);
                            ReturnArgs.Add(arg.Expression);
                        } else if (param.IsParams) {
                            var argOper = model.GetOperation(arg.Expression);
                            if (null != argOper && null != argOper.Type && argOper.Type.TypeKind == TypeKind.Array && i == ct - 1) {
                                ArrayToParams = true;
                            }
                            Args.Add(arg.Expression);
                        } else {
                            Args.Add(arg.Expression);
                        }
                    } else {
                        Args.Add(arg.Expression);
                    }
                }
                for (int i = ct; i < sym.Parameters.Length; ++i) {
                    var param = sym.Parameters[i];
                    if (param.HasExplicitDefaultValue) {
                        var decl = param.DeclaringSyntaxReferences;
                        if (decl.Length == 1) {
                            var node = param.DeclaringSyntaxReferences[0].GetSyntax() as ParameterSyntax;
                            if (null != node) {
                                var exp = node.Default.Value;
                                var tree = node.SyntaxTree;
                                var newModel = SymbolTable.Instance.Compilation.GetSemanticModel(tree, true);
                                if (null != newModel) {
                                    var oper = newModel.GetOperation(exp);
                                    //var dsym = newModel.GetSymbolInfo(exp).Symbol;
                                    DefaultValueArgs.Add(new ArgDefaultValueInfo { Value = param.ExplicitDefaultValue, OperOrSym = oper });
                                }
                            }
                        }
                    }
                }
            }
        }

        internal void Init(IMethodSymbol sym, List<ExpressionSyntax> argList, SemanticModel model)
        {
            Init(sym);

            if (null != argList) {
                for (int i = 0; i < argList.Count; ++i) {
                    var arg = argList[i];
                    var oper = model.GetOperation(arg);
                    if (null != oper && null != oper.Type && oper.Type.TypeKind == TypeKind.Array) {
                        RecordRefArray(arg);
                    }
                    TryAddExternEnum(ClassKey, arg, model);
                }
                Args.AddRange(argList);
            }
        }
        
        internal void OutputInvocation(StringBuilder codeBuilder, CsLuaTranslater cs2lua, ExpressionSyntax exp, bool isMemberAccess, SemanticModel model, SyntaxNode node)
        {
            IMethodSymbol sym = MethodSymbol;
            string mname = cs2lua.NameMangling(sym);
            string prestr = string.Empty;
            if (isMemberAccess) {
                string fnOfIntf = "nil";
                bool isExplicitInterfaceInvoke = cs2lua.CheckExplicitInterfaceAccess(sym, ref fnOfIntf);
                if (isExplicitInterfaceInvoke) {
                    codeBuilder.Append("invokewithinterface(");
                    cs2lua.VisitExpressionSyntax(exp);
                    codeBuilder.Append(", ");
                    codeBuilder.AppendFormat("{0}, \"{1}\"", fnOfIntf, mname);
                    prestr = ", ";
                } else if (IsBasicValueMethod) {
                    string ckey = CalcInvokeTarget(ClassKey, cs2lua, exp, model);
                    codeBuilder.Append("invokeforbasicvalue(");
                    cs2lua.VisitExpressionSyntax(exp);
                    codeBuilder.Append(", ");
                    codeBuilder.AppendFormat("{0}, {1}, \"{2}\"", ClassKey == "System.Enum" ? "true" : "false", ckey, mname);
                    prestr = ", ";
                } else if (IsArrayStaticMethod) {
                    codeBuilder.Append("invokearraystaticmethod(");
                    if (null == FirstRefArray) {
                        codeBuilder.Append("nil, ");
                    } else {
                        cs2lua.VisitExpressionSyntax(FirstRefArray);
                        codeBuilder.Append(", ");
                    }
                    if (null == SecondRefArray) {
                        codeBuilder.Append("nil, ");
                    } else {
                        cs2lua.VisitExpressionSyntax(SecondRefArray);
                        codeBuilder.Append(", ");
                    }
                    codeBuilder.AppendFormat("\"{0}\"", mname);
                    prestr = ", ";
                } else {
                    if (sym.IsStatic) {
                        codeBuilder.Append(ClassKey);
                        codeBuilder.Append(".");
                    } else {
                        cs2lua.VisitExpressionSyntax(exp);
                        codeBuilder.Append(":");
                    }
                    codeBuilder.Append(mname);
                    codeBuilder.Append("(");
                }
            } else {
                if (sym.MethodKind == MethodKind.DelegateInvoke) {
                    cs2lua.VisitExpressionSyntax(exp);
                } else if (sym.IsStatic) {
                    codeBuilder.AppendFormat("{0}.", ClassKey);
                    codeBuilder.Append(mname);
                } else {
                    codeBuilder.Append("this:");
                    codeBuilder.Append(mname);
                }
                codeBuilder.Append("(");
            }
            if (Args.Count + DefaultValueArgs.Count + GenericTypeArgs.Count > 0) {
                codeBuilder.Append(prestr);
            }
            bool useTypeNameString = false;
            if(IsComponentGetOrAdd && SymbolTable.LuaComponentByString){
                var tArgs = sym.TypeArguments;
                if (tArgs.Length > 0 && tArgs[0].ContainingAssembly == AssemblySymbol) {
                    useTypeNameString = true;
                }
            }
            cs2lua.OutputArgumentList(Args, DefaultValueArgs, GenericTypeArgs, ArrayToParams, useTypeNameString, node);
            codeBuilder.Append(")");
        }

        private void Init(IMethodSymbol sym)
        {
            MethodSymbol = sym;
            AssemblySymbol = SymbolTable.Instance.AssemblySymbol;;

            Args.Clear();
            ReturnArgs.Clear();
            GenericTypeArgs.Clear();
            
            ClassKey = ClassInfo.GetFullName(sym.ContainingType);
            GenericClassKey = ClassInfo.GetFullNameWithTypeParameters(sym.ContainingType);
            IsBasicValueMethod = SymbolTable.IsBasicValueMethod(sym);
            IsArrayStaticMethod = ClassKey == "System.Array" && sym.IsStatic;

            if ((ClassKey == "UnityEngine.GameObject" || ClassKey == "UnityEngine.Component") && (sym.Name.StartsWith("GetComponent") || sym.Name.StartsWith("AddComponent"))) {
                IsComponentGetOrAdd = true;
            }

            if (sym.IsGenericMethod) {
                foreach (var arg in sym.TypeArguments) {
                    GenericTypeArgs.Add(arg);
                }
            }
        }

        private void RecordRefArray(ExpressionSyntax exp)
        {
            if (IsArrayStaticMethod) {
                if (null == FirstRefArray) {
                    FirstRefArray = exp;
                } else if (null == SecondRefArray) {
                    SecondRefArray = exp;
                }
            }
        }

        internal static void TryAddExternEnum(string classKey, ExpressionSyntax exp, SemanticModel model)
        {
            if (classKey == "System.Enum") {
                var oper = model.GetOperation(exp);
                if (oper.Type.ContainingAssembly != SymbolTable.Instance.AssemblySymbol && oper.Type.TypeKind == TypeKind.Enum) {
                    string ckey = ClassInfo.GetFullName(oper.Type);
                    SymbolTable.Instance.AddExternEnum(ckey, oper.Type);
                } else {
                    var typeOf = oper as ITypeOfExpression;
                    if (null != typeOf && typeOf.TypeOperand.ContainingAssembly != SymbolTable.Instance.AssemblySymbol && typeOf.TypeOperand.TypeKind == TypeKind.Enum) {
                        string ckey = ClassInfo.GetFullName(typeOf.TypeOperand);
                        SymbolTable.Instance.AddExternEnum(ckey, typeOf.TypeOperand);
                    }
                }
            }
        }

        internal static string CalcInvokeTarget(string classKey, CsLuaTranslater cs2lua, ExpressionSyntax exp, SemanticModel model)
        {
            TryAddExternEnum(classKey, exp, model);
            string ckey = classKey;
            if (classKey == "System.Enum") {
                var oper = model.GetOperation(exp);
                if (oper.Type.TypeKind == TypeKind.Enum) {
                    var ci = cs2lua.GetCurClassInfo();
                    ci.AddReference(oper.Type);

                    ckey = ClassInfo.GetFullName(oper.Type);
                }
            }
            return ckey;
        }
    }
}