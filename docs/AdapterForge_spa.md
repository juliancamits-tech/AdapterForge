# AdapterForge

Una librería .NET que permite centralizar la lógica de negocio y exponerla automáticamente como:

* Minimal API
* Herramientas MCP (Model Context Protocol)

El objetivo es eliminar la duplicación de código entre distintas capas de exposición utilizando una única fuente de verdad basada en operaciones.

Internamente, AdapterForge utiliza Roslyn para generar código mediante Source Generators y realizar validaciones mediante Analyzers.

---

# ⚙️ Instalación

```bash
dotnet add package AdapterForge
```

---

# 🚀 ¿Qué ofrece esta librería?

AdapterForge introduce una capa de abstracción llamada **AdapterForgeOperation**.

Cada operación define:

* Su lógica de negocio.
* Sus parámetros de entrada y salida.
* Su exposición HTTP.
* Su exposición MCP.

A partir de esa definición, AdapterForge genera automáticamente el código necesario para exponer la operación como endpoint HTTP y/o herramienta MCP.

## Filosofía

La operación es la unidad principal del sistema.

HTTP y MCP son únicamente mecanismos de exposición generados automáticamente a partir de la definición de la operación.

Esto permite mantener una única fuente de verdad para la lógica de negocio independientemente de cómo sea consumida.

---

# 📋 Dependencias

AdapterForge no requiere dependencias adicionales para generar endpoints HTTP.

Para la integración MCP se apoya en:

* ModelContextProtocol
* ModelContextProtocol.AspNetCore

Por lo tanto, para utilizar las funcionalidades MCP es necesario instalar dichas dependencias.

---

# 💡 Uso básico

Se debe crear una clase que herede de **AdapterForgeOperation**.

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
            .Description("Obtiene todos los clientes")
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

## Parámetros

AdapterForgeOperation recibe dos tipos genéricos.

### TRequest

Representa los parámetros de entrada de la operación.

### TResponse

Representa el resultado devuelto por la operación.

Aunque una operación no requiera parámetros de entrada, se recomienda utilizar igualmente una clase de request vacía para mantener un modelo consistente.

Estas clases pueden pensarse de forma similar a los modelos utilizados tradicionalmente en Controllers de ASP.NET.

---

# Configure

La función `Configure` define cómo se expone la operación.

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

Define el grupo lógico al que pertenece la operación.

Por ejemplo:

* Client
* Product
* Order
* Invoice

## Name

```csharp
.Name("GetClients")
```

Define el nombre de la operación.

## Description

```csharp
.Description("Obtiene todos los clientes")
```

Describe el propósito de la operación.

Este campo es especialmente importante para MCP, ya que los modelos utilizan esta información para comprender cuándo deben utilizar una herramienta.

Se recomienda que la descripción sea clara, específica y detallada.

---

## Http

```csharp
.Http(HttpMethod.GET)
```

Su sola existencia indica que la operación debe exponerse como endpoint HTTP mediante Minimal APIs.

### Parámetros

#### httpMethod

Enum que define el verbo HTTP.

Valores habituales:

* GET
* POST
* PUT
* PATCH
* DELETE

#### authorization

Nombre de una política registrada mediante:

```csharp
builder.Services.AddAuthorization(...);
```

Permite proteger el endpoint utilizando el sistema estándar de autorización de ASP.NET.

#### summary

Versión resumida del campo `Description`.

Se utiliza principalmente para documentación OpenAPI / Swagger.

### Rutas generadas

Las rutas HTTP siguen la convención:

```text
/api/{Group}/{Name}
```

Por ejemplo:

```csharp
.Group("Client")
.Name("GetClients")
```

Genera:

```text
GET /api/Client/GetClients
```

### Binding de parámetros

AdapterForge resuelve automáticamente el binding de `TRequest` según el verbo HTTP utilizado.

#### GET y DELETE

Los parámetros se obtienen desde Query String.

El objeto `TRequest` se descompone automáticamente en sus propiedades.

Ejemplo:

```csharp
public class GetClientRequest
{
    public int Id { get; set; }
}
```

```http
GET /api/Client/GetClient?id=123
```

#### POST, PUT y PATCH

El objeto `TRequest` se recibe directamente desde el cuerpo (Body) de la petición.

---

## Mcp

```csharp
.Mcp()
```

Su sola existencia indica que la operación debe exponerse como herramienta MCP.

### Parámetros

#### isReadOnly

Indica que la herramienta no modifica estado.

#### isDestructive

Indica que la herramienta puede realizar cambios destructivos.

#### isIdempotent

Indica que múltiples ejecuciones con los mismos parámetros producen el mismo estado final.

#### openWorld

Indica que la herramienta interactúa con sistemas externos.

Ejemplos:

* APIs externas
* Búsquedas web
* Scraping
* Servicios de terceros

---

# Execute

Esta función contiene la lógica de negocio de la operación.

```csharp
public TResponse Execute(TRequest request)
{
    ...
}
```

## Reglas

* Debe devolver el tipo definido como `TResponse`.
* El primer parámetro debe ser `TRequest`.
* Los parámetros adicionales pueden resolverse automáticamente mediante Dependency Injection.

Ejemplo:

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

# Validaciones

AdapterForge no incorpora un mecanismo de validación propio.

Las validaciones son responsabilidad de la implementación de `Execute` o de cualquier componente utilizado internamente por la operación.

Esto permite que cada proyecto adopte la estrategia de validación que considere más apropiada.

---

# Dependency Injection

## Ciclo de vida

Las operaciones se registran automáticamente como **Transient**.

Esto implica que se crea una nueva instancia de la operación para cada ejecución.

---

## HTTP

Para registrar automáticamente los endpoints generados:

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

Registra los servicios generados por AdapterForge.

### MapAdapterForgeEndpoints

Expone los endpoints HTTP generados.

---

## MCP

Para habilitar MCP no se requiere ninguna configuración especial adicional.

Simplemente debe utilizarse `WithToolsFromAssembly` para que el servidor MCP descubra las herramientas generadas.

```csharp
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();
```

---

# Beneficios

* Una única fuente de verdad para la lógica de negocio.
* Eliminación de código repetido.
* Exposición automática como HTTP y MCP.
* Integración nativa con Dependency Injection.
* Compatible con Minimal APIs.
* Compatible con el ecosistema MCP.
* Metadatos centralizados para documentación y descubrimiento.
* Facilita la evolución futura hacia nuevos mecanismos de exposición.

---

# Estado actual

La versión actual está enfocada en:

* Minimal APIs
* MCP Tools
* Source Generators
* Roslyn Analyzers

Las funcionalidades futuras podrán ampliar los mecanismos de exposición sin requerir cambios en las operaciones existentes.
