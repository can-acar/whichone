using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace WhichOne;

public interface IWhichOne
{
	object Value { get; }
	int Index { get; }
	bool Is<T>();
	bool Is(Type type);
	TResult Match<TResult>(params Delegate[] funcs);
	TResult TryMatch<TResult>(params Delegate[] funcs);
	bool Equals(object? obj);
	int GetHashCode();
	string VariantName { get; }
	T As<T>();
	bool TryAs<T>([NotNullWhen(true)] out T? value);
}

[DebuggerDisplay("{_debuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(WhichOneDebugView<,>))]
[StructLayout(LayoutKind.Auto)]
[DebuggerDisplay("{_debuggerDisplay,nq}")]
[DebuggerTypeProxy(typeof(WhichOneDebugView<,>))]
[JsonConverter(typeof(WhichOneJsonConverter))]
public readonly struct WhichOne<T1, T2> : IWhichOne, IEquatable<WhichOne<T1, T2>>
{
	private readonly T1? _value1;
	private readonly T2? _value2;
	public readonly int _index;

	// Constructor'lar private
	private WhichOne(T1 value)
	{
		_value1 = value;
		_value2 = default;
		_index = 1;
	}

	private WhichOne(T2 value)
	{
		_value1 = default;
		_value2 = value;
		_index = 2;
	}

	// Implicit Operators: DX (Developer Experience) için en kritik kısım
	public static implicit operator WhichOne<T1, T2>(T1 t) => new(t);
	public static implicit operator WhichOne<T1, T2>(T2 t) => new(t);

	// IWhichOne Implementation
	public object Value => _index switch
	{
		1 => _value1!,
		2 => _value2!,
		_ => throw new InvalidOperationException()
	};

	public int Index => _index;

	// Generic Is<T> Kontrolü
	public bool Is<T>()
	{
		// Eğer T, T1 veya T2 ile aynı ise ve index doğruysa
		if (_index == 1 && typeof(T).IsAssignableFrom(typeof(T1))) return true;
		if (_index == 2 && typeof(T).IsAssignableFrom(typeof(T2))) return true;
		return false;
	}

	public bool Is(Type type)
	{
		// Eğer T, T1 veya T2 ile aynı ise ve index doğruysa
		if (_index == 1 && type.IsAssignableFrom(typeof(T1))) return true;
		if (_index == 2 && type.IsAssignableFrom(typeof(T2))) return true;
		return false;
	}

	public TResult Match<TResult>(params Delegate[] funcs)
	{
		return _index switch
		{
			1 => (TResult)funcs[0].DynamicInvoke(_value1!)!,
			2 => (TResult)funcs[1].DynamicInvoke(_value2!)!,
			_ => throw new InvalidOperationException()
		};
	}

	public TResult TryMatch<TResult>(params Delegate[] funcs)
	{
		return _index switch
		{
			1 => (TResult)funcs[0].DynamicInvoke(_value1!)!,
			2 => (TResult)funcs[1].DynamicInvoke(_value2!)!,
			_ => throw new InvalidOperationException()
		};
	}

	public TResult Match<TResult>(Func<T1, TResult> f1, Func<T2, TResult> f2)
	{
		return _index switch
		{
			1 => f1(_value1!),
			2 => f2(_value2!),
			_ => throw new InvalidOperationException()
		};
	}

	public TResult TryMatch<TResult>(Func<T1, TResult>? f1 = null, Func<T2, TResult>? f2 = null, Func<TResult>? defaultFunc = null)
	{
		return _index switch
		{
			1 when f1 != null => f1(_value1!),
			2 when f2 != null => f2(_value2!),
			_ when defaultFunc != null => defaultFunc(),
			_ => throw new InvalidOperationException("No matching function provided and no default function.")
		};
	}

	public bool Equals(WhichOne<T1, T2> other)
	{
		return _index == other._index && _index switch
		{
			1 => EqualityComparer<T1>.Default.Equals(_value1, other._value1),
			2 => EqualityComparer<T2>.Default.Equals(_value2, other._value2),
			_ => true
		};
	}

	public override bool Equals(object? obj) => obj is WhichOne<T1, T2> other && Equals(other);
	public override int GetHashCode() => HashCode.Combine(_value1, _value2, _index);

	public string VariantName => _index switch
	{
		1 => typeof(T1).Name,
		2 => typeof(T2).Name,
		_ => "Empty"
	};

	public T As<T>()
	{
		return _index switch
		{
			1 when typeof(T).IsAssignableFrom(typeof(T1)) => (T)(object)_value1!,
			2 when typeof(T).IsAssignableFrom(typeof(T2)) => (T)(object)_value2!,
			_ => throw new InvalidCastException($"Cannot cast the current value to type {typeof(T).Name}.")
		};
	}

	public bool TryAs<T>([NotNullWhen(true)] out T? value)
	{
		value = _index switch
		{
			1 when typeof(T).IsAssignableFrom(typeof(T1)) => (T)(object)_value1!,
			2 when typeof(T).IsAssignableFrom(typeof(T2)) => (T)(object)_value2!,
			_ => default
		};
		return value is not null;
	}

	public static WhichOne<T1, T2> From(T1 value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return new WhichOne<T1, T2>(value);
	}

	public static WhichOne<T1, T2> From(T2 value)
	{
		ArgumentNullException.ThrowIfNull(value);
		return new WhichOne<T1, T2>(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Switch(Action<T1> a1, Action<T2> a2)
	{
		switch (_index)
		{
			case 1: a1(_value1!); break;
			case 2: a2(_value2!); break;
			default: throw new InvalidOperationException("Invalid union state");
		}
	}

	private string _debuggerDisplay =>
		_index switch
		{
			1 => $"Val1({_value1})",
			2 => $"Val2({_value2})",
			_ => "Empty"
		};

	public override string ToString() => _index switch
	{
		1 => $"Val1({_value1})",
		2 => $"Val2({_value2})",
		_ => "Empty"
	};

	public static bool operator ==(WhichOne<T1, T2> left, WhichOne<T1, T2> right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(WhichOne<T1, T2> left, WhichOne<T1, T2> right)
	{
		return !(left == right);
	}
}