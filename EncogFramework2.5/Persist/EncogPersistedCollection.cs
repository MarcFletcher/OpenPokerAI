// Encog(tm) Artificial Intelligence Framework v2.5
// .Net Version
// http://www.heatonresearch.com/encog/
// http://code.google.com/p/encog-java/
// 
// Copyright 2008-2010 by Heaton Research Inc.
// 
// Released under the LGPL.
//
// This is free software; you can redistribute it and/or modify it
// under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation; either version 2.1 of
// the License, or (at your option) any later version.
//
// This software is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this software; if not, write to the Free
// Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
// 02110-1301 USA, or see the FSF site: http://www.fsf.org.
// 
// Encog and Heaton Research are Trademarks of Heaton Research, Inc.
// For information on Heaton Research trademarks, visit:
// 
// http://www.heatonresearch.com/copyright.html

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Encog.Persist.Location;
using System.IO;
using Encog.Neural.Networks;
#if logging
using log4net;
#endif


namespace Encog.Persist
{
    
    /// <summary>
    /// An EncogPersistedCollection holds a collection of EncogPersistedObjects. This
 /// allows the various neural networks and some data sets to be persisted. They
 /// are persisted to an XML form.
 /// 
 /// The EncogPersistedCollection does not load the object into memory at once.
 /// This allows it to manage large files.
    /// </summary>
    public class EncogPersistedCollection: IEncogCollection
    {

        /// <summary>
        /// Generic error message for bad XML.
        /// </summary>
        public const String GENERAL_ERROR = "Malformed XML near tag: ";

        /// <summary>
        /// The type is TextData.
        /// </summary>
        public const String TYPE_TEXT = "TextData";

        /// <summary>
        /// The type is PropertyData.
        /// </summary>
        public const String TYPE_PROPERTY = "PropertyData";

        /// <summary>
        /// The type is BasicNetwork.
        /// </summary>
        public const String TYPE_BASIC_NET = "BasicNetwork";

        /// <summary>
        /// The type is BasicLayer.
        /// </summary>
        public const String TYPE_BASIC_LAYER = "BasicLayer";

        /// <summary>
        /// The type is ContextLayer.
        /// </summary>
        public const String TYPE_CONTEXT_LAYER = "ContextLayer";

        /// <summary>
        /// The type is RadialBasisFunctionLayer.
        /// </summary>
        public const String TYPE_RADIAL_BASIS_LAYER =
            "RadialBasisFunctionLayer";

        /// <summary>
        /// The type is TrainingData.
        /// </summary>
        public const String TYPE_TRAINING = "TrainingData";

        /// <summary>
        ///  The type is WeightedSynapse.
        /// </summary>
        public const String TYPE_WEIGHTED_SYNAPSE = "WeightedSynapse";

        /// <summary>
        /// The type is WeightlessSynapse.
        /// </summary>
        public const String TYPE_WEIGHTLESS_SYNAPSE = "WeightlessSynapse";

        /// <summary>
        /// The type is DirectSynapse.
        /// </summary>
        public const String TYPE_DIRECT_SYNAPSE = "DirectSynapse";

        /// <summary>
        /// The type is OneToOneSynapse.
        /// </summary>
        public const String TYPE_ONE2ONE_SYNAPSE = "OneToOneSynapse";

        /// <summary>
        /// The type is ParseTemplate.
        /// </summary>
        public const String TYPE_PARSE_TEMPLATE = "ParseTemplate";

        /// <summary>
        /// A population.
        /// </summary>
        public const String TYPE_POPULATION = "BasicPopulation";

        /// <summary>
        /// A Support Vector Machine.
        /// </summary>
        public const String TYPE_SVM = "SVM";

        /// <summary>
        /// The name attribute.
        /// </summary>
        public const String ATTRIBUTE_NAME = "name";

        /// <summary>
        /// The description attribute.
        /// </summary>
        public const String ATTRIBUTE_DESCRIPTION = "description";
#if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly static ILog LOGGER = LogManager.GetLogger(typeof(EncogPersistedCollection));
#endif

        /// <summary>
        /// Training continuation.
        /// </summary>
        public const String TYPE_TRAINING_CONTINUATION = "TrainingContinuation";

