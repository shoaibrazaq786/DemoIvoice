# Pak Accounting ERP

Enterprise-level **Pakistan Accounting ERP & FBR Digital Invoicing System** built with ASP.NET Core MVC (.NET 8), SQL Server, Entity Framework Core, Bootstrap 5, jQuery, and Chart.js.

## Features

- Multi-company support with company switcher
- ASP.NET Identity with 6 roles (SuperAdmin, Admin, Accountant, SalesUser, PurchaseUser, ReportsUser)
- Chart of Accounts (tree view)
- Customers & Vendors with ledgers
- Inventory (FIFO/Average costing, multi-warehouse, batch tracking)
- Sales Invoices with live JS calculations & FBR API integration
- Purchase Bills with auto tax calculation
- Banking (deposit, withdrawal, transfer, cheques)
- Bank Reconciliation
- Financial & operational reports (Trial Balance, P&L, Balance Sheet, Sales Tax)
- Audit logs, permissions, QuestPDF invoices with QR codes
- Red/Black responsive dashboard UI

## Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB or full instance)
- Visual Studio 2022 or VS Code

## Quick Start

1. **Update connection string** in `appsettings.json`:
   ```json
   "DefaultConnection": "Server=localhost;Database=PakAccountingERP;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

2. **Restore & run migrations**:
   ```bash
   cd PakAccountingERP
   dotnet restore
   dotnet ef migrations add InitialCreate
   dotnet ef database update
   dotnet run
   ```

3. **Apply stored procedures** (optional, after migrations):
   ```bash
   sqlcmd -S localhost -d PakAccountingERP -i Database/PakAccountingERP_Schema.sql
   ```

4. **Login** at `https://localhost:7001`:
   - Email: `admin@pakaccounting.pk`
   - Password: `Admin@123`

## Project Structure

```
PakAccountingERP/
├── Controllers/       # MVC controllers (CRUD + AJAX)
├── Data/              # DbContext, Seeder
├── Database/          # SQL scripts & stored procedures
├── Interfaces/        # Repository & service contracts
├── Models/            # EF Core entities & enums
├── Repositories/      # Generic repository + Unit of Work
├── Services/          # Business logic, FBR, PDF, Reports
├── ViewModels/        # Form & API view models
├── Views/             # Razor views (Bootstrap 5)
└── wwwroot/           # CSS, JS, uploads
```

## FBR Integration

Configure per company in **Company Settings**:
- FBR HTTP Post URL
- API Token (Bearer)

Invoices can be submitted via **Submit to FBR** button. Responses and QR data are stored on the invoice record.

## Default Seed Data

- 8 Pakistan provinces
- Units: KG, Pound, Per Piece, Cartons
- Default chart of accounts
- Demo company with 18% sales tax setting
- SuperAdmin user

## Roles & Access

| Module | SuperAdmin | Admin | Accountant | Sales | Purchase | Reports |
|--------|-----------|-------|------------|-------|----------|---------|
| Settings | ✓ | ✓ | | | | |
| User Management | ✓ | ✓ | | | | |
| Invoices | ✓ | ✓ | ✓ | ✓ | | |
| Bills | ✓ | ✓ | ✓ | | ✓ | |
| Reports | ✓ | ✓ | ✓ | | | ✓ |

## Currency

All amounts use **Pakistani Rupee (Rs.)** with `decimal(18,2)` precision.

## License

Proprietary - Demo/Development use.
"# DemoIvoice" 
