using AutoMapper;
using BibliotecaAPI;
using BibliotecaAPI.Datos;
using BibliotecaAPI.DTOs;
using BibliotecaAPI.Entidades;
using BibliotecaAPI.Jobs;
using BibliotecaAPI.Servicios;
using BibliotecaAPI.Swagger;
using BibliotecaAPI.Utilidades;
using BibliotecaAPI.Utilidades.V1;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

var dicConfiguraciones = new Dictionary<string, string>
{
    {"quien_soy", "Configuraciones en memoria"}
};

builder.Configuration.AddInMemoryCollection(dicConfiguraciones!); // Agrega las configuraciones en memoria al sistema de configuración de la aplicación.

// AREA DE SERVICIOS

builder.Services.AddRateLimiter(opciones =>
{
    //// La particion es un mecanismo para identificar a usuarios con el fin de aplicar politicas de limites
    //opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    //    RateLimitPartition.GetFixedWindowLimiter( // Algoritmo de ventana fija
    //        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido", // La particion indica que se identificara a usaurios por IP o "desconocido"
    //        factory: _ => new FixedWindowRateLimiterOptions // opcions del Rate Limiter
    //        {
    //            PermitLimit = 5,
    //            Window = TimeSpan.FromSeconds(10)
    //        }));

    // Ejemplos Fixed Window
    opciones.AddPolicy("general", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter( // Algoritmo de limitador. Existen varios.
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido", // Se identifica un usuario por su IP
            factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromSeconds(10)
                }
            );
    });

    opciones.AddPolicy("estricta", context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter( // Algoritmo de limitador. Existen varios.
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido", // Se identifica un usuario por su IP
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(5)
            }
            );
    });

    // Ejemplo Sliding Window
    opciones.AddPolicy("movil", context =>
    {
        return RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromSeconds(10),
                SegmentsPerWindow = 2,
                QueueLimit = 1, // Cantidad de peticiones en cola una vez se acaben
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst // Orden de procesamiento del mas viejo al mas nuevo
            });
    });

    // Ejemplo Token Bucket
    opciones.AddPolicy("cubeta", context =>
    {
        return RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5, // Cantidad maxima de tokens
                TokensPerPeriod = 2, // Se añade 2 tokens por periodo
                ReplenishmentPeriod = TimeSpan.FromSeconds(10) // periodo de 10 segundos
            });
    });

    // Ejemplo Token Bucket
    opciones.AddPolicy("concurrencia", context =>
    {
        return RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 1 // Solo se puede hacer 1 solicitud simultaneamente
            });
    });

    opciones.AddPolicy("prueba-usuario", context =>
    {
        var emailClaim = context.User.Claims.Where(x => x.Type == "email").FirstOrDefault()!;
        var email = emailClaim.Value;

        return RateLimitPartition.GetFixedWindowLimiter( // Algoritmo de limitador. Existen varios.
            partitionKey: email, // Se identifica un usuario por su email (Autenticado)
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 2,
                Window = TimeSpan.FromSeconds(20)
            });
    });

    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opciones.OnRejected = async (context, cancellationToken) =>
    {
        if(context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] = retryAfter.TotalSeconds.ToString();
        }

        await context.HttpContext.Response.WriteAsync("Limite excedido. Intente más tarde.", cancellationToken);
    };

});

// OutputCahce Nativo de .Net
builder.Services.AddOutputCache(opciones =>
{
    // Establece el tiempo de expiración predeterminado para la caché de salida en 60 segundos, lo que significa que las respuestas almacenadas en caché serán consideradas válidas durante ese período antes de ser eliminadas o actualizadas.
    opciones.DefaultExpirationTimeSpan = TimeSpan.FromSeconds(15);
});

//builder.Services.AddStackExchangeRedisOutputCache(opciones =>
//{
//    opciones.Configuration = builder.Configuration.GetConnectionString("redis");
//});

builder.Services.AddDataProtection();

