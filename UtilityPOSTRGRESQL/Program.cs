// See https://aka.ms/new-console-template for more information
using Microsoft.Extensions.Configuration;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UtilityPostgreSQL.Models;

Console.WriteLine("Для загрузки файлов в базу данных введите load " +
    ", тип загружаемых файлов через запятую нужный символ (j - должность, d - подразделение, e - сотрудник) " +
    "и полный путь к файлу также через запятую." +
    "Для вывода текущей структуры данных введите output, при необходимости, через запятую можно указать " +
    "параметр id для вывода данного подразделения и цепочки его родителей. " +
    "Чтобы введите exit.");
do
{
    try
    {
        string input = Console.ReadLine();

        if (input.ToLower() == "exit")
        {
            break;
        }
        else
        {
            string[] inputs = input.Split(',');
            if (inputs[0].ToLower() == "load")
            {
                if (true)
                {
                    Import(inputs[2], inputs[1]);
                    Output();
                    Console.WriteLine("Загрузка успешно выполнена");
                }

            }
            else if (inputs[0].ToLower() == "output")
            {
                if (inputs.Length == 2)
                {
                    if (Int32.TryParse(inputs[1], out int id))
                    {
                        OutputById(id);
                    }
                    else
                    {
                        Console.WriteLine("Число не было распознано");
                    }
                }
                else if (inputs.Length == 1)
                {
                    Output();
                    Console.WriteLine("Вывод был завершён.");
                }
                else
                {
                    Console.WriteLine("Некорректные данные для id");
                }
            }
            else
            {
                Console.WriteLine("Команда не была распознана!");
            }
        }
    }
    catch (Exception)
    {
        Console.WriteLine("Ошибка");
    }
} while (true);


static void Import(string fileName, string importType)
{
    List<string[]> data = GetData(fileName);

    switch (importType)
    {
        case "d":
            ImportDepartments(data);
            break;
        case "e":
            ImportEmployees(data);
            break;
        case "j":
            ImportJobs(data);
            break;
        default:
            throw new Exception();
            break;
    }
}

static void ImportDepartments(List<string[]> rows)
{
    foreach (string[] row in rows)
    {
        try
        {
            if (row.Length != 4 || string.IsNullOrWhiteSpace(row[0]))
            {
                throw new Exception();
            }
            string name = ReductText(row[0]);
            string fullName = ReductFullName(row[2]);
            string parentName = ReductText(row[1]);
            Department? parent = null;
            Employee? manager = null;
            using (UtilityDbContext db = new UtilityDbContext())
            {
                List<Department> departments = db.Departments.Where(d => d.Name.Replace(" ", "").ToLower()
                == name.Replace(" ", "").ToLower()).ToList();
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    if(departments.Count > 0)
                    {
                        foreach (var dep in departments)
                        {
                            var temp_parent = db.Departments.FirstOrDefault(d => d.ID == dep.ParentID);
                            if (temp_parent != null && parentName.Replace(" ", "").ToLower() == 
                                temp_parent.Name.Replace(" ", "").ToLower())
                            {
                                parent = temp_parent;
                                break;
                            }
                        }
                    }
                    if (parent == null)
                    {
                        parent = db.Departments.FirstOrDefault(d => d.Name.Replace(" ", "").ToLower()
                        == parentName.Replace(" ", "").ToLower()); //тут тоже есть нюанс, что мы не знаем какой
                                                               //имеенно родительский департамент будет выбран, так как
                                                               //уникальность задаётся двумя параметрами, а у нас только его имя
                    }
                    if (parent == null)
                    {
                        parent = new Department
                        {
                            Name = parentName,
                            ParentID = 0,
                            ManagerID = null
                        };
                        db.Departments.Add(parent);
                        db.SaveChanges();
                    }
                }
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    manager = db.Employees.FirstOrDefault(e => e.FullName.Replace(" ", "").ToLower()
                    == fullName.Replace(" ", "").ToLower());
                    if (manager == null)
                    {
                        manager = new Employee { FullName = fullName };
                        db.Employees.Add(manager);
                        db.SaveChanges();

                    }
                    else
                    {
                        var oldDepartment = db.Departments.FirstOrDefault(d => d.ID == manager.DepartmentID);
                        if (oldDepartment != null)
                        {
                            oldDepartment.Manager = null;
                            db.Update(oldDepartment);
                            db.SaveChanges();
                        }
                    }

                }
                Department? department = db.Departments.FirstOrDefault(d => d.Name.Replace(" ", "").ToLower()
                == name.Replace(" ", "").ToLower()
                && (d.ParentID == (parent != null ? parent.ID : 0)));

                if (department != null)
                {
                    if(manager == null)
                    {
                        department.ManagerID = null;
                    }
                    else
                    {
                        if (department.ManagerID != manager.ID)
                        {
                            var oldManager = db.Employees.FirstOrDefault(e => e.ID == department.ManagerID);
                            if (oldManager != null)
                            {
                                oldManager.ManagerDepartment = null;
                            }
                            manager.ManagerDepartment = department;
                            department.Employees.Add(manager);
                        }
                    }
                    department.Phone = row[3];
                    db.Update(department);
                    db.SaveChanges();

                }
                else
                {

                    department = new Department
                    {
                        ParentID = parent is not null ? parent.ID : 0,
                        Name = ReductText(row[0]),
                        ManagerID = manager is not null ? manager.ID : null,
                        Phone = row[3],
                        Employees = new()
                    };
                    if(manager != null)
                    {
                        department.Employees.Add(manager);
                    }
                    db.Add(department);
                    db.SaveChanges();
                }


            }


        }
        catch (Exception)
        {
            Console.WriteLine("stderror");
        }
    }


}


