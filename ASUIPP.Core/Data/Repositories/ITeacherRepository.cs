using System.Collections.Generic;
using ASUIPP.Core.Models;

namespace ASUIPP.Core.Data.Repositories
{
    public interface ITeacherRepository
    {
        Teacher GetById(string teacherId);
        Teacher GetByFullName(string fullName);
        List<Teacher> GetAll();
        void Insert(Teacher teacher);
        void Update(Teacher teacher);
        void Delete(string teacherId);
    }
}