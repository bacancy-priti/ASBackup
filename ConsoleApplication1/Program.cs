using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Security.Cryptography;


namespace ConsoleApplication1
{
    class Program
    {
        static ProjectManagementDatabaseEntities context = new ProjectManagementDatabaseEntities();
        static EmployeeDataEntities context2 = new EmployeeDataEntities();
        static void Main(string[] args)
        {
            Console.WriteLine("Fetching the data");
            var FinalList = GetData();
            foreach (var project in FinalList)
            {
                Console.WriteLine("\t "+project.ProjectName);
                var i = 0;
                foreach (var tech in project.technologies)
                {
                    i++;
                    if(i>1)
                        Console.WriteLine("\t");
                    Console.WriteLine("\t\t" + tech);
                    Console.WriteLine("\n");
                }
                Console.WriteLine("\n");
            }
            Console.WriteLine("Done");
            Console.Read();
        }
        public static string Decrypt(string cipherString, bool useHashing)
        {
            if (cipherString == null)
            { return null; }
            byte[] keyArray;
            byte[] toEncryptArray = Convert.FromBase64String(cipherString);

            System.Configuration.AppSettingsReader settingsReader = new AppSettingsReader();

            string key = (string)settingsReader.GetValue("SecurityKey", typeof(String));

            if (useHashing)
            {
                MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
                keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
                hashmd5.Clear();
            }
            else
            {
                keyArray = UTF8Encoding.UTF8.GetBytes(key);
            }
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
        }
        public static List<ProjectDetails> GetData()
        {
            var ProjectsList = context.ProjectMaster.Where(p=>p.IsActive==true).ToList();
            var WorkingLogsList = context.WorkingLogMaster.Select(e=>e.ProjectId).Distinct().ToList();

            var ProjectTechnologies = context.ProjectTechnologyMaster.ToList();
            var TechnologyList = context2.DepartmentMasters.ToList();
            var projectTech = (from pt in ProjectTechnologies
                               join t in TechnologyList on pt.TechnologyId equals t.Id
                               select new
                               {
                                   ProjectId = pt.ProjectId,
                                   DeptName = t.DepartmentName
                               }
                                  ).ToList();

            var FinalList = (from p in ProjectsList
                             join w in WorkingLogsList on p.Id equals w
                             select new ProjectDetails
                             {
                                 ProjectName = Decrypt(p.ProjectName, true),
                                 technologies = projectTech.Where(pt => pt.ProjectId == p.Id).Select(d => d.DeptName).ToList()
                             }).ToList();
            return FinalList;
        }
    }
    public class ProjectDetails
    {
        public string ProjectName { get; set; }
        public List<string> technologies { get; set; }
    }
}
