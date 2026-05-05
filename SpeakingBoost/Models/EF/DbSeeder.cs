using SpeakingBoost.Models.Entities;
using System.Security.Cryptography;
using System.Text;

namespace SpeakingBoost.Models.EF
{
    /// <summary>
    /// DbSeeder — tự động tạo dữ liệu mẫu nếu chưa tồn tại trong DB.
    /// Chỉ chạy khi môi trường là Development.
    ///
    /// Roles: admin | user
    /// Tài khoản seed:
    ///   admin@speakingboost.com / Admin@123  (role: admin) — CHỈ 1 ADMIN
    ///   user1@speakingboost.com / User@123   (role: user)
    ///   ...
    ///   user5@speakingboost.com / User@123   (role: user)
    /// </summary>
    public static class DbSeeder
    {
        public static void Seed(ApplicationDbContext db, ILogger logger)
        {
            try
            {
                // ── Guard: chỉ seed nếu chưa có data ──────────────────
                if (db.Users.Any())
                {
                    logger.LogInformation("[DbSeeder] Database đã có dữ liệu, bỏ qua seed.");
                    return;
                }

                logger.LogInformation("[DbSeeder] Bắt đầu seed dữ liệu mẫu...");

                // ══════════════════════════════════════════════════════
                // 1. USERS — 1 admin + 5 users
                // ══════════════════════════════════════════════════════
                var admin = new User
                {
                    FullName     = "Quản Trị Viên",
                    Email        = "admin@example.com",
                    Role         = "admin",
                    PasswordHash = Hash("123456")
                };

                var users = new[]
                {
                    new User { FullName = "Nguyễn Văn An",    Email = "user1@example.com", Role = "user", PasswordHash = Hash("123456") },
                    new User { FullName = "Trần Thị Bình",    Email = "user2@example.com", Role = "user", PasswordHash = Hash("123456") },
                    new User { FullName = "Lê Hoàng Cường",   Email = "user3@example.com", Role = "user", PasswordHash = Hash("123456") },
                    new User { FullName = "Phạm Minh Dũng",   Email = "user4@example.com", Role = "user", PasswordHash = Hash("123456") },
                    new User { FullName = "Hoàng Thị Lan",    Email = "user5@example.com", Role = "user", PasswordHash = Hash("123456") },
                };

                db.Users.Add(admin);
                db.Users.AddRange(users);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 1 admin + 5 users.");

                // ══════════════════════════════════════════════════════
                // 2. VOCABULARY TOPICS — 3 chủ đề IELTS Speaking
                // ══════════════════════════════════════════════════════
                var topicWork = new VocabularyTopic
                {
                    Name        = "Work & Career",
                    Description = "Các câu hỏi về công việc, nghề nghiệp và môi trường làm việc"
                };
                var topicEnvironment = new VocabularyTopic
                {
                    Name        = "Environment & Nature",
                    Description = "Chủ đề môi trường, thiên nhiên và biến đổi khí hậu"
                };
                var topicTechnology = new VocabularyTopic
                {
                    Name        = "Technology & Innovation",
                    Description = "Công nghệ, internet, mạng xã hội và đổi mới sáng tạo"
                };

                db.VocabularyTopics.AddRange(topicWork, topicEnvironment, topicTechnology);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 3 VocabularyTopics.");

                // ══════════════════════════════════════════════════════
                // 3. EXERCISES — 10 câu hỏi chia cho 3 topic
                // ══════════════════════════════════════════════════════
                var exercises = new[]
                {
                    // Work & Career — 4 câu
                    new Exercise
                    {
                        Title       = "Describe Your Job",
                        Type        = "Part1",
                        Question    = "What do you do for a living? Do you enjoy your work?",
                        SampleAnswer = "I work as a software developer at a tech company. I really enjoy my job because it allows me to solve complex problems and create products that help people.",
                        TopicId     = topicWork.TopicId,
                        MaxAttempts = 3
                    },
                    new Exercise
                    {
                        Title       = "Ideal Workplace",
                        Type        = "Part1",
                        Question    = "What kind of working environment do you prefer? Why?",
                        SampleAnswer = "I prefer a flexible working environment where I can work both from home and the office. This balance helps me stay productive while maintaining work-life balance.",
                        TopicId     = topicWork.TopicId,
                        MaxAttempts = 3
                    },
                    new Exercise
                    {
                        Title       = "Career Ambition",
                        Type        = "Part2",
                        Question    = "Describe a job you would like to do in the future. You should say: what it is, what skills are required, why you want this job, and how you plan to achieve it.",
                        SampleAnswer = "I would love to become an entrepreneur in the future. This role requires creativity, leadership, and financial management skills. I want this job because it would give me the freedom to pursue my own ideas.",
                        TopicId     = topicWork.TopicId,
                        MaxAttempts = 2
                    },
                    new Exercise
                    {
                        Title       = "Work-Life Balance",
                        Type        = "Part3",
                        Question    = "Do you think it is important to have a good work-life balance? What can companies do to support employees?",
                        SampleAnswer = "Work-life balance is crucial for both employee wellbeing and productivity. Companies can support this by offering flexible hours, remote work options, and mental health programs.",
                        TopicId     = topicWork.TopicId,
                        MaxAttempts = 3
                    },

                    // Environment — 3 câu
                    new Exercise
                    {
                        Title       = "Environmental Problems",
                        Type        = "Part1",
                        Question    = "What environmental problems are common in your country?",
                        SampleAnswer = "In my country, air pollution and plastic waste are major environmental issues, especially in large cities. Traffic congestion and industrial emissions contribute significantly to poor air quality.",
                        TopicId     = topicEnvironment.TopicId,
                        MaxAttempts = 3
                    },
                    new Exercise
                    {
                        Title       = "Climate Change Speech",
                        Type        = "Part2",
                        Question    = "Describe a time when you did something to help the environment. You should say: what you did, when and where you did it, and how it helped.",
                        SampleAnswer = "Last year, I participated in a local beach cleanup campaign. I spent a Saturday morning with about 50 volunteers collecting plastic waste along a coastal area. We gathered over 100 kilograms of trash.",
                        TopicId     = topicEnvironment.TopicId,
                        MaxAttempts = 2
                    },
                    new Exercise
                    {
                        Title       = "Government & Environment",
                        Type        = "Part3",
                        Question    = "What should governments do to protect the environment? Do you think individuals can make a significant difference?",
                        SampleAnswer = "Governments should enforce stricter environmental regulations, invest in renewable energy, and raise public awareness. While individual actions matter, systemic change at government and corporate levels is more impactful.",
                        TopicId     = topicEnvironment.TopicId,
                        MaxAttempts = 3
                    },

                    // Technology — 3 câu
                    new Exercise
                    {
                        Title       = "Daily Technology Use",
                        Type        = "Part1",
                        Question    = "What technology do you use every day? How has it changed your life?",
                        SampleAnswer = "I use my smartphone every day for communication, navigation, and accessing information. It has greatly improved my efficiency and keeps me connected with friends and colleagues around the world.",
                        TopicId     = topicTechnology.TopicId,
                        MaxAttempts = 3
                    },
                    new Exercise
                    {
                        Title       = "Favorite App or Website",
                        Type        = "Part2",
                        Question    = "Describe a website or app you find very useful. You should say: what it is, how you use it, why you like it, and recommend it to others.",
                        SampleAnswer = "I would like to talk about Duolingo, a language learning app. I use it every morning for about 15 minutes to practice my English. I appreciate its gamified approach which makes learning enjoyable and consistent.",
                        TopicId     = topicTechnology.TopicId,
                        MaxAttempts = 2
                    },
                    new Exercise
                    {
                        Title       = "Technology & Society",
                        Type        = "Part3",
                        Question    = "Has technology made people more or less social? What are the positive and negative effects of social media on communication?",
                        SampleAnswer = "Technology has a dual effect on social interaction. While platforms like social media allow people to connect globally, they can also reduce face-to-face interactions and create superficial relationships. The key is maintaining a healthy balance.",
                        TopicId     = topicTechnology.TopicId,
                        MaxAttempts = 3
                    },
                };

                db.Exercises.AddRange(exercises);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 10 Exercises.");

                // ══════════════════════════════════════════════════════
                // 4. CLASSES — 3 lớp học
                // ══════════════════════════════════════════════════════
                var classA = new SchoolClass { ClassName = "IELTS Beginner A" };
                var classB = new SchoolClass { ClassName = "IELTS Intermediate B" };
                var classC = new SchoolClass { ClassName = "IELTS Advanced C" };

                db.Classes.AddRange(classA, classB, classC);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 3 Classes.");

                // ══════════════════════════════════════════════════════
                // 5. STUDENT CLASSES — phân học viên vào lớp
                // ══════════════════════════════════════════════════════
                // Lớp A: user1, user2
                // Lớp B: user2, user3, user4
                // Lớp C: user4, user5
                var studentClasses = new[]
                {
                    new StudentClass { StudentId = users[0].UserId, ClassId = classA.ClassId },
                    new StudentClass { StudentId = users[1].UserId, ClassId = classA.ClassId },
                    new StudentClass { StudentId = users[1].UserId, ClassId = classB.ClassId },
                    new StudentClass { StudentId = users[2].UserId, ClassId = classB.ClassId },
                    new StudentClass { StudentId = users[3].UserId, ClassId = classB.ClassId },
                    new StudentClass { StudentId = users[3].UserId, ClassId = classC.ClassId },
                    new StudentClass { StudentId = users[4].UserId, ClassId = classC.ClassId },
                };

                db.StudentClasses.AddRange(studentClasses);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã phân học viên vào lớp.");

                // ══════════════════════════════════════════════════════
                // 6. CLASS EXERCISES (Deadlines theo topic)
                //    Lớp A → Topic Work (4 câu), deadline 30 ngày tới
                //    Lớp B → Topic Environment (3 câu), deadline 14 ngày tới
                //    Lớp C → Topic Technology (3 câu), deadline 21 ngày tới
                // ══════════════════════════════════════════════════════
                var now = DateTime.Now;

                // Lấy exercises theo topic
                var workExercises  = exercises.Where(e => e.TopicId == topicWork.TopicId).ToList();
                var envExercises   = exercises.Where(e => e.TopicId == topicEnvironment.TopicId).ToList();
                var techExercises  = exercises.Where(e => e.TopicId == topicTechnology.TopicId).ToList();

                var classExercises = new List<ClassExercise>();

                // Lớp A ← Work topic
                foreach (var ex in workExercises)
                    classExercises.Add(new ClassExercise { ClassId = classA.ClassId, ExerciseId = ex.ExerciseId, Deadline = now.AddDays(30) });

                // Lớp B ← Environment topic
                foreach (var ex in envExercises)
                    classExercises.Add(new ClassExercise { ClassId = classB.ClassId, ExerciseId = ex.ExerciseId, Deadline = now.AddDays(14) });

                // Lớp C ← Technology topic
                foreach (var ex in techExercises)
                    classExercises.Add(new ClassExercise { ClassId = classC.ClassId, ExerciseId = ex.ExerciseId, Deadline = now.AddDays(21) });

                // Thêm 1 deadline quá hạn cho Lớp A (để test trạng thái overdue)
                classExercises.Add(new ClassExercise { ClassId = classA.ClassId, ExerciseId = envExercises[0].ExerciseId, Deadline = now.AddDays(-7) });

                db.ClassExercises.AddRange(classExercises);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã tạo {Count} ClassExercises (deadlines theo topic).", classExercises.Count);

                // ══════════════════════════════════════════════════════
                // 7. SUBMISSIONS + SCORES — user1 và user2 đã nộp bài
                // ══════════════════════════════════════════════════════
                var user1 = users[0]; // user1 — lớp A
                var user2 = users[1]; // user2 — lớp A + B

                var sub1 = new Submission
                {
                    StudentId     = user1.UserId,
                    ExerciseId    = workExercises[0].ExerciseId,
                    ClassExerciseId = classExercises.First(ce => ce.ClassId == classA.ClassId && ce.ExerciseId == workExercises[0].ExerciseId).ClassExerciseId,
                    AudioPath     = "/uploads/audio/sub1.webm",
                    Transcript    = "I work as a software developer. I really enjoy my job because I get to solve interesting problems every day and learn new technologies constantly.",
                    AttemptNumber = 1,
                    Status        = ProcessingStatus.Completed,
                    CreatedAt     = now.AddDays(-5)
                };

                var sub2 = new Submission
                {
                    StudentId     = user1.UserId,
                    ExerciseId    = workExercises[1].ExerciseId,
                    ClassExerciseId = classExercises.First(ce => ce.ClassId == classA.ClassId && ce.ExerciseId == workExercises[1].ExerciseId).ClassExerciseId,
                    AudioPath     = "/uploads/audio/sub2.webm",
                    Transcript    = "I prefer a flexible working environment. Being able to work from home gives me more focus, while going to the office helps with teamwork and collaboration.",
                    AttemptNumber = 1,
                    Status        = ProcessingStatus.Completed,
                    CreatedAt     = now.AddDays(-3)
                };

                var sub3 = new Submission
                {
                    StudentId     = user2.UserId,
                    ExerciseId    = workExercises[0].ExerciseId,
                    ClassExerciseId = classExercises.First(ce => ce.ClassId == classA.ClassId && ce.ExerciseId == workExercises[0].ExerciseId).ClassExerciseId,
                    AudioPath     = "/uploads/audio/sub3.webm",
                    Transcript    = "My current job is in marketing. I enjoy it because I get to be creative and communicate with many different clients.",
                    AttemptNumber = 1,
                    Status        = ProcessingStatus.Completed,
                    CreatedAt     = now.AddDays(-4)
                };

                var sub4 = new Submission
                {
                    StudentId     = user2.UserId,
                    ExerciseId    = envExercises[0].ExerciseId,
                    ClassExerciseId = classExercises.First(ce => ce.ClassId == classB.ClassId && ce.ExerciseId == envExercises[0].ExerciseId).ClassExerciseId,
                    AudioPath     = "/uploads/audio/sub4.webm",
                    Transcript    = "In my country, air pollution is a serious issue especially in big cities. Factories and vehicles are the main contributors to this problem.",
                    AttemptNumber = 1,
                    Status        = ProcessingStatus.Completed,
                    CreatedAt     = now.AddDays(-2)
                };

                var sub5 = new Submission
                {
                    StudentId     = user1.UserId,
                    ExerciseId    = workExercises[0].ExerciseId,
                    ClassExerciseId = classExercises.First(ce => ce.ClassId == classA.ClassId && ce.ExerciseId == workExercises[0].ExerciseId).ClassExerciseId,
                    AudioPath     = "/uploads/audio/sub5.webm",
                    Transcript    = "I work as a software developer. This time I want to elaborate more on the creative aspects of my role and how it impacts users.",
                    AttemptNumber = 2,
                    Status        = ProcessingStatus.Completed,
                    CreatedAt     = now.AddDays(-1)
                };

                db.Submissions.AddRange(sub1, sub2, sub3, sub4, sub5);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 5 Submissions.");

                // ══════════════════════════════════════════════════════
                // 8. SCORES — điểm IELTS Speaking (thang 0–9)
                // ══════════════════════════════════════════════════════
                var scores = new[]
                {
                    new Score
                    {
                        SubmissionId   = sub1.SubmissionId,
                        Pronunciation  = 6.5,
                        Grammar        = 6.0,
                        LexicalResource = 6.5,
                        Coherence      = 7.0,
                        Overall        = 6.5,
                        AiFeedback     = "{\"strengths\":[\"Clear pronunciation\",\"Good use of examples\"],\"improvements\":[\"Vary sentence structure\",\"Use more advanced vocabulary\"]}",
                        CreatedAt      = now.AddDays(-5).AddMinutes(10)
                    },
                    new Score
                    {
                        SubmissionId   = sub2.SubmissionId,
                        Pronunciation  = 6.0,
                        Grammar        = 6.5,
                        LexicalResource = 6.0,
                        Coherence      = 6.5,
                        Overall        = 6.25,
                        AiFeedback     = "{\"strengths\":[\"Organized response\",\"Relevant content\"],\"improvements\":[\"Improve pronunciation of certain words\",\"Expand ideas more\"]}",
                        CreatedAt      = now.AddDays(-3).AddMinutes(10)
                    },
                    new Score
                    {
                        SubmissionId   = sub3.SubmissionId,
                        Pronunciation  = 5.5,
                        Grammar        = 5.5,
                        LexicalResource = 6.0,
                        Coherence      = 6.0,
                        Overall        = 5.75,
                        AiFeedback     = "{\"strengths\":[\"Good topic knowledge\"],\"improvements\":[\"Work on fluency\",\"Reduce grammatical errors\",\"Use connective words\"]}",
                        CreatedAt      = now.AddDays(-4).AddMinutes(10)
                    },
                    new Score
                    {
                        SubmissionId   = sub4.SubmissionId,
                        Pronunciation  = 7.0,
                        Grammar        = 6.5,
                        LexicalResource = 7.0,
                        Coherence      = 7.0,
                        Overall        = 6.875,
                        AiFeedback     = "{\"strengths\":[\"Excellent vocabulary\",\"Smooth delivery\",\"Good coherence\"],\"improvements\":[\"Minor grammar slips\",\"Add more specific examples\"]}",
                        CreatedAt      = now.AddDays(-2).AddMinutes(10)
                    },
                    new Score
                    {
                        SubmissionId   = sub5.SubmissionId,
                        Pronunciation  = 7.0,
                        Grammar        = 6.5,
                        LexicalResource = 7.0,
                        Coherence      = 7.5,
                        Overall        = 7.0,
                        AiFeedback     = "{\"strengths\":[\"Significant improvement\",\"More elaborate response\",\"Better coherence\"],\"improvements\":[\"Continue expanding vocabulary range\"]}",
                        CreatedAt      = now.AddDays(-1).AddMinutes(10)
                    },
                };

                db.Scores.AddRange(scores);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 5 Scores.");

                // ══════════════════════════════════════════════════════
                // 9. NOTIFICATIONS
                // ══════════════════════════════════════════════════════
                var notifications = new[]
                {
                    new Notification
                    {
                        UserId    = user1.UserId,
                        Message   = "Bạn có deadline mới: Work & Career - Hạn nộp trong 30 ngày.",
                        IsRead    = true,
                        CreatedAt = now.AddDays(-6)
                    },
                    new Notification
                    {
                        UserId    = user1.UserId,
                        Message   = "Bài nộp 'Describe Your Job' đã được chấm điểm. Điểm của bạn: 6.5",
                        IsRead    = false,
                        CreatedAt = now.AddDays(-5).AddMinutes(15)
                    },
                    new Notification
                    {
                        UserId    = user2.UserId,
                        Message   = "Bạn có deadline mới: Environment & Nature - Hạn nộp trong 14 ngày.",
                        IsRead    = false,
                        CreatedAt = now.AddDays(-6)
                    },
                    new Notification
                    {
                        UserId    = user2.UserId,
                        Message   = "Bài nộp 'Environmental Problems' đã được chấm điểm. Điểm của bạn: 6.875",
                        IsRead    = false,
                        CreatedAt = now.AddDays(-2).AddMinutes(15)
                    },
                };

                db.Notifications.AddRange(notifications);
                db.SaveChanges();
                logger.LogInformation("[DbSeeder] ✅ Đã thêm 4 Notifications.");

                logger.LogInformation("[DbSeeder] 🎉 Seed hoàn tất! Tóm tắt:");
                logger.LogInformation("  - Users   : 1 admin + 5 users");
                logger.LogInformation("  - Topics  : 3 VocabularyTopics");
                logger.LogInformation("  - Exercises: 10");
                logger.LogInformation("  - Classes : 3");
                logger.LogInformation("  - Deadlines: {Count} ClassExercises (theo topic)", classExercises.Count);
                logger.LogInformation("  - Submissions: 5 | Scores: 5");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DbSeeder] Lỗi khi seed dữ liệu.");
                throw;
            }
        }

        private static string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
    }
}
