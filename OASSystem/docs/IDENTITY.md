# OAS Identity

Identity is organized as a vertical feature across Domain, Contracts, Application, Infrastructure, API, and Client. `OAS.UiLib` remains a business-agnostic UI library.

## Initial Super Administrator

The initial super administrator is created by the database migration itself. No bootstrap credentials are read from `appsettings`, environment variables, or user secrets.

- User name / email: `admin@gmail.com`
- Initial password: `admin@2026`
- Role: `Administrator`
- `IsSuperAdmin = true`
- `MustChangePassword = true`

Only the ASP.NET Core Identity-compatible password hash is stored. The initial hash is also inserted into `security.UserPasswordHistory`. The account is forced to change the default password after the first successful sign-in.

## Roles and super-administrator semantics

`Administrator` is the administrator role. Super administrator is not a second role: it is an administrator account with `IsSuperAdmin = true`.

A normal administrator can manage normal users but cannot create or manage administrator accounts. Operations that modify administrators must enforce the super-administrator rule in the Application layer, not only in the UI.

## Password history

`security.UserPasswordHistory` stores password hashes only. Password changes reject reuse of the recent password history and append the new hash after a successful change.
