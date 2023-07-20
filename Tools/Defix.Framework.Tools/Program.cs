using System;
using Defix.Framework.Tools.FieldReadingAndWritingSystem;

public class Program
{
    public static void Main(string[] args)
    {
        string pathToWrite = "C:\\Users\\ко\\source\\repos\\Defix.Framework.Tools\\Defix.Framework.Tools\\FieldReadingAndWritingSystem\\testDataFolder\\data.save";
        string header = "TestSaveHeader";

        FieldsWriter writer = new(pathToWrite,
            new FieldsData(header, new TestSaveClass()), new FieldsData("12345", new TestSaveClass()));

        writer.Write();

        FieldsReader reader = new(pathToWrite);

        var testVar = reader.GetValueByNameAndHeader<TestClassToConvert, TestClass>("testWriteClass", header);

        Console.WriteLine($"CUR_RETURN_VALUE: {testVar}, type: {testVar.GetType()}");
    }
}

public class TestSaveClass
{
    [CanWrite("testEnum")]
    public TestEnum TestVar = TestEnum.One;

    [CanWrite("testEnum2")]
    public Types TestVar2 = Types.TestType;

    [CanWrite("OtherVar2")]
    public int OtherVar = 1;

    [CanWrite]
    public TestClass testWriteClass = new(100);
}

public enum TestEnum
{
    One,
    Two,
    Three
}

public enum Types
{
    TestType,
    Other
}

public class TestClass
{
    public int Count;

    public TestClass(int count)
    {
        this.Count = count;
    }

    public override string ToString()
    {
        return Count.ToString();
    }

    public static explicit operator TestClass(int count)
    {
        return new TestClass(count);
    }
}

public class TestClassToConvert : StringTypeToConvert<TestClass>
{
    public TestClassToConvert(string stringToConvert) : base(stringToConvert) { }

    public override TestClass Parse()
    {
        return (TestClass)int.Parse(StringToConvert);
    }
}