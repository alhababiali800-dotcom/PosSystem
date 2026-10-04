using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Pos.Infrastructure.Data // تأكد من أن مساحة الأسماء تطابق تلك الموجودة في PosDbContext
{
    public class PosDbContextFactory : IDesignTimeDbContextFactory<PosDbContext>
    {
        public PosDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PosDbContext>();

            // ⚠️ هام: استخدم نفس مزود قاعدة البيانات المثبت في مشروعك.
            
            // إذا كنت تستخدم SQLite (وهو الشائع لمثل هذه المشاريع):
            optionsBuilder.UseSqlite("Data Source=pos_database.db");

            // أو إذا كنت تستخدم SQL Server (قم بتفعيل هذا السطر وحذف سطر SQLite أعلاه):
            // optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=PosDb;Trusted_Connection=True;");

            return new PosDbContext(optionsBuilder.Options);
        }
    }
}