# AcxiomCRM

AcxiomCRM is a role-based Sales Customer Relationship Management web application built for managing leads, customers, sales opportunities, follow-ups, team activities, and business reports.

## Tech Stack

- ASP.NET Core 8 MVC (C#)
- Entity Framework Core 8
- Microsoft SQL Server Express
- ASP.NET Core Identity
- Bootstrap 5 & Bootstrap Icons
- Chart.js
- Swagger / OpenAPI

## Key Features

- **Role-Based Access Control**: Scoped views and permissions for Admin, Manager, and Sales Executive.
- **Lead Lifecycle & Conversion**: Manage leads from capture to atomic conversion into Customer and Opportunity records.
- **Interactive Kanban Pipeline Board**: Drag-and-drop opportunity cards across stages with automated weighted pipeline calculations (`Amount × Probability / 100`).
- **Follow-Up & Activity Tracking**: Schedule callbacks, log calls, meetings, emails, and tasks with automatic missed-deadline detection.
- **Executive Dashboard**: Role-scoped KPI summary widgets and dynamic Chart.js visualizations.
- **Reports & Analytics Hub**: 8 operational reports with multi-criteria filtering and RFC 4180-compliant CSV exports.
- **Admin Governance & Audit Trail**: Comprehensive audit log viewer with side-by-side JSON change diffs, user account locking, and password resets.
- **RESTful API**: DTO-based API endpoints for Customers, Leads, Opportunities, and Pipeline Reports documented with Swagger UI.

## Default Logins

| Role | Email | Password |
|---|---|---|
| Admin | admin@acxiomcrm.com | Admin@123 |
| Manager | manager@acxiomcrm.com | Manager@123 |
| Sales Executive | rajesh@acxiomcrm.com | Sales@123 |

## How to Run

1. Clone the repository:
```bash
git clone https://github.com/your-username/AcxiomCRM.git
cd AcxiomCRM
```

2. Update the connection string in `appsettings.json` if your SQL Server instance name differs from `localhost\SQLEXPRESS`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=AcxiomCRM_DB;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```

3. Apply database migrations:
```bash
dotnet ef database update
```

4. Run the application:
```bash
dotnet run
```

5. Open your browser and navigate to:
```
https://localhost:7147
```
Swagger API documentation is available at:
```
https://localhost:7147/swagger
```
