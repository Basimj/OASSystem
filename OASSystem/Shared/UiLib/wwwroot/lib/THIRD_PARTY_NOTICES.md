# OAS.UiLib - Third-party UI libraries

The files in this folder are vendored locally so OAS does not depend on a CDN at runtime.

## Bootstrap

- Bundled version: **5.3.6**
- Website: https://getbootstrap.com/
- License: MIT
- Files used by OAS.UiLib:
  - `bootstrap/css/bootstrap.min.css`
  - `bootstrap/js/bootstrap.bundle.min.js` (vendored for optional future use; not loaded by `App.razor`)

## Font Awesome Free

- Bundled version: **6.7.2**
- Website: https://fontawesome.com/
- License information is preserved in `font-awesome/LICENSE.txt`.
- OAS uses Font Awesome as its icon library. Bootstrap Icons are intentionally not included.

## Updating

Run `Shared/UiLib/tools/install-ui-libraries.ps1` from the repository root to download the versions configured at the top of that script.
