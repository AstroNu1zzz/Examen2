using SQLite;
using Examen2.ViewModels;
using System.IO;

namespace Examen2.Data
{
    public class DatabaseService
    {
        private SQLiteAsyncConnection _db;

        private async Task InitAsync()
        {
            if (_db != null)
                return;

            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "classroom_examen.db3");
            _db = new SQLiteAsyncConnection(dbPath);

            await _db.CreateTableAsync<Course>();
            await _db.CreateTableAsync<User>();
            await _db.CreateTableAsync<CourseTeacher>();
            await _db.CreateTableAsync<ClassContentItem>();
            await _db.CreateTableAsync<TaskSubmission>();
            await _db.CreateTableAsync<Unit>();

            await SeedDataAsync();
        }

        public async Task<List<ClassContentItem>> GetCourseContentItemsAsync(int courseId)
        {
            await InitAsync();
            return await _db.Table<ClassContentItem>()
                             .Where(i => i.CourseId == courseId)
                             .OrderByDescending(i => i.CreatedAt)
                             .ToListAsync();
        }

        public async Task<bool> SaveTaskSubmissionAsync(TaskSubmission submission)
        {
            await InitAsync();
            var existing = await _db.Table<TaskSubmission>()
                                    .Where(s => s.ContentItemId == submission.ContentItemId && s.UserId == submission.UserId)
                                    .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.FilePath = submission.FilePath;
                existing.SubmittedAt = DateTime.Now;
                return await _db.UpdateAsync(existing) > 0;
            }

