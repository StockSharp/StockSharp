#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: XMLCommToHTM.DOM.Internal.DocViewer
File: TypeUtilsOld.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMLCommToHTM.DOM.Internal
{
	
    /*
    public static class TypeUtilsOld
    {
     
        //Строка совместимая с обозначениями в XML документации, для generic параметров и типов.
        public static string ToString(Type type, char openGen, char closeGen)
        {
            //int genCount = type.GetGenericArguments().Length;
            if (type.IsArray && type.ContainsGenericParameters)
            //if (type.IsArray && 0<genCount)
            {
                Type elType = GetRootElementType(type);
                string newElemName = ToString(elType,openGen,closeGen);
                return type.Name.Replace(elType.Name, newElemName);
            }
            if (type.IsGenericParameter)
                return (type.DeclaringMethod == null ? "`" : "``") + type.GenericParameterPosition.ToString();
            if (type.IsGenericType)
            {
                string ret2 =
                    (type.DeclaringType == null ? type.Namespace : ToString(type.DeclaringType, openGen,closeGen)) + ".";

                ret2 += type.Name.Split('`')[0];

                var genericArgs = GetGenericArgumentsOnlyOwn(type);
                if (genericArgs.Length > 0)
                    ret2 +=
                         openGen + 
                         genericArgs
                         .Select(_=>ToString(_,openGen,closeGen)).Aggregate((s1, s2) => s1 + "," + s2)
                        + closeGen;
                return ret2;
            }

            return ReplacePlus(type.FullName);
        }


        //-------------------------------------------
        //public static string ToStringRec(Type type, Type[] genericArguments, char openGen, char closeGen)
        //{
        //    if (type.IsArray)
        //    {
        //        Type elType = GetRootElementType(type);
        //        if (0 < genericArguments.Length || elType.IsGenericParameter)
        //        {
        //            string newElemName = ToStringRec(elType, genericArguments, openGen, closeGen);
        //            return type.Name.Replace(elType.Name, newElemName);
        //        }
        //    }
        //    if (type.IsGenericParameter)
        //        return (type.DeclaringMethod == null ? "`" : "``") + type.GenericParameterPosition.ToString();
        //    if (type.IsGenericType)
        //    {
        //        string ret2 = "";
        //        if (type.DeclaringType == null)
        //            ret2 = type.Namespace;
        //        else
        //        {
        //            var parentArgs = genericArguments.Take(type.DeclaringType.GetGenericArguments().Length).ToArray();
        //            ret2 = ToStringRec(type.DeclaringType, parentArgs, openGen, closeGen);
        //        }
        //        ret2 += ".";

        //        ret2 += type.Name.Split('`')[0];

        //        var genericArgs = GetGenericArgumentsOnlyOwn(type, genericArguments);
        //        if (genericArgs.Length > 0)
        //            ret2 +=
        //                 openGen +
        //                 genericArgs
        //                 .Select(_ => ToStringRec(_, _.GetGenericArguments() , openGen, closeGen)).Aggregate((s1, s2) => s1 + "," + s2)
        //                + closeGen;
        //        return ret2;
        //    }
        //    return ReplacePlus(type.FullName);
        //}
     
        
        //static Type[] GetGenericArgumentsOnlyOwn(Type type)
        //{
        //    var args = type.GetGenericArguments();
        //    if (type.DeclaringType != null)
        //        args = args.Skip(type.DeclaringType.GetGenericArguments().Length).ToArray();
        //    return args;
        //}   
        
        
        //static Type[] GetGenericArgumentsOnlyOwn(Type type)
        //{
        //    var args = type.GetGenericArguments();
        //    if (type.DeclaringType == null)
        //        return args;

        //    var defArgs = type.GetGenericTypeDefinition().GetGenericArguments();
        //    if(defArgs.Length!=args.Length)
        //        throw new Exception();
        //    var ret=new List<Type>();
        //    for (int i = 0; i < args.Length; i++)
        //    {
        //        if(args[i].Name!=defArgs[i].Name)
        //            ret.Add(args[i]);
        //    }
        //    return ret.ToArray();
        //}
        

    

    }
    */
}
