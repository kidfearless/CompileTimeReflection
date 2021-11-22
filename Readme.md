
## Compile Time Reflection

This project is a working proof of concept for a more performant reflection. Simply include this project in your solution and reference it in your project using the following snippet.
```
<ItemGroup>
	<ProjectReference Include=".\CompileTimeReflection.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

Once that's included and the path to the project is set properly, then all you have to do is mark the class you want to reflect as partial, and add the `[CompileTimeReflection]` attribute to the class. This will tell the generator add all the reflection code to your class. From their you'll have access to 2 new static properties: Fields, Properties. Methods was created but eventually removed since it was less performant than the already existing methods.

If you like this project and want to contribute feel free to make a pull request.

![Benchmark Results](https://i.imgur.com/p9njTHk.png)

If you like this project and want to support me you can always buy me a pizza 😁
<a href="https://www.buymeacoffee.com/kidfearless" target="_blank"><img src="https://cdn.buymeacoffee.com/buttons/v2/default-violet.png" alt="Buy Me A Coffee" style="height: 60px !important;width: 217px !important;" ></a>