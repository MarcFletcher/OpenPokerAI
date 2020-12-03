using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using System.IO;
using NetworkCommsDotNet.DPSBase;

namespace Encog
{
    /// <summary>
    /// Provides tracking information for use in the genetic algorthim training environment
    /// </summary>
    [ProtoContract]
    public class GATracker
    {
        [ProtoContract]
        public class MutationRecord
        {
            [ProtoMember(1)]
            public double MaxPertubAmount { get; private set; }
            [ProtoMember(2)]
            public double AmountPertubed { get; private set; }

            private MutationRecord() { }

            public MutationRecord(double maxPertubAmount, double amountPertubed)
            {
                this.MaxPertubAmount = maxPertubAmount;
                this.AmountPertubed = amountPertubed;
            }
        }

        [ProtoContract]
        public class MatedElement
        {
            [ProtoMember(1)]
            public int MotherIndex { get; set; }
            [ProtoMember(2)]
            public int FatherIndex { get; set; }
            [ProtoMember(3)]
            public int Child1Index { get; set; }
            [ProtoMember(4)]
            public int Child2Index { get; set; }

            [ProtoMember(5)]
            public bool Child1Mutated { get; set; }
            [ProtoMember(6)]
            public bool Child2Mutated { get; set; }

            [ProtoMember(7)]
            public int NumChild1Genes { get; private set; }
            [ProtoMember(8)]
            public int NumChild2Genes { get; private set; }

            [ProtoMember(9)]
            public List<MutationRecord> Child1MutationRecord { get; set; }
            [ProtoMember(10)]
            public List<MutationRecord> Child2MutationRecord { get; set; }

            private MatedElement()
            {
                if (Child1MutationRecord == null) Child1MutationRecord = new List<MutationRecord>();
                if (Child2MutationRecord == null) Child2MutationRecord = new List<MutationRecord>();
            }

            public MatedElement(int motherIndex, int fatherIndex, int child1Index, int child2Index)
            {
                this.MotherIndex = motherIndex;
                this.FatherIndex = fatherIndex;
                this.Child1Index = child1Index;
                this.Child2Index = child2Index;

                Child1MutationRecord = new List<MutationRecord>();
                Child2MutationRecord = new List<MutationRecord>();
            }

            public void AddChildMutation(MutationRecord mutation, int childIndex)
            {
                if (childIndex == 1)
                    Child1MutationRecord.Add(mutation);
                else if (childIndex == 2)
                    Child2MutationRecord.Add(mutation);
                else
                    throw new Exception("Invalid child index provided.");
            }

            public void SetNumChildGenes(int numChildGenes, int childIndex)
            {
                if (childIndex == 1)
                    NumChild1Genes = numChildGenes;
                else if (childIndex == 2)
                    NumChild2Genes = numChildGenes;
                else
                    throw new Exception("Invalid child index provided.");
            }
        }

        [ProtoMember(1)]
        public string SpeciesName { get; private set; }
        [ProtoMember(2)]
        public int GenerationNum { get; private set; }
        [ProtoMember(3)]
        public List<MatedElement> MatedElements { get; private set; }

        /// <summary>
        /// The thread safety locker
        /// </summary>
        private object locker = new object();

        private GATracker() 
        {
            if (MatedElements == null) MatedElements = new List<MatedElement>();
        }

        public GATracker(string speciesName, int generationNum)
        {
            this.SpeciesName = speciesName;
            this.GenerationNum = generationNum;
            MatedElements = new List<MatedElement>();
        }

        public static GATracker Load(string loadLocation)
        {
            try
            {
                return DPSManager.GetDataSerializer<ProtobufSerializer>().DeserialiseDataObject<GATracker>(File.ReadAllBytes(Path.Combine(loadLocation, "GATracker.gat")));
            }
            catch (Exception)
            {
                //If there is an exception we just return a blank tracker
                return new GATracker();
            }
        }

