using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetMHC.Data;

namespace PeptidAce.SpectrumView.Data
{
    [Serializable]
    public class ModificationView
    {
        public int index;
        public double modMass;
        public string aminoAcid;
        public string description;
        public ModificationView(Modification mod, int theIndex)
        {
            index = theIndex;
            modMass = mod.MonoisotopicMassShift;
            aminoAcid = mod.AminoAcid.ToString();
            description = mod.Description;
        }
    }

    [Serializable]
    public class PeptideView
    {
        public string Sequence;
        public string SequenceWithMods;
        public double[] Masses;
        public double MonoIsotopicMass;
        public double SpectrumScore;
        public ModificationView[] staticMods;
        public ModificationView[] variableMods;
        public PeptideView(string sequence, double[] masses, double monoIsotopicMass, double score, Modification mod = null, int modPos = -1)
        {
            Sequence = sequence;
            SequenceWithMods = "";
            if (mod != null)
            {
                SequenceWithMods = mod.Description + " at " + (modPos + 1);
                variableMods = new ModificationView[] { new ModificationView(mod, modPos) };
                staticMods = new ModificationView[0];
            }
            Masses = masses;
            MonoIsotopicMass = monoIsotopicMass;
            SpectrumScore = score;
        }
    }

    [Serializable]
    public class SpecView
    {
        public PeptideView[] Peptides;
        public string Source;
        public int ScanNumber;
        public double RetentionTimeInMin;
        public double PrecursorMZ;
        public double PrecursorIntensity;
        public int PrecursorCharge;
        public double PrecursorMass;
        public Fragment[] Peaks;
        public double InjectionTime;
        public double Ms1InjectionTime;
        public double MaxPeakIntensity;
        public double AveragePeakIntensity;
        public double SumPeakIntensity;
        public SpecView(Spectrum spectrum, List<PeptideView> peptides, PeptideView peptide)
        {
            Peptides = peptides.ToArray();
            Source = spectrum.Source;
            ScanNumber = spectrum.ScanNumber;
            RetentionTimeInMin = spectrum.RetentionTimeInMin;
            PrecursorMZ = spectrum.PrecursorMZ;
            PrecursorIntensity = spectrum.PrecursorIntensity;
            PrecursorCharge = spectrum.PrecursorCharge;
            PrecursorMass = spectrum.PrecursorMass;
            Peaks = spectrum.Peaks.ToArray();
            InjectionTime = spectrum.InjectionTime;
            Ms1InjectionTime = spectrum.Ms1InjectionTime;
            MaxPeakIntensity = spectrum.MaxPeakIntensity;
            AveragePeakIntensity = spectrum.AveragePeakIntensity;
            SumPeakIntensity = spectrum.SumPeakIntensity;
        }
    }

}
