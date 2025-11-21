namespace WhichOne.Examples;

public class MyClass
{
	public MyClass()
	{
	}

	public void MyMethod()
	{
	}

	public int MyProperty { get; set; }

	private void MyPrivateMethod()
	{
	}

	public WhichOne<MyClass, bool> MyProperty1 { get; set; }
	public WhichOne<MyClass, bool> MyProperty2 { get; set; }

	public WhichOne<MyClass, bool> MyMethod1(string param)
	{
		if (param == "test")
		{
			return this;
		}
		return false;
	}

	public WhichOne<MyClass, bool> MyMethod2(string param, int param2)
	{
		if (param == "test")
		{
			return true;
		}
		return this;
	}
}

public class MyClass2
{
	public MyClass2()
	{
	}

	public void MyMethod()
	{
		var myClass = new MyClass();
		var result1 = myClass.MyMethod1("test");
		var result2 = myClass.MyMethod2("test", 5);
		
		
		
		
		if (result1.Value.Equals(typeof(MyClass)))
		{
			// Do something
		}
	}
}