var origenesPermitidos = builder.Configuration.GetSection("origenerPermitidos").Get<string[]>()!; // Obtiene el valor de la sección "origenerPermitidos" de la configuración y lo convierte a un arreglo de cadenas, que se utiliza para configurar las políticas de CORS en la aplicación, permitiendo solicitudes solo desde los orígenes especificados en la configuración.

builder.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(opcionesCORS =>
    {
        opcionesCORS.WithOrigins(origenesPermitidos)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("cantidad-total-registros")
            ;
    });
});

builder.Services.AddAutoMapper(cfg => { }, typeof   (Program));
//builder.Services.AddAutoMapper(typeof(Program)); // Esta linea marca un error

//builder.Services.AddControllers().AddJsonOptions(opciones => opciones.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles); // Esta linea es para evitar el error de referencia circular al serializar objetos relacionados
builder.Services.AddDbContext<ApplicationDbContext>(opciones => opciones.UseSqlServer("name=DefaultConnection"));

// Agrega el servicio de Identity Core para la autenticación y autorización, utilizando Entity Framework Core para almacenar los datos de los usuarios en la base de datos a través del ApplicationDbContext.
builder.Services.AddIdentityCore<Usuario>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<UserManager<Usuario>>();
builder.Services.AddScoped<SignInManager<Usuario>>();
// Agrega el servicio de ServiciosUsuarios a la colección de servicios de la aplicación, lo que permite que sea inyectado en otras partes de la aplicación donde se necesite acceder a la información del usuario autenticado.
builder.Services.AddTransient<IServiciosUsuarios, ServiciosUsuarios>();
//builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosAzure>(); // Servicio para almacenar archivos en Azure, se comenta para usar el servicio de almacenamiento local.
builder.Services.AddTransient<IAlmacenadorArchivos, AlmacenadorArchivosLocal>();
builder.Services.AddScoped<FiltroValidacionLibro>();
builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IServicioAutores, BibliotecaAPI.Servicios.V1.ServicioAutores>();

builder.Services.AddScoped<BibliotecaAPI.Servicios.V1.IGeneradorEnlaces, BibliotecaAPI.Servicios.V1.GeneradorEnlaces>();
builder.Services.AddScoped<HATEOASAutorAttribute>();
builder.Services.AddScoped<HATEOASAutoresAttribute>();

builder.Services.AddHostedService<FacturasBackgroundService>(); // Servicio de facturas en background

builder.Services.AddScoped<IServicioLlaves, ServicioLlaves>();

builder.Services.AddHttpContextAccessor();// Agrega el servicio de HttpContextAccessor, que permite acceder al contexto HTTP actual desde cualquier parte de la aplicación, lo que es útil para obtener información sobre el usuario autenticado, las solicitudes y las respuestas.

builder.Services.AddAuthentication().AddJwtBearer(opciones =>
{
    opciones.MapInboundClaims = false;
    opciones.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false, // No se valida el emisor del token, lo que significa que cualquier emisor será aceptado. Esto puede ser útil en entornos de desarrollo o pruebas, pero en producción se recomienda validar el emisor para garantizar la seguridad.
        ValidateAudience = false, // No se valida el destinatario del token, lo que significa que cualquier destinatario será aceptado. Esto puede ser útil en entornos de desarrollo o pruebas, pero en producción se recomienda validar el destinatario para garantizar la seguridad.
        ValidateLifetime = true, // Se valida la vida útil del token, lo que significa que el token será rechazado si ha expirado. Esto es importante para garantizar que los tokens no sean utilizados después de su fecha de vencimiento, lo que mejora la seguridad de la aplicación.
        ValidateIssuerSigningKey = true, // Se valida la clave de firma del emisor, lo que significa que el token será rechazado si la firma no es válida. Esto es crucial para garantizar que el token no haya sido alterado y que provenga de una fuente confiable, lo que mejora significativamente la seguridad de la aplicación.
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["llavejwt"]!)), // Se establece la clave de firma del emisor utilizando una clave simétrica generada a partir de una cadena de texto (en este caso, la configuración "llavejwt"). Esta clave se utiliza para verificar la firma del token y garantizar que provenga de una fuente confiable, lo que mejora la seguridad de la aplicación.
        ClockSkew = TimeSpan.Zero // Se establece el tiempo de tolerancia para la validación de la vida útil del token en cero, lo que significa que el token será rechazado inmediatamente después de su fecha de vencimiento. Esto es importante para garantizar que los tokens no sean utilizados después de su fecha de vencimiento, lo que mejora la seguridad de la aplicación.

    };
});