        /// <summary>
        /// Throw and log an error.
        /// </summary>
        /// <param name="tag">The tag this error is for.</param>
        public static void ThrowError(String tag)
        {
            String str = EncogPersistedCollection.GENERAL_ERROR + tag;
#if logging
            if (EncogPersistedCollection.LOGGER.IsErrorEnabled)
            {
                EncogPersistedCollection.LOGGER.Error(str);
            }
#endif
            throw new PersistError(str);
        }

        /// <summary>
        /// The primary file being persisted to.
        /// </summary>
        private IPersistenceLocation filePrimary;

        /// <summary>
        /// The temp file, to be used for merges.
        /// </summary>
        private IPersistenceLocation fileTemp;

        /// <summary>
        /// The platform this collection was created on.
        /// </summary>
        private String platform = EncogFramework.PLATFORM;

        /// <summary>
        /// The version of the persisted file.
        /// </summary>
        private int fileVersion = 1;

        /// <summary>
        /// Directory entries for all of the objects in the current file.
        /// </summary>
        private IList<DirectoryEntry> directory =
                new List<DirectoryEntry>();

        /// <summary>
        /// The version of Encog.
        /// </summary>
        private String encogVersion = EncogFramework.Instance.Properties[EncogFramework.ENCOG_VERSION];

        /// <summary>
        /// Create a persistance collection for the specified file.
        /// </summary>
        /// <param name="file">The file to load/save.</param>
        /// <param name="mode">The file mode</param>
        public EncogPersistedCollection(String file, FileMode mode)
            : this(new FilePersistence(file), mode)
        {

        }

        /// <summary>
        /// Create an object based on the specified location.
        /// </summary>
        /// <param name="location">The location to load/save from.</param>
        /// <param name="mode">The file mode.</param>
        public EncogPersistedCollection(IPersistenceLocation location, FileMode mode)
        {
            this.filePrimary = location;

            if (this.filePrimary is FilePersistence)
            {
                FilePersistence locationObj = (FilePersistence)this.filePrimary;

                if (locationObj.containsByteData)
                {
                    this.fileTemp = new FilePersistence();
                    ((FilePersistence)this.fileTemp).AddStreamData(locationObj.byteData);

                    if (this.filePrimary.Exists())
                    {
                        BuildDirectory();
                    }
                }
                else
                {
                    String file = locationObj.FileName;

                    int index = file.LastIndexOf('.');
                    if (index != -1)
                    {
                        file = file.Substring(0, index);
                    }
                    file += ".tmp";
                    this.fileTemp = new FilePersistence(file);

                    if (this.filePrimary.Exists())
                    {
                        BuildDirectory();
                    }
                    else
                    {
                        Create();
                    }
                }
            }
            else
            {
                this.fileTemp = null;
            }
        }

        /// <summary>
        /// Add an EncogPersistedObject to the collection.
        /// </summary>
        /// <param name="name">The name of the object to load.</param>
        /// <param name="obj">The object to add.</param>
        public void Add(String name, IEncogPersistedObject obj)
        {
            if (obj is BasicNetwork)
            {
                ((BasicNetwork)obj).Structure.UpdateFlatNetwork();
            }

            obj.Name = name;
            PersistWriter writer = new PersistWriter(this.fileTemp);
            writer.Begin();
            writer.WriteHeader();
            writer.BeginObjects();
            writer.WriteObject(obj);
            writer.MergeObjects(this.filePrimary, name);
            writer.EndObjects();
            writer.End();
            writer.Close();
            MergeTemp();
            BuildDirectory();
        }

        /// <summary>
        /// Build a directory of objects.
        /// </summary>
        public void BuildDirectory()
        {
            PersistReader reader = new PersistReader(this.filePrimary);

            IDictionary<String, String> header = reader.ReadHeader();
            if (header != null)
            {
                this.fileVersion = int.Parse(header["fileVersion"]);
                this.encogVersion = header["encogVersion"];
                this.platform = header["platform"];
            }

            this.directory = reader.BuildDirectory();

            reader.Close();
        }

        /// <summary>
        /// Clear the collection.
        /// </summary>
        public void Clear()
        {

        }

