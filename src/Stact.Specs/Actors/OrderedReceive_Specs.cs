// Copyright 2010 Chris Patterson
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
namespace Stact.Specs.Actors
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Magnum.Extensions;
	using Magnum.TestFramework;


	[Scenario]
	public class When_a_stream_of_messages_is_received
	{
		int _count = 1000;
		Future<Status> _response;

		[When]
		public void A_stream_of_messages_is_received()
		{
			ActorFactory<Agent> agentFactory = ActorFactory.Create(inbox => new Agent(inbox));

			ActorInstance agent = agentFactory.GetActor();

			for (int i = 0; i < _count; i++)
			{
				agent.Send(new Add
					{
						Value = i
					});

				if (i % 100 == 0)
					agent.Send<Suspend>();
				else if(i%100 == 50)
					agent.Send<Resume>();
			}

			agent.Send<Resume>();

			_response = new Future<Status>();
			AnonymousActor.New(inbox =>
				{
					Action loop = null;
					loop = () =>
					{
						agent.Request(new Status(), inbox)
							.Receive<Response<Status>>(response =>
							{
								if (response.Body.Count == _count)
									_response.Complete(response.Body);
								else
									loop();
							});
					};

					loop();
				});

			_response.WaitUntilCompleted(4.Seconds());
		}

		[Then]
		public void Should_be_in_order()
		{
			_response.IsCompleted.ShouldBeTrue();
			_response.Value.InOrder.ShouldBeTrue();
		}

		[Then]
		public void Should_have_all_the_values()
		{
			_response.IsCompleted.ShouldBeTrue();
			_response.Value.Count.ShouldEqual(_count);
		}


		class Add
		{
			public int Value { get; set; }
		}


		class Agent :
			Actor
		{
			IList<int> _values = new List<int>();

			public Agent(Inbox inbox)
			{
				inbox.Loop(loop =>
					{
						loop
							.EnableSuspendResume(inbox)
							.Receive<Add>(add =>
								{
									_values.Add(add.Value);

									loop.Repeat();
								})
							.Receive<Request<Status>>(request =>
								{
									request.Respond(new Status
										{
											Count = _values.Count,
											InOrder = !_values.Where((t, i) => t != i).Any(),
										});

									loop.Repeat();
								});
					});
			}
		}


		class Status
		{
			public bool InOrder { get; set; }
			public int Count { get; set; }
		}
	}
}