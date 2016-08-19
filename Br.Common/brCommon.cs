using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;

namespace Br.Common
{
    public class brCommon
    {
        public static string GetAbsolutePath(string RelativePath)
        {
            return brCommon.GetAbsolutePath(RelativePath, true);
        }
        public static string GetAbsolutePath(string RelativePath, bool IsRelativeToAppRoot)
        {
            return Path.GetFullPath(Path.Combine((IsRelativeToAppRoot ? brCommon.GetAppRootPath() : brCommon.GetAssembliesPath()), RelativePath));
        }

        public static string GetAppRootPath()
        {
            string assembliesPath = brCommon.GetAssembliesPath();
            if (assembliesPath.ToLower().EndsWith("bin"))
            {
                assembliesPath = assembliesPath.Remove(assembliesPath.Length - 3, 3);
            }
            if (assembliesPath.IndexOf("/") > -1)
            {
                assembliesPath.Replace("/", "\\");
            }
            return assembliesPath;
        }
        public static string GetAssembliesPath()
        {
            string str = Path.GetDirectoryName(Assembly.GetExecutingAssembly().EscapedCodeBase).Replace("file:\\", "").Replace("%20", " ");
            return str;
        }

    }
}
