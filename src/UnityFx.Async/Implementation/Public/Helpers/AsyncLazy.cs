﻿// Copyright (c) Alexander Bogarsukov.
// Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace UnityFx.Async
{
	/// <summary>
	/// A helper for lazy initialization.
	/// </summary>
	/// <remarks>
	/// This value-type is mutable, so DO NOT create readonly instances of it.
	/// </remarks>
	/// <seealso cref="IAsyncOperation"/>
	public struct AsyncLazy
	{
		#region data

		private Func<IAsyncOperation> _opFactory;
		private IAsyncOperation _op;

		#endregion

		#region interface

		/// <summary>
		/// Gets a value indicating whether the operation has started.
		/// </summary>
		public bool IsStarted => _op != null;

		/// <summary>
		/// Gets a value indicating whether the operation completed successfully (i.e. with <see cref="AsyncOperationStatus.RanToCompletion"/> status).
		/// </summary>
		public bool IsCompletedSuccessfully => _op?.IsCompletedSuccessfully ?? false;

		/// <summary>
		/// Gets a value indicating whether the operation has completed.
		/// </summary>
		public bool IsCompleted => _op?.IsCompleted ?? false;

		/// <summary>
		/// Gets a value indicating whether the operation completed due to an unhandled exception (i.e. with <see cref="AsyncOperationStatus.Faulted"/> status).
		/// </summary>
		public bool IsFaulted => _op?.IsFaulted ?? false;

		/// <summary>
		/// Gets a value indicating whether the operation completed due to being canceled (i.e. with <see cref="AsyncOperationStatus.Canceled"/> status).
		/// </summary>
		public bool IsCanceled => _op?.IsCanceled ?? false;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncLazy"/> struct.
		/// </summary>
		/// <param name="opFactory">The delegate that is invoked to produce the lazily initialized operation when it is needed.</param>
		/// <exception cref="ArgumentNullException">Thrown if <paramref name="opFactory"/> value is <see langword="null"/>.</exception>
		public AsyncLazy(Func<IAsyncOperation> opFactory)
		{
			_opFactory = opFactory ?? throw new ArgumentNullException(nameof(opFactory));
			_op = null;
		}

		/// <summary>
		/// Starts the operation or just updates its state.
		/// </summary>
		/// <exception cref="InvalidOperationException">Thrown if operation factory is <see langword="null"/>.</exception>
		public IAsyncOperation StartOrUpdate()
		{
			if (_op != null)
			{
				UpdateOperation();
			}
			else
			{
				_op = _opFactory?.Invoke() ?? throw new InvalidOperationException();
			}

			return _op;
		}

		/// <summary>
		/// Resets state of the instance to default.
		/// </summary>
		public void Reset()
		{
			_op = null;
		}

#if !NET35

		/// <summary>
		/// Returns the operation awaiter. This method is intended for compiler use only.
		/// </summary>
		public CompilerServices.AsyncAwaiter GetAwaiter()
		{
			var op = StartOrUpdate();
			return new CompilerServices.AsyncAwaiter(op);
		}

#endif

		#endregion

		#region implementation

		private void UpdateOperation()
		{
			if (_opFactory != null)
			{
				var status = _op.Status;

				switch (status)
				{
					case AsyncOperationStatus.RanToCompletion:
						_op = AsyncResult.CompletedOperation;
						_opFactory = null;
						break;
				}
			}
		}

		#endregion
	}
}
