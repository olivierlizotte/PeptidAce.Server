using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Isam.Esent.Collections.Generic;

namespace PeptidAce.Numerics
{
    public class SourceStore
    {
        private static string BaseStoreName = "RawStore";
        private static PersistentDictionary<string, string> _store = new PersistentDictionary<string, string>(BaseStoreName);
        public static PersistentDictionary<string, string> GetDictionary()
        {
            return _store;
        }

        private SourceStore()
        {

        }

        public static IEnumerable<string> ListRawFiles(string nameOfParentFolder)
        {
            foreach (string file in Directory.GetFiles(nameOfParentFolder))
                if (file.ToLower().EndsWith(".raw"))
                    yield return file;
        } 
        public static void FillStore(string folder, PersistentDictionary<string, string> dic)
        {
            foreach (string rawFile in ListRawFiles(folder))
            {
                string source = PeptidAce.Utilities.vsCSV.GetFileName_NoExtension(rawFile);
                if (!dic.ContainsKey(source))
                    dic.Add(source, rawFile);
            }
        }
    }
}
