# Chuyển đổi IeltsSpeakingAI (MVC) → SpeakingBoost (RESTful API)

## Tổng quan

Dự án MVC **IeltsSpeakingAI** hiện tại sử dụng kiến trúc MVC truyền thống (Controller → View → Session/Cookie Auth). Mục tiêu là chuyển toàn bộ logic sang dự án mới **SpeakingBoost** dưới dạng **RESTful API** thuần (JSON in/out, JWT Auth, Swagger docs).

## Phân tích dự án MVC hiện tại

### Database Schema (10 bảng - giữ nguyên)

| Entity | Mô tả |
|---|---|
| `User` | Người dùng (Student / Teacher / SuperAdmin) |
| `SchoolClass` | Lớp học |
| `StudentClass` | Bảng trung gian User ↔ Class |
| `Exercise` | Câu hỏi speaking (Part1/2/3) |
| `ClassExercise` | Gán bài tập cho lớp + Deadline |
| `Submission` | Bài nộp (audio) của sinh viên |
| `Score` | Điểm AI chấm (4 tiêu chí + Overall + AI Feedback JSON) |
| `VocabularyTopic` | Chủ đề từ vựng |
| `Vocabulary` | Từ vựng |
| `Notification` | Thông báo |

### Module MVC → API Mapping

| Area/Controller MVC | Chức năng | API Controller mới |
|---|---|---|
| `LoginController` | Đăng nhập/Đăng xuất/Google Login/Quên MK | `AuthController` |
| `Admin/DashboardController` | Dashboard thống kê | `Admin/DashboardController` |
| `Admin/UserManagementController` | CRUD User | `Admin/UsersController` |
| `Admin/StudentManagementController` | CRUD Lớp + Gán SV + Gán Bài + Thống kê | `Admin/ClassesController` |
| `Admin/TestManagementController` | CRUD Topic + CRUD Exercise + Import Excel | `Admin/TopicsController` + `Admin/ExercisesController` |
| `Admin/DeadlineController` | Gán Deadline hàng loạt + Quản lý | `Admin/DeadlinesController` |
| `Admin/VocabularyAdminController` | CRUD Vocabulary + Import Excel | `Admin/VocabularyController` |
| `Student/DashboardController` | Dashboard sinh viên | `Student/DashboardController` |
| `Student/PracticeSpeakingController` | Luyện tập + Submit Audio | `Student/PracticeController` |
| `Student/SpeakingReviewController` | Xem lịch sử + Chi tiết attempt | `Student/SubmissionsController` |
| `Student/VocabularyController` | Xem từ vựng | `Student/VocabularyController` |

### Services (tái sử dụng trực tiếp)

| Service | Mô tả | Thay đổi |
|---|---|---|
| `LoginServices` | Login, HashPassword, GetUserByEmail | ✅ Giữ nguyên |
| `EmailService` | Gửi email (MailKit) | ✅ Giữ nguyên |
| `AnalyzeOrchestratorService` | Điều phối AI grading | ✅ Giữ nguyên |
| `TranscriptService` | Gọi API Azure Speech-to-Text | ✅ Giữ nguyên |
| `EvaluateService` | Gọi OpenAI để chấm điểm | ✅ Giữ nguyên |
| `SpeechAnalyzeServiceHybrid` | Phân tích phát âm | ✅ Giữ nguyên |
| `WebmToWavService` | Chuyển audio format | ✅ Giữ nguyên |
| `SubmissionHandleService` | Xử lý submission | ✅ Giữ nguyên |
| `BackgroundQueue` + `GradingBackgroundService` | Hàng đợi chấm bài | ✅ Giữ nguyên |
| `DeadlineReminderBackgroundService` | Nhắc deadline tự động | ✅ Giữ nguyên |

---

## Thay đổi kiến trúc quan trọng

### 1. Authentication: Session/Cookie → JWT Token

| MVC (cũ) | RESTful API (mới) |
|---|---|
| `HttpContext.SignInAsync()` + Cookie | JWT Bearer Token |
| `User.FindFirst("StudentId")` | Đọc claims từ JWT |
| `[ValidateAntiForgeryToken]` | Không cần (stateless) |
| Session lưu user info | Token chứa claims |
| Google OAuth redirect | Google OAuth → đổi lấy JWT |

### 2. Response format: View → JSON

| MVC (cũ) | RESTful API (mới) |
|---|---|
| `return View(model)` | `return Ok(new { data = ... })` |
| `return RedirectToAction(...)` | `return Ok/Created/NoContent` |
| `TempData["SuccessMessage"]` | JSON response message |
| `ViewBag`, `ViewData` | Không cần |
| `ModelState` validation errors | `return BadRequest(new { errors = ... })` |

### 3. Routing: Convention-based → Attribute-based

