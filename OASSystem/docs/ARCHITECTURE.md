# OAS Architecture

OAS uses Clean Architecture with feature-first organization inside each layer.

## Presentation hosting

`OAS.API/Components/App.razor` is the HTML shell and host, following the SiriusEMR Blazor Web App pattern. `OAS.Client/Routes.razor` owns client routing and is rendered globally from `App.razor` using Interactive WebAssembly without prerendering. Anonymous pages use `EmptyLayout`; authenticated pages use `MainLayout`. There is no `OAS.Client/wwwroot/index.html`.

`OAS.Client` references only `OAS.Contracts` and `OAS.UiLib`. Client Razor pages render UiLib components only; raw HTML and page/component CSS stay out of the Client project.

## Database profiles

The browser receives only database profile keys and display names. Connection strings stay server-side. The selected profile is sent as `X-OAS-Database-Profile`; after login the profile key is also stored as an authentication claim so server-side data access remains bound to the selected database across requests.

The current configuration contains only `Default`, so the database selector is intentionally disabled until additional profiles are configured.

## Database updating

Migrations are never applied automatically during API startup. The login page checks pending migrations and exposes the update action before authentication. Update execution is serialized per connection string and can only apply migrations already compiled into the application; the client cannot submit SQL or connection strings.
