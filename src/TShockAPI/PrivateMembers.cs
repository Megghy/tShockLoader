using System.Reflection;

namespace TShockAPI;

internal static class PrivateMembers
{
	public static T Get<T>(object target, string name)
	{
		var value = Field(target.GetType(), name).GetValue(target);
		if (value is not T typed)
			throw new InvalidOperationException($"{target.GetType().Name}.{name} is {value?.GetType().Name ?? "null"}, expected {typeof(T).Name}.");
		return typed;
	}

	public static void Set(object target, string name, object value)
		=> Field(target.GetType(), name).SetValue(target, value);

	static FieldInfo Field(Type type, string name)
	{
		for (var current = type; current != null; current = current.BaseType)
		{
			var field = current.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
			if (field != null)
				return field;
		}

		throw new MissingFieldException(type.FullName, name);
	}
}
