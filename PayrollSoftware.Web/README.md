# 💼 Payroll Software

A comprehensive, enterprise-grade payroll management system built with **ASP.NET Core MVC** and a modern responsive frontend using **AdminLTE**. This application streamlines employee management, attendance tracking, leave management, and payroll processing for organizations of all sizes. 

## 📋 Table of Contents

- [Features](#-features)
- [Technology Stack](#-technology-stack)
- [Architecture](#-architecture)
- [Prerequisites](#-prerequisites)
- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)
- [License](#-license)

## ✨ Features

### 👥 Employee Management
- Complete employee profile management with personal and professional details
- Support for multiple employment types and payment systems
- Department and designation tracking
- Bank account information for salary disbursement
- Employee status management (Active/Inactive)
- Unique employee codes and machine codes for biometric integration

### ⏰ Attendance Management
- Daily attendance tracking with check-in/check-out times
- Automatic calculation of working hours
- Late entry and early leave detection
- Shift-based attendance management
- Bulk attendance import functionality
- Integration with leave management

### 📅 Leave Management
- Multiple leave types support (Casual, Sick, Earned, Unpaid)
- Leave application workflow (Pending → Approved/Rejected)
- Leave balance tracking
- Approval/denial functionality with remarks
- Automatic leave days calculation

### 💰 Payroll Processing
- Automated payroll calculation based on attendance and leave data
- Support for allowances and deductions
- Pay period management (monthly processing)
- Payment status tracking (Pending/Paid)
- Salary slip generation
- Net salary calculation with breakdown

### 📊 Dashboard & Reports
- Real-time dashboard with key metrics
- Employee statistics (Active, Inactive, Total)
- Daily attendance overview (Present, Absent, Late)
- Monthly payroll statistics
- Payment status summary

### 🔐 Authentication & Security
- ASP.NET Core Identity integration
- User authentication and authorization
- Role-based access control
- Audit trails (CreatedBy, UpdatedBy, timestamps)

## 🛠 Technology Stack

| Category | Technology |
|----------|------------|
| **Backend** | ASP. NET Core MVC, C# |
| **Frontend** | JavaScript, HTML, CSS |
| **UI Framework** | AdminLTE 3, Bootstrap 4 |
| **Database** | SQL Server |
| **Authentication** | ASP. NET Core Identity, Entity Framework Core |
| **JavaScript Libraries** | jQuery, DataTables, Select2, Chart.js |
| **Icons** | Font Awesome |

## 🏗 Architecture

The project follows a clean architecture pattern with separation of concerns:

```
PayrollSoftware/
├── PayrollSoftware. Web/           # Presentation Layer (MVC)
│   ├── Controllers/               # MVC Controllers
│   ├── Views/                     # Razor Views
│   ├── wwwroot/                   # Static files (CSS, JS, AdminLTE)
│   └── Program.cs                 # Application entry point
│
├── PayrollSoftware.Infrastructure/  # Infrastructure Layer
│   ├── Application/
│   │   ├── DTOs/                  # Data Transfer Objects
│   │   └── Interfaces/            # Repository Interfaces
│   ├── Domain/
│   │   └── Entities/              # Domain Entities
│   ├── Data/                      # Database Context
│   ├── Identity/                  # Identity Configuration
│   ├── Migrations/                # EF Core Migrations
│   └── Repositories/              # Repository Implementations
│
└── PayrollSoftware.sln            # Solution File
```

### Domain Entities

- **Employee** - Core employee information
- **Attendance** - Daily attendance records
- **Leave** - Leave applications and approvals
- **Payroll** - Monthly payroll calculations
- **SalarySlip** - Generated salary slips
- **Shift** - Work shift definitions
- **AllowanceDeduction** - Salary components
- **Lookup** - Reference data (departments, designations, etc.)

## 📋 Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full version)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio. com/)
- [Node.js](https://nodejs.org/) (optional, for frontend package management)

## 🚀 Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/AlMamun09/Payroll-Software. git
   cd Payroll-Software
   ```

2.  **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Update the database connection string**
   
   Edit `appsettings.json` in the `PayrollSoftware.Web` project:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=Your_Server_Name;Database=PayrollSoftwareDb;Trusted_Connection=True;MultipleActiveResultSets=true"
     }
   }
   ```

4. **Apply database migrations**
   ```bash
   cd PayrollSoftware.Web
   dotnet ef database update
   ```

5.  **Run the application**
   ```bash
   dotnet run
   ```

6. **Access the application**
   
   Open your browser and navigate to `https://localhost:5001` or `http://localhost:5000`

## ⚙ Configuration

### Database Configuration
The application uses Entity Framework Core with SQL Server. Configure your connection string in `appsettings.json`. 

### Identity Configuration
User authentication is handled by ASP.NET Core Identity with the `PayrollUser` class.

## 📖 Usage

### Dashboard
Upon login, users are greeted with a comprehensive dashboard displaying:
- Total, active, and inactive employee counts
- Today's attendance summary
- Monthly payroll statistics
- Pending vs. completed payments

### Managing Employees
1. Navigate to **Manage Employees**
2.  Add new employees with complete profile information
3.  Assign departments, designations, and shifts
4. Configure payment systems and bank details

### Recording Attendance
1.  Navigate to **Manage Attendance**
2. Add daily attendance records manually or import in bulk
3. System automatically calculates working hours, late entries, and early leaves

### Leave Management
1. Navigate to **Manage Leave**
2. Create leave applications for employees
3.  Approve or reject pending leave requests
4. View all leaves, pending leaves, or approved leaves

### Processing Payroll
1. Navigate to **Manage Payroll**
2. Select employee and pay period
3. System calculates payroll based on:
   - Basic salary
   - Attendance (present days, absent days)
   - Approved leaves (paid/unpaid)
   - Allowances and deductions
4.  Mark payroll as paid upon disbursement
5. Generate and view salary slips

## 📁 Project Structure

```
PayrollSoftware. Infrastructure/
├── Application/
│   ├── DTOs/
│   │   ├── AttendanceDto.cs
│   │   ├── DashboardDto.cs
│   │   ├── EmployeeDto.cs
│   │   ├── LeaveDto.cs
│   │   ├── PayrollDto.cs
│   │   └── SalarySlipDto.cs
│   └── Interfaces/
│       ├── IAttendanceRepository.cs
│       ├── IEmployeeRepository.cs
│       ├── ILeaveRepository. cs
│       ├── IPayrollRepository.cs
│       └── ISalarySlipRepository. cs
├── Domain/Entities/
│   ├── Attendance.cs
│   ├── Employee.cs
│   ├── Leave. cs
│   ├── Payroll.cs
│   ├── SalarySlip.cs
│   └── Shift.cs
└── Repositories/
    ├── AttendanceRepository.cs
    ├── EmployeeRepository. cs
    ├── LeaveRepository. cs
    └── PayrollRepository. cs

PayrollSoftware. Web/
├── Controllers/
│   ├── AttendanceController.cs
│   ├── EmployeeController.cs
│   ├── HomeController.cs
│   ├── LeaveController.cs
│   └── PayrollController. cs
├── Views/
│   ├── Attendance/
│   ├── Employee/
│   ├── Home/
│   ├── Leave/
│   ├── Payroll/
│   └── Shared/
└── wwwroot/
    └── adminlte/
```

## 🤝 Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2.  Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4.  Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is open source.  Please check the repository for license information.

---

## 👨‍💻 Author

**AlMamun09** - [GitHub Profile](https://github. com/AlMamun09)

---

⭐ If you find this project useful, please consider giving it a star on GitHub! 