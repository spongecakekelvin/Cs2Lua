﻿using System;
using SLua;
using System.Collections.Generic;
[UnityEngine.Scripting.Preserve]
public class Lua_UnityEngine_Ray2D : LuaObject {
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int constructor(IntPtr l) {
		try {
			int argc = LuaDLL.lua_gettop(l);
			UnityEngine.Ray2D o;
			if(argc==3){
				UnityEngine.Vector2 a1;
				checkType(l,2,out a1);
				UnityEngine.Vector2 a2;
				checkType(l,3,out a2);
				o=new UnityEngine.Ray2D(a1,a2);
				pushValue(l,true);
				pushValue(l,o);
				return 2;
			}
			else if(argc<=2){
				o=new UnityEngine.Ray2D();
				pushValue(l,true);
				pushValue(l,o);
				return 2;
			}
			return error(l,"New object failed.");
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int GetPoint(IntPtr l) {
		try {
			UnityEngine.Ray2D self;
			checkValueType(l,1,out self);
			System.Single a1;
			checkType(l,2,out a1);
			var ret=self.GetPoint(a1);
			pushValue(l,true);
			pushValue(l,ret);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_origin(IntPtr l) {
		try {
			UnityEngine.Ray2D self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.origin);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_origin(IntPtr l) {
		try {
			UnityEngine.Ray2D self;
			checkValueType(l,1,out self);
			UnityEngine.Vector2 v;
			checkType(l,2,out v);
			self.origin=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int get_direction(IntPtr l) {
		try {
			UnityEngine.Ray2D self;
			checkValueType(l,1,out self);
			pushValue(l,true);
			pushValue(l,self.direction);
			return 2;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
	[UnityEngine.Scripting.Preserve]
	static public int set_direction(IntPtr l) {
		try {
			UnityEngine.Ray2D self;
			checkValueType(l,1,out self);
			UnityEngine.Vector2 v;
			checkType(l,2,out v);
			self.direction=v;
			setBack(l,self);
			pushValue(l,true);
			return 1;
		}
		catch(Exception e) {
			return error(l,e);
		}
	}
	[UnityEngine.Scripting.Preserve]
	static public void reg(IntPtr l) {
		getTypeTable(l,"UnityEngine.Ray2D");
		addMember(l,GetPoint);
		addMember(l,"origin",get_origin,set_origin,true);
		addMember(l,"direction",get_direction,set_direction,true);
		createTypeMetatable(l,constructor, typeof(UnityEngine.Ray2D),typeof(System.ValueType));
	}
}