builder.Services.AddAuthorization(opciones =>
{
    opciones.AddPolicy("esAdmin", politica => politica.RequireClaim("esAdmin"));
});

builder.Services.AddControllers(opciones =>
{
    opciones.Conventions.Add(new ConvencionAgrupaPorVersion());

}).AddNewtonsoftJson();

builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Biblioteca API",
        Description = "Este es un web api para trabajar con datos de autores y libros",
        Contact = new OpenApiContact
        {
            Email = "francisco@hotmail.com",
            Name = "Francisco",
            Url = new Uri("https://youtube.cl")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    opciones.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "Biblioteca API",
        Description = "Este es un web api para trabajar con datos de autores y libros",
        Contact = new OpenApiContact
        {
            Email = "francisco@hotmail.com",
            Name = "Francisco",
            Url = new Uri("https://youtube.cl")
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    opciones.AddSecurityDefinition("bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });

    opciones.OperationFilter<FiltroAutorizacion>();

    //opciones.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    //{
    //    [new OpenApiSecuritySchemeReference("bearer", document)] = []
    //});
});

// Configurar opcion de LimitarPeticionesDTO
builder.Services.AddOptions<LimitarPeticionesDTO>()
    .Bind(builder.Configuration.GetSection(LimitarPeticionesDTO.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var app = builder.Build();

// AREA DE MIDDLEWARES (El orden de los middlewares es importante)

// Middleware para controlar excepciones
app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context =>
{
    var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
    var exception = exceptionHandlerFeature?.Error!;

    var error = new Error()
    {
        MensajeDeError = exception.Message,
        StackTrace = exception.StackTrace,
        Fecha = DateTime.UtcNow
    };

    var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
    dbContext.Add(error);
    await dbContext.SaveChangesAsync();
    await Results.InternalServerError(new
    {
        tipo = "error",
        mensaje = "Ha ocurrido un error inesperado",
        estatus = 500
    }).ExecuteAsync(context);
}));
app.UseSwagger();
app.UseSwaggerUI(opciones =>
{
    opciones.SwaggerEndpoint("/swagger/v1/swagger.json", "Biblioteca API V1"); // agrega distintos endpoint segun version en la interfaz de Swagger
    opciones.SwaggerEndpoint("/swagger/v2/swagger.json", "Biblioteca API V2");
});

// Habilita el middleware para servir archivos estáticos, lo que permite que la aplicación sirva archivos como imágenes,
// CSS y JavaScript desde una carpeta específica (por defecto, wwwroot) sin necesidad de configurar rutas específicas para
// cada archivo.
app.UseStaticFiles();

app.UseRateLimiter(); // Se coloca despues de la carga de archivos estaticos por temas practicos

// Habilita el middleware de CORS para permitir solicitudes desde cualquier origen, método y encabezado, lo que es útil para permitir la comunicación entre el frontend y el backend de la aplicación, especialmente cuando están alojados en dominios diferentes.
app.UseCors();

// Se debe usar a este nivel, despues de app.UseStaticFiles() y app.UseCors() para evitar problemas en los errores que puedan ocurrir el cual puedan interferir en el correcto funcionamiento
//app.UseLimitarPeticiones();

app.UseOutputCache(); // Habilita el middleware de caché de salida, lo que permite almacenar en caché las respuestas de las solicitudes para mejorar el rendimiento de la aplicación al reducir la carga en el servidor y acelerar la entrega de contenido a los clientes.
//app.MapGet("/", () => "Hello World!");
app.MapControllers();

// Fin area de middlewares

app.Run();

public partial class Program { }