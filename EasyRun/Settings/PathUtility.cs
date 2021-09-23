using System;
using System.IO;

namespace EasyRun.Settings
{
    public static class PathUtility
    {
        /// <summary>
        /// Determines the relative path of the specified path relative to a base path.
        /// </summary>
        /// <param name="path">The path to make relative.</param>
        /// <param name="relBase">The base path.</param>
        /// <param name="throwOnDifferentRoot">If true, an exception is thrown for different roots, otherwise the source path is returned unchanged.</param>
        /// <returns>The relative path.</returns>
        public static string GetRelativePath(string path, string relBase, bool throwOnDifferentRoot = true)
        {

            // Use case-insensitive comparing of path names.
            // NOTE: This may be different on other systems.
            StringComparison sc = StringComparison.InvariantCultureIgnoreCase;

            // Are both paths rooted?
            if (!Path.IsPathRooted(path))
            {
                throw new ArgumentException("path argument is not a rooted path.");
            }

            if (!Path.IsPathRooted(relBase))
            {
                throw new ArgumentException("relBase argument is not a rooted path.");
            }

            // Do both paths share the same root?
            string pathRoot = Path.GetPathRoot(path);
            string baseRoot = Path.GetPathRoot(relBase);
            if (!string.Equals(pathRoot, baseRoot, sc))
            {
                if (throwOnDifferentRoot)
                {
                    throw new InvalidOperationException("Both paths do not share the same root.");
                }
                else
                {
                    return path;
                }
            }

            // Cut off the path roots
            path = path.Substring(pathRoot.Length);
            relBase = relBase.Substring(baseRoot.Length);

            // Cut off the common path parts
            string[] pathParts = path.Split(Path.DirectorySeparatorChar);
            string[] baseParts = relBase.Split(Path.DirectorySeparatorChar);
            int commonCount;
            for (
                commonCount = 0;
                commonCount < pathParts.Length &&
                commonCount < baseParts.Length &&
                string.Equals(pathParts[commonCount], baseParts[commonCount], sc);
                commonCount++)
            {
            }

            // Add .. for the way up from relBase
            string newPath = "";
            for (int i = commonCount; i < baseParts.Length; i++)
            {
                newPath += ".." + Path.DirectorySeparatorChar;
            }

            // Append the remaining part of the path
            for (int i = commonCount; i < pathParts.Length; i++)
            {
                newPath = Path.Combine(newPath, pathParts[i]);
            }

            return newPath;
        }
        
        public static string GetAbsolutePath(string path1, string path2)
        {
            if (Path.IsPathRooted(path2))
            {
                return path2;
            }
            else
            {
                return Path.Combine(path1, path2);
            }
        }
    }
}
