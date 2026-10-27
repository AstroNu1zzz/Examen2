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

        public async Task<bool> AddClassContentItemAsync(ClassContentItem item)
        {
            await InitAsync();
            int rows = await _db.InsertAsync(item);
            return rows > 0;
        }

        private async Task SeedDataAsync()
        {
            // Solo creamos el usuario de prueba inicial si la tabla está vacía.
            // NO se insertan clases falsas.
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

        // Crear clase registrando al creador como Profesor Principal
        public async Task<bool> AddCourseAsync(Course course, User primaryTeacher)
        {
            await InitAsync();

            course.PrimaryTeacherId = primaryTeacher.Id;
            course.PrimaryTeacherName = primaryTeacher.Username;

            int rows = await _db.InsertAsync(course);
            if (rows > 0)
            {
                // Asignar en la tabla de relacion de profesores
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

        // Obtener solo las clases donde el usuario es profesor (principal o secundario)
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

        // Añadir un profesor secundario (Máximo 5 profesores por clase)
        public async Task<(bool Success, string Message)> AddTeacherToCourseAsync(int courseId, string teacherEmailOrUsername)
        {
            await InitAsync();

            // 1. Validar límite de 5 profesores
            int currentTeachersCount = await _db.Table<CourseTeacher>()
                                                .Where(ct => ct.CourseId == courseId)
                                                .CountAsync();

            if (currentTeachersCount >= 5)
            {
                return (false, "La clase ya alcanzó el límite máximo de 5 profesores.");
            }

            // 2. Buscar al usuario a invitar
            var targetUser = await _db.Table<User>()
                                      .Where(u => u.Email == teacherEmailOrUsername || u.Username == teacherEmailOrUsername)
                                      .FirstOrDefaultAsync();

            if (targetUser == null)
            {
                return (false, "El usuario especificado no existe.");
            }

            // 3. Validar si ya es profesor de la clase
            var existingRelation = await _db.Table<CourseTeacher>()
                                             .Where(ct => ct.CourseId == courseId && ct.UserId == targetUser.Id)
                                             .FirstOrDefaultAsync();

            if (existingRelation != null)
            {
                return (false, "Este usuario ya forma parte de los profesores de la clase.");
            }

            // 4. Registrar co-profesor
            await _db.InsertAsync(new CourseTeacher
            {
                CourseId = courseId,
                UserId = targetUser.Id,
                IsPrimary = false
            });

            return (true, $"Profesor {targetUser.Username} asignado correctamente.");
        }

        // Obtener la lista de profesores de una clase
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

            // Eliminar registros de la tabla relacional y la clase
            await _db.Table<CourseTeacher>().Where(ct => ct.CourseId == courseId).DeleteAsync();
            int rows = await _db.DeleteAsync<Course>(courseId);
            return rows > 0;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            await InitAsync();
            return await _db.Table<User>().Where(u => u.Id == id).FirstOrDefaultAsync();
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