```
MVC:     /Admin/UserManagement/Index
API:     GET  /api/admin/users

MVC:     /Admin/UserManagement/CreateUser  [POST]
API:     POST /api/admin/users

MVC:     /Admin/UserManagement/Edit/5
API:     PUT  /api/admin/users/5

MVC:     /Student/PracticeSpeaking/Analyze  [POST]  
API:     POST /api/student/submissions
```

---

## Cấu trúc thư mục mới cho SpeakingBoost

```
SpeakingBoost/
├── Controllers/
│   ├── AuthController.cs                    ← Login, Register, ForgotPassword, GoogleAuth
│   ├── Admin/
│   │   ├── DashboardController.cs           ← GET /api/admin/dashboard
│   │   ├── UsersController.cs               ← CRUD /api/admin/users
│   │   ├── ClassesController.cs             ← CRUD /api/admin/classes
│   │   ├── TopicsController.cs              ← CRUD /api/admin/topics
│   │   ├── ExercisesController.cs           ← CRUD /api/admin/exercises
│   │   ├── DeadlinesController.cs           ← /api/admin/deadlines
│   │   └── VocabularyController.cs          ← CRUD /api/admin/vocabulary
│   └── Student/
│       ├── DashboardController.cs           ← GET /api/student/dashboard
│       ├── PracticeController.cs            ← Luyện tập + Submit
│       ├── SubmissionsController.cs         ← Xem history + detail
│       ├── DeadlineController.cs            ← Xem deadline SV
│       └── VocabularyController.cs          ← Xem từ vựng
│
├── Models/
│   ├── EF/
│   │   └── ApplicationDbContext.cs          ← Giữ nguyên từ MVC
│   ├── Entities/                            ← Copy nguyên từ MVC (10 entity classes)
│   │   ├── User.cs
│   │   ├── Exercise.cs
│   │   ├── Submission.cs
│   │   ├── Score.cs
│   │   ├── SchoolClass.cs
│   │   ├── StudentClass.cs
│   │   ├── ClassExercise.cs
│   │   ├── Notification.cs
│   │   ├── Vocabulary.cs
│   │   └── VocabularyTopic.cs
│   └── DTOs/                               ← MỚI: Data Transfer Objects
│       ├── Auth/
│       │   ├── LoginRequest.cs              ← { email, password }
│       │   ├── LoginResponse.cs             ← { token, user info }
│       │   ├── RegisterRequest.cs
│       │   └── ForgotPasswordRequest.cs
│       ├── Admin/
│       │   ├── UserDto.cs / CreateUserDto.cs / UpdateUserDto.cs
│       │   ├── ClassDto.cs / CreateClassDto.cs
│       │   ├── TopicDto.cs / CreateTopicDto.cs
│       │   ├── ExerciseDto.cs / CreateExerciseDto.cs
│       │   ├── DeadlineDto.cs / AssignDeadlineDto.cs
│       │   ├── VocabularyDto.cs
│       │   └── DashboardDto.cs
│       └── Student/
│           ├── PracticeTopicDto.cs
│           ├── SubmissionDto.cs
│           ├── AttemptHistoryDto.cs
│           ├── AttemptDetailDto.cs
│           └── StudentDashboardDto.cs
│
├── Services/                                ← Copy từ MVC + thêm mới
│   ├── ILoginServices.cs                    ← Giữ nguyên
│   ├── LoginServices.cs                     ← Giữ nguyên
│   ├── IJwtService.cs                       ← MỚI
│   ├── JwtService.cs                        ← MỚI: Generate/Validate JWT
│   ├── Email/
│   │   ├── IEmailService.cs                 ← Giữ nguyên
│   │   └── EmailService.cs                  ← Giữ nguyên
│   └── Speaking/                            ← Copy từ MVC Areas/Student/Models/Service
│       ├── AnalyzeOrchestratorService.cs
│       ├── TranscriptService.cs
│       ├── EvaluateService.cs
│       ├── SpeechAnalyzeServiceHybrid.cs
│       ├── WebmToWavService.cs
│       ├── SubmissionHandleService.cs
│       ├── BackgroundQueue.cs
│       └── GradingBackgroundService.cs
│
├── Helpers/
│   └── ApiResponse.cs                       ← MỚI: Chuẩn hóa response format
│
├── Middleware/                               ← MỚI
│   └── ExceptionMiddleware.cs               ← Global error handling
│
├── Program.cs                               ← Cấu hình JWT + Swagger + DI
├── appsettings.json
└── wwwroot/                                 ← Static files (audio uploads)
```

---

## Kế hoạch triển khai theo Phase

### Phase 1: Foundation (Nền tảng)
> Ưu tiên cao nhất - không có cái này thì không làm được gì

