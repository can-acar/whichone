using System.Diagnostics;

namespace WhichOne;

// Domain-specific union types
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Result<T, TError> : IEquatable<Result<T, TError>>
where TError : Exception
{
	private readonly WhichOne<T, TError> _inner;
	private Result(WhichOne<T, TError> inner) => _inner = inner;
	public static Result<T, TError> Ok(T value) => new(value);
	public static Result<T, TError> Error(TError error) => new(error);
	public bool IsOk => _inner.Is<T>();
	public bool IsError => _inner.Is<TError>();
	public static implicit operator Result<T, TError>(T value) => Ok(value);
	public static implicit operator Result<T, TError>(TError error) => Error(error);

	public TResult Match<TResult>(Func<T, TResult> onOk, Func<TError, TResult> onError) =>
		_inner.Match(onOk, onError);

	public Result<TResult, TError> Map<TResult>(Func<T, TResult> mapper) =>
		new(_inner.Match<WhichOne<TResult, TError>>(
			ok => WhichOne<TResult, TError>.From(mapper(ok)),
			error => WhichOne<TResult, TError>.From(error)
		));

	public Result<TResult, TError> Bind<TResult>(Func<T, Result<TResult, TError>> binder) =>
		Match(
			ok => binder(ok),
			error => error
		);

	// Exception-safe value extraction
	public T GetValueOrThrow() =>
		Match(
			ok => ok,
			error => throw error
		);

	public T GetValueOrDefault(T defaultValue = default!) =>
		Match(
			ok => ok,
			error => defaultValue
		);

	// LINQ support
	public Result<TResult, TError> Select<TResult>(Func<T, TResult> selector) =>
		Map(selector);

	public Result<TResult, TError> SelectMany<T2, TResult>(Func<T, Result<T2, TError>> selector,
		Func<T, T2, TResult> projector) =>
		Bind(t1 => selector(t1).Map(t2 => projector(t1, t2)));

	private string DebuggerDisplay =>
		IsOk ? $"Ok: {_inner.As<T>()}" : $"Error: {_inner.As<TError>().Message}";

	public bool Equals(Result<T, TError> other) => _inner.Equals(other._inner);
	public override bool Equals(object? obj) => obj is Result<T, TError> other && Equals(other);
	public override int GetHashCode() => _inner.GetHashCode();

	public static bool operator ==(Result<T, TError> left, Result<T, TError> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(Result<T, TError> left, Result<T, TError> right)
	{
		return !(left == right);
	}
}

// Option type for nullable values
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct Option<T> : IEquatable<Option<T>>
{
	private readonly WhichOne<T, Unit> _inner;
	private Option(WhichOne<T, Unit> inner) => _inner = inner;
	public static Option<T> Some(T value) => new(value);
	public static Option<T> None() => new(Unit.Default);
	public bool IsSome => _inner.Is<T>();
	public bool IsNone => _inner.Is<T>();
	public static implicit operator Option<T>(T value) => Some(value);

	public TResult Match<TResult>(Func<T, TResult> onSome, Func<TResult> onNone) =>
		_inner.Match(onSome, _ => onNone());

	public Option<TResult> Map<TResult>(Func<T, TResult> mapper) =>
		IsSome ? Option<TResult>.Some(mapper(_inner.As<T>())) : Option<TResult>.None();

	public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> binder) =>
		IsSome ? binder(_inner.As<T>()) : Option<TResult>.None();

	public T GetValueOrDefault(T defaultValue = default!) =>
		IsSome ? _inner.As<T>() : defaultValue;

	public T GetValueOrThrow(string? message = null) =>
		IsSome ? _inner.As<T>() : throw new InvalidOperationException(message ?? "Option is None");

	// LINQ support
	public Option<TResult> Select<TResult>(Func<T, TResult> selector) => Map(selector);

	public Option<TResult> SelectMany<T2, TResult>(Func<T, Option<T2>> selector,
		Func<T, T2, TResult> projector) =>
		Bind(t1 => selector(t1).Map(t2 => projector(t1, t2)));

	public Option<T> Where(Func<T, bool> predicate) =>
		IsSome && predicate(_inner.As<T>()) ? this : None();

	private string DebuggerDisplay =>
		IsSome ? $"Some({_inner.As<T>()})" : "None";

	public bool Equals(Option<T> other) => _inner.Equals(other._inner);
	public override bool Equals(object? obj) => obj is Option<T> other && Equals(other);
	public override int GetHashCode() => _inner.GetHashCode();
}

// Unit type for void representation
public readonly struct Unit : IEquatable<Unit>
{
	public static readonly Unit Default = new();
	public bool Equals(Unit other) => true;
	public override bool Equals(object? obj) => obj is Unit;
	public override int GetHashCode() => 0;
}