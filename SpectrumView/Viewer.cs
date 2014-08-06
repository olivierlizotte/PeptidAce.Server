using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PeptidAce;
using PeptidAce.Utilities.Interfaces;

namespace PeptidAce.SpectrumView
{
    public static class Viewer
    {
        public static DBOptions options;
        public static object CreateView(string rawName, string scan, IConSol consol)
        {
            if(options == null)
                options = DotNetMHC.MHCSearcher.CreateOptions(new string[]{""}, "", 15, 0.05, consol);
            //Get file path
            //Get Spectrum
            Numerics.SequenceStore store = Numerics.SequenceStore.GetStore(0.05);
            string rawFile = Numerics.SourceStore.GetDictionary()[rawName];
            DotNetMHC.Data.Spectrum spectrum = DotNetMHC.RawExtractor.LoadSpectrum(rawFile, scan, options);
            List<Data.PeptideView> peptides = store.GetPeptides(spectrum, options);
            double bestScore = 0.0;
            Data.PeptideView bestPeptide = peptides[0];
            foreach(Data.PeptideView peptide in peptides)
                if(peptide.SpectrumScore > bestScore)
                {
                    bestScore = peptide.SpectrumScore;
                    bestPeptide = peptide;
                }
            return new Data.SpecView(spectrum, peptides, bestPeptide);
        }
    }
}
