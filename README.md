<div align="center">

# 🏥 ClinicMIS

**Clinic Management Information System**

*A comprehensive healthcare management solution built with modern technologies*

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?style=for-the-badge&logo=asp.net)](https://dotnet.microsoft.com/apps/aspnet)
[![Entity Framework](https://img.shields.io/badge/Entity%20Framework-8.0-512BD4?style=for-the-badge&logo=entity-framework)](https://learn.microsoft.com/en-us/ef/)
[![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-CC2927?style=for-the-badge&logo=microsoft-sql-server)](https://www.microsoft.com/sql-server)
[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)

[Features](#-features) • [Quick Start](#-quick-start) • [Documentation](#-documentation) • [Contributing](#-contributing)

</div>

---

## ✨ Features

<table>
<tr>
<td width="50%">

#### 👥 Patient Management
- Auto-generated clinic numbers
- Comprehensive medical profiles
- Advanced search & filtering
- Emergency contacts & allergies

#### 🏥 Visit Management
- Scheduling & tracking
- Status workflow automation
- Visit history & analytics

#### 💊 Prescription Management
- Digital prescriptions
- Multi-drug support
- Pharmacy integration
- Status tracking

</td>
<td width="50%">

#### 💉 Pharmacy Module
- Drug inventory management
- Dispensing workflow
- Stock alerts
- Dispensing records

#### 💰 Billing & Payments
- Invoice generation
- Multiple payment methods
- Payment tracking
- Financial reports

#### 📊 Reports & Analytics
- Real-time dashboard
- Business intelligence
- Patient analytics
- Financial insights

</td>
</tr>
</table>

---

## 🚀 Quick Start

### Prerequisites

| Requirement | Version | Download |
|------------|---------|----------|
| [![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/download) | 8.0+ | [Download](https://dotnet.microsoft.com/download) |
| [![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-CC2927?logo=microsoft-sql-server&logoColor=white)](https://www.microsoft.com/sql-server) | 2019+ | [Download](https://www.microsoft.com/sql-server) |
| [![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2022-5C2D91?logo=visual-studio&logoColor=white)](https://visualstudio.microsoft.com/) | 2022 | [Download](https://visualstudio.microsoft.com/) |

### Installation

<details>
<summary><b>📋 Step-by-Step Guide</b></summary>

#### 1️⃣ Clone Repository
```bash
git clone https://github.com/yourusername/ClinicMIS.git
cd ClinicMIS/ClinicMIS
```

#### 2️⃣ Configure Database
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=ClinicMIS;Integrated Security=True;TrustServerCertificate=True"
  }
}
```

#### 3️⃣ Restore & Run
```bash
dotnet restore
dotnet ef database update
dotnet run
```

#### 4️⃣ Access Application
- 🌐 **HTTP**: `http://localhost:5000`
- 🔒 **HTTPS**: `https://localhost:5001`
- 👤 **Default Login**: `admin@clinic.com` / `Admin@123`

</details>

---

## 🛠 Technology Stack

<div align="center">

| Category | Technology | Logo |
|----------|-----------|------|
| **Framework** | .NET 8.0 | ![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet) |
| **Web Framework** | ASP.NET Core MVC | ![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-8.0-512BD4?logo=asp.net) |
| **ORM** | Entity Framework Core | ![EF Core](https://img.shields.io/badge/EF%20Core-8.0-512BD4?logo=entity-framework) |
| **Database** | SQL Server | ![SQL Server](https://img.shields.io/badge/SQL%20Server-2019+-CC2927?logo=microsoft-sql-server) |
| **Authentication** | ASP.NET Identity | ![Identity](https://img.shields.io/badge/Identity-8.0-512BD4?logo=asp.net) |
| **Frontend** | Bootstrap 5 | ![Bootstrap](https://img.shields.io/badge/Bootstrap-5.0-7952B3?logo=bootstrap) |

</div>

---

## 📁 Project Structure

```
ClinicMIS/
├── 📂 Controllers/          # MVC Controllers (10)
│   ├── AccountController.cs
│   ├── PatientsController.cs
│   ├── VisitsController.cs
│   └── ...
├── 📂 Models/
│   ├── 📂 Entities/         # Domain Models (13)
│   └── 📂 ViewModels/        # View Models (7)
├── 📂 Services/              # Business Logic (4)
│   ├── IPatientService.cs
│   ├── PatientService.cs
│   └── ...
├── 📂 Data/
│   └── ClinicDbContext.cs    # EF Core Context
├── 📂 Views/                 # Razor Views
├── 📂 Migrations/            # Database Migrations
└── 📂 wwwroot/               # Static Files
```

---

## 🔒 Security & Authorization

### Role-Based Access Control

| Role | Permissions |
|------|------------|
| 👑 **Admin** | Full system access |
| 👨‍⚕️ **Doctor** | Prescribe, view patients, reports |
| 👩‍⚕️ **Nurse** | View patients, assist visits |
| 💊 **Pharmacist** | Dispense, view billings |
| 📋 **Receptionist** | Register patients, view billings |

### Security Features

<div align="center">

![Security](https://img.shields.io/badge/Security-Hardened-brightgreen?style=flat-square)
![Audit](https://img.shields.io/badge/Audit-Logging-blue?style=flat-square)
![CSRF](https://img.shields.io/badge/CSRF-Protected-red?style=flat-square)
![HTTPS](https://img.shields.io/badge/HTTPS-Enforced-green?style=flat-square)

</div>

- ✅ Password complexity requirements
- ✅ Account lockout protection
- ✅ Secure cookie configuration
- ✅ CSRF token validation
- ✅ Security headers enforcement
- ✅ Complete audit trail
- ✅ Soft delete functionality

---

## 📊 Database Schema

<div align="center">

![Database](https://img.shields.io/badge/Database-SQL%20Server-CC2927?style=for-the-badge&logo=microsoft-sql-server)

</div>

### Core Entities

| Entity | Description | Icon |
|--------|-------------|------|
| **Patients** | Patient demographics & medical info | 👥 |
| **Visits** | Patient appointments & visits | 🏥 |
| **Prescriptions** | Doctor prescriptions | 💊 |
| **Drugs** | Pharmacy drug catalog | 💉 |
| **Billings** | Financial transactions | 💰 |
| **Staff** | Healthcare staff members | 👨‍⚕️ |
| **Clinics** | Medical departments | 🏢 |
| **AuditLogs** | System audit trail | 📝 |

---

## 📖 Documentation

<details>
<summary><b>📚 Detailed Documentation</b></summary>

### Configuration

**Default Admin Account:**
- Email: `admin@clinic.com`
- Password: `Admin@123`
- ⚠️ **Change immediately after first login!**

**Application Settings:**
```json
{
  "AppSettings": {
    "ClinicName": "University Clinic",
    "LowStockThresholdDays": 30,
    "SessionTimeoutMinutes": 30
  }
}
```

### Pre-seeded Data

The system includes 6 pre-configured clinics:
- 🫀 Cardiology
- 🦠 Oncology
- 🧠 Neurology
- 🦴 Orthopedics
- 👶 Pediatrics
- 🏥 General Medicine

</details>

---

## 🤝 Contributing

<div align="center">

[![Contributions Welcome](https://img.shields.io/badge/Contributions-Welcome-brightgreen.svg?style=flat-square)](CONTRIBUTING.md)
[![PRs Welcome](https://img.shields.io/badge/PRs-Welcome-brightgreen.svg?style=flat-square)](http://makeapullrequest.com)

</div>

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details.

### Quick Contribution Steps

1. 🍴 Fork the repository
2. 🌿 Create feature branch (`git checkout -b feature/AmazingFeature`)
3. 💾 Commit changes (`git commit -m 'Add AmazingFeature'`)
4. 📤 Push to branch (`git push origin feature/AmazingFeature`)
5. 🔀 Open Pull Request

---

## 📝 License

<div align="center">

[![License](https://img.shields.io/badge/License-MIT-green.svg?style=for-the-badge)](LICENSE)

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

</div>

---

## 👥 Developed By

<div align="center">

### 🚀 Ogo Technology

**Professional Software Development & Solutions**

[![Website](https://img.shields.io/badge/Website-www.ogotechnology.net-0066CC?style=for-the-badge&logo=internet-explorer)](https://www.ogotechnology.net)
[![Email](https://img.shields.io/badge/Email-info@ogotechnology.net-D14836?style=for-the-badge&logo=gmail)](mailto:info@ogotechnology.net)

**Made with ❤️ for healthcare professionals**

</div>

---

<div align="center">

### 📧 Support & Contact

For support, inquiries, or custom development services:

**Ogo Technology**  
🌐 [www.ogotechnology.net](https://www.ogotechnology.net)  
📧 [info@ogotechnology.net](mailto:info@ogotechnology.net)

---

### ⭐ Star this repo if you find it helpful!

[![GitHub stars](https://img.shields.io/github/stars/yourusername/ClinicMIS.svg?style=social&label=Star)](https://github.com/yourusername/ClinicMIS)
[![GitHub forks](https://img.shields.io/github/forks/yourusername/ClinicMIS.svg?style=social&label=Fork)](https://github.com/yourusername/ClinicMIS/fork)

</div>
