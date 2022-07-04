using LunarUploader.Misc;
using System;
using System.Threading.Tasks;

namespace LunarUploader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Username / Email:");
            string Username = Console.ReadLine();

            Console.WriteLine("Password:");
            string Password = Console.ReadLine();

            Console.WriteLine("Proxy IP:");
            string ProxyIP = Console.ReadLine();

            string ProxyUser = null;
            string ProxyPW = null;

            if (ProxyIP != "")
            {
                Console.WriteLine("Proxy Username:");
                ProxyUser = Console.ReadLine();
                if (ProxyUser == "") ProxyUser = null;

                Console.WriteLine("Proxy Password:");
                ProxyPW = Console.ReadLine();
                if (ProxyPW == "") ProxyPW = null;
            }
            else ProxyIP = null;

            ReuploadHelper rh = new(Username, Password, ProxyIP, ProxyUser, ProxyPW);
            DownloadHelper.Setup(rh.apiClient.CustomRemoteConfig.SdkUnityVersion);


            for (; ; )
            {
                Console.WriteLine("1 - Reupload Avatar");
                Console.WriteLine("2 - Reupload World");
                Console.WriteLine("3 - Delete Avatar");
                Console.WriteLine("4 - Delete World");

                int Type = Convert.ToInt32(Console.ReadLine());
                switch (Type)
                {
                    case 1:
                        Console.WriteLine("Avatar Name:");
                        string AvatarName = Console.ReadLine();
                        Console.WriteLine("AssetURl or VRCA:");
                        string AvatarPath = Console.ReadLine();
                        if (AvatarPath.StartsWith("http"))
                        {
                            Console.WriteLine("Downloading File...");
                            AvatarPath = DownloadHelper.DownloadToRandomPath(AvatarPath);
                        }
                        Console.WriteLine("ImageURl or Image:");
                        string AvatarImagePath = Console.ReadLine();
                        if (AvatarImagePath.StartsWith("http"))
                        {
                            Console.WriteLine("Downloading Image File...");
                            AvatarImagePath = DownloadHelper.DownloadToRandomPath(AvatarImagePath);
                        }
                        Console.WriteLine("1 - Private Upload");
                        Console.WriteLine("2 - Public Upload");
                        int Status = Convert.ToInt32(Console.ReadLine());
                        bool Private = false;
                        if (Status == 1) Private = true;

                        await rh.ReUploadAvatarAsync(AvatarName, AvatarPath, AvatarImagePath, Private);
                        break;

                    case 2:
                        Console.WriteLine("World Name:");
                        string WorldName = Console.ReadLine();
                        Console.WriteLine("AssetURl or VRCW:");
                        string WorldPath = Console.ReadLine();
                        if (WorldPath.StartsWith("http"))
                        {
                            Console.WriteLine("Downloading File...");
                            WorldPath = DownloadHelper.DownloadToRandomPath(WorldPath);
                        }
                        Console.WriteLine("ImageURl or Image:");
                        string WorldImagePath = Console.ReadLine();
                        if (WorldImagePath.StartsWith("http"))
                        {
                            Console.WriteLine("Downloading Image File...");
                            WorldImagePath = DownloadHelper.DownloadToRandomPath(WorldImagePath);
                        }

                        await rh.ReUploadWorldAsync(WorldName, WorldPath, WorldImagePath, 30);
                        break;

                    case 3:
                        Console.WriteLine("AvatarID:");
                        string Avi = Console.ReadLine();
                        await rh.DeleteAvatarAsync(Avi);
                        break;

                    case 4:
                        Console.WriteLine("WorldID:");
                        string World = Console.ReadLine();
                        await rh.DeleteWorldAsync(World);
                        break;
                }
            }
        }
    }
}
