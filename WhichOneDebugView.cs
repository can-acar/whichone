using System.Diagnostics;

namespace WhichOne;

internal class WhichOneDebugView<T1, T2>(WhichOne<T1, T2> union)
{
	private readonly WhichOne<T1, T2> _union = union;
	public string ActiveCase => _union.VariantName;

	public object? CurrentValue => _union._index switch
	{
		1 => _union,
		2 => _union,
		_ => null
	};

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public object Details => new
	{
		Index = _union._index,
		Type = _union.VariantName,
		Value = CurrentValue
	};
}