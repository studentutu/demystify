﻿using System.Threading.Tasks;
using UnityEngine;

namespace Some.Arbitrary.Namespace
{
	
	public class Result<T, U>
	{

		public static Some.Arbitrary.Namespace.Result<T, U> ReturnSomethingWeird<T>()
		{
			Debug.Log("void UnityEngine.Debug.Log(object message)");
			return new Some.Arbitrary.Namespace.Result<T, U>();
		}

	}
}