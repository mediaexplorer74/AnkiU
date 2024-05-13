﻿/*
Copyright (C) 2016 Anki Universal Team <ankiuniversal@outlook.com>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as
published by the Free Software Foundation, either version 3 of the
License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License for more details.

You should have received a copy of the GNU Affero General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;
using System.IO;
using Windows.Storage;

namespace AnkiU.AnkiCore.Sync
{
    public class FullSyncer : HttpSyncer
    {
        Collection collection;
        public FullSyncer(Collection collection, string hkey) : base(hkey)
        {
            postVars = new Dictionary<string, object>();
            postVars["k"] = hkey;
            postVars["v"] = String.Format("anki,{0},{1}", Utils.APP_VERSION, Utils.GetPlatDesc());
            this.collection = collection;
        }

        public override string SyncURL()
        {
            // Allow user to specify custom sync server
            //TODO: User custom sync server:
            //Get user preferences
            //If verify valid custom sync preference is true
            //  return path + "sync/"
            //else
            // Usual case
            return Syncing.BASE + "sync/";
        }

        public async override Task<object[]> Download()
        {
            HttpResponseMessage ret = await Request("download");
            if (ret == null)
                return null;

            string relativePath = collection.RelativePath;
            string tempRelativePath = relativePath + ".tmp";
            string fullPath = Storage.AppLocalFolder.Path + "\\" + tempRelativePath;
            try
            {
                var content = ret.Content;                
                WriteToFile((await content.ReadAsInputStreamAsync()).AsStreamForRead(), tempRelativePath);
                using (FileStream fis = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
                {
                    if (Stream2String(fis, 15).Equals("upgradeRequired"))
                    {
                        return new object[] { "upgradeRequired" };
                    }
                }

                // check the received file is ok
                using (DB tempDb = new DB(fullPath))
                {
                    if (!tempDb.QueryScalar<string>("PRAGMA integrity_check").Equals("ok", StringComparison.OrdinalIgnoreCase))
                        return new object[] { "remoteDbError" };
                }

                collection.Close();
                await OverWriteCollection(relativePath, tempRelativePath);
                return new object[] { "success" };
            }
            catch (SQLite.Net.SQLiteException)
            {
                await collection.ReOpen();
                throw new Exception("The downloaded database is corrupted!");
            }
            catch (FileLoadException)
            {
                await collection.ReOpen();
                throw new FileLoadException("Failed to overwrite collection! Please try closing then opening the app again to sync your data." );
            }                       
            catch(Exception ex)
            {
                await collection.ReOpen();
                throw ex;
            }
            finally
            {
                if(File.Exists(fullPath))               
                    File.Delete(fullPath);                              
            }
        }

        private static async Task OverWriteCollection(string relativePath, string tempRelativePath)
        {
            StorageFile oldFile = await Storage.AppLocalFolder.GetFileAsync(relativePath);
            if (oldFile != null)
                await oldFile.DeleteAsync();

            StorageFile newFile = await Storage.AppLocalFolder.GetFileAsync(tempRelativePath);
            await newFile.RenameAsync(relativePath, NameCollisionOption.ReplaceExisting);
        }

        public override async Task<object[]> Upload()
        {
            // make sure it's ok before we try to upload            
            if (!collection.Database.QueryScalar<string>("PRAGMA integrity_check")
                    .Equals("ok", StringComparison.OrdinalIgnoreCase))
            {

                throw new Exception("Local database error: Integrity check failed!");                
            }
            if (!collection.BasicCheck())
            {
                throw new Exception("Local database error: Basic check failed!");
            }

            StorageFolder tempFolder = null;
            try
            {
                // apply some adjustments, then upload
                collection.BeforeUpload();
                tempFolder = await Storage.AppLocalFolder.CreateFolderAsync("tempAnkiUpload", CreationCollisionOption.ReplaceExisting);
                var collectionFile = await Storage.AppLocalFolder.GetFileAsync(Constant.COLLECTION_NAME);
                await collectionFile.CopyAsync(tempFolder, collectionFile.Name, NameCollisionOption.ReplaceExisting);
                string filePath = tempFolder.Path + "\\" + collectionFile.Name;
                HttpResponseMessage ret;
                using (FileStream stream = new FileStream(filePath, FileMode.Open))
                {
                    ret = await Request("upload", stream);
                    if (ret == null)
                    {
                        throw new HttpSyncerException("No response from the server");
                    }
                    HttpStatusCode status = ret.StatusCode;
                    if (status != HttpStatusCode.Ok)
                    {
                        throw new HttpSyncerException("Error: " + status.ToString() + "\n" + ret.ReasonPhrase);                        
                    }
                    else
                    {
                        return new object[] { await ret.Content.ReadAsStringAsync() };
                    }
                }
            }
            finally
            {
                if (tempFolder != null)
                {                    
                    await tempFolder.DeleteAsync();
                    tempFolder = null;
                }
                await collection.ReOpen();                
            }
        }

    }
}