static void ImportEmployees(List<string[]> rows)
{
    foreach (string[] row in rows)
    {
        try
        {
            if (row.Length != 5 || string.IsNullOrWhiteSpace(row[1]))
            {
                throw new Exception();
            }
            string deparmentName = ReductText(row[0]);
            string fullName = ReductFullName(row[1]);
            string jobName = ReductText(row[4]);
            Job? job = null;
            Department? department = null;
            using (UtilityDbContext db = new UtilityDbContext())
            {
                Employee? employee = db.Employees.FirstOrDefault(d => d.FullName.Replace(" ", "").ToLower()
                    == fullName.Replace(" ", "").ToLower());
                if (!string.IsNullOrWhiteSpace(deparmentName))
                {
                    if(employee != null && employee.DepartmentID != null)
                    {
                        department = db.Departments.FirstOrDefault(d => d.ID
                        == employee.DepartmentID); 
                        if(department != null && department.Name != deparmentName)
                        {
                            department = null;
                        }
                    }
                    if (department == null)
                    {
                        department = db.Departments.FirstOrDefault(d => d.Name.Replace(" ", "").ToLower()
                        == deparmentName.Replace(" ", "").ToLower()); //Тут есть нюанс, что мы находим первый попавшийся отдел
                    }

                    if (department == null)
                    {
                        department = new Department
                        {
                            Name = deparmentName,
                            ParentID = 0,
                            ManagerID = null
                        };
                        db.Departments.Add(department);
                        db.SaveChanges();
                    }
                }
                if (!string.IsNullOrWhiteSpace(jobName))
                {
                    job = db.Jobs.FirstOrDefault(e => e.Name.Replace(" ", "").ToLower()
                    == jobName.Replace(" ", "").ToLower());
                    if (job == null)
                    {
                        job = new Job { Name = jobName };
                        db.Jobs.Add(job);
                        db.SaveChanges();

                    }

                }
                if (employee != null)
                {
                    if (department != null && employee.DepartmentID != department.ID)
                    {
                        var oldDepartment = db.Departments.FirstOrDefault(d => d.ManagerID == employee.ID);
                        if (oldDepartment != null && oldDepartment.ManagerID == employee.ID)
                        {
                            oldDepartment.ManagerID = null;
                            db.Update(oldDepartment);
                            db.SaveChanges();
                        }
                        db.Update(department);
                    }
                    employee.DepartmentID = department == null ? null : department.ID;
                    employee.Login = row[2];
                    employee.Password = row[3];
                    employee.JobID = job == null ? null : job.ID;
 

                    db.Update(employee);
                    db.SaveChanges();

                }
                else
                {

                    employee = new Employee
                    {
                        FullName = fullName,
                        Login = row[2],
                        Password = row[3]
                    };
                    if(department != null)
                    {
                        employee.DepartmentID = department.ID;
                    }
                    if (job != null)
                    {
                        employee.JobID = job.ID;
                    }

                    db.Add(employee);
                    db.SaveChanges();
                }



          
            }


        }
        catch (Exception)
        {
            Console.WriteLine("stderror");
        }
    }
    


}
static void ImportJobs(List<string[]> rows)
{
    foreach (string[] row in rows)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(row[0]))
            {
                throw new Exception();
            }
            string name = ReductText(row[0]);
            var job = new Job { Name = name };
            using (UtilityDbContext db = new UtilityDbContext())
            {

                db.Add(job);
                db.SaveChanges();
            }
        }
        catch (Exception)
        {
            Console.WriteLine("stderror");
        }
    }
}



static List<string[]> GetData(string fileName)
{
    List<string[]> rows = new();  
    using (StreamReader reader = new StreamReader(fileName))
    {
        reader.ReadLine();
        while (!reader.EndOfStream)
        {
            try
            {
                string? row = reader.ReadLine();
                if (!string.IsNullOrWhiteSpace(row))
                {
                    rows.Add(row.Split('\t'));
                }

            }
            catch (Exception)
            {
                Console.WriteLine("stderror");
            }
        }
    }
    return rows;
}


static void Output()
{
    var departments = new List<Department>();

    using (UtilityDbContext db = new UtilityDbContext())
    {
        departments = db.Departments.ToList();
    }
    var firstNodes = departments.Where(d => d.ParentID == 0).OrderBy(p => p.Name);
    foreach (var node in firstNodes)
    {
        int level = 1;
        WriteNode(node, departments, level);
    }
}


