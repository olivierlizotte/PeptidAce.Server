using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Isam.Esent.Collections.Generic;
using Facet.Combinatorics;
using DotNetMHC.Data;
using PeptidAce.SpectrumView.Data;

namespace PeptidAce.Numerics
{
    public class SequenceStore
    {
        //Singleton pattern
        private static SequenceStore _store = null;
        public static SequenceStore GetStore(double precision, int minPepLength = 8, int maxPepLength = 14)
        {
            if (_store == null)
                _store = new SequenceStore(precision, minPepLength, maxPepLength);
            return _store;
        }

        private static string BaseStoreName = "MassStore";

        private double Precision;
        private string StoreName;
        public int ComputeIndex(double mass)
        {
            return (int)(mass / Precision);        
        }

        public PersistentDictionary<long, string> Dictionary;
        private SequenceStore(double precision, int minPepLength = 8, int maxPepLength = 14)
        {
            Precision = precision;
            StoreName = BaseStoreName + minPepLength + "To" + maxPepLength + "_" + precision.ToString(System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
                
            Dictionary = new PersistentDictionary<long, string>(StoreName);
            if(Dictionary.Count == 0)
                BuildStore(minPepLength, maxPepLength);
        }

        private string[] emptyArray = new string[0];
        public List<string> GetPermutations(double mass)
        {
            List<string> results = new List<string>();
            int index = (int)(mass / Precision);
            string[] combinations = emptyArray;
            if (Dictionary.ContainsKey(index))
                combinations = Dictionary[index].Split(',');
            foreach (string comb in combinations)
            {
                double SeqMass = AminoAcidMasses.GetMass(comb);
                if(Math.Abs(SeqMass - mass) < Precision)
                    foreach (IList<char> str in new Permutations<char>(new List<char>(comb), GenerateOption.WithoutRepetition))
                        results.Add(string.Concat(str));
            }
            return results;
        }

        private IEnumerable<IList<char>> FindPeptides(double mass, char[] ignoreAA)
        {
            int index = (int)(mass / Precision);
            string[] combinations = emptyArray;
            if (Dictionary.ContainsKey(index))
                combinations = Dictionary[index].Split(',');
            foreach (string comb in combinations)
            {
                bool keepIt = true;
                foreach (char c in comb)
                    if (ignoreAA.Contains(c))
                        keepIt = false;
                if (keepIt)
                {
                    double SeqMass = AminoAcidMasses.GetMass(comb);
                    if (Math.Abs(SeqMass - mass) < Precision)
                        foreach (IList<char> str in new Permutations<char>(new List<char>(comb), GenerateOption.WithoutRepetition))
                            yield return str;
                }
            }
        }

        private IEnumerable<IList<char>> FindPeptides(double mass)
        {
            int index = (int)(mass / Precision);
            string[] combinations = emptyArray;
            if (Dictionary.ContainsKey(index))
                combinations = Dictionary[index].Split(',');
            foreach (string comb in combinations)
            {
                double SeqMass = AminoAcidMasses.GetMass(comb);
                if (Math.Abs(SeqMass - mass) < Precision)
                    foreach (IList<char> str in new Permutations<char>(new List<char>(comb), GenerateOption.WithoutRepetition))
                        yield return str;
            }
        }

        private IEnumerable<IList<char>> FindPeptidesWithMods(double mass, Modification mo)
        {
            double modSum = 0.0;
            modSum += mo.MonoisotopicMassShift;
            foreach(IList<char> str in FindPeptides(mass + modSum))
            {
                bool allModsThere = true;
                if (mo.Type != ModificationType.AminoAcidResidue || !str.Contains(mo.AminoAcid))
                    allModsThere = false;
                if (allModsThere)
                    yield return str;
            }
        }

        private char[] GetFixedModAA(List<Modification> fixedModifications)
        {
            int nbFixedAAMods = 0;
            foreach (Modification mod in fixedModifications)
                if (mod.Type == ModificationType.AminoAcidResidue)
                    nbFixedAAMods++;
            char[] ignoreAA = new char[nbFixedAAMods];
            nbFixedAAMods = 0;
            foreach (Modification mod in fixedModifications)
                if (mod.Type == ModificationType.AminoAcidResidue)
                {
                    ignoreAA[nbFixedAAMods] = mod.AminoAcid;
                    nbFixedAAMods++;
                }
            return ignoreAA;
        }

        public List<PeptideView> GetPeptides(Spectrum spectrum, DBOptions options)
        {
            char[] ignoreAA = GetFixedModAA(options.fixedModifications);

            List<PeptideView> results = new List<PeptideView>();

            //Modless and Single variable mod peptides
            List<Modification> allMods = new List<Modification>(options.variableModifications);
            foreach (Modification mod in options.fixedModifications)
                allMods.Add(mod);
            allMods.Add(null);

            foreach (Modification mod in allMods)
            {
                IEnumerable<IList<char>> enumeration;
                if(mod == null)
                    enumeration = FindPeptides(spectrum.PrecursorMass, ignoreAA);
                else
                    enumeration = FindPeptidesWithMods(spectrum.PrecursorMass, mod);
                foreach (IList<char> str in enumeration)
                { 
                    string sequence = string.Concat(str);
                    double[] masses = new double[sequence.Length];
                    for (int i = 0; i < sequence.Length; i++)
                        masses[i] = AminoAcidMasses.GetMonoisotopicMass(sequence[i]);
                    if (mod != null)
                    {
                        for (int i = 0; i < sequence.Length; i++)
                        {
                            if (sequence[i] == mod.AminoAcid)
                            {
                                double[] modMasses = new List<double>(masses).ToArray();
                                modMasses[i] += mod.MonoisotopicMassShift;
                                double score = DotNetMHC.Analysis.PeptideCoverage.PSMScoreMatch(modMasses, sequence.Length, spectrum.PrecursorCharge, spectrum, options);
                                if (score > 0)
                                    results.Add(new PeptideView(sequence, modMasses, spectrum.PrecursorMass, score, mod, i));                                    
                            }
                        }
                    }
                    else
                    {
                        double score = DotNetMHC.Analysis.PeptideCoverage.PSMScoreMatch(masses, sequence.Length, spectrum.PrecursorCharge, spectrum, options);
                        if (score > 0)
                            results.Add(new PeptideView(sequence, masses, spectrum.PrecursorMass, score));
                    }
                }
            }
            return results;
        }

        public void BuildStore(int minPepLength = 8, int maxPepLength = 14)
        {

            for (int size = maxPepLength; size >= minPepLength; size--)
            {
                Dictionary<int, StringBuilder> dicOfStr = new Dictionary<int, StringBuilder>();
                Combinations<char> combinations = new Combinations<char>(new List<char>(PeptidAce.AminoAcidMasses.VALID_AMINO_ACIDS), size, GenerateOption.WithRepetition);
                foreach (IList<char> str in combinations)
                {
                    string sequence = string.Concat(str);
                    double mass = PeptidAce.AminoAcidPolymer.ComputeMonoisotopicMass(sequence);
                    double dIndex = mass / Precision;
                    int index = (int)dIndex;
                    if (dicOfStr.ContainsKey(index))
                        dicOfStr[index].Append("," + sequence);
                    else
                        dicOfStr.Add(index,  new StringBuilder(sequence));

                    if(index > dIndex)
                        index--;
                    else
                        index++;

                    if (dicOfStr.ContainsKey(index))
                        dicOfStr[index].Append("," + sequence);
                    else
                        dicOfStr.Add(index, new StringBuilder(sequence));
                }

                //Now that its all in memory, fill dictionnary
                foreach (KeyValuePair<int, StringBuilder> pair in dicOfStr)
                    if(Dictionary.ContainsKey(pair.Key))
                        Dictionary[pair.Key] += pair.Value.ToString();
                    else
                        Dictionary.Add(pair.Key, pair.Value.ToString());
            }
        }
    }
}
