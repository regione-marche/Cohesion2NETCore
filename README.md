# Cohesion2NETCore
Applicazione che permette di testare l'implementazione di CohesionSSO su Asp net core

## Come testare la demo
Per poter utilizzare la demo sono richiesti:
* Visual studio 2022
* Net Framework 8.0

Successivamente basterà clonare la repository ed avviarla tramite visual studio per verificare il funzionamento del login/logout tramite Cohesion.

## Come integrarlo nel proprio applicativo
### Requisiti
Per poter integrare cohesion nel proprio applicativo sono richiesti:
* Net Framework 8.0
* Installazione tramite gestore di pacchetti nuget di [Flurl.Http](https://www.nuget.org/packages/Flurl.Http/)

### Classi da copiare
Dopo aver installato i requisiti si dovrà procedere alla copia delle seguenti classi:
* [CohesionService.cs](Services/CohesionService.cs) - ovvero il service che utilizzeremo per effettuare le chiamate a cohesion
* [AppSettings.cs](Models/AppSettings.cs) - modello per la gestione delle variabili tramite appSettings.json
* [AuthCohesionCheckResponse.cs](Models/AuthCohesionCheckResponse.cs) - modello per la gestione delle risposte di cohesion
* [AccountController.cs](Controllers/AccountController.cs) - controller dove vengono gestite le chiamate di login e di logout dell'applicativo

### Aggiornamento della classe [Program.cs](Program.cs)

Per impostare l'accesso alle variabili nel file [appsettings.json](appsettings.json)
``` ASP.NET
builder.Services
    .Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddOptions();
```

Per impostare il servizio di cohesion
``` ASP.NET
builder.Services.AddScoped<ICohesionService, CohesionService>();
```

Per impostare la gestione dell'autenticazione tramite cookie
``` ASP.NET
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
).AddCookie();
```

Per impostare l'utilizzo della sessione
``` ASP.NET
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.Cookie.Name = "CohesionNETCore.Session";
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();
app.UseSession();
```


