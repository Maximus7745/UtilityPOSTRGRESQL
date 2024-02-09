using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityPostgreSQL.Models
{
    [Table("Employees")]
    [Index("FullName", IsUnique = true)]
    public class Employee
    {
        public int ID { get; set; }
        public int? DepartmentID {  get; set; }
        public Department? Department { get; set; }
        public Department? ManagerDepartment { get; set; }
        public string FullName { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }
        public int? JobID { get; set; }
        public Job? Job { get; set; }
    }
}
