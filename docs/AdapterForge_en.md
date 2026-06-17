# AdapterForge

[Documentacion en español](AdapterForge_spa.md)

A .NET library that centralizes business logic and automatically exposes it as:

* Minimal API
* MCP tools (Model Context Protocol)

The goal is to eliminate duplicated code between different exposure layers by using a single source of truth based on operations.

Internally, AdapterForge uses Roslyn to generate code through Source Generators and perform validations through Analyzers.

---

# ⚙️ Installation

```bash
dotnet add package AdapterForge
```

---

# 🚀 What does this library offer?

AdapterForge introduces an abstraction layer called **AdapterForgeOperation**.

Each operation defines:

* Its business logic.
* Its input and output parameters.
* Its HTTP exposure.
* Its MCP exposure.

From that definition, AdapterForge automatically generates the code needed to expose the operation as an HTTP endpoint and/or an MCP tool.

## Philosophy

The operation is the system's main unit.

HTTP and MCP are only exposure mechanisms automatically generated from the operation definition.

This allows keeping a single source of truth for business logic regardless of how it is consumed.

---

# 📋 Dependencies

AdapterForge does not require additional dependencies to generate HTTP endpoints.

For MCP integration, it relies on:

* ModelContextProtocol
* ModelContextProtocol.AspNetCore

Therefore, to use MCP features, those dependencies must be installed.

---

# 💡 Basic usage

You must create a class that inherits from **AdapterForgeOperation**.

```csharp
using AdapterForge.Abstractions;

namespace TestApi;

public class GetClients : AdapterForgeOperation<GetClientsRequest, ClientsDto>
{
    protected override void Configure(OperationBuilder builder)
    {
        builder
            .Group("Client")
            .Name("GetClients")
            .Description("Obtains all clients")
            .Http(HttpMethod.GET)
            .Mcp();
    }

    public ClientsDto Execute(GetClientsRequest request)
    {
        var dto = new ClientsDto();

        dto.Names.Add("John Doe");
        dto.Names.Add("Jane Doe");

        return dto;
    }
}

public class GetClientsRequest
{
}

public class ClientsDto
{
    public List<string> Names { get; set; } = [];
}
```

---

# AdapterForgeOperation

```csharp
AdapterForgeOperation<TRequest, TResponse>
```

## Parameters

AdapterForgeOperation receives two generic types.

### TRequest

Represents the operation's input parameters.

### TResponse

Represents the result returned by the operation.

Even if an operation does not require input parameters, it is recommended to still use an empty request class to maintain a consistent model.

These classes can be thought of in a similar way to models traditionally used in ASP.NET Controllers.

---

# Configure

The `Configure` function defines how the operation is exposed.

```csharp
protected override void Configure(OperationBuilder builder)
{
    ...
}
```

## Group

```csharp
.Group("Client")
```

Defines the logical group to which the operation belongs.

For example:

* Client
* Product
* Order
* Invoice

## Name

```csharp
.Name("GetClients")
```

Defines the operation name.

## Description

```csharp
.Description("Obtains all clients")
```

Describes the operation's purpose.

This field is especially important for MCP, as models use this information to understand when to use a tool.

It is recommended that the description be clear, specific, and detailed.

---

## Http

```csharp
.Http(HttpMethod.GET)
```

Its mere existence indicates that the operation should be exposed as an HTTP endpoint using Minimal APIs.

### Parameters

#### httpMethod

Enum that defines the HTTP verb.

Common values:

* GET
* POST
* PUT
* PATCH
* DELETE

#### authorization

Name of a policy registered through:

```csharp
builder.Services.AddAuthorization(...);
```

Allows protecting the endpoint using ASP.NET's standard authorization system.

#### summary

Summarized version of the `Description` field.

It is mainly used for OpenAPI / Swagger documentation.

### Generated routes

HTTP routes follow the convention:

```text
/api/{Group}/{Name}
```

For example:

```text
GET /api/Client/GetClients
```

### Parameter binding

AdapterForge automatically resolves the binding of `TRequest` according to the HTTP verb used.

#### GET and DELETE

Parameters are obtained from the query string.

The `TRequest` object is automatically decomposed into its properties.

Example:

```csharp
public class GetClientRequest
{
    public int Id { get; set; }
}
```

```http
GET /api/Client/GetClient?id=123
```

#### POST, PUT and PATCH

The `TRequest` object is received directly from the request body.

---

## Mcp

```csharp
.Mcp()
```

Its mere existence indicates that the operation should be exposed as an MCP tool.

### Parameters

#### isReadOnly

Indicates that the tool does not modify state.

#### isDestructive

Indicates that the tool may perform destructive changes.

#### isIdempotent

Indicates that multiple executions with the same parameters produce the same final state.

#### openWorld

Indicates that the tool interacts with external systems.

Examples:

* External APIs
* Web searches
* Scraping
* Third-party services

---

# Execute

This function contains the operation's business logic.

```csharp
public TResponse Execute(TRequest request)
{
    ...
}
```

## Rules

* It must return the type defined as `TResponse`.
* The first parameter must be `TRequest`.
* Additional parameters can be automatically resolved through Dependency Injection.

Example:

```csharp
public ClientDto Execute(
    GetClientRequest request,
    IClientRepository repository,
    ILogger<GetClient> logger)
{
    ...
}
```

---

# Validations

AdapterForge does not incorporate its own validation mechanism.

Validations are the responsibility of the `Execute` implementation or any component used internally by the operation.

This allows each project to adopt the validation strategy it considers most appropriate.

---

# Dependency Injection

## Lifetime

Operations are registered automatically as **Transient**.

This means a new instance of the operation is created for each execution.

---

## HTTP

To automatically register the generated endpoints:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Rest of code

builder.Services.AddAdapterForgeEndpoints();

// Rest of code

var app = builder.Build();

// Rest of code

app.MapAdapterForgeEndpoints();

// Rest of code
```

### AddAdapterForgeEndpoints

Registers the services generated by AdapterForge.

### MapAdapterForgeEndpoints

Exposes the generated HTTP endpoints.

---

## MCP

No additional special configuration is required to enable MCP.

You simply need to use `WithToolsFromAssembly` so the MCP server discovers the generated tools.

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
```

---

# Benefits

* A single source of truth for business logic.
* Elimination of repetitive code.
* Automatic exposure as HTTP and MCP.
* Native integration with Dependency Injection.
* Compatible with Minimal APIs.
* Compatible with the MCP ecosystem.
* Centralized metadata for documentation and discovery.
* Facilitates future evolution toward new exposure mechanisms.

---

# Current state

The current version is focused on:

* Minimal APIs
* MCP Tools
* Source Generators
* Roslyn Analyzers

Future features may extend the exposure mechanisms without requiring changes to existing operations.
