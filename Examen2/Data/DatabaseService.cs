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

            await SeedDataAsync();
        }

        private async Task SeedDataAsync()
        {
            // Cursos iniciales
            int courseCount = await _db.Table<Course>().CountAsync();
            if (courseCount == 0)
            {
                var courses = new List<Course>
                {
                    new Course { CourseName = "5IM6 DERECHO MERCANTIL", TeacherName = "Xochitl Rosas Alvarado", ThemeColor = "#1E8E3E" },
                    new Course { CourseName = "CALCULO INTEGRAL 5IM6", TeacherName = "Christopher Cesar Perez", ThemeColor = "#455A64" },
                    new Course { CourseName = "ENGLISH V", TeacherName = "Erika Edith Reyna Cordova", ThemeColor = "#1967D2" }
                };
                await _db.InsertAllAsync(courses);
            }

            // Usuario inicial de prueba
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

        public async Task<List<Course>> GetCoursesAsync()
        {
            await InitAsync();
            return await _db.Table<Course>().ToListAsync();
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

            // Verificar si ya existe el usuario o correo
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
        public async Task<User> GetUserByIdAsync(int id)
        {
            await InitAsync();
            return await _db.Table<User>().Where(u => u.Id == id).FirstOrDefaultAsync();
        }
        public async Task<bool> AddCourseAsync(Course course)
        {
            await InitAsync();
            int rows = await _db.InsertAsync(course);
            return rows > 0;
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            await InitAsync();
            int rows = await _db.DeleteAsync<Course>(courseId);
            return rows > 0;
        }
    }
}