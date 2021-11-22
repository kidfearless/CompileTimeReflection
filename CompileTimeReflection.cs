using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;


namespace CompileTimeReflection
{
	static class BooleanExtension
	{
		const string TrueLiteral = "true";
		const string FalseLiteral = "false";
		public static string StringValue(this bool self) => self ? TrueLiteral : FalseLiteral;
	}

	[Generator]
	public class NotifyPropertyChangedGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
		}

		public void Execute(GeneratorExecutionContext context)
		{
			// uncomment to debug the actual build of the target project
			//Debugger.Launch();

			var compilation = context.Compilation;

			context.AddSource("compiletime", "public class CompileTimeReflectionAttribute : System.Attribute { }");

			foreach (var syntaxTree in compilation.SyntaxTrees)
			{
				var semanticModel = compilation.GetSemanticModel(syntaxTree);
				var root = syntaxTree.GetRoot();
				var descendants = root.DescendantNodes();
				var oftype = descendants.OfType<ClassDeclarationSyntax>();
				var selected = oftype.Select(x => semanticModel.GetDeclaredSymbol(x));
				var oftype2 = selected.OfType<ITypeSymbol>().ToArray();
				if (syntaxTree.FilePath.EndsWith(".Reflection.cs"))
				{
					//syntaxTree.WithChangedText(SourceText.From(""));
					continue;
				}

				foreach (var typeSymbol in oftype2)
				{


					var attributes = typeSymbol.GetAttributes();
					bool found = false;
					foreach (var attribute in attributes)
					{
						if (attribute.AttributeClass?.Name == "CompileTimeReflection")
						{
							found = true;
							break;
						}
					}
					if (!found)
					{
						continue;
					}

					var source = GenerateParialClass(typeSymbol);

					context.AddSource($"{typeSymbol.Name}.Reflection.cs", source);

					// uncomment if you want the files generated in solution
					//File.WriteAllText(syntaxTree.FilePath.Replace(".cs", ".Reflection.cs"), source);
				}
			}

		}

		private string GenerateParialClass(ITypeSymbol typeSymbol)
		{
			var properties = GeneratePropertyInfos(typeSymbol);
			var propertyInfoDefinitions = string.Join("\n", properties.Values);
			var propertyClasses = string.Join("", properties.Keys.Select(t => $"new {t}(),\n"));

			var fields = GenerateFields(typeSymbol);
			var fieldInfoDefinitions = string.Join("\n", fields.Values);
			var fieldClasses = string.Join("", fields.Keys.Select(t => $"new {t}(),\n"));


			var methods = GenerateMethods(typeSymbol);
			var methodInfoDefinitions = string.Join("\n", methods.Values);
			var methodClasses = string.Join("", methods.Keys.Select(t => $"new {t}(),\n"));


			return $@"
namespace {typeSymbol.ContainingNamespace}
{{
	using System;
	using System.Collections.Generic;

	[System.Runtime.CompilerServices.CompilerGenerated]
	partial class {typeSymbol.Name}
	{{
		public interface ICompileTimeMethodInfo
		{{
			bool IsReadOnly {{get;}}
			bool IsAbstract {{get;}}
			bool IsStatic {{get;}}
			bool IsExtern {{get;}}
			bool IsConditional {{get;}}
			bool IsGenericMethod {{get;}}
			bool IsExtensionMethod {{get;}}
			bool HidesBaseMethodsByName {{get;}}
			bool IsAsync {{get;}}
			bool IsVararg {{get;}}
			bool IsCheckedBuiltin {{get;}}
			bool ReturnsVoid {{get;}}
			bool ReturnsByRef {{get;}}
			bool ReturnsByRefReadonly {{get;}}

			object Invoke({typeSymbol.Name} instance, params object[] args);
		}}

		public interface ICompileTimeFieldInfo
		{{
			string Name {{get;}}
			Type Type {{get;}}
			string TypeName {{get;}}
			bool IsConst {{get;}}
			bool IsReadOnly {{get;}}
			bool IsAbstract {{get;}}
			bool IsStatic {{get;}}
			bool IsExtern {{get;}}

			bool HasGetter {{get;}}
			object GetValue({typeSymbol.Name} instance);
			
			bool HasSetter {{get;}}
			void SetValue({typeSymbol.Name} instance, object value);
		}}

		public interface ICompileTimePropertyInfo
		{{
			string Name {{get;}}
			Type Type {{get;}}
			string TypeName {{get;}}
			bool IsAbstract {{get;}}
			bool IsExtern {{get;}}
			bool IsIndexer {{get;}}
			bool IsOverride {{get;}}
			bool IsWriteOnly {{get;}}
			bool IsStatic {{get;}}
			bool IsVirtual {{get;}}
			bool IsReadOnly {{get;}}
			bool IsDefinition {{get;}}
			bool IsSealed {{get;}}

			bool HasGetter {{get;}}
			object GetValue({typeSymbol.Name} instance);
			bool HasSetter {{get;}}
			void SetValue({typeSymbol.Name} instance, object value);
		}}
      {propertyInfoDefinitions}

		[System.Runtime.CompilerServices.CompilerGenerated]
		static ICompileTimePropertyInfo[] ___properties = new ICompileTimePropertyInfo[] {{
				{propertyClasses}
		}};

		[System.Runtime.CompilerServices.CompilerGenerated]
		public static ICompileTimePropertyInfo[] Properties => ___properties;

		{fieldInfoDefinitions}

		[System.Runtime.CompilerServices.CompilerGenerated]
		static ICompileTimeFieldInfo[] ___fields = new ICompileTimeFieldInfo[] {{
			{fieldClasses}		
		}};

		[System.Runtime.CompilerServices.CompilerGenerated]
		public static ICompileTimeFieldInfo[] Fields => ___fields;

		{methodInfoDefinitions}

		[System.Runtime.CompilerServices.CompilerGenerated]
		static ICompileTimeMethodInfo[] ___methods = new ICompileTimeMethodInfo[] {{
		{methodClasses}
		}};

		[System.Runtime.CompilerServices.CompilerGenerated]
		public static ICompileTimeMethodInfo[] Methods => ___methods;
	}}
}}";
		}

		private static Dictionary<string, string> GeneratePropertyInfos(ITypeSymbol typeSymbol)
		{
			var classnames = new Dictionary<string, string>();

			var properties = typeSymbol.GetMembers().OfType<IPropertySymbol>();
			foreach (var property in properties)
			{
				if (!property.CanBeReferencedByName || property.Name.Length == 0 || property.GetAttributes().Any(t => t.AttributeClass.Name.Contains("CompilerGenerated")))
				{
					continue;
				}

				var sb = new StringBuilder(512);

				sb.AppendLine($"public class {property.Name}_PropertyInfo : ICompileTimePropertyInfo\n{{");
				sb.AppendLine($"public string Name => \"{property.Name}\";");
				sb.AppendLine($"public Type Type => typeof({property.Type});");
				sb.AppendLine($"public string TypeName => \"{property.Type}\";");
				sb.AppendLine($"public bool IsAbstract => {property.IsAbstract.StringValue()};");
				sb.AppendLine($"public bool IsExtern => {property.IsExtern.StringValue()};");
				sb.AppendLine($"public bool IsIndexer => {property.IsIndexer.StringValue()};");
				sb.AppendLine($"public bool IsOverride => {property.IsOverride.StringValue()};");
				sb.AppendLine($"public bool IsWriteOnly => {property.IsWriteOnly.StringValue()};");
				sb.AppendLine($"public bool IsStatic => {property.IsStatic.StringValue()};");
				sb.AppendLine($"public bool IsVirtual => {property.IsVirtual.StringValue()};");
				sb.AppendLine($"public bool IsReadOnly => {property.IsReadOnly.StringValue()};");
				sb.AppendLine($"public bool IsDefinition => {property.IsDefinition.StringValue()};");
				sb.AppendLine($"public bool IsSealed => {property.IsSealed.StringValue()};");

				if (property.GetMethod is not null)
				{
					sb.AppendLine($"public bool HasGetter => true;");
					if (property.GetMethod.IsStatic)
					{
						sb.AppendLine($"public object GetValue({typeSymbol.Name} instance) => {typeSymbol.Name}.{property.Name};");
					}
					else
					{
						sb.AppendLine($"public object GetValue({typeSymbol.Name} instance) => instance.{property.Name};");
					}
				}
				else
				{
					sb.AppendLine($"public bool HasGetter => false;");
					sb.AppendLine($"public object GetValue({typeSymbol.Name} instance) => throw new System.NotImplementedException();");
				}

				if (property.SetMethod is not null)
				{
					sb.AppendLine($"public bool HasSetter => true;");
					if (property.SetMethod.IsStatic)
					{
						sb.AppendLine($"public void SetValue({typeSymbol.Name} instance, object value) => {typeSymbol.Name}.{property.Name} = ({property.Type.Name})value;");
					}
					else
					{
						sb.AppendLine($"public void SetValue({typeSymbol.Name} instance, object value) => instance.{property.Name} = ({property.Type.Name})value;");
					}
				}
				else
				{
					sb.AppendLine($"public bool HasSetter => false;");
					sb.AppendLine($"public void SetValue({typeSymbol.Name} instance, object value) => throw new System.NotImplementedException();");
				}
				sb.AppendLine("}");
				classnames.Add($"{property.Name}_PropertyInfo", sb.ToString());
			}

			return classnames;
		}

		private static Dictionary<string, string> GenerateFields(ITypeSymbol typeSymbol)
		{
			var classnames = new Dictionary<string, string>();

			var fields = typeSymbol.GetMembers().OfType<IFieldSymbol>();
			foreach (var field in fields)
			{
				if (!field.CanBeReferencedByName || field.Name.Length == 0 || field.IsImplicitlyDeclared || field.GetAttributes().Any(t => t.AttributeClass.Name.Contains("CompilerGenerated")))
				{
					continue;
				}
				//string Name { { get; } }
				//Type Type { { get; } }
				//string TypeName { { get; } }
				//bool IsConst { { get; } }
				//object GetValue({ typeSymbol.Name}
				//instance);
				//bool HasSetter { { get; } }
				//void SetValue({ typeSymbol.Name}
				//instance, object value);
				var sb = new StringBuilder(512);

				sb.AppendLine($"public class {field.Name}_FieldInfo : ICompileTimeFieldInfo\n{{");
				sb.AppendLine($"public string Name => \"{field.Name}\";");
				sb.AppendLine($"public Type Type => typeof({field.Type});");
				sb.AppendLine($"public string TypeName => \"{field.Type}\";");
				sb.AppendLine($"public bool IsConst => {field.IsConst.StringValue()};");
				sb.AppendLine($"public bool IsReadOnly => {field.IsReadOnly.StringValue()};");
				sb.AppendLine($"public bool IsAbstract => {field.IsAbstract.StringValue()};");
				sb.AppendLine($"public bool IsStatic => {field.IsStatic.StringValue()};");
				sb.AppendLine($"public bool IsExtern => {field.IsExtern.StringValue()};");

				if (field.IsAbstract)
				{
					sb.AppendLine($"public bool HasGetter => false;");
					sb.AppendLine($"public object GetValue({typeSymbol.Name} instance) => throw new System.NotImplementedException();");
				}
				else
				{
					sb.AppendLine($"public bool HasGetter => true;");
					if (field.IsStatic)
					{
						sb.AppendLine($"public object GetValue({typeSymbol.Name} instance) => {typeSymbol.Name}.{field.Name};");
					}
					else
					{
						sb.AppendLine($"public object GetValue({typeSymbol.Name} instance) => instance.{field.Name};");
					}
				}

				if (field.IsConst || field.IsReadOnly || field.IsAbstract)
				{
					sb.AppendLine($"public bool HasSetter => false;");
					sb.AppendLine($"public void SetValue({typeSymbol.Name} instance, object value) => throw new System.NotImplementedException();");
				}
				else
				{
					sb.AppendLine($"public bool HasSetter => true;");
					if (field.IsStatic)
					{
						sb.AppendLine($"public void SetValue({typeSymbol.Name} instance, object value) => {typeSymbol.Name}.{field.Name} = ({field.Type.Name})value;");
					}
					else
					{
						sb.AppendLine($"public void SetValue({typeSymbol.Name} instance, object value) => instance.{field.Name} = ({field.Type.Name})value;");
					}
				}
				sb.AppendLine("}");
				classnames.Add($"{field.Name}_FieldInfo", sb.ToString());
			}

			return classnames;
		}

		private static Dictionary<string, string> GenerateMethods(ITypeSymbol typeSymbol)
		{
			var classnames = new Dictionary<string, string>();

			var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>();
			foreach (var method in methods)
			{
				if (!method.CanBeReferencedByName || method.Name.Length == 0)
				{
					continue;
				}

				var sb = new StringBuilder(512);


				sb.AppendLine($"public class {method.Name}_MethodInfo : ICompileTimeMethodInfo\n{{");
				sb.AppendLine($"public string Name => \"{method.Name}\";");
				sb.AppendLine($"public Type ReturnType => typeof({method.ReturnType.Name});");
				sb.AppendLine($"public string ReturnTypeName => \"{method.ReturnType.Name}\";");
				sb.AppendLine($"public bool IsAsync => {method.IsAsync.StringValue()};");
				sb.AppendLine($"public bool IsReadOnly => {method.IsReadOnly.StringValue()};");
				sb.AppendLine($"public bool IsAbstract => {method.IsAbstract.StringValue()};");
				sb.AppendLine($"public bool IsStatic => {method.IsStatic.StringValue()};");
				sb.AppendLine($"public bool IsExtern => {method.IsExtern.StringValue()};");
				sb.AppendLine($"public bool IsConditional => {method.IsConditional.StringValue()};");
				sb.AppendLine($"public bool IsGenericMethod => {method.IsGenericMethod.StringValue()};");
				sb.AppendLine($"public bool IsExtensionMethod => {method.IsExtensionMethod.StringValue()};");
				sb.AppendLine($"public bool HidesBaseMethodsByName => {method.HidesBaseMethodsByName.StringValue()};");
				sb.AppendLine($"public bool IsVararg => {method.IsVararg.StringValue()};");
				sb.AppendLine($"public bool IsCheckedBuiltin => {method.IsCheckedBuiltin.StringValue()};");
				sb.AppendLine($"public bool ReturnsVoid => {method.ReturnsVoid.StringValue()};");
				sb.AppendLine($"public bool ReturnsByRef => {method.ReturnsByRef.StringValue()};");
				sb.AppendLine($"public bool ReturnsByRefReadonly => {method.ReturnsByRefReadonly.StringValue()};");


				sb.AppendLine($"public object Invoke({method.ContainingType.Name}? instance, params object[] args) {{");
				if (!method.ReturnsVoid)
				{
					sb.Append($"var result = ");
				}
				if (method.IsStatic)
				{
					sb.Append($"{method.ContainingType.Name}.{method.Name}(");
				}
				else
				{
					sb.Append($"instance?.{method.Name}(");
				}

				if (method.Parameters.Length > 0)
				{

					for (int i = 0; i < method.Parameters.Length - 1; i++)
					{
						var arg = method.Parameters[i];
						sb.Append($"({arg.Type.Name})args[{i}], ");
					}

					sb.AppendLine($"({method.Parameters[^1].Type.Name}) args[{method.Parameters.Length - 1}]);");
				}
				else
				{
					sb.AppendLine(");");
				}

				if (method.ReturnsVoid)
				{
					sb.AppendLine("return null;");
				}
				else
				{
					sb.AppendLine("return result;");
				}



				sb.AppendLine("}\n}");
				classnames.Add($"{method.Name}_MethodInfo", sb.ToString());
			}

			return classnames;
		}
	}
}