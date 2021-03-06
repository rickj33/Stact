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
namespace Stact.Configuration.Internal
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using Magnum.Extensions;
	using Magnum.Reflection;


	public class PropertyChannelConnectionConfiguratorImpl<T> :
		FiberFactoryConfiguratorImpl<PropertyChannelConnectionConfigurator<T>>,
		PropertyChannelConnectionConfigurator<T>,
		ConnectionBuilderConfigurator
		where T : class
	{
		T _instance;
		IList<Action<ConnectionBuilder, Fiber, T>> _propertyBinders;

		public void ValidateConfiguration()
		{
			ValidateFiberFactoryConfiguration();

			if (_instance == null)
				throw new ChannelConfigurationException("No instance was provided for " + typeof(T).ToShortTypeName());

			GetChannelBinders();
		}

		public void Configure(ConnectionBuilder builder)
		{
			Fiber fiber = this.GetFiberUsingConfiguredFactory(builder);

			_propertyBinders.Each(x => x(builder, fiber, _instance));
		}

		public PropertyChannelConnectionConfigurator<T> UsingInstance(T instance)
		{
			_instance = instance;

			return this;
		}

		void GetChannelBinders()
		{
			Type actorType = typeof(T);

			_propertyBinders = actorType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(x => x.PropertyType.Implements<Channel>())
				.Select(property =>
					{
						Type inputType = property.PropertyType.GetGenericTypeDeclarations(typeof(Channel<>)).Single();

						return this.FastInvoke<PropertyChannelConnectionConfiguratorImpl<T>,
							Action<ConnectionBuilder, Fiber, T>>(new[] {inputType}, "GetChannelConfigurator", property);
					})
				.ToList();
		}

		Action<ConnectionBuilder, Fiber, T> GetChannelConfigurator<TChannel>(PropertyInfo property)
		{
			ParameterExpression target = Expression.Parameter(typeof(T), "x");
			MethodCallExpression getter = Expression.Call(target, property.GetGetMethod(true));

			ChannelAccessor<T, TChannel> accessor =
				Expression.Lambda<ChannelAccessor<T, TChannel>>(getter, new[] {target}).Compile();

			return (connection, fiber, instance) =>
				{
					connection.AddChannel(fiber, x => accessor(instance));
				};
		}
	}
}