        /// <summary>
        /// Create the file.
        /// </summary>
        public void Create()
        {
            this.filePrimary.Delete();
            PersistWriter writer = new PersistWriter(this.filePrimary);
            writer.Begin();
            writer.WriteHeader();
            writer.BeginObjects();
            writer.EndObjects();
            writer.End();
            writer.Close();

            this.directory.Clear();
        }

       /// <summary>
        /// Delete the specified object, use a directory entry.
       /// </summary>
        /// <param name="d">The object to delete.</param>
        public void Delete(DirectoryEntry d)
        {
            this.Delete(d.Name);

        }

        /// <summary>
        /// Delete the specified object.
        /// </summary>
        /// <param name="obj">The object to delete.</param>
        public void Delete(IEncogPersistedObject obj)
        {
            Delete(obj.Name);
        }

        /// <summary>
        /// Delete the specified object.
        /// </summary>
        /// <param name="name">The object name.</param>
        public void Delete(String name)
        {
            PersistWriter writer = new PersistWriter(this.fileTemp);
            writer.Begin();
            writer.WriteHeader();
            writer.BeginObjects();
            writer.MergeObjects(this.filePrimary, name);
            writer.EndObjects();
            writer.End();
            writer.Close();
            MergeTemp();
            foreach (DirectoryEntry d in this.directory )
            {
                if (d.Name.Equals(name))
                {
                    this.directory.Remove(d);
                    break;
                }
            }
        }

        /// <summary>
        /// Find the specified object, using a DirectoryEntry.
        /// </summary>
        /// <param name="d">The directory entry to find.</param>
        /// <returns>The loaded object.</returns>
        public IEncogPersistedObject Find(DirectoryEntry d)
        {
            return Find(d.Name);
        }

        /// <summary>
        /// Called to search all Encog objects in this collection for one with a name
        /// that passes what was passed in.
        /// </summary>
        /// <param name="name">The name we are searching for.</param>
        /// <returns>The Encog object with the correct name.</returns>
        public IEncogPersistedObject Find(String name)
        {
            PersistReader reader = new PersistReader(this.filePrimary);
            IEncogPersistedObject result = reader.ReadObject(name);
            reader.Close();
            return result;
        }

        /// <summary>
        /// The directory entries for the objects in this file.
        /// </summary>
        public IList<DirectoryEntry> Directory
        {
            get
            {
                return this.directory;
            }
        }

        /// <summary>
        /// The version of Encog this file was created with.
        /// </summary>
        public String EncogVersion
        {
            get
            {
                return this.encogVersion;
            }
        }

        /// <summary>
        /// The file version.
        /// </summary>
        public int FileVersion
        {
            get
            {
                return this.fileVersion;
            }
        }

        /// <summary>
        /// The platform this file was created on.
        /// </summary>
        public String Platform
        {
            get
            {
                return this.platform;
            }
        }

        /// <summary>
        /// Merge the temp file with the main one, call this to make any
        /// changes permanent.
        /// </summary>
        public void MergeTemp()
        {
            this.filePrimary.Delete();
            this.fileTemp.RenameTo(this.filePrimary);
        }

        /// <summary>
        /// Update any header properties for an Encog object, for example,
        /// a rename.
        /// </summary>
        /// <param name="name">The name of the object to change. </param>
        /// <param name="newName">The new name of this object.</param>
        /// <param name="newDesc">The description for this object.</param>
        public void UpdateProperties(String name, String newName,
                 String newDesc)
        {
            PersistWriter writer = new PersistWriter(this.fileTemp);
            writer.Begin();
            writer.WriteHeader();
            writer.BeginObjects();
            writer.ModifyObject(this.filePrimary, name, newName, newDesc);
            writer.EndObjects();
            writer.End();
            writer.Close();
            MergeTemp();
            BuildDirectory();

        }

        /// <summary>
        /// Determine if the specified resource exists. 
        /// </summary>
        /// <param name="name">The name of the resource to check.</param>
        /// <returns>True if it exists.</returns>
        public bool Exists(String name)
        {
            foreach (DirectoryEntry dir in this.directory)
            {
                if (dir.Name.Equals(name))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The location.
        /// </summary>
        public IPersistenceLocation Location
        {
            get
            {
                return this.filePrimary;
            }
        }


    }
}