- [x] ~~Entities + DbContext~~ (đã copy sang)
- [ ] **Đổi namespace** từ `IeltsSpeakingAI` → `SpeakingBoost` cho tất cả files
- [ ] Cấu hình `Program.cs`:
  - Thêm EF Core DbContext (mở khóa dòng comment)
  - Thêm JWT Authentication
  - Thêm CORS (cho frontend gọi API)
  - Thêm Swagger (đã có)
- [ ] Tạo `ApiResponse<T>` helper (chuẩn hóa JSON response)
- [ ] Tạo `JwtService` (generate + validate token)
- [ ] Tạo `AuthController` (Login, ForgotPassword)
- [ ] Thêm NuGet packages: `Microsoft.AspNetCore.Authentication.JwtBearer`

### Phase 2: Admin APIs
> CRUD cơ bản cho phía giáo viên

- [ ] `Admin/UsersController` — CRUD Users
  - `GET /api/admin/users` — Danh sách users
  - `POST /api/admin/users` — Tạo user mới
  - `PUT /api/admin/users/{id}` — Sửa user
  - `DELETE /api/admin/users/{id}` — Xóa user
- [ ] `Admin/ClassesController` — CRUD Lớp + Gán SV
  - `GET /api/admin/classes` — Danh sách lớp
  - `POST /api/admin/classes` — Tạo lớp
  - `GET /api/admin/classes/{id}` — Chi tiết lớp (SV + Bài tập)
  - `PUT /api/admin/classes/{id}` — Sửa lớp
  - `DELETE /api/admin/classes/{id}` — Xóa lớp
  - `POST /api/admin/classes/{id}/students` — Thêm SV vào lớp
  - `DELETE /api/admin/classes/{classId}/students/{studentClassId}` — Xóa SV khỏi lớp
- [ ] `Admin/TopicsController` — CRUD Chủ đề
  - `GET /api/admin/topics` — Danh sách topics
  - `POST /api/admin/topics` — Tạo topic
  - `GET /api/admin/topics/{id}` — Chi tiết topic + exercises
  - `DELETE /api/admin/topics/{id}` — Xóa topic
- [ ] `Admin/ExercisesController` — CRUD Câu hỏi
  - `POST /api/admin/exercises` — Tạo exercise
  - `PUT /api/admin/exercises/{id}` — Sửa exercise
  - `DELETE /api/admin/exercises/{id}` — Xóa exercise
  - `POST /api/admin/exercises/import` — Import từ Excel
- [ ] `Admin/DeadlinesController` — Quản lý Deadline
  - `GET /api/admin/deadlines` — Danh sách deadlines đang chạy
  - `POST /api/admin/deadlines` — Gán deadline hàng loạt
  - `PUT /api/admin/deadlines/{id}` — Cập nhật deadline
  - `DELETE /api/admin/deadlines/{id}` — Xóa deadline
- [ ] `Admin/VocabularyController` — CRUD Từ vựng
  - `GET /api/admin/vocabulary?topicId=1` — Danh sách từ
  - `POST /api/admin/vocabulary` — Thêm từ
  - `DELETE /api/admin/vocabulary/{id}` — Xóa từ
  - `POST /api/admin/vocabulary/import` — Import từ Excel
- [ ] `Admin/DashboardController` — Dashboard
  - `GET /api/admin/dashboard?classId=1` — Thống kê lớp

### Phase 3: Student APIs
> Phần dành cho sinh viên

- [ ] `Student/DashboardController` — Dashboard SV
  - `GET /api/student/dashboard` — Thông tin tổng quan
- [ ] `Student/PracticeController` — Luyện tập
  - `GET /api/student/practice/topics?part=1` — Danh sách topics
  - `GET /api/student/practice/topics/{id}?part=1` — Chi tiết topic (câu hỏi)
  - `POST /api/student/submissions` — Submit audio (multipart/form-data)
  - `GET /api/student/submissions/{id}/status` — Kiểm tra trạng thái chấm
- [ ] `Student/SubmissionsController` — Lịch sử
  - `GET /api/student/submissions?exerciseId=1` — Lịch sử nộp bài
  - `GET /api/student/submissions/{id}` — Chi tiết attempt (điểm + feedback)
- [ ] `Student/VocabularyController` — Từ vựng
  - `GET /api/student/vocabulary/topics` — Danh sách topics
  - `GET /api/student/vocabulary/topics/{id}` — Từ vựng theo topic

### Phase 4: Speaking AI Services
> Copy và tích hợp các service AI từ MVC

- [ ] Copy toàn bộ thư mục `Speaking Services` sang `Services/Speaking/`
- [ ] Copy `BackgroundQueue` + `GradingBackgroundService`
- [ ] Copy `DeadlineReminderBackgroundService`
- [ ] Đăng ký tất cả services trong `Program.cs`
- [ ] Cài thêm NuGet nếu cần (HttpClient, Azure SDK, v.v.)