        /// <summary>
        /// Saves the current GATracker to disk at the provided location.
        /// </summary>
        /// <param name="saveLocation"></param>
        /// <param name="includeLogFile">If true also saves out a human readable log file</param>
        public void Save(string saveLocation, bool includeLogFile)
        {
            lock (locker)
            {
                File.WriteAllBytes(saveLocation + "\\GATracker.gat", DPSManager.GetDataSerializer<ProtobufSerializer>().SerialiseDataObject<GATracker>(this).ThreadSafeStream.ToArray());
                if (includeLogFile) WriteOutLogFile(saveLocation);
            }
        }

        /// <summary>
        /// Writes out the human readable log file
        /// </summary>
        /// <param name="logLocation"></param>
        private void WriteOutLogFile(string logLocation)
        {
            using (StreamWriter sw = new StreamWriter(logLocation + "\\GALog.csv", false))
            {
                //Parent index versus number of children
                SortedDictionary<int, int> parentChildCount = new SortedDictionary<int, int>();

                foreach (MatedElement element in MatedElements)
                {
                    if (parentChildCount.ContainsKey(element.MotherIndex))
                        parentChildCount[element.MotherIndex]++;
                    else
                        parentChildCount.Add(element.MotherIndex, 1);

                    if (parentChildCount.ContainsKey(element.FatherIndex))
                        parentChildCount[element.FatherIndex]++;
                    else
                        parentChildCount.Add(element.FatherIndex, 1);
                }

                sw.WriteLine("ParentIndex, NumChildren");
                foreach (var parent in parentChildCount)
                    sw.WriteLine(parent.Key + ", " + parent.Value);

                sw.WriteLine("\n------------------------------------------\n------------------------------------------\n");

                sw.WriteLine("ChildIndex, MotherIndex, FatherIndex, NumGenes, Distort Factor - #Neurons Mutated,");

                foreach (MatedElement element in MatedElements)
                {
                    //Write out information for child1
                    sw.Write(element.Child1Index + ", " + element.MotherIndex + ", " + element.FatherIndex + ", " + element.NumChild1Genes + ", ");
 
                    if (element.Child1Mutated)
                    {
                        //Calculate the sum of the various mutation records
                        double[] mutationFactors = (from current in element.Child1MutationRecord orderby current.MaxPertubAmount ascending select current.MaxPertubAmount).Distinct().ToArray();

                        //Write out the counts for each mutation factor
                        for (int i = 0; i < mutationFactors.Length; i++)
                            sw.Write(mutationFactors[i] + " - " + (from current in element.Child1MutationRecord where current.MaxPertubAmount == mutationFactors[i] select current.AmountPertubed).Count() + ", ");

                        sw.WriteLine("");
                    }
                    else
                        sw.WriteLine("NM,");

                    //Write out information for child2
                    sw.Write(element.Child2Index + ", " + element.MotherIndex + ", " + element.FatherIndex + ", " + element.NumChild2Genes + ", ");

                    if (element.Child2Mutated)
                    {
                        //Calculate the sum of the various mutation records
                        double[] mutationFactors = (from current in element.Child2MutationRecord orderby current.MaxPertubAmount ascending select current.MaxPertubAmount).Distinct().ToArray();

                        //Write out the counts for each mutation factor
                        for (int i = 0; i < mutationFactors.Length; i++)
                            sw.Write(mutationFactors[i] + " - " + (from current in element.Child2MutationRecord where current.MaxPertubAmount == mutationFactors[i] select current.AmountPertubed).Count() + ", ");

                        sw.WriteLine("");
                    }
                    else
                        sw.WriteLine("NM,");
                }
            }
        }

        /// <summary>
        /// Returns true if the provided index was mutated. If not found or not mutated returns false
        /// </summary>
        /// <param name="childIndex"></param>
        /// <returns></returns>
        public bool ChildMutated(int childIndex)
        {
            lock (locker)
            {
                foreach (MatedElement element in MatedElements)
                {
                    if (element.Child1Index == childIndex)
                        return element.Child1Mutated;
                    else if (element.Child2Index == childIndex)
                        return element.Child2Mutated;
                }
            }

            //If we did not find the index return false
            throw new Exception("Unable to locate provided childIndex");
        }

        public void AddMatedElement(MatedElement matedElement)
        {
            lock (locker)
            {
                MatedElements.Add(matedElement);
            }
        }
    }
}
