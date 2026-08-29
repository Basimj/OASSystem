# Verification

Run from the solution root:

```powershell
dotnet restore OASSystem.sln
dotnet build OASSystem.sln --no-restore
dotnet test OASSystem.sln --no-build
```

Set `OAS.API` as the startup project. The browser opens `/login`; Swagger remains available at `/swagger` in Development.

## First database initialization

The API no longer applies migrations during startup. The login screen checks the selected database profile.

1. Start `OAS.API`.
2. Open `/login`.
3. The `Default` database profile is selected. While it is the only configured profile, the combo box is disabled.
4. If migrations are pending, the database indicator is orange, login controls are disabled, and **Update database** is enabled.
5. Click **Update database**. The API applies only the migrations embedded in the application.
6. When no migrations remain, the indicator becomes green, the update button is disabled, and sign-in controls are enabled.
7. Sign in with `admin@gmail.com` / `admin@2026` on a new database and immediately change the required default password.

Migration bodies use SQL Raw (`migrationBuilder.Sql`) while EF Core continues to track applied/pending migration IDs in `__EFMigrationsHistory`.
