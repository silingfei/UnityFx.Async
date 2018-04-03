﻿// Copyright (c) Alexander Bogarsukov.
// Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;

namespace UnityFx.Async
{
	internal abstract class PromiseResult<T> : AsyncResult<T>
	{
		#region data

		private static SendOrPostCallback _postCallback;

		private readonly SynchronizationContext _syncContext;
		private IAsyncOperation _op;

		#endregion

		#region interface

		protected PromiseResult()
			: base(AsyncOperationStatus.Running)
		{
			_syncContext = SynchronizationContext.Current;
		}

		protected void InvokeOnSyncContext(IAsyncOperation op, bool completedSynchronously)
		{
			if (completedSynchronously || _syncContext == null || _syncContext == SynchronizationContext.Current)
			{
				try
				{
					InvokeCallbacks(op, completedSynchronously);
				}
				catch (Exception e)
				{
					TrySetException(e, completedSynchronously);
				}
			}
			else
			{
				_op = op;

				if (_postCallback == null)
				{
					_postCallback = args =>
					{
						var c = args as PromiseResult<T>;

						try
						{
							c.InvokeCallbacks(c._op, false);
						}
						catch (Exception e)
						{
							c.TrySetException(e, false);
						}
					};
				}

				_syncContext.Post(_postCallback, this);
			}
		}

		protected abstract void InvokeCallbacks(IAsyncOperation op, bool completedSynchronously);

		#endregion
	}
}
