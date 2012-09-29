using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EZPlayer.PlayList
{
    public class PlayListUtil
    {
        /// <summary>
        /// Gets play list base on the given file
        /// Will add all the similar files to the play list.
        /// Will add all files that have the same extension if no similar files are found.
        /// The first file in the play list will be the base file.
        /// All files that less than the base file will be excluded.
        /// </summary>
        public static List<string> GetPlayList(string filePathToGetPlayListFor, IDirectorySearcher dirSearcher)
        {
            var dir = Path.GetDirectoryName(filePathToGetPlayListFor);
            var ext = Path.GetExtension(filePathToGetPlayListFor);
            var filesInTheSameDir = dirSearcher.SearchFiles(dir,
                "*" + ext,
                SearchOption.TopDirectoryOnly)
                .Where(f => f.CompareTo(filePathToGetPlayListFor) >= 0).ToList();

            var similarFiles = filesInTheSameDir.Where(f => IsSimilarFile(filePathToGetPlayListFor, f)).ToList();
            if (similarFiles.Count == 1)
            {// only find itself.
                return filesInTheSameDir;
            }
            return similarFiles;
        }

        private static bool IsSimilarFile(string baseFile, string fileToCompare)
        {
            var baseFileName = Path.GetFileNameWithoutExtension(baseFile);
            var fileNameToCompare = Path.GetFileNameWithoutExtension(fileToCompare);
            var similarity = LevenshteinDistance.CalculateSimilarity(fileNameToCompare, baseFileName);
            return similarity >= 90.0 / 100.0;
        }
    }
}
