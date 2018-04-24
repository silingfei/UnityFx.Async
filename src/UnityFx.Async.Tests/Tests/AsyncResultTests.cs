﻿// Copyright (c) Alexander Bogarsukov.
// Licensed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace UnityFx.Async
{
	public class AsyncResultTests
	{
		#region constructors

		[Fact]
		public void DefaultConstructor_SetsStatusToCreated()
		{
			// Act
			var op = new AsyncResult();

			// Assert
			AssertNotCompleted(op, AsyncOperationStatus.Created);
		}

		[Fact]
		public void Constructor_SetsStatusToScheduled()
		{
			// Act
			var op = new AsyncResult(AsyncOperationStatus.Scheduled);

			// Assert
			AssertNotCompleted(op, AsyncOperationStatus.Scheduled);
		}

		[Fact]
		public void Constructor_SetsStatusToRunning()
		{
			// Act
			var op = new AsyncResult(AsyncOperationStatus.Running);

			// Assert
			AssertNotCompleted(op, AsyncOperationStatus.Running);
		}

		[Fact]
		public void Constructor_SetsStatusToCompleted()
		{
			// Act
			var op = new AsyncResult(AsyncOperationStatus.RanToCompletion);

			// Assert
			AssertCompleted(op);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void Constructor_SetsStatusToFaulted()
		{
			// Act
			var op = new AsyncResult(AsyncOperationStatus.Faulted);

			// Assert
			AssertFaulted(op);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void Constructor_SetsStatusToCanceled()
		{
			// Act
			var op = new AsyncResult(AsyncOperationStatus.Canceled);

			// Assert
			AssertCanceled(op);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void Constructor_SetsAsyncState()
		{
			// Arrange
			var state = new object();

			// Act
			var op = new AsyncResult(null, state);

			// Assert
			AssertNotCompleted(op, AsyncOperationStatus.Created);
			Assert.Equal(state, op.AsyncState);
		}

		#endregion

		#region static methods

		[Fact]
		public void CompletedOperation_ReturnsCompletedOperation()
		{
			// Act
			var op = AsyncResult.CompletedOperation;

			// Assert
			AssertCompleted(op);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void FromCanceled_ReturnsCanceledOperation()
		{
			// Act
			var op = AsyncResult.FromCanceled();

			// Assert
			AssertCanceled(op);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void FromCanceled_ReturnsCanceledOperation_Generic()
		{
			// Act
			var op = AsyncResult.FromCanceled<int>();

			// Assert
			AssertCanceled(op);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void FromException_ReturnsFailedOperation()
		{
			// Arrange
			var e = new InvalidCastException();

			// Act
			var op = AsyncResult.FromException(e);

			// Assert
			AssertFaulted(op, e);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void FromException_ReturnsCanceledOperation()
		{
			// Arrange
			var e = new OperationCanceledException();

			// Act
			var op = AsyncResult.FromException(e);

			// Assert
			AssertCanceled(op, e);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public void FromResult_ReturnsCompletedOperation()
		{
			// Arrange
			var result = 25;

			// Act
			var op = AsyncResult.FromResult(result);

			// Assert
			AssertCompletedWithResult(op, result);
			Assert.True(op.CompletedSynchronously);
		}

		[Fact]
		public async Task Retry_CompletesWhenSourceCompletes()
		{
			// Arrange
			var counter = 3;

			IAsyncOperation OpFactory()
			{
				if (--counter > 0)
				{
					return AsyncResult.FromException(new Exception());
				}
				else
				{
					return AsyncResult.Delay(1);
				}
			}

			// Act
			var op = AsyncResult.Retry(OpFactory, 1);
			await op;

			// Assert
			AssertCompleted(op);
		}

		[Fact]
		public async Task Retry_CompletesAfterMaxRetriesExceeded()
		{
			// Arrange
			var counter = 3;
			var e = new Exception();

			IAsyncOperation OpFactory()
			{
				--counter;
				return AsyncResult.FromException(e);
			}

			// Act
			var op = AsyncResult.Retry(OpFactory, 1, 1);

			try
			{
				await op;
			}
			catch
			{
			}

			// Assert
			AssertFaulted(op, e);
			Assert.Equal(2, counter);
		}

		[Fact]
		public async Task WhenAll_CompletesWhenAllOperationsCompleted()
		{
			// Arrange
			var op1 = AsyncResult.Delay(1);
			var op2 = AsyncResult.Delay(2);

			// Act
			await AsyncResult.WhenAll(op1, op2);

			// Assert
			AssertCompleted(op1);
			AssertCompleted(op2);
		}

		[Fact]
		public async Task WhenAny_CompletesWhenAnyOperationCompletes()
		{
			// Arrange
			var op1 = AsyncResult.Delay(1);
			var op2 = AsyncResult.Delay(Timeout.Infinite);

			// Act
			await AsyncResult.WhenAny(op1, op2);

			// Assert
			AssertCompleted(op1);
		}

		#endregion

		#region interface

		#region SetScheduled

		[Theory]
		[InlineData(AsyncOperationStatus.Created)]
		public void SetScheduled_SetsStatusToScheduled(AsyncOperationStatus status)
		{
			// Arrange
			var op = new AsyncCompletionSource(status);

			// Act
			op.SetScheduled();

			// Assert
			AssertNotCompleted(op, AsyncOperationStatus.Scheduled);
		}

		[Theory]
		[InlineData(AsyncOperationStatus.Scheduled)]
		[InlineData(AsyncOperationStatus.Running)]
		[InlineData(AsyncOperationStatus.RanToCompletion)]
		[InlineData(AsyncOperationStatus.Faulted)]
		[InlineData(AsyncOperationStatus.Canceled)]
		public void SetScheduled_ThrowsIfOperationIsNotCreated(AsyncOperationStatus status)
		{
			// Arrange
			var op = new AsyncCompletionSource(status);

			// Act/Assert
			Assert.Throws<InvalidOperationException>(() => op.SetScheduled());
		}

		[Fact]
		public void SetScheduled_ThrowsIfOperationIsDisposed()
		{
			// Arrange
			var op = new AsyncCompletionSource(AsyncOperationStatus.RanToCompletion);
			op.Dispose();

			// Act/Assert
			Assert.Throws<ObjectDisposedException>(() => op.SetScheduled());
		}

		#endregion

		#region SetRunning

		[Theory]
		[InlineData(AsyncOperationStatus.Created)]
		[InlineData(AsyncOperationStatus.Scheduled)]
		public void SetRunning_SetsStatusToRunning(AsyncOperationStatus status)
		{
			// Arrange
			var op = new AsyncCompletionSource(status);

			// Act
			op.SetRunning();

			// Assert
			AssertNotCompleted(op, AsyncOperationStatus.Running);
		}

		[Theory]
		[InlineData(AsyncOperationStatus.Running)]
		[InlineData(AsyncOperationStatus.RanToCompletion)]
		[InlineData(AsyncOperationStatus.Faulted)]
		[InlineData(AsyncOperationStatus.Canceled)]
		public void SetRunning_ThrowsIfOperationIsNotCreatedOrScheduled(AsyncOperationStatus status)
		{
			// Arrange
			var op = new AsyncCompletionSource(status);

			// Act/Assert
			Assert.Throws<InvalidOperationException>(() => op.SetRunning());
		}

		[Fact]
		public void SetRunning_ThrowsIfOperationIsDisposed()
		{
			// Arrange
			var op = new AsyncCompletionSource(AsyncOperationStatus.RanToCompletion);
			op.Dispose();

			// Act/Assert
			Assert.Throws<ObjectDisposedException>(() => op.SetRunning());
		}

		#endregion

		#endregion

		#region async/await

		[Fact]
		public async Task Await_CollbackIsTriggered()
		{
			// Arrange
			var op = new AsyncCompletionSource();
			var task = Task.Run(() =>
			{
				Thread.Sleep(10);
				op.SetCompleted();
			});

			// Act
			await op;

			// Assert
			AssertCompleted(op);
		}

		[Fact]
		public async Task Await_ShouldThrowIfFaulted()
		{
			// Arrange
			var op = new AsyncCompletionSource();
			var expectedException = new Exception();
			var actualException = default(Exception);
			var task = Task.Run(() =>
			{
				Thread.Sleep(10);
				op.SetException(expectedException);
			});

			// Act
			try
			{
				await op;
			}
			catch (Exception e)
			{
				actualException = e;
			}

			// Assert
			Assert.Equal(expectedException, actualException);
			AssertFaulted(op);
		}

		[Fact]
		public async Task Await_ShouldThrowIfCanceled()
		{
			// Arrange
			var op = new AsyncCompletionSource();
			var actualException = default(Exception);
			var task = Task.Run(() =>
			{
				Thread.Sleep(10);
				op.SetCanceled();
			});

			// Act
			try
			{
				await op;
			}
			catch (Exception e)
			{
				actualException = e;
			}

			// Assert
			Assert.IsType<OperationCanceledException>(actualException);
			AssertCanceled(op);
		}

		#endregion

		#region IAsyncResult

		[Fact]
		public void AsyncWaitHandle_ThrowsIfDisposed()
		{
			// Arrange
			var op = new AsyncResult(AsyncOperationStatus.Canceled);
			op.Dispose();

			// Act/Assert
			Assert.Throws<ObjectDisposedException>(() => op.AsyncWaitHandle);
		}

		#endregion

		#region IDisposable

		[Fact]
		public void Dispose_ThrowsIfOperationIsNotCompleted()
		{
			// Arrange
			var op = new AsyncResult();

			// Act/Assert
			Assert.Throws<InvalidOperationException>(() => op.Dispose());
		}

		[Fact]
		public void Dispose_CanBeCalledMultipleTimes()
		{
			// Arrange
			var op = new AsyncResult(AsyncOperationStatus.Canceled);

			// Act/Assert
			op.Dispose();
			op.Dispose();
		}

		[Fact]
		public void Dispose_CallsDispose()
		{
			// Arrange
			var op = new AsyncResultOverrides();
			op.TrySetCompleted();

			// Act
			op.Dispose();

			// Assert
			Assert.True(op.DisposeCalled);
		}

		#endregion

		#region implementation

		private void AssertNotCompleted(IAsyncOperation op, AsyncOperationStatus status)
		{
			Assert.Equal(status, op.Status);
			Assert.False(op.IsCompleted);
			Assert.False(op.IsCompletedSuccessfully);
			Assert.False(op.IsCanceled);
			Assert.False(op.IsFaulted);
			Assert.False(op.CompletedSynchronously);
			Assert.Null(op.Exception);
		}

		private void AssertCompleted(IAsyncOperation op)
		{
			Assert.Equal(AsyncOperationStatus.RanToCompletion, op.Status);
			Assert.True(op.IsCompleted);
			Assert.True(op.IsCompletedSuccessfully);
			Assert.False(op.IsCanceled);
			Assert.False(op.IsFaulted);
			Assert.Null(op.Exception);
		}

		private void AssertCompletedWithResult<T>(IAsyncOperation<T> op, T result)
		{
			Assert.Equal(AsyncOperationStatus.RanToCompletion, op.Status);
			Assert.Equal(result, op.Result);
			Assert.True(op.IsCompleted);
			Assert.True(op.IsCompletedSuccessfully);
			Assert.False(op.IsCanceled);
			Assert.False(op.IsFaulted);
			Assert.Null(op.Exception);
		}

		private void AssertCanceled(IAsyncOperation op, OperationCanceledException e)
		{
			Assert.Equal(AsyncOperationStatus.Canceled, op.Status);
			Assert.Equal(e, op.Exception.InnerException);
			Assert.True(op.IsCompleted);
			Assert.True(op.IsCanceled);
			Assert.False(op.IsCompletedSuccessfully);
			Assert.False(op.IsFaulted);
		}

		private void AssertCanceled(IAsyncOperation op)
		{
			Assert.Equal(AsyncOperationStatus.Canceled, op.Status);
			Assert.True(op.IsCompleted);
			Assert.True(op.IsCanceled);
			Assert.False(op.IsCompletedSuccessfully);
			Assert.False(op.IsFaulted);
		}

		private void AssertFaulted(IAsyncOperation op, Exception e)
		{
			Assert.Equal(AsyncOperationStatus.Faulted, op.Status);
			Assert.Equal(e, op.Exception.InnerException);
			Assert.True(op.IsCompleted);
			Assert.True(op.IsFaulted);
			Assert.False(op.IsCompletedSuccessfully);
			Assert.False(op.IsCanceled);
		}

		private void AssertFaulted(IAsyncOperation op)
		{
			Assert.Equal(AsyncOperationStatus.Faulted, op.Status);
			Assert.NotNull(op.Exception);
			Assert.True(op.IsCompleted);
			Assert.True(op.IsFaulted);
			Assert.False(op.IsCompletedSuccessfully);
			Assert.False(op.IsCanceled);
		}

		#endregion
	}
}
