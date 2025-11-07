# Payroll Software

A comprehensive payroll management system built with ASP.NET Core MVC (.NET 9) and SQL Server, featuring modern web technologies and a dark-mode AdminLTE interface.

## ?? Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Technology Stack](#technology-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Database Configuration](#database-configuration)
- [Project Structure](#project-structure)
- [Key Features Details](#key-features-details)
- [User Interface](#user-interface)
- [Contributing](#contributing)

## ?? Overview

This Payroll Software is a full-featured Human Resources and Payroll Management System designed to streamline employee management, attendance tracking, leave management, and payroll processing. Built with modern web technologies, it provides a responsive, user-friendly interface with real-time data updates and comprehensive validation.

## ? Features

### Human Resources Management
- **Employee Management**
  - Complete employee profile management
  - Employee code auto-generation
  - Department and designation assignment
  - Shift scheduling
  - Employment type tracking (Permanent, Contract, Intern)
  - Multiple payment systems (Bank Transfer, Mobile Banking, Cash Payment)
  - Status tracking (Active, Resigned)
  - Age calculation from date of birth

### Attendance Management
- **Smart Attendance Tracking**
  - Real-time attendance recording
  - Automatic working hours calculation
  - Late entry detection
  - Early leave tracking
  - Shift-based validation
  - Overnight shift support
  - Gap detection and handling
  - Integration with leave system
  - Prevents duplicate attendance entries

### Leave Management
- **Leave Request System**
  - Multiple leave types (Sick, Casual, Annual, etc.)
  - Leave balance tracking
- Approval workflow
  - Leave status management
  - Date range validation
  - Integration with attendance system

### Organizational Structure
- **Department Management** - Create and manage departments with active/inactive status
- **Designation Management** - Job title management with role hierarchy
- **Shift Management** - Flexible shift creation with overnight shift support

### Finance & Payroll
- **Allowances & Deductions** - Configurable with percentage or fixed amount calculations
- **Payroll Processing** - Automated salary calculations with allowances and deductions
- **Salary Slips** - Automated generation with comprehensive breakdown

## ?? Technology Stack

### Backend
- **Framework**: ASP.NET Core MVC 9.0
- **Language**: C# 13.0
- **Database**: SQL Server (LocalDB)
- **ORM**: Entity Framework Core 9.0.10
- **Authentication**: ASP.NET Core Identity

### Frontend
- **Template**: AdminLTE 3 (Dark Mode)
- **JavaScript Libraries**:
  - jQuery 3.x
  - Select2 4.1.0-rc.0
  - SweetAlert2 11
  - DataTables
- **UI Framework**: Bootstrap 4
- **Icons**: Font Awesome

### Additional Technologies
- **Email Service**: SMTP (Gmail)
- **Charting**: uPlot.js
- **Form Validation**: Client-side and server-side
- **AJAX**: Custom AJAX utilities for seamless operations

## ?? Architecture

The application follows a clean architecture pattern with clear separation of concerns:

```
PayrollSoftware/
??? PayrollSoftware.Web/       # Presentation Layer
?   ??? Controllers/        # MVC Controllers
?   ??? Views/   # Razor Views
?   ??? Areas/Identity/          # Identity scaffolded pages
?   ??? wwwroot/       # Static files
?   ??? js/     # Custom JavaScript
?       ??? adminlte/    # AdminLTE assets
?
??? PayrollSoftware.Infrastructure/ # Data & Business Logic Layer
    ??? Application/
    ?   ??? DTOs/     # Data Transfer Objects
    ?   ??? Interfaces/            # Repository Interfaces
  ??? Domain/
    ?   ??? Entities/    # Domain Models
    ??? Identity/        # Identity configuration
    ??? Repositories/          # Repository implementations
    ??? Services/           # Business services
    ??? Data/        # Database context
```

## ?? Getting Started

### Prerequisites
- .NET 9.0 SDK
- SQL Server or SQL Server LocalDB
- Visual Studio 2022 or VS Code
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/AlMamun09/Payroll-Software.git
   cd Payroll-Software
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
 ```

3. **Update database connection string**
   
   Open `PayrollSoftware.Web/appsettings.json` and update the connection string:
   ```json
   {
     "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=PayrollSoftware;Trusted_Connection=True;MultipleActiveResultSets=true"
 }
   }
   ```

4. **Apply database migrations**
   ```bash
   cd PayrollSoftware.Web
   dotnet ef database update
   ```

5. **Configure email settings (optional)**
   
   Update email settings in `appsettings.json`:
   ```json
   {
     "EmailSettings": {
       "SmtpServer": "smtp.gmail.com",
       "SmtpPort": 587,
       "SmtpUsername": "your-email@gmail.com",
       "SmtpPassword": "your-app-password",
       "FromEmail": "your-email@gmail.com",
       "EnableSsl": true
     }
   }
   ```

6. **Run the application**
   ```bash
   dotnet run
   ```

7. **Access the application**
   
   Open your browser and navigate to:
   - HTTPS: `https://localhost:5001`
   - HTTP: `http://localhost:5000`

## ?? Database Configuration

### Migrations

The project uses Entity Framework Core migrations for database schema management. Key migrations include:

- `InitialMigration` - Base schema
- `Registration` - User registration tables
- `Regi` - Registration updates
- `DesignationIdInEmployee` - Designation integration
- `New` - Additional features
- `ShiftIdInAttendence` - Shift tracking in attendance
- `LeaveInAttendence` - Leave integration

### Running Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Update database
dotnet ef database update

# Rollback migration
dotnet ef database update PreviousMigrationName

# Remove last migration
dotnet ef migrations remove
```

## ?? Project Structure

### PayrollSoftware.Web
- **Controllers**: Handle HTTP requests and business logic flow
  - `EmployeeController` - Employee CRUD operations
  - `AttendanceController` - Attendance management
  - `LeaveController` - Leave request handling
  - `DepartmentController` - Department management
  - `DesignationController` - Designation management
  - `ShiftController` - Shift scheduling
  - `AllowanceDeductionController` - Finance configuration

- **Views**: Razor templates for UI
  - Shared layout with dark mode
  - Form validation
  - DataTables integration
  - Modal-based CRUD operations

- **wwwroot/js**: Custom JavaScript utilities
  - `common-ajax.js` - AJAX helper functions
  - `crud-datatable.js` - DataTable utilities
  - `crud-modal.js` - Modal operations

### PayrollSoftware.Infrastructure
- **Domain/Entities**: Core business entities
  - `Employee`
  - `Attendance`
  - `Leave`
  - `Department`
  - `Designation`
  - `Shift`
  - `Payroll`
  - `SalarySlip`
  - `AllowanceDeduction`

- **Application/DTOs**: Data transfer objects for API/View communication

- **Repositories**: Data access layer with business logic validation
  - `EmployeeRepository` - Employee operations with validation
  - `AttendanceRepository` - Attendance with smart validation
  - `LeaveRepository` - Leave management
  - `DepartmentRepository` - Department operations
  - `DesignationRepository` - Designation operations
  - `ShiftRepository` - Shift management
  - `AllowanceDeductionRepository` - Finance configuration

- **Services**: External service integrations
  - `EmailService` - Email notifications

## ?? Key Features Details

### Employee Management
- **Auto-generated Employee Codes**
- **Comprehensive Profile**:
  - Personal information (Name, DOB, Gender)
  - Contact details (Mobile number)
  - Employment details (Joining date, Employment type, Status)
  - Payment information (Payment system, Bank details)
- Department and Designation assignment
  - Shift scheduling

### Attendance System
- **Smart Validation**:
  - Prevents attendance before joining date
  - Blocks attendance for resigned employees
  - Prevents attendance during approved leaves
  - Validates shift assignments
  - Detects duplicate entries for same date

- **Automatic Calculations**:
  - Working hours (supports overnight shifts)
  - Late entry time (compared to shift start)
  - Early leave time (compared to shift end)
  - Status determination (Present/Absent)

- **Shift Support**:
  - Regular shifts (same day)
  - Overnight shifts (crossing midnight)
  - Flexible shift assignments
  - Auto-assignment from employee profile

### Leave Management
- **Leave Types**: Sick, Casual, Annual, Unpaid
- **Status Workflow**: Pending ? Approved/Rejected
- **Validation**:
  - Date range validation
  - Overlap detection
  - End date must be after start date
  - Integration with attendance system

### Real-time Features
- **AJAX Operations**: Fast, seamless data operations without page reloads
- **Instant Validation**: Client and server-side validation
- **SweetAlert Notifications**: User-friendly success/error messages
- **Dynamic Forms**: Auto-calculating fields (age, working hours, late entry, early leave)

## ?? User Interface

### Design Highlights
- **Dark Mode Theme**: Easy on the eyes for extended use
- **Responsive Design**: Works on desktop, tablet, and mobile
- **Modern UI Components**:
  - Select2 dropdowns with search functionality
  - Date pickers for date selection
  - Time pickers for attendance
  - DataTables with sorting/filtering/pagination
  - Modal dialogs for CRUD operations
  - Toast notifications for user feedback

### Navigation
- **Sidebar Menu**:
  - Dashboard
  - Human Resources section
    - Manage Employees
- Manage Attendance
    - Manage Leaves
    - Manage Departments
    - Manage Designations
    - Manage Shifts
  - Finance & Payroll section
    - Allowances & Deductions
    - Process Payroll
    - Salary Slips

### User Profile
- Displays logged-in user information
- Quick access to account management
- Profile picture support

## ?? Email Configuration

The system includes email functionality for:
- Password reset
- Account confirmation
- Notifications (extensible)

Configure SMTP settings in `appsettings.json` using Gmail or another provider.

### Gmail App Password Setup
1. Enable 2-Factor Authentication in Gmail
2. Go to Google Account Settings ? Security
3. Generate App Password
4. Use the app password in `SmtpPassword` field

## ?? Security Features

- **ASP.NET Core Identity**: Robust authentication and authorization
- **Password Requirements**: Configurable (currently simplified for development)
- **Anti-Forgery Tokens**: CSRF protection on all forms
- **SQL Injection Prevention**: Entity Framework parameterized queries
- **XSS Protection**: Razor encoding
- **User Secrets**: Secure configuration management

## ?? Responsive Design

The application is fully responsive and works seamlessly across:
- Desktop computers (1920px and above)
- Laptops (1366px - 1920px)
- Tablets (768px - 1366px)
- Mobile phones (320px - 768px)

## ?? Testing

### Manual Testing Checklist
- ? Test employee CRUD operations
- ? Verify attendance calculations
- ? Check leave approval workflow
- ? Test all validation rules
- ? Verify duplicate entry prevention
- ? Test overnight shift calculations
- ? Verify leave-attendance integration

### Browser Compatibility
Tested on:
- Chrome (recommended)
- Firefox
- Edge
- Safari

## ?? Future Enhancements

Potential features for future versions:
- [ ] Dashboard with charts and statistics
- [ ] Payroll processing automation
- [ ] Leave balance tracking
- [ ] Employee self-service portal
- [ ] Overtime calculation
- [ ] Tax calculation (Income tax, provident fund)
- [ ] Report generation (PDF/Excel)
- [ ] Biometric integration
- [ ] Multi-tenant support
- [ ] Role-based access control (Admin, HR, Employee)
- [ ] Audit logging
- [ ] Mobile app (iOS/Android)
- [ ] Performance review module
- [ ] Training management
- [ ] Asset management

## ?? Contributing

Contributions are welcome! Please follow these steps:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Coding Standards
- Follow C# naming conventions (PascalCase for classes, camelCase for variables)
- Write clean, maintainable code
- Add XML comments for public methods
- Add comments for complex logic
- Update documentation for new features
- Write unit tests (when test project is added)
- Follow SOLID principles

## ?? License

This project is licensed under the MIT License - see the LICENSE file for details.

## ?? Team

**DevMates Solution**

### Contributors
- Al Mamun - Lead Developer

## ?? Support

For issues, questions, or suggestions:
- Create an issue on GitHub: https://github.com/AlMamun09/Payroll-Software/issues
- Email: abd.al.mamun001@gmail.com

## ?? Acknowledgments

- AdminLTE for the beautiful dashboard template
- Bootstrap team for the responsive framework
- Select2 for enhanced select boxes
- SweetAlert2 for beautiful alerts
- DataTables for powerful grid functionality
- The ASP.NET Core team for the excellent framework
- Entity Framework Core team for the ORM
- Font Awesome for the icons

---

**Version**: 1.0.1  
**Last Updated**: January 2025

## ?? Development Notes

### Development Mode Settings
The application includes simplified password requirements for development:
```csharp
options.Password.RequireDigit = false;
options.Password.RequiredLength = 1;
options.Password.RequireNonAlphanumeric = false;
options.Password.RequireUppercase = false;
options.Password.RequireLowercase = false;
```

**?? Important**: Update these settings for production deployment to ensure security.

### Database Seeding
To add initial data:
1. Create seed data in `ApplicationDbContext.OnModelCreating()`
2. Call seeding method in `Program.cs`
3. Run the application or `dotnet ef database update`

Example:
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    // Seed Departments
    modelBuilder.Entity<Department>().HasData(
 new Department { DepartmentId = Guid.NewGuid(), DepartmentName = "IT", IsActive = true },
        new Department { DepartmentId = Guid.NewGuid(), DepartmentName = "HR", IsActive = true }
    );
}
```

### Custom JavaScript Utilities

**common-ajax.js**:
- `initSelect2()` - Initialize Select2 dropdowns
- `calculateAge()` - Calculate age from date of birth
- `submitForm()` - Handle form submission with validation
- `toastSuccess()`, `toastError()` - Show toast notifications

**crud-datatable.js**:
- Initialize DataTables with server-side processing
- Handle CRUD operations via AJAX

**crud-modal.js**:
- Handle modal-based CRUD operations
- Form validation and submission

### Troubleshooting

**Issue**: Database connection fails
- Check SQL Server is running
- Verify connection string in `appsettings.json`
- Ensure LocalDB is installed (`sqllocaldb info`)

**Issue**: Migrations fail
- Delete the database and try again
- Check for conflicting migrations
- Ensure proper EF Core tools are installed:
  ```bash
  dotnet tool install --global dotnet-ef
  ```

**Issue**: Email not sending
- Verify SMTP settings in `appsettings.json`
- Check firewall settings
- Use app-specific password for Gmail
- Enable "Less secure app access" (not recommended) or use OAuth2

**Issue**: Select2 dropdowns not working
- Check jQuery is loaded before Select2
- Verify Select2 CSS and JS files are included
- Check browser console for errors

**Issue**: SweetAlert not showing
- Verify SweetAlert2 library is included
- Check for JavaScript errors in console
- Ensure proper version compatibility

### Performance Tips

1. **Database Indexing**: Add indexes for frequently queried columns
2. **Caching**: Implement caching for dropdown data
3. **Pagination**: Use server-side pagination for large datasets
4. **Lazy Loading**: Enable lazy loading for navigation properties
5. **Async Operations**: Use async/await for I/O operations

### Security Best Practices

1. **Input Validation**: Always validate user input
2. **SQL Injection**: Use parameterized queries (EF Core does this)
3. **XSS Prevention**: Use Razor encoding
4. **CSRF Protection**: Use anti-forgery tokens
5. **HTTPS**: Always use HTTPS in production
6. **Secrets Management**: Use User Secrets for development, Azure Key Vault for production

---

Made with ?? by DevMates Solution
