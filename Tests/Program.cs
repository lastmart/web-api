using NUnitLite;

namespace Tests
{
    public class Program
    {
        public static void Main()
        {
            var testsToRun = new string[]
            {
                typeof(Task1_GetUserByIdTests).FullName,
                typeof(Task2_CreateUserTests).FullName,
                //typeof(Task3_UpdateUserTests).FullName,
                //typeof(Task4_PartiallyUpdateUserTests).FullName,
                //typeof(Task5_DeleteUserTests).FullName,
                //typeof(Task6_HeadUserByIdTests).FullName,
                //typeof(Task7_GetUsersTests).FullName,
                //typeof(Task8_GetUsersOptionsTests).FullName,
            };
            new AutoRun().Execute(new[]
            {
                // раскомментируй, чтоб останавливать выполнение после первой ошибки
                // "--stoponerror", 
                "--noresult",
                "--test=" + string.Join(",", testsToRun)
            });
        }
    }
}
