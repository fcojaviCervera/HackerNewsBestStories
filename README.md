# Hacker News Best Stories API

API REST en ASP.NET Core (.NET 10) que devuelve las `n` mejores historias de Hacker News ordenadas por score, de mayor a menor.

## Cómo ejecutarlo

Hace falta el SDK de .NET 10.

```bash
dotnet run --project src/HackerNewsBestStories.Api
```

La API arranca en `http://localhost:5193` y se abre Swagger en `http://localhost:5193/swagger`.

También se puede ejecutar con Docker:

```bash
docker build -t hackernews-best-stories .
docker run --rm -p 8080:8080 hackernews-best-stories
```

Tests:

```bash
dotnet test
```

## Uso

```
GET /api/beststories?n=10
```

`n` tiene que estar entre 1 y 200. Si no viene o está fuera de rango, la API devuelve 400.

Ejemplo de respuesta:

```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
```

## Cómo evito sobrecargar la API de Hacker News

- **Caché en memoria** con `HybridCache`: la lista de ids se guarda 1 minuto y cada historia 5 minutos. Mientras la caché está llena, las peticiones no llaman a Hacker News.
- **Protección contra estampidas**: si llegan muchas peticiones a la vez y la caché está vacía, `HybridCache` hace una sola llamada por clave y el resto espera ese resultado. Así no se lanzan cientos de llamadas iguales a la vez.
- **Máximo 10 llamadas en paralelo** a Hacker News por petición al cargar las historias.
- **Resiliencia** con `AddStandardResilienceHandler`: reintentos, timeouts y circuit breaker. Si Hacker News falla, la API devuelve 503.
- **Rate limiting** en nuestra API: 100 peticiones cada 10 segundos.

### Estrategias de caché que he valorado

1. **Caché bajo demanda (la que he usado)**. Cada dato se pide la primera vez que hace falta y se guarda con un tiempo de expiración. Es la opción más sencilla y limita bien las llamadas a Hacker News. La pega es que la primera petición después de que caduque la caché es más lenta (1-2 segundos con la caché vacía).
2. **Refresco en segundo plano**. Un `BackgroundService` descarga cada X segundos los ids y todas las historias y los deja en memoria, y las peticiones solo leen de ahí. La carga sobre Hacker News es siempre la misma, venga el tráfico que venga, y todas las respuestas son rápidas. Además, si Hacker News se cae se pueden seguir sirviendo los últimos datos. Como contra, descarga datos aunque nadie los pida, y hay que decidir qué hacer al arrancar mientras no hay datos.
3. **Híbrida**. Refresco en segundo plano solo de la lista de ids (que es una única llamada) y caché bajo demanda para cada historia.

He elegido la primera porque cumple el requisito con poco código. Si fuera a producción con tráfico constante, pasaría a la segunda.

## Suposiciones

- "Best n stories as determined by their score": el orden de `beststories.json` no es exactamente por score, así que cojo todas las historias (máximo 200), las ordeno por score y devuelvo las `n` primeras.
- Si se piden más historias de las que hay, se devuelven todas las disponibles.
- Se ignoran los items que no existen, los borrados, los "dead" y los que no son de tipo `story`.
- Las historias sin url (por ejemplo, los "Ask HN") devuelven como `uri` el enlace a la discusión en Hacker News.
- Debido a el uso de caché, es aceptable que los datos tengan unos minutos de antigüedad.
- Si falla la llamada a Hacker News después de los reintentos, se devuelve 503 .

## Mejoras con más tiempo

- Pasar al refresco en segundo plano y seguir sirviendo los últimos datos si Hacker News está caído.
- Usar Redis como caché distribuida si hay varias instancias, para que no multipliquen las llamadas a Hacker News.
- Si falla una sola historia, devolver el resto en lugar de un error.
- Rate limiting por cliente (IP o API key) en lugar de global.
- Mover a `appsettings.json` los tiempos de caché y los límites, que ahora están en el código.
- Tests de integración con `WebApplicationFactory` para probar el controller (validación, 503, 429).
- Health checks, métricas y logs más completos.
- Pipeline de CI en GitHub Actions.

## Uso de IA

He utilizado un asistente de IA (Claude) como apoyo durante el desarrollo, principalmente para:

- Contrastar alternativas de diseño, como las estrategias de caché descritas arriba.
- Generar parte del código inicial y de los tests, que después he revisado y adaptado.
- Revisar la redacción de este README.

Las decisiones de diseño son mías y entiendo y puedo explicar todo el código del repositorio.
