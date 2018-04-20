using System;
using System.IO;

namespace Adadev.Util 
{
    public class FileUtil 
    {

        public static string GetServerDirectory() {
            return Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
        }
		
		public static string GetParentDirectory(string path, int levels) {
            if(string.IsNullOrEmpty(path)) {
                return string.Empty;
            }

            string parentPath = path;
            for(int i = 0; i < levels; i++) {
                parentPath = Directory.GetParent(parentPath).ToString();
            }

            return parentPath;
        }

        public static string AddSufixToFile(string filePath, string sufix) {
            return Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar
                + Path.GetFileNameWithoutExtension(filePath) + sufix + Path.GetExtension(filePath);
        }
    }
}