            return await _db.InsertAsync(submission) > 0;
        }

        public async Task<TaskSubmission> GetTaskSubmissionAsync(int contentItemId, int userId)
        {
            await InitAsync();
            return await _db.Table<TaskSubmission>()
                            .Where(s => s.ContentItemId == contentItemId && s.UserId == userId)
                            .FirstOrDefaultAsync();
        }

        public async Task<bool> AddClassContentItemAsync(ClassContentItem item)
        {
            await InitAsync();

            var activeUnit = await _db.Table<Unit>()
                                      .Where(u => u.CourseId == item.CourseId &&
                                                  item.CreatedAt >= u.StartDate &&
                                                  item.CreatedAt <= u.EndDate)
                                      .FirstOrDefaultAsync();

            if (activeUnit != null)
            {
                item.UnitId = activeUnit.Id;
            }

            int rows = await _db.InsertAsync(item);
            return rows > 0;
        }

        public async Task<List<ClassContentItem>> GetGlobalActivitiesAsync(int userId)
        {
            await InitAsync();

            var teacherRelations = await _db.Table<CourseTeacher>().Where(ct => ct.UserId == userId).ToListAsync();
            var courseIds = teacherRelations.Select(ct => ct.CourseId).ToList();

            if (!courseIds.Any()) return new List<ClassContentItem>();

            return await _db.Table<ClassContentItem>()
                            .Where(i => courseIds.Contains(i.CourseId))
                            .OrderByDescending(i => i.CreatedAt)
                            .Take(20)
                            .ToListAsync();
        }

        private async Task SeedDataAsync()
        {
            int userCount = await _db.Table<User>().CountAsync();
            if (userCount == 0)
            {
                var defaultUser = new User
                {
                    Username = "alumno",
                    Email = "alumno@cecyt13.ipn.mx",
                    Password = "123"
                };
                await _db.InsertAsync(defaultUser);
            }
        }

        public async Task<bool> AddCourseAsync(Course course, User primaryTeacher)
        {
            await InitAsync();

            course.PrimaryTeacherId = primaryTeacher.Id;
            course.PrimaryTeacherName = primaryTeacher.Username;

            int rows = await _db.InsertAsync(course);
            if (rows > 0)
            {
                var relation = new CourseTeacher
                {
                    CourseId = course.Id,
                    UserId = primaryTeacher.Id,
                    IsPrimary = true
                };
                await _db.InsertAsync(relation);
                return true;
            }
            return false;
        }

        public async Task<List<Course>> GetCoursesAsync(int userId)
        {
            await InitAsync();

            var teacherRelations = await _db.Table<CourseTeacher>()
                                            .Where(ct => ct.UserId == userId)
                                            .ToListAsync();

            var courseIds = teacherRelations.Select(ct => ct.CourseId).ToList();

            return await _db.Table<Course>()
                            .Where(c => courseIds.Contains(c.Id))
                            .ToListAsync();
        }

        public async Task<(bool Success, string Message)> AddTeacherToCourseAsync(int courseId, string teacherEmailOrUsername)
        {
            await InitAsync();

            int currentTeachersCount = await _db.Table<CourseTeacher>()
                                                .Where(ct => ct.CourseId == courseId)
                                                .CountAsync();

            if (currentTeachersCount >= 5)
            {
                return (false, "La clase ya alcanzó el límite máximo de 5 profesores.");
            }

            var targetUser = await _db.Table<User>()
                                      .Where(u => u.Email == teacherEmailOrUsername || u.Username == teacherEmailOrUsername)
                                      .FirstOrDefaultAsync();

            if (targetUser == null)
            {
                return (false, "El usuario especificado no existe.");
            }

            var existingRelation = await _db.Table<CourseTeacher>()
                                             .Where(ct => ct.CourseId == courseId && ct.UserId == targetUser.Id)
                                             .FirstOrDefaultAsync();

            if (existingRelation != null)
            {
                return (false, "Este usuario ya forma parte de los profesores de la clase.");
            }

            await _db.InsertAsync(new CourseTeacher
            {
                CourseId = courseId,
                UserId = targetUser.Id,
                IsPrimary = false
            });

            return (true, $"Profesor {targetUser.Username} asignado correctamente.");
        }

        public async Task<bool> AddUnitAsync(Unit unit)
        {
            await InitAsync();
            return await _db.InsertAsync(unit) > 0;
        }

        public async Task<List<Unit>> GetCourseUnitsAsync(int courseId)
        {
            await InitAsync();
            return await _db.Table<Unit>().Where(u => u.CourseId == courseId).OrderBy(u => u.StartDate).ToListAsync();
        }

        public async Task<List<User>> GetCourseTeachersAsync(int courseId)
        {
            await InitAsync();

            var relations = await _db.Table<CourseTeacher>()
                                      .Where(ct => ct.CourseId == courseId)
                                      .ToListAsync();

            var userIds = relations.Select(r => r.UserId).ToList();

            return await _db.Table<User>()
                            .Where(u => userIds.Contains(u.Id))
                            .ToListAsync();
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            await InitAsync();

            await _db.Table<CourseTeacher>().Where(ct => ct.CourseId == courseId).DeleteAsync();
            int rows = await _db.DeleteAsync<Course>(courseId);
            return rows > 0;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            await InitAsync();
            return await _db.Table<User>().Where(u => u.Id == id).FirstOrDefaultAsync();
        }

        // MÉTODO REQUERIDO POR LOGINPAGE
        public async Task<User> GetUserByUsernameOrEmailAsync(string usernameOrEmail)
        {
            await InitAsync();
            return await _db.Table<User>()
                            .Where(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail)
                            .FirstOrDefaultAsync();
        }

        public async Task<User> ValidateUserAsync(string usernameOrEmail, string password)
        {
            await InitAsync();
            return await _db.Table<User>()
                             .Where(u => (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && u.Password == password)
                             .FirstOrDefaultAsync();
        }

        public async Task<bool> UserExistsAsync(string username, string email)
        {
            await InitAsync();
            var existing = await _db.Table<User>()
                                    .Where(u => u.Username == username || u.Email == email)
                                    .FirstOrDefaultAsync();
            return existing != null;
        }

        public async Task<bool> RegisterUserAsync(User newUser)
        {
            await InitAsync();
            if (await UserExistsAsync(newUser.Username, newUser.Email))
                return false;

            int rows = await _db.InsertAsync(newUser);
            return rows > 0;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            await InitAsync();
            int rows = await _db.UpdateAsync(user);
            return rows > 0;
        }
    }
}