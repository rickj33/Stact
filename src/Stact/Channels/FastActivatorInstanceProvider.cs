﻿// Copyright 2010 Chris Patterson
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Stact.Channels
{
	using System;
	using Magnum.Extensions;
	using Magnum.Reflection;


	public class FastActivatorInstanceProvider<TInstance, TChannel> :
		InstanceProvider<TInstance, TChannel>
		where TInstance : class
	{
		readonly object[] _args;

		public FastActivatorInstanceProvider(params object[] args)
		{
			_args = args ?? new object[] {};
		}

		public TInstance GetInstance(TChannel message)
		{
			TInstance instance = FastActivator<TInstance>.Create(_args);
			if (instance == null)
			{
				throw new InvalidOperationException(
					"Failed to create type {0} for the message type {1}".FormatWith(typeof(TInstance).ToShortTypeName(),
					                                                                typeof(TChannel).ToShortTypeName()));
			}

			return instance;
		}
	}
}