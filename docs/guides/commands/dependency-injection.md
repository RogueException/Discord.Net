---
uid: Guides.Commands.DI
title: Dependency Injection
---

# Dependency Injection

The Command Service is bundled with a very barebone Dependency
Injection service for your convenience. It is recommended that you use
DI when writing your modules.

## Setup

1. Create an @System.IServiceProvider.
2. Add the dependencies to the service collection that you wish
 to use in the modules.
3. Pass the service collection into `AddModulesAsync`.

### Example - Setting up Injection

[!code-csharp[IServiceProvider Setup](samples/dependency_map_setup.cs)]

## Usage in Modules

In the constructor of your module, any parameters will be filled in by
the @System.IServiceProvider that you've passed.

Any publicly settable properties will also be filled in the same
manner.

> [!NOTE]
> Annotating a property with a [DontInjectAttribute] attribute will
> prevent the property from being injected.

> [!NOTE]
> If you accept `CommandService` or `IServiceProvider` as a parameter
> in your constructor or as an injectable property, these entries will
> be filled by the `CommandService` that the module is loaded from and
> the `IServiceProvider` that is passed into it respectively.

### Example - Injection in Modules

[!code-csharp[IServiceProvider in Modules](samples/dependency_module.cs)]

[DontInjectAttribute]: xref:Discord.Commands.DontInjectAttribute