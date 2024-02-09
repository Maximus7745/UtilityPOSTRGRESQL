using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityPostgreSQL.Models
{
    [Table("Departments")]
    [Index("Name", "ParentID", IsUnique = true)]
    public class Department
    {
        public int ID { get; set; }
        public int ParentID { get; set; }
        public int? ManagerID { get; set; }
        public Employee? Manager {  get; set; }
        public string Name { get; set; }
        public string? Phone {  get; set; }
        public List<Employee> Employees { get; set; } = new();
    }
}
