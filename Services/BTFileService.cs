using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TheBugTracker.Services.Interfaces;

namespace TheBugTracker.Services
{
    public class BTFileService : IBTFileService
    {
        //1024 bits in a byte, 2048 in a kb, 4096 in a mb etc..
        private readonly string[] suffixes = { "Bytes", "KB", "MB", "GB", "TB", "PB" };
        public async Task<byte[]> ConvertFileToByteArrayAsync(IFormFile file)
        {
            try
            {
                MemoryStream ms = new();
                await file.CopyToAsync(ms);
                byte[] bytefile = ms.ToArray();
                ms.Close();
                ms.Dispose();

                return bytefile;
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string ConvertByteArrayToFile(byte[] fileData, string extension)
        {
            try
            {
                string imageBase64Data = Convert.ToBase64String(fileData);
                return string.Format($"data:{extension};base64,{imageBase64Data}");
            }
            catch (Exception)
            {

                throw;
            }
        }

        public string FormatFileSize(long bytes)
        {
            int counter = 0;
            decimal fileSize = bytes;
            while (Math.Round(fileSize / 1024) >= 1)
            {
                fileSize /= bytes;
                counter++;
            }
            return string.Format("{0:n1}{1}", fileSize, suffixes[counter]);

        }

        public string GetFileIcon(string file)
        {
            string fileImage = "default";

            if(!string.IsNullOrWhiteSpace(file))
            {
                fileImage = Path.GetExtension(file).Replace(".","");
                //path to where the file is stored in the solution explorer
                return $"/img/png/{fileImage}.png";
            }
            return fileImage;
        }
    }
}