### Phase 5: Polish
> Hoàn thiện

- [ ] Global Exception Middleware
- [ ] Thêm `[Authorize]` roles cho từng endpoint
- [ ] Validation bằng Data Annotations trên DTOs
- [ ] CORS policy cho frontend
- [ ] Swagger documentation (XML comments)

---

## Chi tiết kỹ thuật từng phần

### 1. JWT Authentication (thay Cookie Auth)

**appsettings.json** — thêm section:
```json
"Jwt": {
  "Key": "YourSuperSecretKey32CharactersLong!!",
  "Issuer": "SpeakingBoost",
  "Audience": "SpeakingBoostClient",
  "ExpireMinutes": 180
}
```

**JwtService.cs** — tạo token chứa claims:
```csharp
// Claims tương đương MVC:
// ClaimTypes.Name      → user.FullName
// ClaimTypes.Email     → user.Email
// "StudentId"          → user.UserId.ToString()
// ClaimTypes.Role      → user.Role
```

**AuthController** flow:
```
POST /api/auth/login
Body: { "email": "...", "password": "..." }
Response: { "token": "eyJ...", "user": { "userId": 1, "fullName": "...", "role": "Student" } }
```

### 2. Chuẩn hóa API Response

```csharp
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }
}

// Sử dụng:
return Ok(ApiResponse<UserDto>.SuccessResponse(userData, "Lấy thông tin thành công"));
return BadRequest(ApiResponse<object>.ErrorResponse("Email đã tồn tại"));
```

### 3. Ví dụ chuyển MVC Controller → API Controller

**MVC (cũ):**
```csharp
[Area("Admin")]
public class UserManagementController : Controller
{
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(UserManagementIndexViewModel model)
    {
        // ... validation ...
        _context.Add(user);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Tạo người dùng thành công!";
        return RedirectToAction(nameof(Index));
    }
}
```

**API (mới):**
```csharp
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "SuperAdmin")]
public class UsersController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        // ... validation ...
        _context.Add(user);
        await _context.SaveChangesAsync();
        return Created($"/api/admin/users/{user.UserId}", 
            ApiResponse<UserDto>.SuccessResponse(userDto, "Tạo người dùng thành công!"));
    }
}
```

### 4. Quy tắc chuyển đổi tổng quát

| Yếu tố MVC | Chuyển sang API |
|---|---|
| `Controller` base class | `ControllerBase` base class |
| `[Area("Admin")]` | `[Route("api/admin/...")]` |
| `[ValidateAntiForgeryToken]` | Bỏ (JWT stateless) |
| `return View(model)` | `return Ok(data)` |
| `return RedirectToAction(...)` | `return Ok / Created / NoContent` |
| `TempData[...]` | JSON response message |
| `ViewBag`, `ViewData` | Không cần |
| `ModelState.AddModelError(...)` | `return BadRequest(errors)` |
| `User.FindFirst("StudentId")` | `User.FindFirst("StudentId")` (giữ nguyên, JWT vẫn có Claims) |
| `_context.XXX` trực tiếp | Giữ nguyên hoặc tách ra Repository (tùy chọn) |
| `IFormFile` upload | Giữ nguyên (`[FromForm]`) |

---

## Open Questions

> [!IMPORTANT]
> **1. Namespace**: Bạn muốn đổi namespace toàn bộ từ `IeltsSpeakingAI` → `SpeakingBoost` không? Hiện tại DbContext và Entities đang dùng namespace `IeltsSpeakingAI`.

> [!IMPORTANT]
> **2. Database**: Bạn muốn dùng chung database `IeltsSpeakingAI` (của dự án MVC cũ) hay tạo database mới riêng cho project môn học?

> [!IMPORTANT]
> **3. Google Login**: Dự án mới có cần Google OAuth không? (Nếu có, flow sẽ khác — client gửi Google Token → API verify → trả JWT).

> [!IMPORTANT]
> **4. Triển khai theo thứ tự**: Bạn muốn tôi bắt đầu code từ Phase nào trước? Tôi đề xuất Phase 1 (Foundation) → Phase 2 (Admin APIs) → Phase 3 (Student APIs) → Phase 4 (AI Services).

> [!NOTE]
> **5. Frontend**: RESTful API chỉ trả JSON. Bạn sẽ dùng frontend gì để gọi API? (React, Vue, Angular, hay HTML/JS thuần trong wwwroot?)

---

## Verification Plan

### Automated Tests
- Chạy `dotnet build` sau mỗi phase để đảm bảo compile thành công
- Test API endpoints qua Swagger UI (`/swagger`)
- Test JWT authentication flow

### Manual Verification
- Gọi thử từng endpoint qua Swagger hoặc Postman
- Kiểm tra database sau khi gọi CRUD APIs
- Test upload audio file qua multipart/form-data
