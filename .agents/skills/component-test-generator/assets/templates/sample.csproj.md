# Sample TUnit component-test project file

Purpose: provide a starting `.csproj` for a new component-test project. Adapt target framework, package versions, and project references to match the target solution.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- TUnit test projects should be executable. -->
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="TUnit" Version="1.3.15" />
    <PackageReference Include="TUnit.Assertions" Version="0.0.0" />
    <PackageReference Include="Moq" Version="4.20.72" />
    <PackageReference Include="Moq.Contrib.HttpClient" Version="1.4.0" />
    <PackageReference Include="Shouldly" Version="4.3.0" />
    <PackageReference Include="Polly" Version="8.6.5" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
    <PackageReference Include="Microsoft.Extensions.TimeProvider.Testing" Version="10.0.0" />
    <PackageReference Include="Azure.Messaging.ServiceBus" Version="7.20.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\{AppName}\{AppName}.csproj" />
    <ProjectReference Include="..\..\src\{EventListenerApp}\{EventListenerApp}.csproj" />
    <ProjectReference Include="..\..\src\{InfrastructureProject}\{InfrastructureProject}.csproj" />
  </ItemGroup>
</Project>
```

Note: TUnit requires `<OutputType>Exe</OutputType>`. If the solution targets a different framework, change `net9.0` to the repo's actual test-compatible target framework.
