\# SpeakingBoost - AI-Powered IELTS Speaking Practice Platform



\*\*SpeakingBoost\*\* is a comprehensive web-based platform designed to assist students in practicing and improving their IELTS Speaking skills using Artificial Intelligence. This project is developed as a final requirement for the \*\*CSW 306 (Backend Programming)\*\* course.



\## 📖 Course Information

\- \*\*Course Name:\*\* Backend Programming

\- \*\*Course ID:\*\* CSW 306

\- \*\*Project Name:\*\* SpeakingBoost

\- \*\*Academic Year:\*\* 2025 - 2026



\## 👥 Team Members



1\. \*\*Au Duong Tai\*\* 

2\. \*\*Huynh Khanh Duy\*\* 

3\. \*\*Hoang Quoc Viet\*\* 

4\. \*\*Duong Tan Phat\*\* 

5\. \*\*Tran Manh Tuan\*\* 



\---



\## 🚀 Project Overview

SpeakingBoost utilizes a decoupled architecture, separating the \*\*Backend API\*\* (built with .NET) and the \*\*Frontend\*\* (static files hosted in `wwwroot`). The system focus is on robust backend logic, secure authentication, and efficient database management.



\### Key Features:

\- \*\*RESTful API Architecture:\*\* Professional endpoint management using ASP.NET Core Web API.

\- \*\*Authentication \& Authorization:\*\* Secure login system featuring JWT (JSON Web Tokens) and Role-based access control (Admin, Teacher, Student).

\- \*\*AI Scoring Integration:\*\* (Simulation/Integration) Logic to handle and store AI-driven speaking evaluations.

\- \*\*User Management:\*\* Complete CRUD operations for managing students and teachers.

\- \*\*Exercise \& Submission Tracking:\*\* Backend logic to manage IELTS topics, exercises, and student submissions.

\- \*\*Email Service:\*\* Integrated MailKit for automated password recovery and notifications.



\## 🛠️ Technology Stack



\### Backend:

\- \*\*Framework:\*\* .NET 8.0 

\- \*\*Database:\*\* Microsoft SQL Server

\- \*\*ORM:\*\* Entity Framework Core (Code First Approach)

\- \*\*Security:\*\* JWT Authentication, Password Hashing

\- \*\*Email:\*\* MailKit \& MimeKit



\### Frontend:

\- \*\*Framework:\*\* HTML5, CSS3, JavaScript (ES6+)

\- \*\*UI Library:\*\* Bootstrap 5

\- \*\*Components:\*\* SweetAlert2, jQuery, FlatIcons



\---



\## 📂 Project Structure

```text

SpeakingBoost/

├── Controllers/         # API Endpoints (Login, User, Exercise, etc.)

├── Models/              # Database Entities \& Data Transfer Objects (DTOs)

├── Services/            # Business Logic \& Infrastructure (Auth, Email)

├── wwwroot/             # Frontend Static Files (HTML, JS, CSS)

├── Program.cs           # Main Application Entry \& Middleware Configuration

└── appsettings.json     # Configuration (Database connection, JWT keys)

