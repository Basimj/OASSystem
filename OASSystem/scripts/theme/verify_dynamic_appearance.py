#!/usr/bin/env python3
"""Dependency-free OAS theme wiring regression checks.

Usage: python scripts/theme/verify_dynamic_appearance.py
Does not replace running the app and testing each workflow.
"""
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
CSS=ROOT/'Shared/UiLib/wwwroot/css'
FILES={
    'shared bridge': CSS/'oas-global-appearance.css',
    'accounting':CSS/'features/accounting.css',
    'sales':CSS/'features/sales.css',
    'HR':CSS/'features/hr-foundation.css',
    'purchasing':CSS/'features/purchasing-theme.css',
    'inventory':CSS/'features/inventory-modern.css',
    'appearance':CSS/'oas-theme-studio.css',
    'customer workspace':ROOT/'Shared/UiLib/Components/Accounting/Parties/UiCustomerWorkspace.razor.css',
    'data grid':ROOT/'Shared/UiLib/Components/Workspace/UiDataGridPanel.razor.css',
}
for name,path in FILES.items():
    if not path.is_file():raise SystemExit(f'Missing {name}: {path}')
    css=path.read_text('utf-8-sig')
    if '--oas-color-' not in css and '--ui-' not in css:
        raise SystemExit(f'{name} has no appearance or bridged legacy tokens')

bundle=(CSS/'oas-ui-bundle.css').read_text()
imports=[x.strip() for x in bundle.splitlines() if x.strip().startswith('@import')]
assert imports[-1] == '@import "./oas-global-appearance.css";', 'Shared theme bridge must load LAST.'

acct=FILES['accounting'].read_text('utf-8-sig')
assert '--oas-acc-primary-rgb' not in acct, 'Hard-coded purple accounting gradients must not return.'

sales=(ROOT/'OAS.Client/Sales/Pages/SalesHomePage.razor').read_text()
assert 'stroke="#258afa"' not in sales and 'fill="#48a2ff"' not in sales
assert 'var(--oas-color-primary)' in sales

sky=(CSS/'oas-sky-sun-theme.css').read_text()
assert '#F9FF26' in sky or '#f9ff26' in sky, 'Sky and Sun colors must remain present'

print('Appearance token wiring checks passed:',len(FILES),'representative component families.')
