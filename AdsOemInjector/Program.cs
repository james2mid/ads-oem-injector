using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Security.Principal;

namespace AdsOemInjector
{
    class Program
    {
        const string FILE_PATH = @"C:\adslogo.bmp";
        const string KEY_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion\OEMInformation";

        static void Main (string[] args)
        {
            enforceAdmin();

            // allows the program to be executed without any input required
            // by specifying `/y` as an argument
            bool unconditional = args.Contains("y");
            if (unconditional)
            {
                try
                {
                    inject();
                }
                catch
                {
                    Environment.Exit(50);
                }
                
                Environment.Exit(0);
            }

            Console.WriteLine("This application injects 'ads computers' OEM information.");
            Console.WriteLine();
            
            if (hasExistingOemInformation())
            {
                Console.WriteLine("OEM information already exists for this machine.");

                RegistryKey key = getOemKey();
                printValues(key);
                key.Close();

                Console.WriteLine();
                Console.WriteLine("Are you happy to overwrite this? [y/n]");

                bool overwrite = Console.ReadKey().Key == ConsoleKey.Y;
                Console.WriteLine();
                if (!overwrite)
                {
                    Console.WriteLine();
                    Console.WriteLine("The OEM information has not been altered.");
                    pressAnyKeyPrompt();
                    Environment.Exit(0);
                }
            }

            Console.WriteLine();
            Console.WriteLine("Injecting OEM information ...");
            try
            {
                inject();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to inject:");
                Console.WriteLine(e.Message);
                pressAnyKeyPrompt();
                Environment.Exit(50);
            }

            Console.WriteLine("Injection completed error-free.");
            pressAnyKeyPrompt();
            Environment.Exit(0);
        }

        /// <summary>
        /// Prompts to press any key to continue.
        /// </summary>
        static void pressAnyKeyPrompt ()
        {
            Console.WriteLine();
            Console.WriteLine("Press any key to continue ...");
            Console.ReadKey();
        }

        /// <summary>
        /// Checks for admin privilages and closes the program if not.
        /// </summary>
        static void enforceAdmin ()
        {
            bool isElevated;
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }

            if (!isElevated)
            {
                Console.WriteLine();
                Console.WriteLine("This application must be run with elevated (administrative) privilages.");
                Console.WriteLine("The application will now close.");
                pressAnyKeyPrompt();
                Environment.Exit(5);
            }
        }

        /// <summary>
        /// Does everything.
        /// </summary>
        static void inject ()
        {
            copyImage();
            updateRegistry();
        }

        /// <summary>
        /// Copies the project resource 'logo' onto C:\ as a hidden folder with filename 'logo.bmp'
        /// </summary>
        static void copyImage ()
        {
            AdsOemInjector.Properties.Resources.logo.Save(FILE_PATH);
            File.SetAttributes(FILE_PATH, File.GetAttributes(FILE_PATH) | FileAttributes.Hidden);
        }

        /// <summary>
        /// Updates the registry keys only.
        /// </summary>
        static void updateRegistry ()
        {
            RegistryKey oemKey = getOemKey();

            oemKey.SetValue("HelpCustomized", 0);
            oemKey.SetValue("Manufacturer", "ads computers");
            oemKey.DeleteValue("Model", false);
            oemKey.DeleteValue("SupportHours", false);
            oemKey.SetValue("SupportPhone", "0115 849 8919");
            oemKey.SetValue("SupportURL", "www.adscomputers.co.uk");
            oemKey.SetValue("Logo", FILE_PATH);

            oemKey.Flush();
            oemKey.Close();
        }

        /// <summary>
        /// Retrieves the existing OEM registry key or creates it and then returns it.
        /// </summary>
        static RegistryKey getOemKey ()
        {
            // use `CreateSubKey` in case it does not exist
            return Registry.LocalMachine.CreateSubKey
                (KEY_PATH, true);
        }

        /// <summary>
        /// Prints out all values of the supplied `key` to the console.
        /// </summary>
        static void printValues (RegistryKey key)
        {
            string[] valueNames = key.GetValueNames();

            foreach (string valueName in valueNames)
            {
                object value = key.GetValue(valueName);
                Console.WriteLine(valueName + ": '" + value.ToString() + "'");
            }
        }

        /// <summary>
        /// Returns a boolean indicating whether the computer has existing oem information.
        /// </summary>
        static bool hasExistingOemInformation ()
        {
            RegistryKey key = getOemKey();
            bool output = (key.ValueCount != 0);
            key.Close();
            return output;
        }
    }
}
