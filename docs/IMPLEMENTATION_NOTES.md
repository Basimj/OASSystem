# Implementation notes

- Every project declares `.NET 9` and its common compiler settings directly in its own `.csproj`; no `Directory.Build.props` is used.
- Business entity folders intentionally contain no entities yet.
- The source project was used as an architectural reference only; hospital-specific entities, modules, devices, laboratory middleware, waiting screens, and medical workflows were not copied.
- `OAS.UiLib` remains feature-agnostic and owns all reusable application UI implementation. The technical pre-Blazor loader stylesheet also lives in UiLib because Razor components are unavailable until the WebAssembly runtime starts.
- No secrets are embedded. Replace development connection/authentication values with environment-specific configuration or a secret store before deployment.

- Startup metadata is read from `Application:Name` and `Localization:DefaultCulture`; `/api/bootstrap` exposes only those public values to the pre-Blazor host loader.
