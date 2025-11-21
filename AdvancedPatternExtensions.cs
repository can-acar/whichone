namespace WhichOne;

public static class AdvancedPatternExtensions
{
	// Partial application support
	public static Func<WhichOne<T1, T2>, TResult> Curry<T1, T2, TResult>(Func<T1, TResult> onT1,
		Func<T2, TResult> onT2)
	{
		return union => union.Match(onT1, onT2);
	}

	// Memoization for expensive computations
	public static Func<WhichOne<T1, T2>, TResult> Memoize<T1, T2, TResult>(this Func<WhichOne<T1, T2>, TResult> func)
	{
		var cache = new Dictionary<WhichOne<T1, T2>, TResult>();
		return union =>
		{
			if (!cache.TryGetValue(union, out var result))
			{
				result = func(union);
				cache[union] = result;
			}
			return result;
		};
	}

	// Retry logic for Result types
	public static async Task<Result<T, TError>> RetryAsync<T, TError>(Func<Task<Result<T, TError>>> operation,
		int maxRetries = 3,
		TimeSpan? delay = null)
	where TError : Exception
	{
		for (int i = 0; i < maxRetries; i++)
		{
			var result = await operation();
			if (result.IsOk)
				return result;
			if (i < maxRetries - 1 && delay.HasValue)
				await Task.Delay(delay.Value);
		}
		return await operation();
	}

	// Circuit breaker pattern
	public class CircuitBreaker<T, TError> where TError : Exception
	{
		private int _failureCount;
		private DateTime _lastFailureTime;
		private readonly int _threshold;
		private readonly TimeSpan _timeout;
		private CircuitState _state = CircuitState.Closed;

		public CircuitBreaker(int threshold = 3, TimeSpan? timeout = null)
		{
			_threshold = threshold;
			_timeout = timeout ?? TimeSpan.FromMinutes(1);
		}

		public async Task<Result<T, TError>> ExecuteAsync(Func<Task<Result<T, TError>>> operation)
		{
			if (_state == CircuitState.Open)
			{
				if (DateTime.UtcNow - _lastFailureTime > _timeout)
				{
					_state = CircuitState.HalfOpen;
				}
				else
				{
					return Result<T, TError>.Error((TError)(object)new InvalidOperationException("Circuit breaker is open"));
				}
			}
			var result = await operation();
			if (result.IsOk)
			{
				_failureCount = 0;
				_state = CircuitState.Closed;
			}
			else
			{
				_failureCount++;
				_lastFailureTime = DateTime.UtcNow;
				if (_failureCount >= _threshold)
					_state = CircuitState.Open;
			}
			return result;
		}

		private enum CircuitState
		{
			Closed,
			Open,
			HalfOpen
		}
	}

	// Validation combinator
	public static Result<T, ValidationError> Validate<T>(T value,
		params Func<T, Result<T, ValidationError>>[] validators)
	{
		foreach (var validator in validators)
		{
			var result = validator(value);
			if (result.IsError)
				return result;
		}
		return Result<T, ValidationError>.Ok(value);
	}

	// Parallel execution with error aggregation
	public static async Task<Result<T[], AggregateException>> WhenAll<T, TError>(params Task<Result<T, TError>>[] tasks)
	where TError : Exception
	{
		var results = await Task.WhenAll(tasks);
		var errors = results
					 .Where(r => r.IsError)
					 .Select(r => r.Match(_ => null!, e => e))
					 .ToArray();
		if (errors.Length > 0)
			return new AggregateException(errors);
		var values = results
					 .Select(r => r.Match(v => v, _ => default!))
					 .ToArray();
		return values;
	}
}

public class ValidationError : Exception
{
	public string Field { get; }

	public ValidationError(string field, string message) : base(message)
	{
		Field = field;
	}
}