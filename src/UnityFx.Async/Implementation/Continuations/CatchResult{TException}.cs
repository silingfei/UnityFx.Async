﻿// Copyright (c) Alexander Bogarsukov.
// Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;

namespace UnityFx.Async
{
	internal class CatchResult<TException> : AsyncResult, IAsyncContinuation where TException : Exception
	{
		#region data

		private static SendOrPostCallback _postCallback;

		private readonly SynchronizationContext _syncContext;
		private readonly Action<TException> _errorCallback;
		private IAsyncOperation _op;

		#endregion

		#region interface

		public CatchResult(Action<TException> errorCallback)
			: base(AsyncOperationStatus.Running)
		{
			_syncContext = SynchronizationContext.Current;
			_errorCallback = errorCallback;
		}

		#endregion

		#region IAsyncContinuation

		public void Invoke(IAsyncOperation op, bool completedSynchronously)
		{
			if (op.IsCompletedSuccessfully || !(op.Exception.InnerException is TException))
			{
				TrySetCompleted(completedSynchronously);
			}
			else if (_syncContext == null || _syncContext == SynchronizationContext.Current)
			{
				InvokeErrorCallback(op, completedSynchronously);
			}
			else
			{
				_op = op;

				if (_postCallback == null)
				{
					_postCallback = args =>
					{
						var c = args as CatchResult<TException>;
						c.InvokeErrorCallback(c._op, false);
					};
				}

				_syncContext.Post(_postCallback, this);
			}
		}

		#endregion

		#region implementation

		private void InvokeErrorCallback(IAsyncOperation op, bool completedSynchronously)
		{
			try
			{
				_errorCallback.Invoke(op.Exception.InnerException as TException);
				TrySetCompleted(completedSynchronously);
			}
			catch (Exception e)
			{
				TrySetException(e, completedSynchronously);
			}
		}

		#endregion
	}
}