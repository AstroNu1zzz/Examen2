using Supabase;
using Examen2.ViewModels;

namespace Examen2.Data
{
    public class DatabaseService
    {
        private readonly Supabase.Client _client;
        private bool _isInitialized;

        public DatabaseService()
        {
            var url = "https://ebndevmaperaoshfrblu.supabase.co"; // Ejemplo: https://xyz.supabase.co
            var key = "sb_publishable_JrSXPoMHeeJYEe3jgJQ5ZQ_OGWoE-A1"; // Tu sb_publishable_...

            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            _client = new Supabase.Client(url, key, options);
        }

        private async Task InitAsync()
        {
            if (_isInitialized) return;
            await _client.InitializeAsync();
            _isInitialized = true;
        }

        // --- AUTENTICACIÓN Y USUARIOS ---
        public async Task<User> ValidateUserAsync(string usernameOrEmail, string password)
        {
            await InitAsync();
            var response = await _client.From<User>()
                                       .Where(u => u.Password == password && (u.Username == usernameOrEmail || u.Email == usernameOrEmail))
                                       .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task<User> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            await InitAsync();
            var response = await _client.From<User>()
                                       .Where(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail)
                                       .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            await InitAsync();
            var response = await _client.From<User>()
                                       .Where(u => u.Id == id)
                                       .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            await InitAsync();
            var response = await _client.From<User>()
                                       .Where(u => u.Username == username || u.Email == email)
                                       .Get();
            return response.Models.Any();
        }

        public async Task<bool> RegisterUserAsync(User newUser)
        {
            await InitAsync();
            if (await UserExistsAsync(newUser.Username, newUser.Email))
                return false;

            var response = await _client.From<User>().Insert(newUser);
            return response.Models.Count > 0;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            await InitAsync();
            var response = await _client.From<User>().Update(user);
            return response.Models.Count > 0;
        }

        // --- CURSOS Y MIEMBROS ---
        public async Task<List<Course>> GetCoursesAsync(int userId)
        {
            await InitAsync();
            var memberResponse = await _client.From<CourseMember>()
                                               .Where(m => m.UserId == userId)
                                               .Get();

            var courseIds = memberResponse.Models.Select(m => m.CourseId).ToList();
            if (!courseIds.Any()) return new List<Course>();

            var coursesResponse = await _client.From<Course>()
                                               .Filter("id", Postgrest.Constants.Operator.In, courseIds)
                                               .Get();

            return coursesResponse.Models;
        }

        public async Task<bool> AddCourseAsync(Course course, User primaryTeacher)
        {
            await InitAsync();
            course.PrimaryTeacherId = primaryTeacher.Id;
            course.PrimaryTeacherName = primaryTeacher.Username;

            var courseResponse = await _client.From<Course>().Insert(course);
            var createdCourse = courseResponse.Models.FirstOrDefault();

            if (createdCourse != null)
            {
                var member = new CourseMember
                {
                    CourseId = createdCourse.Id,
                    UserId = primaryTeacher.Id,
                    Role = "Teacher",
                    IsPrimary = true
                };
                await _client.From<CourseMember>().Insert(member);
                return true;
            }
            return false;
        }

        public async Task<(bool Success, string Message)> AddTeacherToCourseAsync(int courseId, string teacherEmailOrUsername)
        {
            await InitAsync();

            var currentTeachers = await _client.From<CourseMember>()
                                               .Where(m => m.CourseId == courseId && m.Role == "Teacher")
                                               .Get();

            if (currentTeachers.Models.Count >= 5)
                return (false, "La clase ya alcanzó el límite máximo de 5 profesores.");

            var targetUser = await GetUserByUsernameOrEmailAsync(teacherEmailOrUsername);
            if (targetUser == null)
                return (false, "El usuario especificado no existe.");

            var existing = await _client.From<CourseMember>()
                                       .Where(m => m.CourseId == courseId && m.UserId == targetUser.Id)
                                       .Get();

            if (existing.Models.Any())
                return (false, "Este usuario ya forma parte de la clase.");

            var newMember = new CourseMember
            {
                CourseId = courseId,
                UserId = targetUser.Id,
                Role = "Teacher",
                IsPrimary = false
            };
            await _client.From<CourseMember>().Insert(newMember);

            return (true, $"Profesor {targetUser.Username} asignado correctamente.");
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            await InitAsync();
            await _client.From<CourseMember>().Where(m => m.CourseId == courseId).Delete();
            await _client.From<Course>().Where(c => c.Id == courseId).Delete();
            return true;
        }

        // --- PUBLICACIONES Y CONTENIDOS ---
        public async Task<List<ClassContentItem>> GetCourseContentItemsAsync(int courseId)
        {
            await InitAsync();
            var response = await _client.From<ClassContentItem>()
                                       .Where(i => i.CourseId == courseId)
                                       .Order(i => i.CreatedAt, Postgrest.Constants.Ordering.Descending)
                                       .Get();
            return response.Models;
        }

        public async Task<bool> AddClassContentItemAsync(ClassContentItem item)
        {
            await InitAsync();
            var response = await _client.From<ClassContentItem>().Insert(item);
            return response.Models.Count > 0;
        }

        public async Task<List<ClassContentItem>> GetGlobalActivitiesAsync(int userId)
        {
            await InitAsync();
            var memberResponse = await _client.From<CourseMember>()
                                               .Where(m => m.UserId == userId)
                                               .Get();

            var courseIds = memberResponse.Models.Select(m => m.CourseId).ToList();
            if (!courseIds.Any()) return new List<ClassContentItem>();

            var response = await _client.From<ClassContentItem>()
                                       .Filter("course_id", Postgrest.Constants.Operator.In, courseIds)
                                       .Order(i => i.CreatedAt, Postgrest.Constants.Ordering.Descending)
                                       .Limit(20)
                                       .Get();

            return response.Models;
        }

        // --- ENTREGAS DE TAREAS ---
        public async Task<TaskSubmission> GetTaskSubmissionAsync(int contentItemId, int userId)
        {
            await InitAsync();
            var response = await _client.From<TaskSubmission>()
                                       .Where(s => s.ContentItemId == contentItemId && s.UserId == userId)
                                       .Get();
            return response.Models.FirstOrDefault();
        }

        public async Task<bool> SaveTaskSubmissionAsync(TaskSubmission submission)
        {
            await InitAsync();
            var existing = await GetTaskSubmissionAsync(submission.ContentItemId, submission.UserId);

            if (existing != null)
            {
                existing.FilePath = submission.FilePath;
                existing.SubmittedAt = DateTime.Now;
                var updateResponse = await _client.From<TaskSubmission>().Update(existing);
                return updateResponse.Models.Count > 0;
            }

            var insertResponse = await _client.From<TaskSubmission>().Insert(submission);
            return insertResponse.Models.Count > 0;
        }
    }
}