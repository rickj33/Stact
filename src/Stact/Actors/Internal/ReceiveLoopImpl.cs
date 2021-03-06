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
namespace Stact.Internal
{
	using System;
	using System.Collections.Generic;


	public class ReceiveLoopImpl :
		ReceiveLoop
	{
		readonly Inbox _inbox;
		readonly PendingReceiveList _pending;
		readonly IList<Func<PendingReceive>> _receivers;

		public ReceiveLoopImpl(Inbox inbox)
		{
			_inbox = inbox;

			_pending = new PendingReceiveList();

			_receivers = new List<Func<PendingReceive>>
				{
					() => _inbox.Receive<Request<Exit>>(HandleExit),
					() => _inbox.Receive<Kill>(message => CancelPendingReceives()),
				};
		}

		public ReceiveLoop Receive<T>(SelectiveConsumer<T> consumer)
		{
			Func<PendingReceive> receiver = () => _inbox.Receive((SelectiveConsumer<T>)(candidate =>
				{
					Consumer<T> accepted = consumer(candidate);
					if (accepted == null)
						return null;

					CancelPendingReceives();

					return accepted;
				}));

			_receivers.Add(receiver);

			return this;
		}

		public void Repeat()
		{
			foreach (var receiver in _receivers)
				_pending.Add(receiver());
		}

		Consumer<Request<Exit>> HandleExit(Request<Exit> request)
		{
			return message =>
				{
					CancelPendingReceives();

					request.Respond(message.Body);
				};
		}

		void CancelPendingReceives()
		{
			_pending.CancelAll();
		}
	}
}