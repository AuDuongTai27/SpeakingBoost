using SpeakingBoost.Models.DTOs.Admin;
using SpeakingBoost.Models.Entities;
using SpeakingBoost.Repositories.Interfaces.Admin;
using SpeakingBoost.Services.Interfaces.Admin;

namespace SpeakingBoost.Services.Implementations.Admin
{
    public class ClassService : IClassService
    {
        private readonly IClassRepository _classRepository;

        public ClassService(IClassRepository classRepository)
        {
            _classRepository = classRepository;
        }

        public async Task<List<ClassDto>> GetAllClassesAsync()
        {
            var classes = await _classRepository.GetAllClassesAsync();
            return classes.Select(c => new ClassDto
            {
                ClassId      = c.ClassId,
                ClassName    = c.ClassName,
                StudentCount = c.StudentClasses?.Count ?? 0
            }).ToList();
        }

        public async Task<ClassDto> GetClassByIdAsync(int id)
        {
            var c = await _classRepository.GetClassByIdAsync(id);
            if (c == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lớp học.");
            }

            return new ClassDto
            {
                ClassId      = c.ClassId,
                ClassName    = c.ClassName,
                StudentCount = c.StudentClasses?.Count ?? 0
            };
        }

        public async Task<ClassDto> CreateClassAsync(CreateClassDto dto)
        {
            if (await _classRepository.ClassNameExistsAsync(dto.ClassName))
            {
                throw new InvalidOperationException("Tên lớp này đã tồn tại.");
            }

            var schoolClass = new SchoolClass
            {
                ClassName = dto.ClassName
            };

            await _classRepository.AddClassAsync(schoolClass);

            return new ClassDto
            {
                ClassId   = schoolClass.ClassId,
                ClassName = schoolClass.ClassName
            };
        }

        public async Task UpdateClassAsync(int id, UpdateClassDto dto)
        {
            var schoolClass = await _classRepository.GetClassByIdAsync(id);
            if (schoolClass == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lớp học.");
            }

            if (await _classRepository.ClassNameExistsExceptIdAsync(dto.ClassName, id))
            {
                throw new InvalidOperationException("Tên lớp này đã tồn tại.");
            }

            schoolClass.ClassName = dto.ClassName;
            await _classRepository.UpdateClassAsync(schoolClass);
        }

        public async Task DeleteClassAsync(int id)
        {
            var schoolClass = await _classRepository.GetClassByIdAsync(id);
            var assignedExercises = await _classRepository.GetAssignedExercisesByClassIdAsync(id);

            if (schoolClass == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lớp học.");
            }
            if (schoolClass.StudentClasses != null && schoolClass.StudentClasses.Count > 0)
            {
                throw new InvalidOperationException("Lớp đang có học viên. Vui lòng xóa hoặc chuyển các học viên trước khi xóa lớp.");
            }
            if (assignedExercises != null && assignedExercises.Count > 0)
            {
                throw new InvalidOperationException("Lớp đang có bài tập được giao. Vui lòng gỡ bài tập trước khi xóa lớp.");
            }


            await _classRepository.DeleteClassAsync(schoolClass);
        }

        public async Task<ClassDetailsDto> GetClassDetailsAsync(int id)
        {
            var schoolClass = await _classRepository.GetClassWithStudentsAndExercisesAsync(id);
            if (schoolClass == null)
            {
                throw new KeyNotFoundException("Không tìm thấy lớp học.");
            }

            var assignedExercises = await _classRepository.GetAssignedExercisesByClassIdAsync(id);

            var studentIds = schoolClass.StudentClasses?.Select(sc => sc.StudentId).ToList() ?? new List<int>();
            var submissionCounts = await _classRepository.GetSubmissionCountsByStudentIdsAsync(studentIds);

            var dto = new ClassDetailsDto
            {
                ClassId   = schoolClass.ClassId,
                ClassName = schoolClass.ClassName,
                Students = schoolClass.StudentClasses?.Select(sc => new StudentInClassDto
                {
                    StudentClassId  = sc.StudentClassId,
                    StudentId       = sc.StudentId,
                    FullName        = sc.Student?.FullName ?? "",
                    Email           = sc.Student?.Email ?? "",
                    SubmissionCount = submissionCounts.GetValueOrDefault(sc.StudentId, 0)
                }).ToList() ?? new List<StudentInClassDto>(),
                AssignedExercises = assignedExercises.Select(ce => new AssignedExerciseDto
                {
                    ClassExerciseId = ce.ClassExerciseId,
                    ExerciseId      = ce.ExerciseId,
                    Title           = ce.Exercise?.Title ?? "",
                    Type            = ce.Exercise?.Type ?? "",
                    TopicName       = ce.Exercise?.VocabularyTopic?.Name ?? "",
                    Deadline        = ce.Deadline
                }).ToList()
            };

            return dto;
        }

        public async Task AddStudentToClassAsync(int classId, AddStudentToClassDto dto)
        {
            var exists = await _classRepository.IsStudentInClassAsync(classId, dto.StudentId);
            if (exists)
            {
                throw new InvalidOperationException("Học viên đã có trong lớp này.");
            }

            var studentClass = new StudentClass 
            { 
                ClassId = classId, 
                StudentId = dto.StudentId 
            };
            await _classRepository.AddStudentToClassAsync(studentClass);
        }

        public async Task RemoveStudentFromClassAsync(int classId, int studentClassId)
        {
            var record = await _classRepository.GetStudentClassByIdAsync(studentClassId);
            if (record == null || record.ClassId != classId)
            {
                throw new KeyNotFoundException("Không tìm thấy bản ghi.");
            }

            await _classRepository.RemoveStudentFromClassAsync(record);
        }

        public async Task UpdateDeadlineAsync(int classExerciseId, UpdateDeadlineInClassDto dto)
        {
            var assignment = await _classRepository.GetClassExerciseByIdAsync(classExerciseId);
            if (assignment == null)
            {
                throw new KeyNotFoundException("Không tìm thấy bài tập được gán.");
            }

            assignment.Deadline = dto.Deadline;
            await _classRepository.UpdateClassExerciseAsync(assignment);
        }
    }
}
