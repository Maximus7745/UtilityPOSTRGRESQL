using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UtilityPostgreSQL.Models
{
    [Table("JobTitle")]
    [Index("Name", IsUnique = true)]
    public class Job
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public List<Employee> Employees { get; set; } = new();
    }
}
