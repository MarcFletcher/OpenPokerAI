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
using System.IO;

#if logging
using log4net;
#endif

namespace Encog.Persist.Location
{
    /// <summary>
    /// A persistence location based on a file.
    /// </summary>
    public class FilePersistence : IPersistenceLocation
    {
#if logging
        /// <summary>
        /// The logging object.
        /// </summary>
        private readonly ILog logger = LogManager.GetLogger(typeof(FilePersistence));
#endif
        /// <summary>
        /// The file to persist to/from.
        /// </summary>
        private String file;

        public bool containsByteData = false;
        public byte[] byteData;

        /// <summary>
        /// Construct a persistance location based on a file.
        /// </summary>
        /// <param name="file">The file to use.</param>
        public FilePersistence(String file)
        {
            this.file = file;
        }

        public FilePersistence()
        {
           
        }

        public void AddStreamData(byte[] fileBytes)
        {
            this.byteData = fileBytes;
            containsByteData = true;
        }

        /// <summary>
        /// Create a stream to a access the file.
        /// </summary>
        /// <returns>A new InputStream for this file.</returns>
        public Stream CreateStream(FileMode mode)
        {
            try
            {
                if (containsByteData)
                    //return new MemoryStream((from current in byteData.Split('-') select Convert.ToByte(current, 16)).ToArray());
                    return new MemoryStream(byteData);
                else
                {
                    if (mode == FileMode.Open)
                        return new FileStream(this.file, mode, FileAccess.Read);
                    else
                        return new FileStream(this.file, mode);
                }
            }
            catch (IOException e)
            {
#if logging
                if (this.logger.IsErrorEnabled)
                {
                    this.logger.Error("Exception", e);
                }
#endif
                throw new PersistError(e);
            }
        }

        /// <summary>
        /// Attempt to delete the file.
        /// </summary>
        public void Delete()
        {
            File.Delete(this.file);
        }

        /// <summary>
        /// Does the file exist?
        /// </summary>
        /// <returns>True if the file exists.</returns>
        public bool Exists()
        {
            if (containsByteData)
                return true;
            else
                return File.Exists(this.file);
        }

        /// <summary>
        /// The file this location is based on.
        /// </summary>
        public String FileName
        {
            get
            {
                if (containsByteData)
                    throw new Exception("This method should not be called if this object contains xml lines.");

                return this.file;
            }
        }

        /// <summary>
        /// Rename this file to a different location.
        /// </summary>
        /// <param name="toLocation">What to rename to.</param>
        public void RenameTo(IPersistenceLocation toLocation)
        {
            if (!(toLocation is FilePersistence))
            {
                String str =
                   "Can only rename from one FilePersistence location to another";
#if logging
                if (this.logger.IsErrorEnabled)
                {
                    this.logger.Error(str);
                }
#endif
                throw new PersistError(str);
            }

            String toFile = ((FilePersistence)toLocation).FileName;

            File.Move(this.file, toFile);
        }
    }
}