static void WriteNode(Department node, List<Department> departments, int level)
{
    var employees = new List<Employee>();
    Employee? manager = null;
    using (UtilityDbContext db = new UtilityDbContext())
    {
        employees = db.Employees.Where(e => e.DepartmentID == node.ID).OrderBy(e => e.FullName).ToList();
        manager = db.Employees.FirstOrDefault(e => e.ID == node.ManagerID);
    }
    Console.WriteLine($"{new string('=', level)} {node.Name} ID={node.ID}");
    WriteManager(manager, level);
    WriteEmloyees(node, employees, level);
    var children = departments.Where(d => d.ParentID == node.ID).OrderBy(p => p.Name);
    foreach (var child in children)
    {
        int newLevel = level + 1;
        WriteNode(child, departments, newLevel);
    }
}
static void WriteManager(Employee? manager, int level)
{
    if (manager != null)
    {
        string job = "";
        if (manager.JobID != null)
        {
            using (UtilityDbContext db = new UtilityDbContext())
            {
                job += db.Jobs.First(j => j.ID == manager.JobID).Name;
                if (job != "")
                {
                    job = "(" + job + ")";
                }

            }
        }
        Console.WriteLine($"{new string(' ', level - 1)}* {manager.FullName} ID={manager.ID} {job}");
    }
}
static void WriteEmloyees(Department node, List<Employee> employees, int level)
{
    foreach (var employee in employees)
    {
        if (employee.ID == node.ManagerID)
        {
            continue;
        }
        string job = "";
        if (employee.JobID != null)
        {
            using (UtilityDbContext db = new UtilityDbContext())
            {
                job += db.Jobs.First(j => j.ID == employee.JobID).Name;
                if (job != "")
                {
                    job = "(" + job + ")";
                }

            }
        }

        Console.WriteLine($"{new string(' ', level - 1)}- {employee.FullName} ID={employee.ID} {job}");
    }
}
static List<Department> WriteParents(Department node, List<Department> departments)
{
    List<Department> parents = new List<Department>();
    if (node.ParentID != 0)
    {
        Department parent = departments.First(d => d.ID == node.ParentID);
        parents.Add(parent);
        while (parent.ParentID != 0)
        {
            parent = departments.First(d => d.ID == parent.ParentID);
            parents.Add(parent);
        }
        for (int i = parents.Count - 1; i > -1; i--)
        {
            Console.WriteLine($"{new string('=', parents.Count - i)} {parents[i].Name} ID={parents[i].ID}");
        }

    }
    return parents;
}


static void OutputById(int id)
{
    var departments = new List<Department>();

    using (UtilityDbContext db = new UtilityDbContext())
    {
        departments = db.Departments.ToList();
    }

    var node = departments.FirstOrDefault(d => d.ID == id);
    if (node == null)
    {
        Console.WriteLine("Данный id не существует!");
        return;
    }
    List<Department> parents = WriteParents(node, departments);
   
    Console.WriteLine($"{new string('=', parents.Count + 1)} {node.Name} ID={node.ID}");

    var employees = new List<Employee>();
    Employee? manager = null;
    using (UtilityDbContext db = new UtilityDbContext())
    {
        employees = db.Employees.Where(e => e.DepartmentID == node.ID).OrderBy(e => e.FullName).ToList();
        manager = db.Employees.FirstOrDefault(e => e.ID == node.ManagerID);
    }
    WriteManager(manager, parents.Count + 1);
    WriteEmloyees(node, employees, parents.Count + 1);
   
}


static string ReductText(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return "";
    }
    if (text.Length < 2)
    {
        return text.ToUpper();
    }
    text = text.Trim().ToLower();
    text = Char.ToUpper(text[0]).ToString() + text.Substring(1);
    string newText = "";
    for (int i = 0; i < text.Length; i++)
    {
        if (text[i] == ' ')
        {
            if (text[i + 1] == ' ' || Char.IsPunctuation(text[i + 1]) || Char.IsPunctuation(newText[^1]))
            {
                continue;
            }
        }
        newText += text[i];
    }

    return newText;
}

static string ReductFullName(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
    {
        return "";
    }
    if (text.Length < 2)
    {
        return text.ToUpper();
    }
    text = text.Trim().ToLower();
    string newText = Char.ToUpper(text[0]).ToString();
    for (int i = 1; i < text.Length; i++)
    {
        if (text[i] == ' ')
        {
            if (text[i + 1] == ' ' || Char.IsPunctuation(text[i + 1]) || Char.IsPunctuation(newText[^1]))
            {
                continue;
            }
        }
        if (newText[^1] == ' ' || newText[^1] == '\'' || newText[^1] == '-'
            || newText[^1] == '.' || newText[^1] == '`' || newText[^1] == ',')
            newText += Char.ToUpper(text[i]);
        else
        {
            newText += text[i];
        }
    }
    return newText;
}