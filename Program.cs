using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Windows.Forms;

namespace HostsDeployer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            /**
             * 当前用户是管理员的时候，直接启动应用程序
             * 如果不是管理员，则使用启动对象启动程序，以确保使用管理员身份运行
             */
            //判断当前登录用户是否为管理员
            if (RunAsAdmin.IsRunAsAdmin)
            {
                //如果是管理员，则直接运行

                LcHostDeployer deployer = new LcHostDeployer();
                if (deployer.Apply())
                    Console.WriteLine(deployer);
                else Console.WriteLine("hosts 无改动。");

                Console.ReadKey();
            }
            else
            {
                //创建启动对象
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = Application.ExecutablePath;
                //设置启动动作,确保以管理员身份运行
                startInfo.Verb = "runas";
                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch
                {
                    return;
                }
                //退出
                Application.Exit();
            }
        }

    }
}
