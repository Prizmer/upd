using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;


using SharpSvn;
using Newtonsoft.Json;


namespace PrizmerUpdater
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Добро пожаловать в установщик продуктов компании \"Правильные измерения\"!");

            string userName = "prizmer";

            string gitPublicApiUrl = "https://api.github.com/orgs/" + userName + "/repos?per_page=1000";

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            LBL_START:
            Console.WriteLine("Список продуктов загружается...\n");
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers["User-Agent"] = "Mozilla/5.0";
                var json = webClient.DownloadString(gitPublicApiUrl);

                var repositoryList = RepositoryList.FromJson(json);
                int i = 0;
                foreach (var repository in repositoryList)
                {
                    Console.WriteLine(i.ToString() + ": " + repository.Name);
                    i++;
                }
                Console.WriteLine();

                INPUT_IDX:

                Console.Write("Введите индекс репозитория для копирования: ");
                string inpStr = Console.ReadLine().Trim();
                int selectedIdx = 0;
                bool res = int.TryParse(inpStr, out selectedIdx);
                if (!res || selectedIdx < 0 || selectedIdx > repositoryList.Count) goto INPUT_IDX;

                string repositoryName = repositoryList[selectedIdx].Name;
                string projectName = repositoryList[selectedIdx].Description;

                string remoteURL = "https://github.com/" + userName + "/" + repositoryName + "/trunk/";
                bool bFullProject = false;
                if (projectName != null && projectName.Length > 0)
                {
                    Console.Write("Введите [1] для копирования всего проекта, если нужен лишь исполнимый файл, нажмите ввод: ");
                    inpStr = Console.ReadLine().Trim();
                    if (inpStr.Length == 0)
                    {
                        remoteURL = $"https://github.com/{userName}/{repositoryName}/trunk/{projectName}/bin/Debug/";
                    }
                    else
                    {
                        bFullProject = true;
                    }
                }

                LBL_REPEAT:

                string targetDirectory = @"C:\" + userName + @"\" + repositoryName;

                Console.WriteLine("\nURL: " + remoteURL);
                Console.WriteLine("Дирректория установки: " + targetDirectory);
                Console.WriteLine("Копировать весь проект: " + bFullProject);


                if (Directory.Exists(targetDirectory))
                {
                    Console.Write("\nУдаляем старые файлы...");
                    Directory.Delete(targetDirectory, true);
                    Console.Write("Готово!\n");
                }
                else
                {
                    Console.WriteLine("");
                }

                Console.Write("Ожидайте...");

                SvnClient svnClient = new SvnClient();
                SvnUriTarget target = new SvnUriTarget(remoteURL);
                SvnExportArgs svnExpArgs = new SvnExportArgs();
                try
                {
                    svnClient.Export(target, targetDirectory, svnExpArgs, out SvnUpdateResult svnUpdRes);
                    Console.Write("Завершено!\n");

                    if (Directory.Exists(targetDirectory))
                    {
                        Process.Start(targetDirectory);
                    }
                }
                catch (Exception ex)
                {
                    Console.Write("ОШИБКА:\n" + ex.Message + "\n");
                }


                Console.WriteLine("Выход - ESC, Повтор - SPACE, начать заново - любая клавиша");
                ConsoleKeyInfo cki = Console.ReadKey();
                if (cki.Key == ConsoleKey.Escape)
                {
                    Environment.Exit(-1);
                } 
                else if (cki.Key == ConsoleKey.Spacebar)
                {
                    Console.Clear();
                    goto LBL_REPEAT;
                }
                else
                {
                    Console.Clear();
                    goto LBL_START;
                }
            }
        }
    }


}
